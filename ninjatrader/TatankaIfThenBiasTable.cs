// Copyright (c) 2026 Tatanka Trading LLC.
// This Source Code Form is subject to the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, obtain one at https://mozilla.org/MPL/2.0/.
// Tatanka If/Then Bias Table v1.0.0 for NinjaTrader 8 - tatankatrading.com
// NinjaTrader 8 port of the supplied Pine v6 "If/Then Bias Table".
// Import the complete file. NinjaTrader generates its own indicator accessors.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using WpfBrush = System.Windows.Media.Brush;
using WpfSolidBrush = System.Windows.Media.SolidColorBrush;
using WpfColor = System.Windows.Media.Color;

namespace NinjaTrader.NinjaScript.Indicators
{
    public class TatankaIfThenBiasTable : Indicator
    {
        private TatankaBiasRow[] rows = new TatankaBiasRow[0];
        private TatankaBiasDailyAtr dailyAtr;
        private TatankaBiasStyle style;
        private volatile TatankaBiasFrame frame;
        private double lastPrice = double.NaN;
        private int lastPrimaryBar = -1;

        public override string DisplayName { get { return Name; } }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "If/Then Bias Table";
                Description = "Tatanka Trading If/Then Bias Table — Pine v6 port.";
                Calculate = Calculate.OnEachTick;
                IsOverlay = true;
                DrawOnPricePanel = true;
                DisplayInDataBox = false;
                PaintPriceMarkers = false;
                IsAutoScale = false;
                IsChartOnly = true;
                IsSuspendedWhileInactive = false;
                BarsRequiredToPlot = 0;
                InstrumentLabel = "ES";
                PriceSource = "Close";
                TablePosition = "Middle Right";
                TextSize = "Normal";
                ShowLines = true;
                LineWidth = 2;
                LineStyle = "Solid";
                LineColorMode = "Bias";
                AtrLength = 14;
                AtrPercent = 10;
                ShowZones = false;
                ZonePercent = 2;
                ZoneOpacity = 75;
                AboveBrush = MakeBrush(242, 54, 69);
                BelowBrush = MakeBrush(76, 175, 80);
                WithinBrush = MakeBrush(255, 235, 59);
                LineLabels = true;
                LabelContent = "Price + Text";
                WrapCharacters = 50;
                LabelOffset = 75;
                LabelBackground = true;
                ShowTable = false;
                BackgroundBrush = MakeBrush(6, 6, 6);
                ActiveRowOpacity = 25;
                BorderBrush = MakeBrush(6, 6, 6);
                RowsBlock = TatankaBiasLogic.DefaultBlock;
            }
            else if (State == State.Configure)
            {
                // Fixed arguments comply with NT's multi-series configuration rules.
                // Separate daily history also works on a chart loading only a few days.
                AddDataSeries(null, new BarsPeriod { BarsPeriodType = BarsPeriodType.Day,
                    Value = 1 }, 2048, null, null);
            }
            else if (State == State.DataLoaded)
            {
                rows = TatankaBiasLogic.Parse(RowsBlock);
                dailyAtr = new TatankaBiasDailyAtr(AtrLength);
                style = new TatankaBiasStyle(this);
                lastPrice = double.NaN;
                lastPrimaryBar = -1;
                frame = null;
            }
            else if (State == State.Terminated)
                frame = null;
        }

        protected override void OnBarUpdate()
        {
            if (dailyAtr == null)
                return;
            if (BarsInProgress == 1)
            {
                dailyAtr.Update(CurrentBars[1], Highs[1][0], Lows[1][0], Closes[1][0]);
                // Historical primary bars run before a secondary bar with the same
                // timestamp. Publish again here so the final daily close is included.
            }
            else if (BarsInProgress == 0)
            {
                lastPrimaryBar = CurrentBars[0];
                lastPrice = ReadPrice();
            }
            else
                return;

            if (lastPrimaryBar >= 0)
            {
                // Immutable snapshot: no barsAgo reads or mutable arrays in OnRender.
                frame = new TatankaBiasFrame(rows, lastPrice, dailyAtr.Previous,
                    TatankaBiasLogic.ActiveRow(rows, lastPrice), lastPrimaryBar);
            }
        }

        private double ReadPrice()
        {
            switch (PriceSource)
            {
                case "Open": return Open[0];
                case "High": return High[0];
                case "Low": return Low[0];
                case "HL2": return (High[0] + Low[0]) / 2.0;
                case "HLC3": return (High[0] + Low[0] + Close[0]) / 3.0;
                case "OHLC4": return (Open[0] + High[0] + Low[0] + Close[0]) / 4.0;
                case "HLCC4": return (High[0] + Low[0] + Close[0] + Close[0]) / 4.0;
                case "Input series": return Inputs[0][0];
                default: return Close[0];
            }
        }

        protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
            TatankaBiasFrame snapshot = frame;
            if (RenderTarget == null || ChartBars == null || ChartPanel == null
                || snapshot == null || style == null || IsInHitTest)
                return;

            float anchor = 0;
            if (style.LineLabels)
            {
                // Extrapolate from the visible bar spacing. Do not clamp the label
                // to the viewport: Pine permits future labels to be off-screen.
                int index = Math.Max(0, Math.Min(ChartBars.ToIndex, snapshot.BarIndex));
                float x = chartControl.GetXByBarIndex(ChartBars, index);
                float step = index > 0
                    ? x - chartControl.GetXByBarIndex(ChartBars, index - 1)
                    : (float)chartControl.Properties.BarDistance;
                anchor = x + (snapshot.BarIndex - index + LabelOffset) * Math.Max(1, step);
            }
            TatankaBiasPainter.Draw(RenderTarget, Core.Globals.DirectWriteFactory,
                new RectangleF(ChartPanel.X, ChartPanel.Y, ChartPanel.W, ChartPanel.H),
                snapshot, style, anchor, delegate(double price) { return chartScale.GetYByValue(price); });
        }

        private static WpfBrush MakeBrush(byte r, byte g, byte b)
        {
            WpfSolidBrush brush = new WpfSolidBrush(WpfColor.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }

        // SETTINGS
        [NinjaScriptProperty]
        [Display(Name = "Instrument label", GroupName = "1. General", Order = 0, Description = "Header instrument label; independent of the chart instrument.")]
        public string InstrumentLabel { get; set; }

        [NinjaScriptProperty]
        [TypeConverter(typeof(TatankaBiasOptionsConverter))]
        [PropertyEditor("NinjaTrader.Gui.Tools.StringStandardValuesEditorKey")]
        [Display(Name = "Price source", GroupName = "1. General", Order = 1, Description = "Close updates live each tick. Input series uses the Data series > Input series selector, including another indicator.")]
        public string PriceSource { get; set; }

        [NinjaScriptProperty]
        [TypeConverter(typeof(TatankaBiasOptionsConverter))]
        [PropertyEditor("NinjaTrader.Gui.Tools.StringStandardValuesEditorKey")]
        [Display(Name = "Table position", GroupName = "1. General", Order = 2, Description = "Anchor the table to one of six chart-panel positions.")]
        public string TablePosition { get; set; }

        [NinjaScriptProperty]
        [TypeConverter(typeof(TatankaBiasOptionsConverter))]
        [PropertyEditor("NinjaTrader.Gui.Tools.StringStandardValuesEditorKey")]
        [Display(Name = "Text size", GroupName = "1. General", Order = 3, Description = "Tiny / Small / Normal / Large. Font metrics depend on the platform.")]
        public string TextSize { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Plot boundary lines on chart", GroupName = "1. General", Order = 4, Description = "Draw a horizontal line at each positive Lo.")]
        public bool ShowLines { get; set; }

        [NinjaScriptProperty]
        [Range(1, 5)]
        [Display(Name = "Boundary line width", GroupName = "1. General", Order = 5, Description = "Thickness of the boundary lines.")]
        public int LineWidth { get; set; }

        [NinjaScriptProperty]
        [TypeConverter(typeof(TatankaBiasOptionsConverter))]
        [PropertyEditor("NinjaTrader.Gui.Tools.StringStandardValuesEditorKey")]
        [Display(Name = "Boundary line style", GroupName = "1. General", Order = 6, Description = "Solid, Dashed or Dotted.")]
        public string LineStyle { get; set; }

        [NinjaScriptProperty]
        [TypeConverter(typeof(TatankaBiasOptionsConverter))]
        [PropertyEditor("NinjaTrader.Gui.Tools.StringStandardValuesEditorKey")]
        [Display(Name = "Line/label color", GroupName = "1. General", Order = 7, Description = "Position: above price = resistance; below = support; within the ATR band = neutral. Pivot remains yellow.")]
        public string LineColorMode { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "ATR length (Position band, daily)", GroupName = "1. General", Order = 8, Description = "Daily Wilder ATR length. Uses the previous daily ATR, irrespective of chart timeframe.")]
        public int AtrLength { get; set; }

        [NinjaScriptProperty]
        [Range(0.0, double.MaxValue)]
        [Display(Name = "ATR band threshold (%)", GroupName = "1. General", Order = 9, Description = "Distance from price as a percentage of daily ATR that remains neutral.")]
        public double AtrPercent { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Plot ATR zones around lines", GroupName = "1. General", Order = 10, Description = "Band on each side of a positive Lo, sized by daily ATR.")]
        public bool ShowZones { get; set; }

        [NinjaScriptProperty]
        [Range(0.0, double.MaxValue)]
        [Display(Name = "Zone ATR % (each side)", GroupName = "1. General", Order = 11, Description = "Zone half-width = daily ATR times this percentage.")]
        public double ZonePercent { get; set; }

        [NinjaScriptProperty]
        [Range(0, 100)]
        [Display(Name = "Zone fill opacity", GroupName = "1. General", Order = 12, Description = "Source semantics: TRANSPARENCY. 100 = invisible; 0 = solid. Inactive rows add 8.")]
        public int ZoneOpacity { get; set; }

        [XmlIgnore]
        [Display(Name = "Position: above price (resistance)", GroupName = "1. General", Order = 13, Description = "Position color above price.")]
        public WpfBrush AboveBrush { get; set; }
        [Browsable(false)]
        public string AboveBrushSerializable
        {
            get { return Serialize.BrushToString(AboveBrush); }
            set { AboveBrush = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "Position: below price (support)", GroupName = "1. General", Order = 14, Description = "Position color below price.")]
        public WpfBrush BelowBrush { get; set; }
        [Browsable(false)]
        public string BelowBrushSerializable
        {
            get { return Serialize.BrushToString(BelowBrush); }
            set { BelowBrush = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "Position: within ATR band (neutral)", GroupName = "1. General", Order = 15, Description = "Position color inside the neutral band, including its edges.")]
        public WpfBrush WithinBrush { get; set; }
        [Browsable(false)]
        public string WithinBrushSerializable
        {
            get { return Serialize.BrushToString(WithinBrush); }
            set { WithinBrush = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [Display(Name = "Label boundary lines", GroupName = "1. General", Order = 16, Description = "Print right-aligned text above each positive Lo.")]
        public bool LineLabels { get; set; }

        [NinjaScriptProperty]
        [TypeConverter(typeof(TatankaBiasOptionsConverter))]
        [PropertyEditor("NinjaTrader.Gui.Tools.StringStandardValuesEditorKey")]
        [Display(Name = "Line label content", GroupName = "1. General", Order = 17, Description = "Text, Price or Price + Text.")]
        public string LabelContent { get; set; }

        [NinjaScriptProperty]
        [Range(0, 120)]
        [Display(Name = "Line label wrap width (chars, 0 = off)", GroupName = "1. General", Order = 18, Description = "Wrap at word boundaries; preserve literal backslash-n manual breaks. Long words remain whole.")]
        public int WrapCharacters { get; set; }

        [NinjaScriptProperty]
        [Range(0, 500)]
        [Display(Name = "Line label offset (bars right of last candle)", GroupName = "1. General", Order = 19, Description = "Future bar offset, independent of horizontal scrolling. Increase chart right-side margin to see future labels.")]
        public int LabelOffset { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Label background", GroupName = "1. General", Order = 20, Description = "Use table background color behind line labels.")]
        public bool LabelBackground { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show table", GroupName = "1. General", Order = 21, Description = "Show or hide the table independently of lines, labels and zones.")]
        public bool ShowTable { get; set; }

        [XmlIgnore]
        [Display(Name = "Background color", GroupName = "2. Table style", Order = 22, Description = "Base table cell color and optional label plate color. Alpha is honored.")]
        public WpfBrush BackgroundBrush { get; set; }
        [Browsable(false)]
        public string BackgroundBrushSerializable
        {
            get { return Serialize.BrushToString(BackgroundBrush); }
            set { BackgroundBrush = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [Range(0, 100)]
        [Display(Name = "Active-row opacity", GroupName = "2. Table style", Order = 23, Description = "Active row fill strength: 0 = transparent, 100 = solid.")]
        public int ActiveRowOpacity { get; set; }

        [XmlIgnore]
        [Display(Name = "Border color", GroupName = "2. Table style", Order = 24, Description = "One-pixel table cell borders and frame.")]
        public WpfBrush BorderBrush { get; set; }
        [Browsable(false)]
        public string BorderBrushSerializable
        {
            get { return Serialize.BrushToString(BorderBrush); }
            set { BorderBrush = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [PropertyEditor("NinjaTrader.Gui.Tools.MultilineEditor")]
        [Display(Name = "Rows  (BIAS|LO|HI|TEXT)", GroupName = "3. If/Then block", Order = 25, Description = "One row per line. U/V/N/D/P. 9999999 = open-top display, 0 = open-bottom display. Literal backslash-n creates text breaks; # at column 1 is a comment.")]
        public string RowsBlock { get; set; }
    }

    // String drop-downs retain the original option names, including their spaces.
    public class TatankaBiasOptionsConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) { return true; }
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) { return true; }
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            string name = context == null || context.PropertyDescriptor == null
                ? "" : context.PropertyDescriptor.Name;
            switch (name)
            {
                case "PriceSource": return new StandardValuesCollection(new[] { "Close", "Open", "High", "Low", "HL2", "HLC3", "OHLC4", "HLCC4", "Input series" });
                case "TablePosition": return new StandardValuesCollection(new[] { "Top Right", "Middle Right", "Bottom Right", "Top Left", "Middle Left", "Bottom Left" });
                case "TextSize": return new StandardValuesCollection(new[] { "Tiny", "Small", "Normal", "Large" });
                case "LineStyle": return new StandardValuesCollection(new[] { "Solid", "Dashed", "Dotted" });
                case "LineColorMode": return new StandardValuesCollection(new[] { "Bias", "Position" });
                case "LabelContent": return new StandardValuesCollection(new[] { "Text", "Price", "Price + Text" });
                default: return new StandardValuesCollection(new string[0]);
            }
        }
    }

    internal sealed class TatankaBiasRow
    {
        internal readonly string Bias, Text;
        internal readonly double Low, High;
        internal TatankaBiasRow(string bias, double low, double high, string text)
        { Bias = bias; Low = low; High = high; Text = text; }
    }

    internal static class TatankaBiasLogic
    {
        internal const string DefaultBlock =
            "U|5050|9999999|Accepts above 5050 → continuation, next target 5085\n" +
            "V|5005|5050|Above pivot, holding value → grind higher\n" +
            "P|4995|5005|Pivot ~5000 — the day's reference\n" +
            "D|4960|4995|Below pivot, no reclaim → 4960 support in play\n" +
            "D|0|4960|Below 4960 → range break, downside opens up";

        internal static TatankaBiasRow[] Parse(string block)
        {
            List<TatankaBiasRow> result = new List<TatankaBiasRow>();
            // Normalize Windows line endings; do not trim bias/text or sort levels.
            foreach (string line in (block ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
            {
                if (line.Length == 0 || line[0] == '#') continue;
                string[] parts = line.Split('|');
                double lo, hi;
                if (parts.Length < 4 || !Number(parts[1], out lo) || !Number(parts[2], out hi)) continue;
                string raw = parts[0].ToUpperInvariant();
                string bias = raw.Length > 0 ? raw.Substring(0, 1) : "N";
                string text = string.Join("|", parts, 3, parts.Length - 3).Replace("\\n", "\n");
                result.Add(new TatankaBiasRow(bias, lo, hi, text));
            }
            return result.ToArray();
        }

        private static bool Number(string text, out double value)
        {
            return double.TryParse(text.Replace(" ", ""), NumberStyles.Float,
                CultureInfo.InvariantCulture, out value) && Finite(value);
        }

        internal static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }

        internal static int ActiveRow(TatankaBiasRow[] rows, double price)
        {
            int active = -1;
            double best = 1e18;
            for (int i = 0; i < rows.Length; i++)
            {
                double distance = price > rows[i].High ? price - rows[i].High
                    : price < rows[i].Low ? rows[i].Low - price : 0;
                if (distance < best) { best = distance; active = i; }
            }
            return active;
        }

        internal static string Format(double value)
        {
            if (!Finite(value)) return "NaN";
            // Pine's #.##: at most two decimals; prices are not rounded to tick size.
            return Math.Round(value, 2, MidpointRounding.AwayFromZero).ToString("0.##", CultureInfo.InvariantCulture);
        }

        internal static string Range(TatankaBiasRow row)
        {
            return row.High > 1000000 ? Format(row.Low) + "+"
                : row.Low <= 0 ? "<" + Format(row.High)
                : Format(row.Low) + "–" + Format(row.High);
        }

        internal static string Wrap(string text, int maxChars)
        {
            // Intentional translation of the source's empty-line/space behavior.
            string output = "";
            foreach (string segment in text.Replace("\\n", "\n").Split('\n'))
            {
                string segmentOutput = "";
                if (maxChars <= 0) segmentOutput = segment;
                else
                {
                    string current = "";
                    foreach (string word in segment.Split(' '))
                    {
                        if (current.Length == 0) current = word;
                        else if (current.Length + word.Length + 1 <= maxChars) current += " " + word;
                        else
                        {
                            segmentOutput += (segmentOutput.Length > 0 ? "\n" : "") + current;
                            current = word;
                        }
                    }
                    segmentOutput += (segmentOutput.Length > 0 ? "\n" : "") + current;
                }
                output += (output.Length > 0 ? "\n" : "") + segmentOutput;
            }
            return output;
        }

        internal static Color4 BiasColor(string bias)
        {
            switch (bias)
            {
                case "U": return Rgb(99, 153, 34);
                case "V": return Rgb(186, 117, 23);
                case "D": return Rgb(168, 68, 64);
                case "P": return Rgb(235, 208, 52);
                default: return Rgb(136, 135, 128);
            }
        }

        internal static Color4 Rgb(int r, int g, int b) { return new Color4(r / 255f, g / 255f, b / 255f, 1); }
        internal static Color4 Alpha(Color4 c, double opacity) { return new Color4(c.Red, c.Green, c.Blue, (float)opacity); }

        internal static Color4 LevelColor(TatankaBiasRow row, double price, double atr, TatankaBiasStyle s)
        {
            if (row.Bias == "P" || s.LineColorMode == "Bias") return BiasColor(row.Bias);
            double buffer = atr * s.AtrPercent / 100.0;
            // NaN comparisons are false, reproducing Pine's neutral fallback.
            return row.Low > price + buffer ? s.Above : row.Low < price - buffer ? s.Below : s.Within;
        }
    }

    internal sealed class TatankaBiasDailyAtr
    {
        private readonly int length;
        private readonly List<double> closes = new List<double>();
        private readonly List<double> sums = new List<double>();
        private readonly List<double> values = new List<double>();
        private int current = -1;
        internal TatankaBiasDailyAtr(int length) { this.length = Math.Max(1, length); }
        internal double Previous { get { return current > 0 ? values[current - 1] : double.NaN; } }
        internal double Current { get { return current >= 0 ? values[current] : double.NaN; } }
        internal void Update(int index, double high, double low, double close)
        {
            if (index < 0) return;
            if (index > values.Count) throw new InvalidOperationException("Nonsequential daily ATR data.");
            if (index == values.Count) { values.Add(double.NaN); closes.Add(0); sums.Add(0); }
            double tr = index == 0 ? high - low
                : Math.Max(high - low, Math.Max(Math.Abs(high - closes[index - 1]), Math.Abs(low - closes[index - 1])));
            sums[index] = (index == 0 ? 0 : sums[index - 1]) + tr;
            // Pine ta.atr = RMA(TR): seed with an SMA only after length observations.
            // Recompute the developing day from the previous day on EVERY tick.
            values[index] = index < length - 1 ? double.NaN
                : index == length - 1 ? sums[index] / length
                : (1.0 / length) * tr + (1.0 - 1.0 / length) * values[index - 1];
            closes[index] = close;
            current = index;
        }
    }

    internal sealed class TatankaBiasFrame
    {
        internal readonly TatankaBiasRow[] Rows;
        internal readonly double Price, Atr;
        internal readonly int Active, BarIndex;
        internal TatankaBiasFrame(TatankaBiasRow[] rows, double price, double atr, int active, int barIndex)
        { Rows = rows; Price = price; Atr = atr; Active = active; BarIndex = barIndex; }
    }

    internal sealed class TatankaBiasStyle
    {
        internal string InstrumentLabel, Position, Size, LineStyle, LineColorMode, LabelContent;
        internal bool ShowLines, ShowZones, LineLabels, LabelBackground, ShowTable;
        internal int LineWidth, ZoneOpacity, WrapCharacters, ActiveRowOpacity;
        internal double AtrPercent, ZonePercent;
        internal Color4 Above, Below, Within, Background, Border;
        internal TatankaBiasStyle(TatankaIfThenBiasTable p)
        {
            InstrumentLabel = p.InstrumentLabel; Position = p.TablePosition; Size = p.TextSize;
            LineStyle = p.LineStyle; LineColorMode = p.LineColorMode; LabelContent = p.LabelContent;
            ShowLines = p.ShowLines; ShowZones = p.ShowZones; LineLabels = p.LineLabels;
            LabelBackground = p.LabelBackground; ShowTable = p.ShowTable; LineWidth = p.LineWidth;
            ZoneOpacity = p.ZoneOpacity; WrapCharacters = p.WrapCharacters; ActiveRowOpacity = p.ActiveRowOpacity;
            AtrPercent = p.AtrPercent; ZonePercent = p.ZonePercent;
            Above = Color(p.AboveBrush); Below = Color(p.BelowBrush); Within = Color(p.WithinBrush);
            Background = Color(p.BackgroundBrush); Border = Color(p.BorderBrush);
        }
        private static Color4 Color(WpfBrush brush)
        {
            WpfSolidBrush solid = brush as WpfSolidBrush;
            if (solid == null) return new Color4(0, 0, 0, 1);
            WpfColor c = solid.Color;
            return new Color4(c.R / 255f, c.G / 255f, c.B / 255f, (float)(c.A / 255.0 * solid.Opacity));
        }
    }

    // No persistent DirectX resources: each render owns/disposes its own brushes,
    // formats, layouts and stroke. Safe across chart resize/render-target changes.
    internal sealed class TatankaBiasPainter : IDisposable
    {
        private readonly RenderTarget target;
        private readonly SharpDX.DirectWrite.Factory factory;
        private readonly TextFormat format;
        private readonly Dictionary<Color4, SolidColorBrush> brushes = new Dictionary<Color4, SolidColorBrush>();
        private readonly List<TextLayout> layouts = new List<TextLayout>();
        private TatankaBiasPainter(RenderTarget target, SharpDX.DirectWrite.Factory factory, string size)
        {
            this.target = target; this.factory = factory;
            float fontSize = size == "Tiny" ? 10 : size == "Small" ? 12 : size == "Large" ? 20 : 14;
            format = new TextFormat(factory, "Arial", fontSize) { WordWrapping = WordWrapping.NoWrap };
        }

        internal static void Draw(RenderTarget target, SharpDX.DirectWrite.Factory factory,
            RectangleF panel, TatankaBiasFrame frame, TatankaBiasStyle style, float labelX, Func<double, float> y)
        {
            using (TatankaBiasPainter painter = new TatankaBiasPainter(target, factory, style.Size))
            {
                target.PushAxisAlignedClip(panel, AntialiasMode.PerPrimitive);
                try
                {
                    if (style.ShowLines || style.LineLabels || style.ShowZones)
                        painter.Levels(panel, frame, style, labelX, y);
                    if (style.ShowTable) painter.Table(panel, frame, style);
                }
                finally { target.PopAxisAlignedClip(); }
            }
        }

        private SolidColorBrush Brush(Color4 c)
        {
            SolidColorBrush brush;
            if (!brushes.TryGetValue(c, out brush)) { brush = new SolidColorBrush(target, c); brushes.Add(c, brush); }
            return brush;
        }

        private TextLayout Layout(string text)
        {
            TextLayout layout = new TextLayout(factory, text ?? "", format, 1000000, 1000000);
            layouts.Add(layout);
            return layout;
        }

        private void Levels(RectangleF panel, TatankaBiasFrame f, TatankaBiasStyle s, float labelX, Func<double, float> y)
        {
            // Pine garbage-collects the oldest shapes beyond its max_*_count = 100.
            int skip = Math.Max(0, f.Rows.Count(row => row.Low > 0) - 100);
            List<int> levels = new List<int>();
            for (int i = 0; i < f.Rows.Length; i++) if (f.Rows[i].Low > 0)
            { if (skip > 0) skip--; else levels.Add(i); }
            double band = TatankaBiasLogic.Finite(f.Atr) ? f.Atr * s.ZonePercent / 100.0 : 0;
            if (s.ShowZones && band > 0)
            {
                foreach (int i in levels)
                {
                    float a = y(f.Rows[i].Low + band), b = y(f.Rows[i].Low - band);
                    float top = Math.Max(panel.Top, Math.Min(a, b)), bottom = Math.Min(panel.Bottom, Math.Max(a, b));
                    if (bottom <= top) continue;
                    int transparency = i == f.Active ? s.ZoneOpacity : Math.Min(100, s.ZoneOpacity + 8);
                    Color4 color = TatankaBiasLogic.LevelColor(f.Rows[i], f.Price, f.Atr, s);
                    target.FillRectangle(new RectangleF(panel.Left, top, panel.Width, bottom - top),
                        Brush(TatankaBiasLogic.Alpha(color, (100 - transparency) / 100.0)));
                }
            }
            if (s.ShowLines)
            {
                StrokeStyleProperties properties = new StrokeStyleProperties
                {
                    DashStyle = s.LineStyle == "Solid" ? DashStyle.Solid : s.LineStyle == "Dotted" ? DashStyle.Dot : DashStyle.Dash,
                    DashCap = s.LineStyle == "Dotted" ? CapStyle.Round : CapStyle.Flat
                };
                using (StrokeStyle stroke = new StrokeStyle(target.Factory, properties))
                {
                    foreach (int i in levels)
                    {
                        float lineY = y(f.Rows[i].Low);
                        if (lineY < panel.Top || lineY > panel.Bottom) continue;
                        Color4 color = TatankaBiasLogic.LevelColor(f.Rows[i], f.Price, f.Atr, s);
                        target.DrawLine(new Vector2(panel.Left, lineY), new Vector2(panel.Right, lineY),
                            Brush(TatankaBiasLogic.Alpha(color, i == f.Active ? 1 : .55)), s.LineWidth, stroke);
                    }
                }
            }
            if (s.LineLabels)
            {
                foreach (int i in levels)
                {
                    TatankaBiasRow row = f.Rows[i];
                    string text = s.LabelContent == "Price" ? TatankaBiasLogic.Format(row.Low)
                        : (s.LabelContent == "Price + Text" ? TatankaBiasLogic.Format(row.Low) + " - " : "")
                            + TatankaBiasLogic.Wrap(row.Text, s.WrapCharacters);
                    TextLayout layout = Layout(text);
                    float width = Math.Max(1, layout.Metrics.WidthIncludingTrailingWhitespace);
                    float height = layout.Metrics.Height;
                    layout.MaxWidth = width + 1;
                    layout.TextAlignment = TextAlignment.Trailing;
                    float top = y(row.Low) - height - 4;
                    RectangleF plate = new RectangleF(labelX - width - 8, top - 2, width + 8, height + 4);
                    if (plate.Right < panel.Left || plate.Left > panel.Right || plate.Bottom < panel.Top || plate.Top > panel.Bottom) continue;
                    if (s.LabelBackground) target.FillRectangle(plate, Brush(s.Background));
                    Color4 color = TatankaBiasLogic.LevelColor(row, f.Price, f.Atr, s);
                    target.DrawTextLayout(new Vector2(labelX - width - 4, top), layout,
                        Brush(TatankaBiasLogic.Alpha(color, i == f.Active ? 1 : .70)));
                }
            }
        }

        private void Table(RectangleF panel, TatankaBiasFrame f, TatankaBiasStyle s)
        {
            int count = f.Rows.Length + 1;
            TextLayout[,] cells = new TextLayout[count, 3];
            float[] widths = new float[3];
            float[] heights = new float[count];
            for (int row = 0; row < count; row++)
            {
                string[] text = row == 0 ? new[] { "", TatankaBiasLogic.Format(f.Price), s.InstrumentLabel + "  if / then" }
                    : new[] { row - 1 == f.Active ? "►" : "", TatankaBiasLogic.Range(f.Rows[row - 1]), f.Rows[row - 1].Text };
                for (int col = 0; col < 3; col++)
                {
                    cells[row, col] = Layout(text[col]);
                    widths[col] = Math.Max(widths[col], cells[row, col].Metrics.WidthIncludingTrailingWhitespace + 16);
                    heights[row] = Math.Max(heights[row], cells[row, col].Metrics.Height + 8);
                }
            }
            float totalW = widths.Sum(), totalH = heights.Sum();
            float left = s.Position.EndsWith("Right", StringComparison.Ordinal) ? panel.Right - totalW : panel.Left;
            float top = s.Position.StartsWith("Top", StringComparison.Ordinal) ? panel.Top
                : s.Position.StartsWith("Bottom", StringComparison.Ordinal) ? panel.Bottom - totalH
                : panel.Top + (panel.Height - totalH) / 2;
            Color4 white = TatankaBiasLogic.Rgb(255, 255, 255);
            float cellY = top;
            for (int row = 0; row < count; row++)
            {
                bool active = row > 0 && row - 1 == f.Active;
                Color4 bias = row == 0 ? (f.Active >= 0 ? TatankaBiasLogic.BiasColor(f.Rows[f.Active].Bias)
                    : TatankaBiasLogic.Rgb(128, 128, 128)) : TatankaBiasLogic.BiasColor(f.Rows[row - 1].Bias);
                Color4 background = active ? TatankaBiasLogic.Alpha(bias, s.ActiveRowOpacity / 100.0) : s.Background;
                float cellX = left;
                for (int col = 0; col < 3; col++)
                {
                    RectangleF rect = new RectangleF(cellX, cellY, widths[col], heights[row]);
                    target.FillRectangle(rect, Brush(row == 0 && col == 1 ? bias : background));
                    TextLayout layout = cells[row, col];
                    float available = Math.Max(1, widths[col] - 16);
                    layout.MaxWidth = available;
                    layout.TextAlignment = col == 0 || (row == 0 && col == 1) ? TextAlignment.Center
                        : col == 1 ? TextAlignment.Trailing : TextAlignment.Leading;
                    Color4 foreground = row == 0 ? white : col < 2 ? bias
                        : active ? white : TatankaBiasLogic.Alpha(TatankaBiasLogic.Rgb(192, 192, 192), .70);
                    target.DrawTextLayout(new Vector2(cellX + 8, cellY + (heights[row] - layout.Metrics.Height) / 2), layout, Brush(foreground));
                    cellX += widths[col];
                }
                cellY += heights[row];
            }
            // Draw shared edges once so transparent borders are not doubled.
            target.DrawRectangle(new RectangleF(left, top, totalW, totalH), Brush(s.Border), 1);
            float x = left;
            for (int col = 0; col < 2; col++)
            { x += widths[col]; target.DrawLine(new Vector2(x, top), new Vector2(x, top + totalH), Brush(s.Border), 1); }
            float edgeY = top;
            for (int row = 0; row < count - 1; row++)
            { edgeY += heights[row]; target.DrawLine(new Vector2(left, edgeY), new Vector2(left + totalW, edgeY), Brush(s.Border), 1); }
        }

        public void Dispose()
        {
            foreach (TextLayout layout in layouts) layout.Dispose();
            foreach (SolidColorBrush brush in brushes.Values) brush.Dispose();
            format.Dispose();
        }
    }
}
