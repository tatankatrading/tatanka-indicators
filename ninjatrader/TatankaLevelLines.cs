// Copyright (c) 2026 Tatanka Trading LLC.
// This Source Code Form is subject to the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, obtain one at https://mozilla.org/MPL/2.0/.
// Tatanka Level Lines v1.0.0 for NinjaTrader 8 - tatankatrading.com

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

namespace NinjaTrader.NinjaScript.Indicators
{
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

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= "Labeled horizontal levels: a range box (top, bottom, midpoint) plus any number of custom lines.";
				Name						= "Level Lines";
				Calculate					= Calculate.OnBarClose;
				IsOverlay					= true;
				IsAutoScale					= false;
				DisplayInDataBox			= false;
				DrawOnPricePanel			= true;
				PaintPriceMarkers			= false;
				IsSuspendedWhileInactive	= true;
				ScaleJustification			= ScaleJustification.Right;

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
			}
		}

		protected override void OnBarUpdate()
		{
			// Nothing to calculate per bar; all drawing happens in OnRender.
		}

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

			float x0		= ChartPanel.X;
			float x1		= ChartPanel.X + ChartPanel.W;
			bool hasTop		= RangeTop > 0;
			bool hasBottom	= RangeBottom > 0;

			SharpDX.Direct2D1.AntialiasMode oldMode = RenderTarget.AntialiasMode;
			RenderTarget.AntialiasMode = SharpDX.Direct2D1.AntialiasMode.Aliased;

			// Range shading
			if (FillRange && hasTop && hasBottom && RangeFillBrush != null)
			{
				float yHi = chartScale.GetYByValue(Math.Max(RangeTop, RangeBottom));
				float yLo = chartScale.GetYByValue(Math.Min(RangeTop, RangeBottom));
				using (SharpDX.Direct2D1.Brush fill = RangeFillBrush.ToDxBrush(RenderTarget))
				{
					fill.Opacity = Math.Max(0, Math.Min(100, RangeFillOpacity)) / 100f;
					RenderTarget.FillRectangle(new SharpDX.RectangleF(x0, yHi, x1 - x0, yLo - yHi), fill);
				}
			}

			// Collect every line: range lines first, then the custom list
			List<LevelLine> items = new List<LevelLine>();
			if (hasTop)
				items.Add(new LevelLine { Price = RangeTop, Label = RangeTopLabel, Stroke = RangeStroke });
			if (hasBottom)
				items.Add(new LevelLine { Price = RangeBottom, Label = RangeBottomLabel, Stroke = RangeStroke });
			if (ShowRangeMid && hasTop && hasBottom)
				items.Add(new LevelLine { Price = (RangeTop + RangeBottom) / 2.0, Label = RangeMidLabel, Stroke = RangeMidStroke });
			items.AddRange(customLines);

			// Draw the lines, queue the labels
			List<PendingLabel> labels = new List<PendingLabel>();
			foreach (LevelLine item in items)
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
