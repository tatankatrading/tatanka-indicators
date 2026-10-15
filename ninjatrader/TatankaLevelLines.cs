// Copyright (c) 2026 Tatanka Trading LLC.
// This Source Code Form is subject to the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, obtain one at https://mozilla.org/MPL/2.0/.
// Tatanka Level Lines v1.1.0 for NinjaTrader 8 - tatankatrading.com

#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

// TatankaLevelLines
// Labeled horizontal levels for NinjaTrader 8:
//   1. A range box: top, bottom, optional midpoint and shading.
//   2. Any number of custom lines in one large text box, separated by commas, semicolons, or new lines:
//        price|label|color|style|width
//      Only the price is required. Colors by name (Orange, Magenta, Gray) or hex (#FF00FF).
//      Styles: Solid, Dash, Dot, DashDot, DashDotDot.
// Labels sit on the right edge above each line, with the price appended, and stack
// instead of overlapping when two lines are close.
//   3. Optional sharing: one chart set to Source, others set to Follow draw its lines.
//      Charts share within one contract month; a micro shares with its full-size
//      contract (MES with ES, MNQ with NQ, ...).

namespace NinjaTrader.NinjaScript.Indicators
{
	public enum TatankaLevelLinesSharingMode
	{
		Off,
		Source,
		Follow
	}

	public class TatankaLevelLines : Indicator
	{
		private class LevelLine
		{
			public double Price;
			public string Label;
			public Stroke Stroke;
		}

		private struct PendingLabel
		{
			public float Y;
			public string Text;
			public SharpDX.Direct2D1.Brush Brush;
			public float LineWidth;
		}

		private readonly List<LevelLine> customLines = new List<LevelLine>();
		private SharedLevels localLevels;
		private SharedLevels renderedLevels;
		private List<LevelLine> renderedLines;
		private Brush renderedFillBrush;
		private string sharedInstrument;
		private string sharedDisplay;
		private TatankaLevelLinesSharingMode activeSharingMode;
		private volatile bool sharingActive;
		// Identifies this instance's publish, so it can withdraw only its own levels.
		private readonly object sourceToken = new object();

		// Only values and serialized brushes cross chart threads. Every chart owns
		// its own Stroke objects and DirectX resources; those must never be shared.
		private sealed class SharedLine
		{
			public readonly double Price;
			public readonly string Label;
			public readonly string Brush;
			public readonly DashStyleHelper Dash;
			public readonly float Width;
			public readonly int Opacity;

			public SharedLine(LevelLine line)
			{
				Price = line.Price;
				Label = line.Label;
				Brush = Serialize.BrushToString(line.Stroke.Brush);
				Dash = line.Stroke.DashStyleHelper;
				Width = line.Stroke.Width;
				Opacity = line.Stroke.Opacity;
			}
		}

		private sealed class SharedLevels
		{
			public readonly SharedLine[] Lines;
			public readonly double RangeTop;
			public readonly double RangeBottom;
			public readonly bool FillRange;
			public readonly string FillBrush;
			public readonly int FillOpacity;

			public SharedLevels(SharedLine[] lines, double top, double bottom, bool fill, string brush, int opacity)
			{
				Lines = lines;
				RangeTop = top;
				RangeBottom = bottom;
				FillRange = fill;
				FillBrush = brush;
				FillOpacity = opacity;
			}
		}

		private static class SharedLevelStore
		{
			private static readonly object gate = new object();
			private static readonly Dictionary<string, List<Action>> listeners = new Dictionary<string, List<Action>>(StringComparer.Ordinal);

			// Every live Source's levels per instrument, oldest first. Follow charts show the
			// newest. When a Source goes away, the next newest takes over, or Follow charts
			// go back to waiting when none is left.
			private sealed class Publication
			{
				public readonly object Owner;
				public readonly SharedLevels Levels;

				public Publication(object owner, SharedLevels levels)
				{
					Owner = owner;
					Levels = levels;
				}
			}
			private static readonly Dictionary<string, List<Publication>> sources = new Dictionary<string, List<Publication>>(StringComparer.Ordinal);

			public static SharedLevels Get(string instrument)
			{
				lock (gate)
				{
					List<Publication> list;
					return instrument != null && sources.TryGetValue(instrument, out list) && list.Count > 0
						? list[list.Count - 1].Levels : null;
				}
			}

			public static void Publish(string instrument, SharedLevels value, object owner)
			{
				Action[] notify;
				lock (gate)
				{
					List<Publication> list;
					if (!sources.TryGetValue(instrument, out list))
						sources[instrument] = list = new List<Publication>();
					list.RemoveAll(p => ReferenceEquals(p.Owner, owner));
					list.Add(new Publication(owner, value));
					notify = ListenersFor(instrument);
				}
				// Do not invoke chart callbacks while holding the store lock.
				foreach (Action callback in notify)
					callback();
			}

			// Removes only this Source's levels. On Apply, NinjaTrader may terminate the old
			// instance before or after the new one publishes; either order leaves the new
			// levels showing.
			public static void Withdraw(string instrument, object owner)
			{
				Action[] notify;
				lock (gate)
				{
					List<Publication> list;
					if (instrument == null || !sources.TryGetValue(instrument, out list))
						return;
					int index = list.FindIndex(p => ReferenceEquals(p.Owner, owner));
					if (index < 0)
						return;
					bool wasShowing = index == list.Count - 1;
					list.RemoveAt(index);
					if (list.Count == 0)
						sources.Remove(instrument);
					if (!wasShowing)
						return;
					notify = ListenersFor(instrument);
				}
				foreach (Action callback in notify)
					callback();
			}

			// Call while holding gate.
			private static Action[] ListenersFor(string instrument)
			{
				List<Action> callbacks;
				return listeners.TryGetValue(instrument, out callbacks) ? callbacks.ToArray() : new Action[0];
			}

			public static void Subscribe(string instrument, Action callback)
			{
				lock (gate)
				{
					List<Action> callbacks;
					if (!listeners.TryGetValue(instrument, out callbacks))
						listeners[instrument] = callbacks = new List<Action>();
					if (!callbacks.Contains(callback))
						callbacks.Add(callback);
				}
			}

			public static void Unsubscribe(string instrument, Action callback)
			{
				lock (gate)
				{
					List<Action> callbacks;
					if (instrument == null || !listeners.TryGetValue(instrument, out callbacks))
						return;
					callbacks.Remove(callback);
					if (callbacks.Count == 0)
						listeners.Remove(instrument);
				}
				// The store holds values only, never chart objects. Each Source withdraws its
				// own levels when it terminates; a recompile resets the store.
			}
		}

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= "Labeled horizontal levels: a range box (top, bottom, midpoint) plus any number of custom lines, with optional Source/Follow sharing between charts of the same contract (micro and full-size included).";
				Name						= "Level Lines";
				Calculate					= Calculate.OnBarClose;
				IsOverlay					= true;
				IsAutoScale					= false;
				DisplayInDataBox			= false;
				DrawOnPricePanel			= true;
				PaintPriceMarkers			= false;
				IsSuspendedWhileInactive	= true;
				ScaleJustification			= ScaleJustification.Right;
				SharingMode			= TatankaLevelLinesSharingMode.Off;

				RangeTop			= 0;
				RangeBottom			= 0;
				RangeTopLabel		= "TOP OF RANGE";
				RangeBottomLabel	= "BOTTOM OF RANGE";
				ShowRangeMid		= true;
				RangeMidLabel		= "RANGE MID";
				RangeStroke			= new Stroke(Brushes.Orange, DashStyleHelper.Solid, 2);
				RangeMidStroke		= new Stroke(Brushes.Orange, DashStyleHelper.Dash, 1);
				FillRange			= true;
				RangeFillBrush		= Brushes.Orange;
				RangeFillOpacity	= 6;

				CustomLevels		= string.Empty;
				DefaultStroke		= new Stroke(Brushes.Magenta, DashStyleHelper.Solid, 2);

				LabelFont			= new SimpleFont("Arial", 11);
				ShowPriceInLabel	= true;
				LabelsOnRight		= true;
				LabelAboveLine		= true;
				LabelBackground		= true;
			}
			else if (State == State.DataLoaded)
			{
				BuildCustomLines();
				localLevels = CaptureLocalLevels();
				activeSharingMode = SharingMode;
			}
			else if (State == State.Historical)
			{
				// ChartControl is not guaranteed to exist before State.Historical.
				// Ignore settings-preview instances and non-chart hosts.
				if (ChartControl != null && Instrument != null && activeSharingMode != TatankaLevelLinesSharingMode.Off)
				{
					sharedInstrument = ShareKey(Instrument, out sharedDisplay);
					sharingActive = true;
					if (activeSharingMode == TatankaLevelLinesSharingMode.Source)
						SharedLevelStore.Publish(sharedInstrument, localLevels, sourceToken);
					else
						SharedLevelStore.Subscribe(sharedInstrument, RefreshSharedLevels);
				}
			}
			else if (State == State.Terminated)
			{
				sharingActive = false;
				// Removing the Source, closing its chart, switching it to Off/Follow or to
				// another instrument sends its Follow charts back to "waiting for Source"
				// instead of leaving stale lines on them.
				if (activeSharingMode == TatankaLevelLinesSharingMode.Source)
					SharedLevelStore.Withdraw(sharedInstrument, sourceToken);
				SharedLevelStore.Unsubscribe(sharedInstrument, RefreshSharedLevels);
			}
		}

		protected override void OnBarUpdate()
		{
			// Nothing to calculate per bar; all drawing happens in OnRender.
		}

		#region Sharing
		// Full-size and micro roots quoted in the same price units, so one set of levels
		// fits both. Each pair shares under the full-size root.
		private static readonly string[][] MicroPairs =
		{
			new[] { "ES",  "MES" },
			new[] { "NQ",  "MNQ" },
			new[] { "YM",  "MYM" },
			new[] { "RTY", "M2K" },
			new[] { "CL",  "MCL" },
			new[] { "GC",  "MGC" },
			new[] { "SI",  "SIL" },
			new[] { "HG",  "MHG" }
		};

		// Sharing key: the contract's full name with a micro root swapped for its
		// full-size root, so MES 12-26 and ES 12-26 share, ES 12-26 and ES 03-27 don't.
		// display is what the waiting message shows, e.g. "ES/MES 12-26".
		private static string ShareKey(Instrument instrument, out string display)
		{
			string full = instrument.FullName ?? string.Empty;
			string root = instrument.MasterInstrument != null ? instrument.MasterInstrument.Name : null;
			if (!string.IsNullOrEmpty(root) && full.StartsWith(root, StringComparison.Ordinal))
			{
				string month = full.Substring(root.Length);
				foreach (string[] pair in MicroPairs)
				{
					if (string.Equals(root, pair[0], StringComparison.OrdinalIgnoreCase)
						|| string.Equals(root, pair[1], StringComparison.OrdinalIgnoreCase))
					{
						display = pair[0] + "/" + pair[1] + month;
						return pair[0] + month;
					}
				}
			}
			display = full;
			return full;
		}

		private SharedLevels CaptureLocalLevels()
		{
			List<LevelLine> lines = new List<LevelLine>();
			if (RangeTop > 0)
				lines.Add(new LevelLine { Price = RangeTop, Label = RangeTopLabel, Stroke = RangeStroke });
			if (RangeBottom > 0)
				lines.Add(new LevelLine { Price = RangeBottom, Label = RangeBottomLabel, Stroke = RangeStroke });
			if (ShowRangeMid && RangeTop > 0 && RangeBottom > 0)
				lines.Add(new LevelLine { Price = (RangeTop + RangeBottom) / 2.0, Label = RangeMidLabel, Stroke = RangeMidStroke });
			lines.AddRange(customLines);
			return new SharedLevels(lines.Where(line => line.Stroke != null).Select(line => new SharedLine(line)).ToArray(),
				RangeTop, RangeBottom, FillRange,
				RangeFillBrush == null ? null : Serialize.BrushToString(RangeFillBrush), RangeFillOpacity);
		}

		private void RefreshSharedLevels()
		{
			ChartControl chart = ChartControl;
			if (!sharingActive || chart == null || chart.Dispatcher.HasShutdownStarted || chart.Dispatcher.HasShutdownFinished)
				return;
			// A source can be on another chart's UI thread. Queue refresh on this
			// chart without waiting for a bar or blocking the source dispatcher.
			chart.Dispatcher.InvokeAsync(new Action(delegate
			{
				if (sharingActive)
					ForceRefresh();
			}));
		}

		private static Brush RestoreBrush(string value)
		{
			if (value == null)
				return null;
			Brush brush = Serialize.StringToBrush(value);
			if (brush != null && brush.CanFreeze && !brush.IsFrozen)
				brush.Freeze();
			return brush;
		}

		private void PrepareRenderLevels(SharedLevels levels)
		{
			if (ReferenceEquals(renderedLevels, levels))
				return;
			ReleaseRenderResources();
			renderedLines = new List<LevelLine>();
			foreach (SharedLine line in levels.Lines)
			{
				Stroke stroke = new Stroke(RestoreBrush(line.Brush), line.Dash, line.Width);
				stroke.Opacity = line.Opacity;
				renderedLines.Add(new LevelLine { Price = line.Price, Label = line.Label, Stroke = stroke });
			}
			renderedFillBrush = RestoreBrush(levels.FillBrush);
			renderedLevels = levels;
		}

		private void ReleaseRenderResources()
		{
			if (renderedLines != null)
				foreach (LevelLine line in renderedLines)
					line.Stroke.RenderTarget = null;
		}

		public override void OnRenderTargetChanged()
		{
			ReleaseRenderResources();
			base.OnRenderTargetChanged();
		}

		private void DrawWaitingForSource()
		{
			using (SharpDX.DirectWrite.TextFormat format = new SimpleFont("Arial", 12).ToDirectWriteTextFormat())
			using (SharpDX.Direct2D1.Brush brush = Brushes.Gray.ToDxBrush(RenderTarget))
			{
				RenderTarget.DrawText("Level Lines: waiting for Source on " + sharedDisplay, format,
					new SharpDX.RectangleF(ChartPanel.X + 8, ChartPanel.Y + 24, Math.Max(10, ChartPanel.W - 16), 48), brush);
			}
		}
		#endregion

		#region Parsing
		private void BuildCustomLines()
		{
			customLines.Clear();
			if (string.IsNullOrWhiteSpace(CustomLevels))
				return;

			string[] entries = CustomLevels.Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string raw in entries)
			{
				string[] f = raw.Split('|');
				double price;
				if (!double.TryParse(f[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out price) || price <= 0)
					continue;

				string label			= f.Length > 1 ? f[1].Trim() : string.Empty;
				Brush brush				= f.Length > 2 ? ParseBrush(f[2].Trim()) : null;
				DashStyleHelper dash	= f.Length > 3 ? ParseDash(f[3].Trim(), DefaultStroke.DashStyleHelper) : DefaultStroke.DashStyleHelper;

				float width;
				if (f.Length < 5 || !float.TryParse(f[4].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out width) || width <= 0)
					width = DefaultStroke.Width;

				customLines.Add(new LevelLine
				{
					Price	= price,
					Label	= label,
					Stroke	= new Stroke(brush ?? DefaultStroke.Brush, dash, width)
				});
			}
		}

		private static Brush ParseBrush(string text)
		{
			if (string.IsNullOrEmpty(text))
				return null;
			try
			{
				object c = ColorConverter.ConvertFromString(text);
				if (c is Color)
				{
					SolidColorBrush b = new SolidColorBrush((Color)c);
					b.Freeze();
					return b;
				}
			}
			catch { }
			return null;
		}

		private static DashStyleHelper ParseDash(string text, DashStyleHelper fallback)
		{
			switch (text.Replace(" ", string.Empty).ToLowerInvariant())
			{
				case "solid":		return DashStyleHelper.Solid;
				case "dash":
				case "dashed":		return DashStyleHelper.Dash;
				case "dot":
				case "dotted":		return DashStyleHelper.Dot;
				case "dashdot":		return DashStyleHelper.DashDot;
				case "dashdotdot":	return DashStyleHelper.DashDotDot;
				default:			return fallback;
			}
		}
		#endregion

		#region Rendering
		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			base.OnRender(chartControl, chartScale);
			if (RenderTarget == null || ChartPanel == null)
				return;

			SharedLevels levels = activeSharingMode == TatankaLevelLinesSharingMode.Follow
				? SharedLevelStore.Get(sharedInstrument) : localLevels;
			if (levels == null)
			{
				if (sharingActive && activeSharingMode == TatankaLevelLinesSharingMode.Follow)
					DrawWaitingForSource();
				return;
			}
			PrepareRenderLevels(levels);

			float x0		= ChartPanel.X;
			float x1		= ChartPanel.X + ChartPanel.W;
			bool hasTop		= levels.RangeTop > 0;
			bool hasBottom	= levels.RangeBottom > 0;

			SharpDX.Direct2D1.AntialiasMode oldMode = RenderTarget.AntialiasMode;
			RenderTarget.AntialiasMode = SharpDX.Direct2D1.AntialiasMode.Aliased;

			// Range shading
			if (levels.FillRange && hasTop && hasBottom && renderedFillBrush != null)
			{
				float yHi = chartScale.GetYByValue(Math.Max(levels.RangeTop, levels.RangeBottom));
				float yLo = chartScale.GetYByValue(Math.Min(levels.RangeTop, levels.RangeBottom));
				using (SharpDX.Direct2D1.Brush fill = renderedFillBrush.ToDxBrush(RenderTarget))
				{
					fill.Opacity = Math.Max(0, Math.Min(100, levels.FillOpacity)) / 100f;
					RenderTarget.FillRectangle(new SharpDX.RectangleF(x0, yHi, x1 - x0, yLo - yHi), fill);
				}
			}

			// Draw the lines, queue the labels
			List<PendingLabel> labels = new List<PendingLabel>();
			foreach (LevelLine item in renderedLines)
			{
				if (item.Stroke == null || item.Price < chartScale.MinValue || item.Price > chartScale.MaxValue)
					continue;

				float y = chartScale.GetYByValue(item.Price);
				item.Stroke.RenderTarget = RenderTarget;
				RenderTarget.DrawLine(new SharpDX.Vector2(x0, y), new SharpDX.Vector2(x1, y),
					item.Stroke.BrushDX, item.Stroke.Width, item.Stroke.StrokeStyle);

				string text = BuildLabelText(item.Label, item.Price);
				if (text.Length > 0)
					labels.Add(new PendingLabel { Y = y, Text = text, Brush = item.Stroke.BrushDX, LineWidth = item.Stroke.Width });
			}

			RenderTarget.AntialiasMode = oldMode;

			if (labels.Count > 0)
				DrawLabels(labels, x0, x1);
		}

		private string BuildLabelText(string label, double price)
		{
			string text = (label ?? string.Empty).Trim();
			if (!ShowPriceInLabel)
				return text;

			string p = Instrument != null
				? Instrument.MasterInstrument.FormatPrice(price)
				: price.ToString("0.00", CultureInfo.InvariantCulture);
			return text.Length > 0 ? text + "  " + p : p;
		}

		private void DrawLabels(List<PendingLabel> labels, float x0, float x1)
		{
			if (LabelFont == null)
				LabelFont = new SimpleFont("Arial", 11);

			// Top to bottom, so a label that would overlap the one above it gets pushed down instead
			labels.Sort((a, b) => a.Y.CompareTo(b.Y));

			const float pad	= 4f;
			float lastBottom	= float.MinValue;

			using (SharpDX.DirectWrite.TextFormat format = LabelFont.ToDirectWriteTextFormat())
			using (SharpDX.Direct2D1.SolidColorBrush bg = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, new SharpDX.Color4(0f, 0f, 0f, 0.6f)))
			{
				foreach (PendingLabel l in labels)
				{
					using (SharpDX.DirectWrite.TextLayout layout = new SharpDX.DirectWrite.TextLayout(
						NinjaTrader.Core.Globals.DirectWriteFactory, l.Text, format, Math.Max(10f, x1 - x0), format.FontSize * 3))
					{
						float w = layout.Metrics.Width;
						float h = layout.Metrics.Height;

						float ty = LabelAboveLine
							? l.Y - h - l.LineWidth / 2f - 1f
							: l.Y + l.LineWidth / 2f + 1f;
						if (ty < lastBottom + 1f)
							ty = lastBottom + 1f;

						float tx = LabelsOnRight ? x1 - w - pad * 2f : x0 + pad;

						if (LabelBackground)
							RenderTarget.FillRectangle(new SharpDX.RectangleF(tx - pad, ty, w + pad * 2f, h), bg);

						RenderTarget.DrawTextLayout(new SharpDX.Vector2(tx, ty), layout, l.Brush);
						lastBottom = ty + h;
					}
				}
			}
		}
		#endregion

		#region Properties
		[Display(Name = "Sharing mode", Description = "Off: use this chart's settings. Source: publish levels, labels, line styles and range shading for this contract month. Follow: display the Source's levels. Add this indicator to every participating chart. A micro shares with its full-size contract (ES/MES, NQ/MNQ, YM/MYM, RTY/M2K, CL/MCL, GC/MGC, SI/SIL, HG/MHG), same month only. Use one Source per contract; if several are running, the newest wins. Label display settings remain local to each chart.", Order = 1, GroupName = "0. Sharing")]
		public TatankaLevelLinesSharingMode SharingMode { get; set; }

		[Display(Name = "Range top (0 = off)", Order = 1, GroupName = "1. Range")]
		public double RangeTop { get; set; }

		[Display(Name = "Range bottom (0 = off)", Order = 2, GroupName = "1. Range")]
		public double RangeBottom { get; set; }

		[Display(Name = "Top label", Order = 3, GroupName = "1. Range")]
		public string RangeTopLabel { get; set; }

		[Display(Name = "Bottom label", Order = 4, GroupName = "1. Range")]
		public string RangeBottomLabel { get; set; }

		[Display(Name = "Show midpoint", Order = 5, GroupName = "1. Range")]
		public bool ShowRangeMid { get; set; }

		[Display(Name = "Midpoint label", Order = 6, GroupName = "1. Range")]
		public string RangeMidLabel { get; set; }

		[Display(Name = "Top/bottom line", Order = 7, GroupName = "1. Range")]
		public Stroke RangeStroke { get; set; }

		[Display(Name = "Midpoint line", Order = 8, GroupName = "1. Range")]
		public Stroke RangeMidStroke { get; set; }

		[Display(Name = "Shade the range", Order = 9, GroupName = "1. Range")]
		public bool FillRange { get; set; }

		[XmlIgnore]
		[Display(Name = "Shade color", Order = 10, GroupName = "1. Range")]
		public Brush RangeFillBrush { get; set; }

		[Browsable(false)]
		public string RangeFillBrushSerializable
		{
			get { return Serialize.BrushToString(RangeFillBrush); }
			set { RangeFillBrush = Serialize.StringToBrush(value); }
		}

		[Range(0, 100)]
		[Display(Name = "Shade opacity %", Order = 11, GroupName = "1. Range")]
		public int RangeFillOpacity { get; set; }

		[XmlIgnore]
		[RefreshProperties(RefreshProperties.All)]
		[Display(Name = "Open large levels editor", Description = "Click this checkbox to open a large, resizable text box with examples. Save levels, then click Apply or OK in the indicator settings. The checkbox resets after opening.", Order = 0, GroupName = "2. Custom levels")]
		public bool OpenCustomLevelsEditor
		{
			get { return false; }
			set
			{
				// An action only: never saved in templates or run during indicator cloning.
				if (!value)
					return;

				var dialog = new TatankaLevelLinesDialog(CustomLevels);
				DependencyObject focusedElement = Keyboard.FocusedElement as DependencyObject;
				Window owner = focusedElement != null ? Window.GetWindow(focusedElement) : null;
				if (owner != null)
				{
					dialog.Owner = owner;
					dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
				}

				if (dialog.ShowDialog() == true)
					CustomLevels = dialog.Levels;
			}
		}

		[PropertyEditor("NinjaTrader.Gui.Tools.MultilineEditor")]
		[Display(Name = "Levels (comma / semicolon separated)", Description = "Use Open large levels editor for a roomy text box. Separate LEVELS with commas, semicolons, or new lines. Example: 7845.75|VAH, 7830|POC, 7810|VAL. Within each level, use pipes: price|label|color|style|width. Only price is required; 7845.75, 7830, 7810 also works. Use decimal points and no thousands separators. Commas and semicolons are separators, so do not use them inside labels. Colors: names or hex (#FF00FF). Styles: Solid, Dash, Dot, DashDot, DashDotDot. Full example: 7845.75|VAH|Magenta|Dash|2", Order = 1, GroupName = "2. Custom levels")]
		public string CustomLevels { get; set; }

		[XmlIgnore]
		[Display(Name = "Example (3 levels)", Description = "Copy this example into Levels or the large editor. Commas, semicolons, or new lines separate levels. The | character separates the price from its label. This example does not draw any lines.", Order = 2, GroupName = "2. Custom levels")]
		public string CustomLevelsExample
		{
			get { return "7845.75|VAH, 7830|POC, 7810|VAL"; }
		}

		[Display(Name = "Default line", Description = "Used for any field a custom level leaves out.", Order = 3, GroupName = "2. Custom levels")]
		public Stroke DefaultStroke { get; set; }

		[Display(Name = "Label font", Order = 1, GroupName = "3. Labels")]
		public SimpleFont LabelFont { get; set; }

		[Display(Name = "Show price in label", Order = 2, GroupName = "3. Labels")]
		public bool ShowPriceInLabel { get; set; }

		[Display(Name = "Labels on right edge", Order = 3, GroupName = "3. Labels")]
		public bool LabelsOnRight { get; set; }

		[Display(Name = "Label above line", Order = 4, GroupName = "3. Labels")]
		public bool LabelAboveLine { get; set; }

		[Display(Name = "Label background", Order = 5, GroupName = "3. Labels")]
		public bool LabelBackground { get; set; }
		#endregion
	}
}

// Plain WPF window: uses NinjaTrader's existing references, with no extra DLL setup.
namespace NinjaTrader.Gui.Tools
{
	internal sealed class TatankaLevelLinesDialog : Window
	{
		private readonly System.Windows.Controls.TextBox levelsInput;

		public string Levels { get { return levelsInput.Text; } }

		public TatankaLevelLinesDialog(string levels)
		{
			Title = "Level Lines - Custom levels";
			Width = 760;
			Height = 570;
			MinWidth = 520;
			MinHeight = 420;
			WindowStartupLocation = WindowStartupLocation.CenterScreen;
			ShowInTaskbar = false;
			Background = SystemColors.WindowBrush;
			Foreground = SystemColors.WindowTextBrush;
			FontFamily = new FontFamily("Segoe UI");
			FontSize = 13;

			var panel = new System.Windows.Controls.Grid { Margin = new Thickness(16) };
			panel.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
			panel.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
			panel.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
			panel.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });

			var instructions = new System.Windows.Controls.TextBlock
			{
				Text = "Enter or paste your levels below. Separate LEVELS with commas, semicolons, or new lines.\n"
					+ "Press Enter for a new line. Only the price is required.",
				TextWrapping = TextWrapping.Wrap,
				Margin = new Thickness(0, 0, 0, 10)
			};
			panel.Children.Add(instructions);

			levelsInput = new System.Windows.Controls.TextBox
			{
				Name = "TatankaCustomLevelsInput",
				Text = levels ?? string.Empty,
				AcceptsReturn = true,
				TextWrapping = TextWrapping.Wrap,
				VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
				VerticalContentAlignment = VerticalAlignment.Top,
				Padding = new Thickness(8),
				Background = SystemColors.WindowBrush,
				Foreground = SystemColors.WindowTextBrush
			};
			System.Windows.Controls.Grid.SetRow(levelsInput, 1);
			panel.Children.Add(levelsInput);

			var examples = new System.Windows.Controls.TextBlock
			{
				Text = "Price only: 7845.75, 7830, 7810\n"
					+ "With labels: 7845.75|VAH, 7830|POC, 7810|VAL\n\n"
					+ "Within ONE level, use | (pipe) between fields: price|label|color|style|width\n"
					+ "Full example: 7845.75|VAH|Magenta|Dash|2\n"
					+ "Optional color, style, and width use Default line when omitted.\n\n"
					+ "Use decimal points; no thousands commas. Do not use commas or semicolons inside labels.\n"
					+ "After saving, click Apply or OK in the indicator settings to update the chart.",
				TextWrapping = TextWrapping.Wrap,
				Margin = new Thickness(0, 10, 0, 14)
			};
			System.Windows.Controls.Grid.SetRow(examples, 2);
			panel.Children.Add(examples);

			var buttons = new System.Windows.Controls.StackPanel
			{
				Orientation = System.Windows.Controls.Orientation.Horizontal,
				HorizontalAlignment = HorizontalAlignment.Right
			};
			var save = new System.Windows.Controls.Button
			{
				Content = "Save levels", IsDefault = true, MinWidth = 100,
				Padding = new Thickness(10, 5, 10, 5), Margin = new Thickness(0, 0, 8, 0)
			};
			save.Click += delegate { DialogResult = true; };
			buttons.Children.Add(save);
			buttons.Children.Add(new System.Windows.Controls.Button
			{
				Content = "Cancel", IsCancel = true, MinWidth = 90, Padding = new Thickness(10, 5, 10, 5)
			});
			System.Windows.Controls.Grid.SetRow(buttons, 3);
			panel.Children.Add(buttons);
			Content = panel;
			Loaded += delegate { levelsInput.Focus(); levelsInput.CaretIndex = levelsInput.Text.Length; };
		}
	}
}
