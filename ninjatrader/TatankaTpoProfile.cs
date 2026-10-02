// Copyright (c) 2026 Tatanka Trading LLC.
// This Source Code Form is subject to the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, obtain one at https://mozilla.org/MPL/2.0/.
// Tatanka TPO Profile v1.0.0 for NinjaTrader 8 - tatankatrading.com

#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
	public class TatankaTpoProfile : Indicator
	{
		private const string Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

		private ATR dailyAtr;
		private readonly Dictionary<long, List<int>> liveRows = new Dictionary<long, List<int>>();
		private readonly List<SavedProfile> saved = new List<SavedProfile>();

		private DateTime sessionStartTime;	// actual session open (letter math)
		private DateTime drawStartTime;		// first in-session bar timestamp (render anchor)
		private DateTime lastSessBarTime;	// last in-session bar timestamp
		private bool hasLive;
		private bool liveInSession;
		private SessionIterator sessionIterator;

		private double tickStep;
		private double colSeconds;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= @"TPO Profile ported from TradingView Pine v6. Letter boxes for recent sessions, polyline outlines for older days, live developing session.";
				Name						= "TPO Profile";
				Calculate					= Calculate.OnBarClose;
				IsOverlay					= true;
				DisplayInDataBox			= false;
				DrawOnPricePanel			= true;
				PaintPriceMarkers			= false;
				IsAutoScale					= false;
				IsSuspendedWhileInactive	= true;
				ScaleJustification			= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				MaximumBarsLookBack			= MaximumBarsLookBack.Infinite;

				FilterByTime				= true;
				SessionTime					= "0830-1500";
				BlockMinutes				= 30;
				AutoRowHeight				= true;
				TargetRows					= 80;
				RowHeightTicks				= 8;
				LetterDays					= 3;
				ShowPolyline				= true;
				PolylineDays				= 15;
				BoxTransparency				= 0;
				StartColor					= Brushes.Blue;
				EndColor					= Brushes.Fuchsia;
				TextColor					= Brushes.White;
				PocColor					= Brushes.Yellow;
				PolyColor					= Brushes.Gray;
				VaColor						= Brushes.Teal;
				XOffsetBars					= 0;
				ShowLive					= false;
				ShowValueArea				= false;
				ExtendPoc					= false;
			}
			else if (State == State.Configure)
			{
				AddDataSeries(BarsPeriodType.Day, 1);
			}
			else if (State == State.DataLoaded)
			{
				dailyAtr = ATR(BarsArray[1], 10);
				liveRows.Clear();
				saved.Clear();
				hasLive = false;
				liveInSession = false;
				colSeconds = GetBarSeconds();
				sessionIterator = new SessionIterator(Bars);
			}
			else if (State == State.Terminated)
			{
				liveRows.Clear();
				saved.Clear();
			}
		}

		protected override void OnBarUpdate()
		{
			if (BarsInProgress != 0)
				return;
			if (CurrentBars[0] < 1)
				return;

			tickStep = GetTickStep();
			if (tickStep <= 0)
				return;

			// NinjaTrader stamps bars with their CLOSE time (TradingView/Pine uses OPEN time).
			// Session window is evaluated as (start, end] on the close stamp so the 08:30 bar
			// (pre-open) is excluded and the 15:00 bar (final RTH bar) is included.
			DateTime t = Time[0];
			bool timeOk;
			bool newSess;

			if (FilterByTime)
			{
				timeOk = IsInSession(t);
				// Session-open change also catches RTH-only templates where no out-of-window bar exists.
				newSess = timeOk && (!hasLive || GetSessionOpen(t) != sessionStartTime);
			}
			else
			{
				timeOk = true;
				newSess = !hasLive || Bars.IsFirstBarOfSession;
			}

			if (hasLive && (newSess || !timeOk))
			{
				SaveProfile();
				liveRows.Clear();
				hasLive = false;
			}

			if (newSess)
			{
				sessionStartTime = FilterByTime ? GetSessionOpen(t) : GetTemplateSessionOpen(t);
				drawStartTime = t;
				hasLive = true;
			}

			liveInSession = timeOk && hasLive;

			if (liveInSession)
			{
				int tIdx = GetLetterIndex(t);
				long lo = PriceKey(Low[0]);
				long hi = PriceKey(High[0]);
				if (hi < lo)
				{
					long tmp = lo;
					lo = hi;
					hi = tmp;
				}

				for (long k = lo; k <= hi; k++)
				{
					List<int> letters;
					if (!liveRows.TryGetValue(k, out letters))
					{
						letters = new List<int>();
						liveRows[k] = letters;
					}
					if (letters.Count == 0 || letters[letters.Count - 1] != tIdx)
						letters.Add(tIdx);
				}
				lastSessBarTime = t;
			}
		}

		#region Session / bins
		private bool IsInSession(DateTime t)
		{
			int startMin, endMin;
			if (!ParseSession(SessionTime, out startMin, out endMin))
				return true;

			// (start, end] on bar close time
			double now = t.TimeOfDay.TotalMinutes;
			if (startMin <= endMin)
				return now > startMin && now <= endMin;
			return now > startMin || now <= endMin;
		}

		// Actual open of the custom session window this close-stamped bar belongs to.
		private DateTime GetSessionOpen(DateTime t)
		{
			int startMin, endMin;
			ParseSession(SessionTime, out startMin, out endMin);
			double now = t.TimeOfDay.TotalMinutes;
			DateTime day = t.Date;
			if (startMin > endMin && now <= endMin)	// overnight window, after-midnight part
				day = day.AddDays(-1);
			return day.AddMinutes(startMin);
		}

		// Actual open of the trading-hours-template session (used when FilterByTime is off).
		private DateTime GetTemplateSessionOpen(DateTime t)
		{
			if (sessionIterator == null)
				return t;
			sessionIterator.GetNextSession(t, true);
			return sessionIterator.ActualSessionBegin;
		}

		private static bool ParseSession(string s, out int startMin, out int endMin)
		{
			startMin = 8 * 60 + 30;
			endMin = 15 * 60;
			if (string.IsNullOrEmpty(s))
				return false;

			string[] parts = s.Split('-');
			if (parts.Length != 2)
				return false;

			int a, b;
			if (!TryParseHhmm(parts[0], out a) || !TryParseHhmm(parts[1], out b))
				return false;

			startMin = a;
			endMin = b;
			return true;
		}

		private static bool TryParseHhmm(string raw, out int minutes)
		{
			minutes = 0;
			string t = raw.Trim();
			if (t.Length == 3)
				t = "0" + t;
			if (t.Length != 4)
				return false;
			int hh, mm;
			if (!int.TryParse(t.Substring(0, 2), out hh) || !int.TryParse(t.Substring(2, 2), out mm))
				return false;
			if (hh < 0 || hh > 23 || mm < 0 || mm > 59)
				return false;
			minutes = hh * 60 + mm;
			return true;
		}

		private int GetLetterIndex(DateTime t)
		{
			// -1ms: a bar closing exactly on a block boundary belongs to the block it ends
			double ms = (t - sessionStartTime).TotalMilliseconds - 1.0;
			double blockMs = BlockMinutes * 60.0 * 1000.0;
			if (blockMs <= 0)
				return 0;
			int idx = (int)Math.Floor(ms / blockMs);
			if (idx < 0)
				idx = 0;
			return idx;
		}

		private double GetTickStep()
		{
			double tick = TickSize;
			if (tick <= 0)
				tick = 0.01;

			double mult = RowHeightTicks;
			if (AutoRowHeight && CurrentBars.Length > 1 && CurrentBars[1] >= 10 && dailyAtr != null)
			{
				double atr = dailyAtr[0];
				if (!double.IsNaN(atr) && atr > 0)
					mult = Math.Max(Math.Round(atr / TargetRows / tick), 1.0);
			}
			return tick * Math.Max(mult, 1.0);
		}

		private long PriceKey(double price)
		{
			return (long)Math.Round(price / tickStep);
		}

		private double KeyToPrice(long key)
		{
			return key * tickStep;
		}

		private double GetBarSeconds()
		{
			switch (BarsPeriod.BarsPeriodType)
			{
				case BarsPeriodType.Second: return BarsPeriod.Value;
				case BarsPeriodType.Minute: return BarsPeriod.Value * 60.0;
				case BarsPeriodType.Day: return BarsPeriod.Value * 86400.0;
				case BarsPeriodType.Week: return BarsPeriod.Value * 604800.0;
				default: return 60.0;
			}
		}

		private static string GetLetter(int index)
		{
			if (index >= 0 && index < Letters.Length)
				return Letters.Substring(index, 1);
			return "?";
		}
		#endregion

		#region Save / VA
		private void SaveProfile()
		{
			if (liveRows.Count == 0)
				return;

			SavedProfile prof = BuildProfileFromMap(liveRows, drawStartTime, lastSessBarTime);
			if (prof != null)
				saved.Add(prof);
		}

		private SavedProfile BuildProfileFromMap(Dictionary<long, List<int>> map, DateTime tStart, DateTime tEnd)
		{
			if (map.Count == 0)
				return null;

			List<long> keys = new List<long>(map.Keys);
			keys.Sort();

			SavedProfile p = new SavedProfile();
			p.TimeStart = tStart;
			p.TimeEnd = tEnd;
			p.TickStep = tickStep;
			p.MaxCount = 0;
			p.PocPrice = 0;

			foreach (long k in keys)
			{
				List<int> letters = map[k];
				if (letters == null || letters.Count == 0)
					continue;
				TpoRow row = new TpoRow();
				row.Price = KeyToPrice(k);
				row.Letters = new List<int>(letters);
				p.Rows.Add(row);
				if (letters.Count >= p.MaxCount)
				{
					p.MaxCount = letters.Count;
					p.PocPrice = row.Price;
				}
			}

			if (p.Rows.Count == 0)
				return null;
			return p;
		}

		private static void GetValueArea(SavedProfile p, out double vah, out double val)
		{
			vah = p.PocPrice;
			val = p.PocPrice;
			int sz = p.Rows.Count;
			if (sz == 0)
				return;

			int total = 0;
			int pocIdx = 0;
			for (int i = 0; i < sz; i++)
			{
				total += p.Rows[i].Letters.Count;
				if (Math.Abs(p.Rows[i].Price - p.PocPrice) < 1e-10)
					pocIdx = i;
			}

			int target = (int)Math.Round(total * 0.70);
			int cum = p.Rows[pocIdx].Letters.Count;
			int lowIdx = pocIdx;
			int highIdx = pocIdx;

			while (cum < target && (lowIdx > 0 || highIdx < sz - 1))
			{
				int nextLow = lowIdx > 0 ? p.Rows[lowIdx - 1].Letters.Count : 0;
				int nextHigh = highIdx < sz - 1 ? p.Rows[highIdx + 1].Letters.Count : 0;
				if (nextHigh > nextLow)
				{
					highIdx++;
					cum += nextHigh;
				}
				else if (nextLow > 0)
				{
					lowIdx--;
					cum += nextLow;
				}
				else if (nextHigh > 0)
				{
					highIdx++;
					cum += nextHigh;
				}
				else
					break;
			}

			vah = p.Rows[highIdx].Price;
			val = p.Rows[lowIdx].Price;
		}
		#endregion

		#region Render
		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			base.OnRender(chartControl, chartScale);

			if (Bars == null || ChartBars == null || RenderTarget == null)
				return;
			if (colSeconds <= 0)
				colSeconds = GetBarSeconds();

			double offsetSec = XOffsetBars * colSeconds;
			int total = saved.Count;
			int letterStart = Math.Max(total - LetterDays, 0);

			System.Windows.Media.Color cStart = ToMedia(StartColor);
			System.Windows.Media.Color cEnd = ToMedia(EndColor);
			System.Windows.Media.Color cTxt = ToMedia(TextColor);
			System.Windows.Media.Color cPoc = ToMedia(PocColor);
			System.Windows.Media.Color cPoly = ToMedia(PolyColor);
			System.Windows.Media.Color cVa = ToMedia(VaColor);

			byte boxAlpha = (byte)Math.Max(0, Math.Min(255, (int)Math.Round((100 - BoxTransparency) * 2.55)));

			StrokeStyle dash = new StrokeStyle(Core.Globals.D2DFactory, new StrokeStyleProperties
			{
				DashStyle = SharpDX.Direct2D1.DashStyle.Dash
			});

			TextFormat fmt = new TextFormat(Core.Globals.DirectWriteFactory, "Arial", 10)
			{
				TextAlignment = SharpDX.DirectWrite.TextAlignment.Center,
				ParagraphAlignment = ParagraphAlignment.Center
			};

			try
			{
				if (ShowPolyline && letterStart > 0)
				{
					int polyStart = Math.Max(letterStart - PolylineDays, 0);
					for (int i = polyStart; i < letterStart; i++)
						DrawPolylineProfile(chartControl, chartScale, saved[i], i, total, offsetSec, cPoly, cPoc, cVa, dash);
				}

				for (int i = letterStart; i < total; i++)
					DrawLetterProfile(chartControl, chartScale, saved[i], i, total, offsetSec, cStart, cEnd, cTxt, cPoc, cVa, boxAlpha, fmt, dash);

				if (ShowLive && liveInSession && liveRows.Count > 0)
				{
					SavedProfile live = BuildProfileFromMap(liveRows, drawStartTime, Times[0][0]);
					if (live != null)
						DrawLetterProfile(chartControl, chartScale, live, total, total + 1, offsetSec, cStart, cEnd, cTxt, cPoc, cVa, boxAlpha, fmt, dash);
				}
			}
			finally
			{
				dash.Dispose();
				fmt.Dispose();
			}
		}

		private DateTime GetPocExtEnd(int idx, int total)
		{
			if (!ExtendPoc)
				return DateTime.MinValue;
			if (idx + 1 < total)
				return saved[idx + 1].TimeEnd;
			return Times[0][0].AddSeconds(colSeconds);
		}

		private void DrawLetterProfile(ChartControl chartControl, ChartScale chartScale, SavedProfile prof, int idx, int total,
			double offsetSec, System.Windows.Media.Color cStart, System.Windows.Media.Color cEnd,
			System.Windows.Media.Color cTxt, System.Windows.Media.Color cPoc, System.Windows.Media.Color cVa,
			byte boxAlpha, TextFormat fmt, StrokeStyle dash)
		{
			if (prof.Rows.Count == 0)
				return;

			double step = prof.TickStep > 0 ? prof.TickStep : tickStep;
			DateTime ext = GetPocExtEnd(idx, total);

			using (SharpDX.Direct2D1.SolidColorBrush txtBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDx(cTxt, 230)))
			using (SharpDX.Direct2D1.SolidColorBrush pocBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDx(cPoc, 255)))
			using (SharpDX.Direct2D1.SolidColorBrush vaBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDx(cVa, 255)))
			{
				foreach (TpoRow row in prof.Rows)
				{
					for (int j = 0; j < row.Letters.Count; j++)
					{
						int tIdx = row.Letters[j];
						DateTime tLeft = prof.TimeStart.AddSeconds(j * colSeconds + offsetSec);
						DateTime tRight = prof.TimeStart.AddSeconds((j + 1) * colSeconds + offsetSec);

						float x1 = chartControl.GetXByTime(tLeft);
						float x2 = chartControl.GetXByTime(tRight);
						float yTop = chartScale.GetYByValue(row.Price + step / 2.0);
						float yBot = chartScale.GetYByValue(row.Price - step / 2.0);
						if (x2 < x1)
						{
							float tmp = x1;
							x1 = x2;
							x2 = tmp;
						}
						if (yBot < yTop)
						{
							float tmp = yTop;
							yTop = yBot;
							yBot = tmp;
						}

						float w = x2 - x1;
						float h = yBot - yTop;
						if (w < 0.5f || h < 0.5f)
							continue;

						System.Windows.Media.Color bg = Lerp(cStart, cEnd, Clamp01(tIdx / 20.0));
						using (SharpDX.Direct2D1.SolidColorBrush fill = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDx(bg, boxAlpha)))
						{
							SharpDX.RectangleF rc = new SharpDX.RectangleF(x1, yTop, w, h);
							RenderTarget.FillRectangle(rc, fill);
							if (Math.Abs(row.Price - prof.PocPrice) < 1e-10)
								RenderTarget.DrawRectangle(rc, pocBrush, 1.2f);
						}

						if (w >= 6 && h >= 7)
						{
							string let = GetLetter(tIdx);
							using (TextLayout layout = new TextLayout(Core.Globals.DirectWriteFactory, let, fmt, w, h))
								RenderTarget.DrawTextLayout(new Vector2(x1, yTop), layout, txtBrush);
						}
					}
				}

				DateTime pocEnd = prof.TimeStart.AddSeconds((prof.MaxCount + 5) * colSeconds + offsetSec);
				if (ext != DateTime.MinValue && ext > pocEnd)
					pocEnd = ext;

				DrawHLine(chartControl, chartScale, prof.TimeStart.AddSeconds(offsetSec), pocEnd, prof.PocPrice, pocBrush, 2f, null);

				if (ShowValueArea)
				{
					double vah, val;
					GetValueArea(prof, out vah, out val);
					DateTime vaEnd = prof.TimeStart.AddSeconds((prof.MaxCount + 5) * colSeconds + offsetSec);
					DrawHLine(chartControl, chartScale, prof.TimeStart.AddSeconds(offsetSec), vaEnd, vah, vaBrush, 1f, dash);
					DrawHLine(chartControl, chartScale, prof.TimeStart.AddSeconds(offsetSec), vaEnd, val, vaBrush, 1f, dash);
				}
			}
		}

		private void DrawPolylineProfile(ChartControl chartControl, ChartScale chartScale, SavedProfile prof, int idx, int total,
			double offsetSec, System.Windows.Media.Color cPoly, System.Windows.Media.Color cPoc, System.Windows.Media.Color cVa, StrokeStyle dash)
		{
			if (prof.Rows.Count < 2)
				return;

			double step = prof.TickStep > 0 ? prof.TickStep : tickStep;
			List<Vector2> pts = new List<Vector2>();

			for (int i = 0; i < prof.Rows.Count; i++)
			{
				TpoRow row = prof.Rows[i];
				DateTime t = prof.TimeStart.AddSeconds(row.Letters.Count * colSeconds + offsetSec);
				float x = chartControl.GetXByTime(t);
				pts.Add(new Vector2(x, chartScale.GetYByValue(row.Price - step / 2.0)));
				pts.Add(new Vector2(x, chartScale.GetYByValue(row.Price + step / 2.0)));
			}

			DateTime leftT = prof.TimeStart.AddSeconds(offsetSec);
			float xL = chartControl.GetXByTime(leftT);
			for (int i = prof.Rows.Count - 1; i >= 0; i--)
			{
				TpoRow row = prof.Rows[i];
				pts.Add(new Vector2(xL, chartScale.GetYByValue(row.Price + step / 2.0)));
				pts.Add(new Vector2(xL, chartScale.GetYByValue(row.Price - step / 2.0)));
			}

			if (pts.Count < 3)
				return;

			SharpDX.Direct2D1.PathGeometry geo = new SharpDX.Direct2D1.PathGeometry(Core.Globals.D2DFactory);
			try
			{
				using (GeometrySink sink = geo.Open())
				{
					sink.BeginFigure(pts[0], FigureBegin.Filled);
					for (int i = 1; i < pts.Count; i++)
						sink.AddLine(pts[i]);
					sink.EndFigure(FigureEnd.Closed);
					sink.Close();
				}

				using (SharpDX.Direct2D1.SolidColorBrush fill = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDx(cPoly, 80)))
				using (SharpDX.Direct2D1.SolidColorBrush line = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDx(cPoly, 180)))
				{
					RenderTarget.FillGeometry(geo, fill);
					RenderTarget.DrawGeometry(geo, line, 1f);
				}
			}
			finally
			{
				geo.Dispose();
			}

			DateTime ext = GetPocExtEnd(idx, total);
			DateTime pocEnd = prof.TimeStart.AddSeconds((prof.MaxCount + 3) * colSeconds + offsetSec);
			if (ext != DateTime.MinValue && ext > pocEnd)
				pocEnd = ext;

			using (SharpDX.Direct2D1.SolidColorBrush pocBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDx(cPoc, 255)))
			using (SharpDX.Direct2D1.SolidColorBrush vaBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, ToDx(cVa, 255)))
			{
				DrawHLine(chartControl, chartScale, leftT, pocEnd, prof.PocPrice, pocBrush, 1f, dash);
				if (ShowValueArea)
				{
					double vah, val;
					GetValueArea(prof, out vah, out val);
					DateTime vaEnd = prof.TimeStart.AddSeconds((prof.MaxCount + 3) * colSeconds + offsetSec);
					DrawHLine(chartControl, chartScale, leftT, vaEnd, vah, vaBrush, 1f, dash);
					DrawHLine(chartControl, chartScale, leftT, vaEnd, val, vaBrush, 1f, dash);
				}
			}
		}

		private void DrawHLine(ChartControl chartControl, ChartScale chartScale, DateTime t1, DateTime t2, double price,
			SharpDX.Direct2D1.SolidColorBrush brush, float width, StrokeStyle style)
		{
			float x1 = chartControl.GetXByTime(t1);
			float x2 = chartControl.GetXByTime(t2);
			float y = chartScale.GetYByValue(price);
			if (style == null)
				RenderTarget.DrawLine(new Vector2(x1, y), new Vector2(x2, y), brush, width);
			else
				RenderTarget.DrawLine(new Vector2(x1, y), new Vector2(x2, y), brush, width, style);
		}
		#endregion

		#region Color helpers
		private static System.Windows.Media.Color ToMedia(System.Windows.Media.Brush b)
		{
			System.Windows.Media.SolidColorBrush scb = b as System.Windows.Media.SolidColorBrush;
			if (scb != null)
				return scb.Color;
			return Colors.Gray;
		}

		private static SharpDX.Color ToDx(System.Windows.Media.Color c, byte alpha)
		{
			return new SharpDX.Color(c.R, c.G, c.B, alpha);
		}

		private static System.Windows.Media.Color Lerp(System.Windows.Media.Color a, System.Windows.Media.Color b, double t)
		{
			t = Clamp01(t);
			return System.Windows.Media.Color.FromRgb(
				(byte)Math.Round(a.R + (b.R - a.R) * t),
				(byte)Math.Round(a.G + (b.G - a.G) * t),
				(byte)Math.Round(a.B + (b.B - a.B) * t));
		}

		private static double Clamp01(double v)
		{
			if (v < 0) return 0;
			if (v > 1) return 1;
			return v;
		}
		#endregion

		public override string DisplayName
		{
			get { return string.Format("TPO Profile ({0}m, {1})", BlockMinutes, FilterByTime ? SessionTime : "all"); }
		}

		#region Properties
		[NinjaScriptProperty]
		[Display(Name = "Filter by Time? (RTH Only)", GroupName = "Session", Order = 1)]
		public bool FilterByTime { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Session Time", Description = "HHmm-HHmm in chart time, e.g. 0830-1500", GroupName = "Session", Order = 2)]
		public string SessionTime { get; set; }

		[NinjaScriptProperty]
		[Range(5, 240)]
		[Display(Name = "Block Size (Minutes)", GroupName = "Session", Order = 3)]
		public int BlockMinutes { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Auto Row Height", GroupName = "Session", Order = 4)]
		public bool AutoRowHeight { get; set; }

		[NinjaScriptProperty]
		[Range(15, 100)]
		[Display(Name = "Target Rows Per Day (Auto)", GroupName = "Session", Order = 5)]
		public int TargetRows { get; set; }

		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Row Height (Ticks, Manual)", GroupName = "Session", Order = 6)]
		public int RowHeightTicks { get; set; }

		[NinjaScriptProperty]
		[Range(1, 5)]
		[Display(Name = "Days with Letter Detail", GroupName = "History", Order = 1)]
		public int LetterDays { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Polyline Outlines (Older Days)", GroupName = "History", Order = 2)]
		public bool ShowPolyline { get; set; }

		[NinjaScriptProperty]
		[Range(1, 50)]
		[Display(Name = "Max Polyline Days", GroupName = "History", Order = 3)]
		public int PolylineDays { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Box Transparency (0-100)", GroupName = "Visuals", Order = 1)]
		public int BoxTransparency { get; set; }

		[XmlIgnore]
		[Display(Name = "Start Color (Open)", GroupName = "Visuals", Order = 2)]
		public System.Windows.Media.Brush StartColor { get; set; }

		[Browsable(false)]
		public string StartColorSerializable
		{
			get { return Serialize.BrushToString(StartColor); }
			set { StartColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "End Color (Close)", GroupName = "Visuals", Order = 3)]
		public System.Windows.Media.Brush EndColor { get; set; }

		[Browsable(false)]
		public string EndColorSerializable
		{
			get { return Serialize.BrushToString(EndColor); }
			set { EndColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Text Color", GroupName = "Visuals", Order = 4)]
		public System.Windows.Media.Brush TextColor { get; set; }

		[Browsable(false)]
		public string TextColorSerializable
		{
			get { return Serialize.BrushToString(TextColor); }
			set { TextColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "POC Color", GroupName = "Visuals", Order = 5)]
		public System.Windows.Media.Brush PocColor { get; set; }

		[Browsable(false)]
		public string PocColorSerializable
		{
			get { return Serialize.BrushToString(PocColor); }
			set { PocColor = Serialize.StringToBrush(value); }
		}

		[XmlIgnore]
		[Display(Name = "Polyline Fill Color", GroupName = "Visuals", Order = 6)]
		public System.Windows.Media.Brush PolyColor { get; set; }

		[Browsable(false)]
		public string PolyColorSerializable
		{
			get { return Serialize.BrushToString(PolyColor); }
			set { PolyColor = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[Display(Name = "X Offset (Bars)", GroupName = "Visuals", Order = 7)]
		public int XOffsetBars { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Developing (Current Day)", GroupName = "Visuals", Order = 8)]
		public bool ShowLive { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Value Area (VAH/VAL)", GroupName = "Visuals", Order = 9)]
		public bool ShowValueArea { get; set; }

		[XmlIgnore]
		[Display(Name = "Value Area Color", GroupName = "Visuals", Order = 10)]
		public System.Windows.Media.Brush VaColor { get; set; }

		[Browsable(false)]
		public string VaColorSerializable
		{
			get { return Serialize.BrushToString(VaColor); }
			set { VaColor = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty]
		[Display(Name = "Extend POC Through Next RTH Session", GroupName = "Visuals", Order = 11)]
		public bool ExtendPoc { get; set; }
		#endregion

		private class TpoRow
		{
			public double Price;
			public List<int> Letters = new List<int>();
		}

		private class SavedProfile
		{
			public DateTime TimeStart;
			public DateTime TimeEnd;
			public double PocPrice;
			public int MaxCount;
			public double TickStep;
			public List<TpoRow> Rows = new List<TpoRow>();
		}
	}
}


#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private TatankaTpoProfile[] cacheTatankaTpoProfile;
		public TatankaTpoProfile TatankaTpoProfile(bool filterByTime, string sessionTime, int blockMinutes, bool autoRowHeight, int targetRows, int rowHeightTicks, int letterDays, bool showPolyline, int polylineDays, int boxTransparency, int xOffsetBars, bool showLive, bool showValueArea, bool extendPoc)
		{
			return TatankaTpoProfile(Input, filterByTime, sessionTime, blockMinutes, autoRowHeight, targetRows, rowHeightTicks, letterDays, showPolyline, polylineDays, boxTransparency, xOffsetBars, showLive, showValueArea, extendPoc);
		}

		public TatankaTpoProfile TatankaTpoProfile(ISeries<double> input, bool filterByTime, string sessionTime, int blockMinutes, bool autoRowHeight, int targetRows, int rowHeightTicks, int letterDays, bool showPolyline, int polylineDays, int boxTransparency, int xOffsetBars, bool showLive, bool showValueArea, bool extendPoc)
		{
			if (cacheTatankaTpoProfile != null)
				for (int idx = 0; idx < cacheTatankaTpoProfile.Length; idx++)
					if (cacheTatankaTpoProfile[idx] != null && cacheTatankaTpoProfile[idx].FilterByTime == filterByTime && cacheTatankaTpoProfile[idx].SessionTime == sessionTime && cacheTatankaTpoProfile[idx].BlockMinutes == blockMinutes && cacheTatankaTpoProfile[idx].AutoRowHeight == autoRowHeight && cacheTatankaTpoProfile[idx].TargetRows == targetRows && cacheTatankaTpoProfile[idx].RowHeightTicks == rowHeightTicks && cacheTatankaTpoProfile[idx].LetterDays == letterDays && cacheTatankaTpoProfile[idx].ShowPolyline == showPolyline && cacheTatankaTpoProfile[idx].PolylineDays == polylineDays && cacheTatankaTpoProfile[idx].BoxTransparency == boxTransparency && cacheTatankaTpoProfile[idx].XOffsetBars == xOffsetBars && cacheTatankaTpoProfile[idx].ShowLive == showLive && cacheTatankaTpoProfile[idx].ShowValueArea == showValueArea && cacheTatankaTpoProfile[idx].ExtendPoc == extendPoc && cacheTatankaTpoProfile[idx].EqualsInput(input))
						return cacheTatankaTpoProfile[idx];
			return CacheIndicator<TatankaTpoProfile>(new TatankaTpoProfile(){ FilterByTime = filterByTime, SessionTime = sessionTime, BlockMinutes = blockMinutes, AutoRowHeight = autoRowHeight, TargetRows = targetRows, RowHeightTicks = rowHeightTicks, LetterDays = letterDays, ShowPolyline = showPolyline, PolylineDays = polylineDays, BoxTransparency = boxTransparency, XOffsetBars = xOffsetBars, ShowLive = showLive, ShowValueArea = showValueArea, ExtendPoc = extendPoc }, input, ref cacheTatankaTpoProfile);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.TatankaTpoProfile TatankaTpoProfile(bool filterByTime, string sessionTime, int blockMinutes, bool autoRowHeight, int targetRows, int rowHeightTicks, int letterDays, bool showPolyline, int polylineDays, int boxTransparency, int xOffsetBars, bool showLive, bool showValueArea, bool extendPoc)
		{
			return indicator.TatankaTpoProfile(Input, filterByTime, sessionTime, blockMinutes, autoRowHeight, targetRows, rowHeightTicks, letterDays, showPolyline, polylineDays, boxTransparency, xOffsetBars, showLive, showValueArea, extendPoc);
		}

		public Indicators.TatankaTpoProfile TatankaTpoProfile(ISeries<double> input , bool filterByTime, string sessionTime, int blockMinutes, bool autoRowHeight, int targetRows, int rowHeightTicks, int letterDays, bool showPolyline, int polylineDays, int boxTransparency, int xOffsetBars, bool showLive, bool showValueArea, bool extendPoc)
		{
			return indicator.TatankaTpoProfile(input, filterByTime, sessionTime, blockMinutes, autoRowHeight, targetRows, rowHeightTicks, letterDays, showPolyline, polylineDays, boxTransparency, xOffsetBars, showLive, showValueArea, extendPoc);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.TatankaTpoProfile TatankaTpoProfile(bool filterByTime, string sessionTime, int blockMinutes, bool autoRowHeight, int targetRows, int rowHeightTicks, int letterDays, bool showPolyline, int polylineDays, int boxTransparency, int xOffsetBars, bool showLive, bool showValueArea, bool extendPoc)
		{
			return indicator.TatankaTpoProfile(Input, filterByTime, sessionTime, blockMinutes, autoRowHeight, targetRows, rowHeightTicks, letterDays, showPolyline, polylineDays, boxTransparency, xOffsetBars, showLive, showValueArea, extendPoc);
		}

		public Indicators.TatankaTpoProfile TatankaTpoProfile(ISeries<double> input , bool filterByTime, string sessionTime, int blockMinutes, bool autoRowHeight, int targetRows, int rowHeightTicks, int letterDays, bool showPolyline, int polylineDays, int boxTransparency, int xOffsetBars, bool showLive, bool showValueArea, bool extendPoc)
		{
			return indicator.TatankaTpoProfile(input, filterByTime, sessionTime, blockMinutes, autoRowHeight, targetRows, rowHeightTicks, letterDays, showPolyline, polylineDays, boxTransparency, xOffsetBars, showLive, showValueArea, extendPoc);
		}
	}
}

#endregion
