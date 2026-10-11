// Copyright (c) 2026 Tatanka Trading LLC.
// This Source Code Form is subject to the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, obtain one at https://mozilla.org/MPL/2.0/.
// Tatanka Gap Analysis v1.0.0 for NinjaTrader 8 - tatankatrading.com
// NinjaTrader 8 desktop indicator. Uses the last RTH chart-bar close, not exchange settlement.
// Guide (settings, calculation, TradingView differences): https://github.com/TatankaTrading/tatanka-indicators/blob/main/docs/Other-Indicators-Guide.md#gap-analysis
#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators.TatankaGapInternal;
using SharpDX;
using SharpDX.DirectWrite;
using D2D = SharpDX.Direct2D1;
using WpfBrush = System.Windows.Media.Brush;
#endregion

namespace NinjaTrader.NinjaScript
{
    public enum TatankaGapLineStyle { Solid, Dashed, Dotted }
    public enum TatankaGapTablePosition { TopRight, TopLeft, BottomRight, BottomLeft, MiddleRight, MiddleLeft }
}

namespace NinjaTrader.NinjaScript.Indicators
{
    public class TatankaGapAnalysis : Indicator
    {
        private readonly object dataLock = new object();
        private GapEngine engine;
        private TimeZoneInfo chicagoZone;
        private TimeZoneInfo applicationZone;
        private string configurationError;
        private string alertPrefix;

        public override string DisplayName { get { return Name; } }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "Tatanka Gap Analysis";
                Description = "RTH opening gaps with partial fills, optional right extension, and all loaded chart history. Use Minute or Second bars.";
                Calculate = Calculate.OnPriceChange;
                IsOverlay = true;
                IsChartOnly = true;
                IsAutoScale = false;
                DisplayInDataBox = false;
                DrawOnPricePanel = true;
                PaintPriceMarkers = false;
                IsSuspendedWhileInactive = false;
                BarsRequiredToPlot = 0;
                EnabledAnalysis = true;
                RthStart = 83000;
                RthEnd = 150000;
                MinimumGapPoints = 0;
                MaximumGapsToShow = 0;
                ExtendUnfilledRight = true;
                ShowGapZones = true;
                ShrinkPartiallyFilledZones = true;
                ShowTouchedArea = true;
                HideFilledGaps = true;
                ShowSettleLines = true;
                SettleLineStyle = TatankaGapLineStyle.Dashed;
                SettleLineWidth = 1;
                ShowGapLabels = true;
                LabelFontSize = 12;
                BullGapBrush = Brushes.LimeGreen;
                BearGapBrush = Brushes.IndianRed;
                FilledGapBrush = Brushes.SlateGray;
                GapOpacity = 20;
                FilledOpacity = 10;
                ShowGapTable = true;
                TablePosition = TatankaGapTablePosition.TopRight;
                TableFontSize = 13;
                AlertOnNewGap = true;
                AlertOnGapFill = true;
                AlertSound = "Alert1.wav";
            }
            else if (State == State.DataLoaded)
            {
                configurationError = null;
                engine = null;
                alertPrefix = "TatankaGap_" + Guid.NewGuid().ToString("N") + "_";
                TimeSpan start;
                TimeSpan end;
                if (!GapEngine.TryParseTime(RthStart, out start) || !GapEngine.TryParseTime(RthEnd, out end) || start == end)
                    configurationError = "Enter different, valid RTH start/end times in HHmmss format.";
                else if (BarsPeriod.BarsPeriodType != BarsPeriodType.Minute && BarsPeriod.BarsPeriodType != BarsPeriodType.Second)
                    configurationError = "Use a Minute or Second chart. Session boundaries require time-based bars.";
                else
                {
                    try
                    {
                        chicagoZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
                        applicationZone = NinjaTrader.Core.Globals.GeneralOptions.TimeZoneInfo;
                        engine = new GapEngine(start, end, MinimumGapPoints);
                    }
                    catch (TimeZoneNotFoundException)
                    {
                        configurationError = "The Windows Central Standard Time zone is unavailable.";
                    }
                    catch (InvalidTimeZoneException)
                    {
                        configurationError = "The Windows Chicago time-zone definition is invalid.";
                    }
                }
            }
        }

        protected override void OnBarUpdate()
        {
            if (BarsInProgress != 0 || !EnabledAnalysis || engine == null || CurrentBar < 0)
                return;

            // NinjaTrader timestamps time bars at their END. Conversion also handles US DST.
            DateTime sourceTime = DateTime.SpecifyKind(Time[0], DateTimeKind.Unspecified);
            DateTime chicagoTime = TimeZoneInfo.ConvertTime(sourceTime, applicationZone, chicagoZone);
            GapUpdate update;
            lock (dataLock)
                update = engine.Update(CurrentBar, chicagoTime, Open[0], High[0], Low[0], Close[0]);

            // Historical calculation populates the chart without replaying old alerts.
            if (State != State.Realtime)
                return;
            if (AlertOnNewGap && update.NewGap != null)
            {
                GapRecord gap = update.NewGap;
                SendGapAlert("New_" + gap.StartBar, "GAP " + (gap.Direction > 0 ? "UP" : "DOWN")
                    + " " + Price(gap.Size) + " pts | Open: " + Price(gap.OpenPrice) + " | RTH close: " + Price(gap.SettlePrice));
            }
            if (AlertOnGapFill && update.FilledGaps != null)
                foreach (GapRecord gap in update.FilledGaps)
                    SendGapAlert("Fill_" + gap.StartBar, "GAP " + (gap.Direction > 0 ? "UP" : "DOWN")
                        + " FILLED | " + Price(gap.Size) + " pts in " + gap.BarsToFill + " bars");
        }

        private void SendGapAlert(string id, string message)
        {
            string sound = string.IsNullOrWhiteSpace(AlertSound) ? string.Empty : AlertSound;
            if (sound.Length > 0 && !Path.IsPathRooted(sound))
                sound = Path.Combine(NinjaTrader.Core.Globals.InstallDir, "sounds", sound);
            Alert(alertPrefix + id, Priority.Medium, message, sound, 0, Brushes.Black, Brushes.White);
        }

        private string Price(double value)
        {
            return Instrument == null ? value.ToString("0.########", CultureInfo.InvariantCulture)
                : Instrument.MasterInstrument.FormatPrice(value);
        }

        protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
            if (!EnabledAnalysis || ChartBars == null || ChartPanel == null || RenderTarget == null || IsInHitTest || ChartPanel.W < 40 || ChartPanel.H < 40)
                return;

            GapFrame frame = null;
            lock (dataLock)
                if (engine != null)
                    frame = engine.Snapshot(ChartBars.FromIndex, ChartBars.ToIndex, ExtendUnfilledRight, HideFilledGaps, MaximumGapsToShow);

            // Per-render resources always belong to the current RenderTarget and are disposed
            // on every path, including resizing or switching charts. No device resources escape.
            using (D2D.Brush bull = (BullGapBrush ?? Brushes.LimeGreen).ToDxBrush(RenderTarget))
            using (D2D.Brush bear = (BearGapBrush ?? Brushes.IndianRed).ToDxBrush(RenderTarget))
            using (D2D.Brush filled = (FilledGapBrush ?? Brushes.SlateGray).ToDxBrush(RenderTarget))
            using (D2D.Brush white = Brushes.WhiteSmoke.ToDxBrush(RenderTarget))
            using (D2D.Brush background = Brushes.Black.ToDxBrush(RenderTarget))
            using (D2D.Brush warning = Brushes.Goldenrod.ToDxBrush(RenderTarget))
            using (TextFormat labelFormat = new TextFormat(NinjaTrader.Core.Globals.DirectWriteFactory, "Segoe UI", LabelFontSize))
            using (TextFormat tableFormat = new TextFormat(NinjaTrader.Core.Globals.DirectWriteFactory, "Segoe UI", TableFontSize))
            using (D2D.StrokeStyle settleStroke = new D2D.StrokeStyle(NinjaTrader.Core.Globals.D2DFactory,
                new D2D.StrokeStyleProperties { DashStyle = SettleLineStyle == TatankaGapLineStyle.Solid ? D2D.DashStyle.Solid
                    : SettleLineStyle == TatankaGapLineStyle.Dotted ? D2D.DashStyle.Dot : D2D.DashStyle.Dash }))
            {
                RectangleF panel = new RectangleF(ChartPanel.X, ChartPanel.Y, ChartPanel.W, ChartPanel.H);
                RenderTarget.PushAxisAlignedClip(panel, D2D.AntialiasMode.PerPrimitive);
                try
                {
                    if (configurationError != null)
                    {
                        RenderTarget.DrawText("Tatanka Gap Analysis\n" + configurationError, tableFormat,
                            new RectangleF(panel.Left + 12, panel.Top + 20, Math.Max(1, panel.Width - 24), 100), warning);
                        return;
                    }
                    if (frame == null)
                        return;

                    if (ShowGapZones || ShowSettleLines || ShowGapLabels)
                        foreach (GapRecord gap in frame.VisibleGaps)
                            RenderGap(chartControl, chartScale, panel, frame, gap, bull, bear, filled, background, labelFormat, settleStroke);
                    if (ShowGapTable)
                        RenderTable(panel, frame, tableFormat, white, background, warning, bull, bear, filled);
                }
                finally
                {
                    RenderTarget.PopAxisAlignedClip();
                }
            }
        }

        private float BarLeft(ChartControl control, int index, RectangleF panel)
        {
            if (index < ChartBars.FromIndex)
                return panel.Left;
            return Math.Max(panel.Left, control.GetXByBarIndex(ChartBars, index) - Math.Max(1f, (float)control.BarWidth / 2));
        }

        private float BarRight(ChartControl control, int index, RectangleF panel)
        {
            if (index > ChartBars.ToIndex)
                return panel.Right;
            return Math.Min(panel.Right, control.GetXByBarIndex(ChartBars, index) + Math.Max(1f, (float)control.BarWidth / 2));
        }

        private void RenderGap(ChartControl control, ChartScale scale, RectangleF panel, GapFrame frame, GapRecord gap,
            D2D.Brush bull, D2D.Brush bear, D2D.Brush filled, D2D.Brush background, TextFormat labelFormat, D2D.StrokeStyle stroke)
        {
            float left = BarLeft(control, gap.StartBar, panel);
            bool extends = ExtendUnfilledRight && !gap.Filled;
            int endBar = gap.DrawingEndBar(frame.CurrentBar, ExtendUnfilledRight);
            float right = extends ? panel.Right : BarRight(control, endBar, panel);
            if (right <= left)
                return;

            D2D.Brush direction = gap.Direction > 0 ? bull : bear;
            D2D.Brush activeBrush = gap.Filled ? filled : direction;
            float fade = Math.Max(0.4f, 1f - Math.Min(frame.StoredCount - 1 - gap.Ordinal, 4) * 0.15f);
            double top = Math.Max(gap.OpenPrice, gap.SettlePrice);
            double bottom = Math.Min(gap.OpenPrice, gap.SettlePrice);
            if (ShowGapZones)
            {
                if (gap.Filled || !ShrinkPartiallyFilledZones)
                    PaintZone(scale, panel, left, right, top, bottom, activeBrush,
                        (gap.Filled ? FilledOpacity : GapOpacity) / 100f * fade, true);
                else
                {
                    if (ShowTouchedArea && gap.FillPercent > 0)
                    {
                        float touchedRight = extends ? BarRight(control, frame.CurrentBar, panel) : right;
                        PaintZone(scale, panel, left, touchedRight,
                            gap.Direction > 0 ? top : gap.FillEdge, gap.Direction > 0 ? gap.FillEdge : bottom,
                            filled, FilledOpacity / 100f * fade, false);
                    }
                    PaintZone(scale, panel, left, right,
                        gap.Direction > 0 ? gap.FillEdge : top, gap.Direction > 0 ? bottom : gap.FillEdge,
                        direction, GapOpacity / 100f * fade, true);
                }
            }

            activeBrush.Opacity = 0.75f * fade;
            float settleY = scale.GetYByValue(gap.SettlePrice);
            if (ShowSettleLines && settleY >= panel.Top && settleY <= panel.Bottom)
                RenderTarget.DrawLine(new Vector2(left, settleY), new Vector2(right, settleY), activeBrush, SettleLineWidth, stroke);

            // Labels stay at the gap origin; old extended zones do not pile labels at the left edge.
            if (ShowGapLabels && gap.StartBar >= ChartBars.FromIndex && gap.StartBar <= ChartBars.ToIndex)
            {
                float anchorY = scale.GetYByValue(gap.OpenPrice);
                if (anchorY >= panel.Top && anchorY <= panel.Bottom)
                {
                    string text = (gap.Direction > 0 ? "GAP UP" : "GAP DOWN") + "  " + Price(gap.Size) + " pts\n"
                        + (gap.Filled ? "FILLED" : gap.FillPercent.ToString("0.##", CultureInfo.InvariantCulture) + "% FILLED");
                    using (TextLayout layout = new TextLayout(NinjaTrader.Core.Globals.DirectWriteFactory, text, labelFormat, 240, 80))
                    {
                        float y = gap.Direction > 0 ? anchorY - layout.Metrics.Height - 5 : anchorY + 5;
                        y = Math.Max(panel.Top, Math.Min(panel.Bottom - layout.Metrics.Height - 4, y));
                        float x = Math.Max(panel.Left, Math.Min(left, panel.Right - layout.Metrics.Width - 8));
                        background.Opacity = 0.7f;
                        RenderTarget.FillRectangle(new RectangleF(x, y, layout.Metrics.Width + 8, layout.Metrics.Height + 4), background);
                        activeBrush.Opacity = 1;
                        RenderTarget.DrawTextLayout(new Vector2(x + 4, y + 2), layout, activeBrush);
                    }
                }
            }
        }

        private void PaintZone(ChartScale scale, RectangleF panel, float left, float right, double top, double bottom,
            D2D.Brush brush, float opacity, bool outline)
        {
            if (top <= bottom || right <= left)
                return;
            float y1 = scale.GetYByValue(top);
            float y2 = scale.GetYByValue(bottom);
            float high = Math.Max(panel.Top, Math.Min(y1, y2));
            float low = Math.Min(panel.Bottom, Math.Max(y1, y2));
            if (low <= high)
                return;
            RectangleF rectangle = new RectangleF(left, high, right - left, low - high);
            brush.Opacity = opacity;
            RenderTarget.FillRectangle(rectangle, brush);
            if (outline)
            {
                brush.Opacity = Math.Min(0.7f, opacity + 0.25f);
                RenderTarget.DrawRectangle(rectangle, brush, 1);
            }
        }

        private void RenderTable(RectangleF panel, GapFrame frame, TextFormat format, D2D.Brush white, D2D.Brush background,
            D2D.Brush warning, D2D.Brush bull, D2D.Brush bear, D2D.Brush filled)
        {
            GapRecord latest = frame.Latest;
            List<string> keys = new List<string> { "TATANKA", "HISTORY" };
            List<string> values = new List<string> { "GAP ANALYSIS", frame.StoredCount + " stored / " + frame.VisibleGaps.Count + " in view" };
            if (latest == null)
            {
                keys.Add("STATUS");
                values.Add("Waiting for a qualifying RTH gap");
            }
            else
            {
                keys.AddRange(new[] { "GAP", "SIZE", "STATUS", "RTH CLOSE", "BARS", "CONSECUTIVE", "EXHAUSTION" });
                values.AddRange(new[] {
                    latest.Direction > 0 ? "UP" : "DOWN",
                    Price(latest.Size) + " pts",
                    latest.Filled ? "FILLED" : latest.FillPercent.ToString("0.##", CultureInfo.InvariantCulture) + "% FILLED",
                    Price(latest.SettlePrice),
                    latest.Filled ? latest.BarsToFill + " to fill" : (frame.CurrentBar - latest.StartBar) + " elapsed",
                    frame.Consecutive + (latest.Direction > 0 ? " bull" : " bear"),
                    frame.Consecutive >= 3 ? "ELEVATED" : "---"
                });
            }
            float keyWidth = 0;
            float valueWidth = 0;
            for (int row = 0; row < keys.Count; row++)
            {
                using (TextLayout key = new TextLayout(NinjaTrader.Core.Globals.DirectWriteFactory, keys[row], format, 600, 80))
                    keyWidth = Math.Max(keyWidth, key.Metrics.Width);
                using (TextLayout value = new TextLayout(NinjaTrader.Core.Globals.DirectWriteFactory, values[row], format, 600, 80))
                    valueWidth = Math.Max(valueWidth, value.Metrics.Width);
            }
            float width = Math.Min(panel.Width - 16, keyWidth + valueWidth + 36);
            float rowHeight = TableFontSize + 9;
            float height = rowHeight * keys.Count + 12;
            bool onLeft = TablePosition == TatankaGapTablePosition.TopLeft || TablePosition == TatankaGapTablePosition.BottomLeft || TablePosition == TatankaGapTablePosition.MiddleLeft;
            bool onBottom = TablePosition == TatankaGapTablePosition.BottomLeft || TablePosition == TatankaGapTablePosition.BottomRight;
            bool inMiddle = TablePosition == TatankaGapTablePosition.MiddleLeft || TablePosition == TatankaGapTablePosition.MiddleRight;
            float x = onLeft ? panel.Left + 8 : panel.Right - width - 8;
            float y = onBottom ? panel.Bottom - height - 8 : inMiddle ? panel.Top + (panel.Height - height) / 2 : panel.Top + 20;
            y = Math.Max(panel.Top + 2, y);
            background.Opacity = 0.83f;
            filled.Opacity = 0.6f;
            white.Opacity = bull.Opacity = bear.Opacity = warning.Opacity = 1;
            RenderTarget.FillRectangle(new RectangleF(x, y, width, height), background);
            RenderTarget.DrawRectangle(new RectangleF(x, y, width, height), filled, 1);
            for (int row = 0; row < keys.Count; row++)
            {
                float rowY = y + 6 + row * rowHeight;
                D2D.Brush valueBrush = white;
                if (latest != null && keys[row] == "GAP")
                    valueBrush = latest.Direction > 0 ? bull : bear;
                if (latest != null && keys[row] == "STATUS")
                    valueBrush = latest.Filled ? bull : latest.FillPercent > 50 ? warning : white;
                if (keys[row] == "EXHAUSTION" && frame.Consecutive >= 3)
                    valueBrush = warning;
                RenderTarget.DrawText(keys[row], format, new RectangleF(x + 10, rowY, keyWidth + 2, rowHeight), white);
                using (TextLayout layout = new TextLayout(NinjaTrader.Core.Globals.DirectWriteFactory, values[row], format, Math.Max(1, width - keyWidth - 32), rowHeight))
                {
                    layout.TextAlignment = TextAlignment.Trailing;
                    RenderTarget.DrawTextLayout(new Vector2(x + keyWidth + 22, rowY), layout, valueBrush);
                }
            }
        }

        #region Properties
        [NinjaScriptProperty]
        [Display(Name = "Enable gap analysis", GroupName = "01. Gap Settings", Order = 0)]
        public bool EnabledAnalysis { get; set; }

        [NinjaScriptProperty]
        [Range(0, 235959)]
        [Display(Name = "RTH start (Chicago HHmmss)", GroupName = "01. Gap Settings", Order = 1, Description = "Chicago time, including daylight saving time. Default 08:30:00.")]
        public int RthStart { get; set; }

        [NinjaScriptProperty]
        [Range(0, 235959)]
        [Display(Name = "RTH end (Chicago HHmmss)", GroupName = "01. Gap Settings", Order = 2, Description = "Default 15:00:00. Reference price is the last loaded RTH chart-bar close.")]
        public int RthEnd { get; set; }

        [NinjaScriptProperty]
        [Range(0.0, double.MaxValue)]
        [Display(Name = "Minimum gap size (points)", GroupName = "01. Gap Settings", Order = 3)]
        public double MinimumGapPoints { get; set; }

        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "Maximum visible gaps (0 = all)", GroupName = "01. Gap Settings", Order = 4, Description = "Zero displays all eligible gaps intersecting the visible time range. All loaded gaps remain tracked regardless of this display setting.")]
        public int MaximumGapsToShow { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Extend unfilled gaps right", GroupName = "01. Gap Settings", Order = 5)]
        public bool ExtendUnfilledRight { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show gap shading", GroupName = "02. Visual Settings", Order = 0)]
        public bool ShowGapZones { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Shrink partially filled zones", GroupName = "02. Visual Settings", Order = 1, Description = "Wicks reduce the unfilled area. Filled portions never grow back when price retreats.")]
        public bool ShrinkPartiallyFilledZones { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show partially filled area", GroupName = "02. Visual Settings", Order = 2, Description = "Shade the touched portion separately. Applies when shrinking is enabled.")]
        public bool ShowTouchedArea { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Hide fully filled gaps", GroupName = "02. Visual Settings", Order = 3)]
        public bool HideFilledGaps { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show RTH close target lines", GroupName = "02. Visual Settings", Order = 4)]
        public bool ShowSettleLines { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Target line style", GroupName = "02. Visual Settings", Order = 5)]
        public TatankaGapLineStyle SettleLineStyle { get; set; }

        [NinjaScriptProperty]
        [Range(1, 4)]
        [Display(Name = "Target line width", GroupName = "02. Visual Settings", Order = 6)]
        public int SettleLineWidth { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show gap labels", GroupName = "02. Visual Settings", Order = 7)]
        public bool ShowGapLabels { get; set; }

        [NinjaScriptProperty]
        [Range(8, 36)]
        [Display(Name = "Label font size", GroupName = "02. Visual Settings", Order = 8)]
        public int LabelFontSize { get; set; }

        [NinjaScriptProperty]
        [Range(0, 100)]
        [Display(Name = "Unfilled zone opacity (%)", GroupName = "02. Visual Settings", Order = 9)]
        public int GapOpacity { get; set; }

        [NinjaScriptProperty]
        [Range(0, 100)]
        [Display(Name = "Filled area opacity (%)", GroupName = "02. Visual Settings", Order = 10)]
        public int FilledOpacity { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show gap table", GroupName = "03. Table Settings", Order = 0)]
        public bool ShowGapTable { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Table position", GroupName = "03. Table Settings", Order = 1)]
        public TatankaGapTablePosition TablePosition { get; set; }

        [NinjaScriptProperty]
        [Range(8, 24)]
        [Display(Name = "Table font size", GroupName = "03. Table Settings", Order = 2)]
        public int TableFontSize { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Alert on new gap", GroupName = "04. Alerts", Order = 0, Description = "Real-time alerts only. Historical gaps do not replay alerts.")]
        public bool AlertOnNewGap { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Alert on full gap fill", GroupName = "04. Alerts", Order = 1)]
        public bool AlertOnGapFill { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Alert sound file", GroupName = "04. Alerts", Order = 2, Description = "A filename in NinjaTrader sounds, an absolute WAV path, or blank for a visual alert only.")]
        public string AlertSound { get; set; }

        [XmlIgnore]
        [Display(Name = "Up gap color", GroupName = "02. Visual Settings", Order = 11)]
        public WpfBrush BullGapBrush { get; set; }
        [Browsable(false)]
        public string BullGapBrushSerializable
        {
            get { return Serialize.BrushToString(BullGapBrush); }
            set { BullGapBrush = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "Down gap color", GroupName = "02. Visual Settings", Order = 12)]
        public WpfBrush BearGapBrush { get; set; }
        [Browsable(false)]
        public string BearGapBrushSerializable
        {
            get { return Serialize.BrushToString(BearGapBrush); }
            set { BearGapBrush = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "Filled gap color", GroupName = "02. Visual Settings", Order = 13)]
        public WpfBrush FilledGapBrush { get; set; }
        [Browsable(false)]
        public string FilledGapBrushSerializable
        {
            get { return Serialize.BrushToString(FilledGapBrush); }
            set { FilledGapBrush = Serialize.StringToBrush(value); }
        }

        #endregion
    }
}

// ENGINE_BEGIN -- chart-independent logic is shared by the indicator and local tests.
namespace NinjaTrader.NinjaScript.Indicators.TatankaGapInternal
{
    internal sealed class GapRecord
    {
        internal int Ordinal;
        internal int StartBar;
        internal int NextGapBar = -1;
        internal int FillBar = -1;
        internal int Direction;
        internal double OpenPrice;
        internal double SettlePrice;
        internal double FillEdge;
        internal double Size { get { return Math.Abs(OpenPrice - SettlePrice); } }
        internal bool Filled { get { return FillBar >= 0; } }
        internal int BarsToFill { get { return Filled ? FillBar - StartBar : 0; } }
        internal double FillPercent { get { return Filled ? 100.0 : Math.Min(100, Math.Abs(OpenPrice - FillEdge) / Size * 100); } }
        internal GapRecord Copy() { return (GapRecord)MemberwiseClone(); }

        internal int DrawingEndBar(int currentBar, bool extend)
        {
            int end = Filled ? FillBar : currentBar;
            if (!extend && NextGapBar >= 0)
                end = Math.Min(end, NextGapBar - 1);
            return Math.Max(StartBar, end);
        }
    }

    internal sealed class GapUpdate
    {
        internal GapRecord NewGap;
        internal List<GapRecord> FilledGaps;
    }

    internal sealed class GapFrame
    {
        internal int StoredCount;
        internal int CurrentBar;
        internal int Consecutive;
        internal GapRecord Latest;
        internal readonly List<GapRecord> VisibleGaps = new List<GapRecord>();
    }

    internal sealed class GapEngine
    {
        internal readonly List<GapRecord> History = new List<GapRecord>();
        internal readonly List<GapRecord> Active = new List<GapRecord>();
        private readonly TimeSpan rthStart;
        private readonly TimeSpan rthEnd;
        private readonly double minimumGap;
        private DateTime trackedSession = DateTime.MinValue;
        private double trackedClose;
        private int currentBar = -1;
        private int consecutive;

        internal GapEngine(TimeSpan start, TimeSpan end, double minimum)
        {
            if (start == end || start < TimeSpan.Zero || end < TimeSpan.Zero || start >= TimeSpan.FromDays(1) || end >= TimeSpan.FromDays(1))
                throw new ArgumentException("Invalid RTH session.");
            rthStart = start;
            rthEnd = end;
            minimumGap = minimum;
        }

        internal static bool TryParseTime(int hhmmss, out TimeSpan result)
        {
            int hour = hhmmss / 10000;
            int minute = hhmmss / 100 % 100;
            int second = hhmmss % 100;
            bool valid = hhmmss >= 0 && hour < 24 && minute < 60 && second < 60;
            result = valid ? new TimeSpan(hour, minute, second) : TimeSpan.Zero;
            return valid;
        }

        internal DateTime SessionKey(DateTime barEndChicago)
        {
            // 08:30 end-stamped minute bar is pre-RTH; 15:00 is the last RTH bar.
            DateTime instant = barEndChicago.AddTicks(-1);
            TimeSpan clock = instant.TimeOfDay;
            if (rthStart < rthEnd)
                return clock >= rthStart && clock < rthEnd ? instant.Date : DateTime.MinValue;
            if (clock >= rthStart)
                return instant.Date;
            return clock < rthEnd ? instant.Date.AddDays(-1) : DateTime.MinValue;
        }

        internal GapUpdate Update(int bar, DateTime barEndChicago, double open, double high, double low, double close)
        {
            GapUpdate update = new GapUpdate();
            currentBar = bar;
            DateTime session = SessionKey(barEndChicago);
            if (session != DateTime.MinValue)
            {
                if (session != trackedSession)
                {
                    // The first loaded session warms up the RTH close. The next session
                    // can create a gap, even on RTH-only charts with no overnight bars.
                    if (trackedSession != DateTime.MinValue)
                    {
                        double signedSize = open - trackedClose;
                        if (Math.Abs(signedSize) > 0 && Math.Abs(signedSize) >= minimumGap)
                        {
                            GapRecord prior = History.Count == 0 ? null : History[History.Count - 1];
                            GapRecord gap = new GapRecord {
                                Ordinal = History.Count, StartBar = bar, Direction = signedSize > 0 ? 1 : -1,
                                OpenPrice = open, SettlePrice = trackedClose, FillEdge = open
                            };
                            if (prior != null)
                                prior.NextGapBar = bar;
                            consecutive = prior != null && prior.Direction == gap.Direction ? consecutive + 1 : 1;
                            History.Add(gap);
                            Active.Add(gap);
                            update.NewGap = gap;
                        }
                    }
                    trackedSession = session;
                }
                trackedClose = close;
            }

            // Filled history is never rescanned on a price update.
            for (int i = Active.Count - 1; i >= 0; i--)
            {
                GapRecord gap = Active[i];
                gap.FillEdge = gap.Direction > 0
                    ? Math.Max(gap.SettlePrice, Math.Min(gap.FillEdge, low))
                    : Math.Min(gap.SettlePrice, Math.Max(gap.FillEdge, high));
                bool isFilled = gap.Direction > 0 ? low <= gap.SettlePrice : high >= gap.SettlePrice;
                if (isFilled)
                {
                    gap.FillEdge = gap.SettlePrice;
                    gap.FillBar = bar;
                    if (update.FilledGaps == null)
                        update.FilledGaps = new List<GapRecord>();
                    update.FilledGaps.Add(gap);
                    Active.RemoveAt(i);
                }
            }
            return update;
        }

        internal GapFrame Snapshot(int firstVisibleBar, int lastVisibleBar, bool extend, bool hideFilled, int maximumVisible)
        {
            GapFrame frame = new GapFrame {
                StoredCount = History.Count, CurrentBar = currentBar, Consecutive = consecutive,
                Latest = History.Count == 0 ? null : History[History.Count - 1].Copy()
            };
            List<GapRecord> candidates = hideFilled ? Active : History;
            for (int i = candidates.Count - 1; i >= 0; i--)
            {
                GapRecord gap = candidates[i];
                if (gap.StartBar > lastVisibleBar)
                    continue;
                bool reachesView = extend && !gap.Filled || gap.DrawingEndBar(currentBar, extend) >= firstVisibleBar;
                if (!reachesView)
                    continue;
                if (maximumVisible > 0 && frame.VisibleGaps.Count >= maximumVisible)
                    break;
                frame.VisibleGaps.Add(gap.Copy());
            }
            frame.VisibleGaps.Reverse(); // Newest zones are painted last.
            return frame;
        }
    }
}
// ENGINE_END
