// Copyright (c) 2026 Tatanka Trading LLC.
// This Source Code Form is subject to the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, obtain one at https://mozilla.org/MPL/2.0/.
// Tatanka Auto Anchored Volume Profile v1.1.0 for NinjaTrader 8 - tatankatrading.com
// Chart-only. One-minute source bars; uniform estimated volume across each bar's price ticks.
// Guide (setup, calculation, TradingView differences): https://github.com/TatankaTrading/tatanka-indicators/blob/main/docs/Other-Indicators-Guide.md#auto-anchored-volume-profile
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
using NinjaTrader.Gui.Tools;
using SharpDX;
using D2D = SharpDX.Direct2D1;
using WpfBrush = System.Windows.Media.Brush;

namespace NinjaTrader.NinjaScript.Indicators
{
    public class TatankaAutoAnchoredVolumeProfile : Indicator
    {
        private readonly object sync = new object();
        private TatankaAavpCore.AnchorSeries series;
        private SessionIterator iterator;
        private DateTime sessionBegin, sessionEnd, tradingDay;
        private DateTime lastPublish;
        private int lastPrimary = -1;
        private bool isDailyChart;
        private string error;
        private double latestPoc = double.NaN, latestVah = double.NaN, latestVal = double.NaN;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "TatankaAutoAnchoredVolumeProfile";
                Description = "One current profile from an automatic calendar anchor through the latest data, on intraday or 1 Day charts. Defaults: Year, Total, 68% value, 200 target rows, 30% width, Left.";
                Calculate = Calculate.OnEachTick;
                IsOverlay = true;
                IsChartOnly = true;
                IsAutoScale = false;
                DrawOnPricePanel = true;
                DisplayInDataBox = true;
                PaintPriceMarkers = true;
                IsSuspendedWhileInactive = false;
                BarsRequiredToPlot = 0;
                ArePlotsConfigurable = false;
                AnchorPeriod = TatankaAavpAnchor.Year;
                VolumeMode = TatankaAavpVolume.Total;
                ValueAreaPercent = 68;
                ValueAreaMethod = TatankaAavpValueArea.TradingViewStyle;
                RowsLayout = TatankaAavpRows.NumberOfRows;
                RowSize = 200;
                Placement = TatankaAavpPlacement.Left;
                WidthPercent = 30;
                PinToVisibleEdge = false;
                EdgePaddingPixels = 12;
                RowGapPixels = 1;
                ShowProfile = true;
                ShowRowValues = false;
                ShowPoc = ShowVah = ShowVal = true;
                ShowLevelLabels = false;
                ShowInputsInLabel = ShowValuesInLabel = true;
                ShowHistoryNotice = true;
                LineWidth = 1;
                UpVolumeBrush = ColorBrush(0x50, 0x55, 0x61);
                DownVolumeBrush = ColorBrush(0x50, 0x55, 0x61);
                ValueAreaUpBrush = ColorBrush(0x80, 0x0C, 0x93);
                ValueAreaDownBrush = ColorBrush(0x23, 0x09, 0x3F);
                ValueAreaLineBrush = ColorBrush(0xDF, 0xFF, 0x00);
                PocLineBrush = ColorBrush(0x98, 0x75, 0xCD);
                TextBrush = Brushes.LightGray;
                HistogramBackgroundBrush = Brushes.Transparent;
                AddPlot(new Stroke(PocLineBrush, 1), PlotStyle.PriceBox, "POC");
                AddPlot(new Stroke(ValueAreaLineBrush, 1), PlotStyle.PriceBox, "VAH");
                AddPlot(new Stroke(ValueAreaLineBrush, 1), PlotStyle.PriceBox, "VAL");
            }
            else if (State == State.Configure)
            {
                // One fixed source for every period. No dynamic series and no tick download.
                AddDataSeries(BarsPeriodType.Minute, 1);
                Calculate = Calculate.OnEachTick;
                Plots[0].Brush = ShowPoc ? PocLineBrush : Brushes.Transparent;
                Plots[1].Brush = ShowVah ? ValueAreaLineBrush : Brushes.Transparent;
                Plots[2].Brush = ShowVal ? ValueAreaLineBrush : Brushes.Transparent;
            }
            else if (State == State.DataLoaded)
            {
                lock (sync) { series = null; }
                latestPoc = latestVah = latestVal = double.NaN;
                lastPrimary = -1;
                lastPublish = DateTime.MinValue;
                sessionBegin = sessionEnd = tradingDay = DateTime.MinValue;
                iterator = null;
                error = null;
                isDailyChart = BarsPeriod.BarsPeriodType == BarsPeriodType.Day && BarsPeriod.Value == 1;
                if (!Bars.BarsType.IsIntraday && !isDailyChart)
                    error = "Use an intraday chart or a 1 Day chart. The anchor period can still be Year, Quarter, Month, Week, or Session.";
                else if (TickSize <= 0 || RowSize < 1 || ValueAreaPercent < 1 || ValueAreaPercent > 100)
                    error = "Invalid tick size, row size, or value-area setting.";
                else
                {
                    iterator = new SessionIterator(BarsArray[1]);
                    series = new TatankaAavpCore.AnchorSeries(AnchorPeriod, TickSize, RowsLayout, RowSize,
                        ValueAreaPercent, ValueAreaMethod == TatankaAavpValueArea.AtLeastTarget);
                }
                if (error != null) Print(Name + ": " + error);
            }
            else if (State == State.Transition && error == null)
                Publish(true);
        }

        public override string DisplayName
        {
            get
            {
                string name = "Tatanka AAVP";
                if (ShowInputsInLabel)
                    name += " (" + AnchorPeriod + ", " + VolumeMode + ", VA "
                        + ValueAreaPercent.ToString("0.#", CultureInfo.InvariantCulture) + "%, " + RowSize + " " + RowsLayout + ")";
                if (ShowValuesInLabel && !double.IsNaN(latestPoc) && Instrument != null)
                    name += "  POC " + FormatPriceMarker(latestPoc) + "  VAH " + FormatPriceMarker(latestVah) + "  VAL " + FormatPriceMarker(latestVal);
                return name;
            }
        }

        public override string FormatPriceMarker(double price)
        { return Instrument == null ? price.ToString("0.########", CultureInfo.InvariantCulture) : Instrument.MasterInstrument.FormatPrice(price); }

        protected override void OnBarUpdate()
        {
            if (error != null || iterator == null || series == null || CurrentBars[BarsInProgress] < 0) return;
            if (BarsInProgress == 1)
            {
                DateTime time = Times[1][0];
                if (sessionEnd == DateTime.MinValue || time > sessionEnd || time <= sessionBegin)
                {
                    // Minute bars are end-stamped: the closing minute belongs to that session.
                    if (!iterator.GetNextSession(time, true)) return;
                    sessionBegin = iterator.ActualSessionBegin;
                    sessionEnd = iterator.ActualSessionEnd;
                    tradingDay = iterator.ActualTradingDayExchange.Date;
                }
                lock (sync)
                {
                    series.Upsert(tradingDay, sessionBegin, CurrentBars[1], Lows[1][0], Highs[1][0],
                        Volumes[1][0], Closes[1][0] >= Opens[1][0], time);
                }
                // Accumulate every source minute, but only build display rows when needed.
                if (State != State.Historical || CurrentBars[1] == BarsArray[1].Count - 1)
                    Publish(State == State.Historical);
            }
            else if (BarsInProgress == 0)
            {
                if (State == State.Historical)
                {
                    // No historical developing paths. Native price boxes show current levels.
                    Values[0].Reset(); Values[1].Reset(); Values[2].Reset();
                }
                else Publish(CurrentBars[0] != lastPrimary);
            }
        }

        private void Publish(bool force)
        {
            if (CurrentBars == null || CurrentBars.Length < 2 || CurrentBars[0] < 0) return;
            if (!force && (DateTime.UtcNow - lastPublish).TotalMilliseconds < 250) return;
            lock (sync)
            {
                if (series == null || series.Current == null) return;
                TatankaAavpCore.Snapshot p = series.Current.GetSnapshot(force);
                if (p.Rows.Length == 0) return;
                latestPoc = p.Poc; latestVah = p.Vah; latestVal = p.Val;
                Values[0][0] = latestPoc; Values[1][0] = latestVah; Values[2][0] = latestVal;
            }
            lastPublish = DateTime.UtcNow;
            lastPrimary = CurrentBars[0];
        }

        protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
            if (RenderTarget == null || ChartBars == null || ChartPanel == null || IsInHitTest
                || ChartBars.FromIndex < 0 || ChartBars.ToIndex < 0) return;
            base.OnRender(chartControl, chartScale);
            TatankaAavpCore.Snapshot p = null;
            DateTime start = DateTime.MinValue, end = DateTime.MinValue, anchorDate = DateTime.MinValue;
            DateTime firstTradingDay = DateTime.MinValue, lastTradingDay = DateTime.MinValue;
            bool firstLoaded = false;
            lock (sync)
            {
                if (series != null && series.Current != null)
                {
                    p = series.Current.GetSnapshot(false);
                    start = series.Current.StartChart;
                    end = series.Current.LastChart;
                    firstTradingDay = series.FirstTradingDay;
                    lastTradingDay = series.LastTradingDay;
                    anchorDate = series.Current.Window.Start;
                    firstLoaded = series.Current.FirstLoadedProfile;
                }
            }
            using (D2D.Brush up = UpVolumeBrush.ToDxBrush(RenderTarget))
            using (D2D.Brush down = DownVolumeBrush.ToDxBrush(RenderTarget))
            using (D2D.Brush vaUp = ValueAreaUpBrush.ToDxBrush(RenderTarget))
            using (D2D.Brush vaDown = ValueAreaDownBrush.ToDxBrush(RenderTarget))
            using (D2D.Brush vaLine = ValueAreaLineBrush.ToDxBrush(RenderTarget))
            using (D2D.Brush pocLine = PocLineBrush.ToDxBrush(RenderTarget))
            using (D2D.Brush text = TextBrush.ToDxBrush(RenderTarget))
            using (D2D.Brush background = HistogramBackgroundBrush.ToDxBrush(RenderTarget))
            using (SharpDX.DirectWrite.TextFormat font = new SimpleFont("Segoe UI", 11).ToDirectWriteTextFormat())
            {
                float rightEdge = Math.Min(ChartPanel.X + ChartPanel.W, chartControl.CanvasRight);
                RenderTarget.PushAxisAlignedClip(new RectangleF(ChartPanel.X, ChartPanel.Y, Math.Max(1, rightEdge - ChartPanel.X), ChartPanel.H), D2D.AntialiasMode.Aliased);
                try
                {
                    if (error != null || p == null || p.Rows.Length == 0 || p.MaxVolume <= 0)
                    {
                        RenderTarget.DrawText(error ?? "Tatanka AAVP: waiting for 1-minute history. Check the connection, Days to load, and Trading Hours.",
                            font, new RectangleF(ChartPanel.X + 12, ChartPanel.Y + 35, Math.Max(100, ChartPanel.W - 24), 45), text);
                        return;
                    }
                    // Scrolling does not re-anchor or recalculate a visible-range profile.
                    // Hide it only when the viewport is entirely outside the active range.
                    TatankaAavpCore.ProfileLayout geometry;
                    if (TryGetLayout(chartControl, start, end, firstTradingDay, lastTradingDay, rightEdge, out geometry))
                    {
                        float left = geometry.Left, right = geometry.Right, width = geometry.Width;
                        {
                            float top = chartScale.GetYByValue(p.HighEdge), bottom = chartScale.GetYByValue(p.LowEdge);
                            RenderTarget.FillRectangle(new RectangleF(left, Math.Min(top, bottom), Math.Max(1, right - left), Math.Abs(bottom - top)), background);
                            if (ShowProfile)
                            {
                                for (int i = 0; i < p.Rows.Length; i++)
                                {
                                    TatankaAavpCore.Row row = p.Rows[i];
                                    if (row.Total <= 0) continue;
                                    double lo = (row.LowTick - 0.5) * TickSize, hi = (row.HighTick + 0.5) * TickSize;
                                    if (hi < chartScale.MinValue || lo > chartScale.MaxValue) continue;
                                    float y1 = chartScale.GetYByValue(hi), y2 = chartScale.GetYByValue(lo);
                                    float fullHeight = Math.Abs(y2 - y1);
                                    float height = Math.Max(0.15f, fullHeight - Math.Min(RowGapPixels, fullHeight * 0.35f));
                                    float y = Math.Min(y1, y2) + (fullHeight - height) / 2;
                                    bool va = i >= p.ValIndex && i <= p.VahIndex;
                                    bool alignLeft = Placement == TatankaAavpPlacement.Left;
                                    float firstWidth, secondWidth = 0;
                                    D2D.Brush firstBrush = va ? vaUp : up, secondBrush = va ? vaDown : down;
                                    string label;
                                    if (VolumeMode == TatankaAavpVolume.UpDown)
                                    {
                                        firstWidth = (float)(width * row.Up / p.MaxVolume);
                                        secondWidth = (float)(width * row.Down / p.MaxVolume);
                                        label = row.Up.ToString("0", CultureInfo.InvariantCulture) + " / " + row.Down.ToString("0", CultureInfo.InvariantCulture);
                                    }
                                    else if (VolumeMode == TatankaAavpVolume.Delta)
                                    {
                                        double delta = row.Up - row.Down;
                                        firstWidth = p.MaxAbsDelta > 0 ? (float)(width * Math.Abs(delta) / p.MaxAbsDelta) : 0;
                                        if (delta < 0) firstBrush = secondBrush;
                                        label = delta.ToString("+0;-0;0", CultureInfo.InvariantCulture);
                                    }
                                    else
                                    {
                                        firstWidth = (float)(width * row.Total / p.MaxVolume);
                                        label = row.Total.ToString("0", CultureInfo.InvariantCulture);
                                    }
                                    float rowLeft = alignLeft ? left : right - firstWidth - secondWidth;
                                    if (firstWidth > 0) RenderTarget.FillRectangle(new RectangleF(rowLeft, y, firstWidth, height), firstBrush);
                                    if (secondWidth > 0) RenderTarget.FillRectangle(new RectangleF(rowLeft + firstWidth, y, secondWidth, height), secondBrush);
                                    if (ShowRowValues && fullHeight >= 12 && firstWidth + secondWidth >= 55)
                                        RenderTarget.DrawText(label, font, new RectangleF(rowLeft + 3, y, firstWidth + secondWidth - 3, 14), text);
                                }
                            }
                        }
                        if (ShowVah) RenderLevel(p.Vah, "VAH", left, right, rightEdge, vaLine, font, chartScale);
                        if (ShowVal) RenderLevel(p.Val, "VAL", left, right, rightEdge, vaLine, font, chartScale);
                        if (ShowPoc) RenderLevel(p.Poc, "POC", left, right, rightEdge, pocLine, font, chartScale);
                    }
                    if (ShowHistoryNotice && firstLoaded)
                        RenderTarget.DrawText("AAVP: load history from before " + anchorDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                            + " for the full " + AnchorPeriod + " anchor. Missing history cannot be reconstructed.", font,
                            new RectangleF(ChartPanel.X + 12, ChartPanel.Y + ChartPanel.H - 23, Math.Max(100, ChartPanel.W - 24), 20), text);
                    else if (p.WasCapped)
                        RenderTarget.DrawText("AAVP: row height increased to keep the profile within 20,000 rows.", font,
                            new RectangleF(ChartPanel.X + 12, ChartPanel.Y + ChartPanel.H - 23, Math.Max(100, ChartPanel.W - 24), 20), text);
                }
                finally { RenderTarget.PopAxisAlignedClip(); }
            }
        }

        private bool TryGetLayout(ChartControl chartControl, DateTime start, DateTime end,
            DateTime firstTradingDay, DateTime lastTradingDay, float rightEdge,
            out TatankaAavpCore.ProfileLayout geometry)
        {
            geometry = new TatankaAavpCore.ProfileLayout();
            if (isDailyChart)
            {
                int first, last;
                int count = ChartBars.Bars.Count;
                if (!TatankaAavpCore.DailyBarRange.TryGet(count, ChartBars.Bars.GetTime,
                    firstTradingDay, lastTradingDay, out first, out last)
                    || first > ChartBars.ToIndex || last < ChartBars.FromIndex) return false;
                float firstX = chartControl.GetXByBarIndex(ChartBars, first);
                float lastX = chartControl.GetXByBarIndex(ChartBars, last);
                float spacing = Math.Max(2, chartControl.GetBarPaintWidth(ChartBars));
                if (count > 1)
                {
                    int neighbor = first > 0 ? first - 1 : first + 1;
                    spacing = Math.Max(2, Math.Abs(firstX - chartControl.GetXByBarIndex(ChartBars, neighbor)));
                }
                geometry = TatankaAavpCore.DailyBarRange.Layout(firstX, lastX, spacing, ChartPanel.X,
                    rightEdge, WidthPercent, PinToVisibleEdge, EdgePaddingPixels);
            }
            else
            {
                DateTime firstVisible = ChartBars.Bars.GetTime(ChartBars.FromIndex);
                DateTime lastVisible = ChartBars.Bars.GetTime(ChartBars.ToIndex);
                if (start > lastVisible || end < firstVisible) return false;
                geometry = TatankaAavpCore.ProfileLayout.Get(chartControl.GetXByTime(start),
                    chartControl.GetXByTime(end), ChartPanel.X, rightEdge,
                    WidthPercent, PinToVisibleEdge, EdgePaddingPixels);
            }
            return true;
        }

        private void RenderLevel(double price, string label, float left, float right, float edge,
            D2D.Brush brush, SharpDX.DirectWrite.TextFormat font, ChartScale scale)
        {
            if (price < scale.MinValue || price > scale.MaxValue) return;
            float y = scale.GetYByValue(price);
            if (right >= ChartPanel.X && left <= edge)
                RenderTarget.DrawLine(new Vector2(Math.Max(ChartPanel.X, left), y), new Vector2(Math.Min(edge, right), y), brush, LineWidth);
            if (ShowLevelLabels)
                RenderTarget.DrawText(label + " " + FormatPriceMarker(price), font,
                    new RectangleF(Math.Max(ChartPanel.X + 4, Math.Min(right + 4, edge - 110)), y - 15, 110, 15), brush);
        }

        private static WpfBrush ColorBrush(byte r, byte g, byte b)
        {
            SolidColorBrush brush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(r, g, b));
            brush.Freeze(); return brush;
        }

        #region Settings
        [Display(Name = "Anchor period", GroupName = "01. Anchor and calculation", Order = 0)]
        public TatankaAavpAnchor AnchorPeriod { get; set; }
        [Display(Name = "Volume", GroupName = "01. Anchor and calculation", Order = 3)]
        public TatankaAavpVolume VolumeMode { get; set; }
        [Range(1, 100), Display(Name = "Value area volume (%)", GroupName = "01. Anchor and calculation", Order = 4)]
        public double ValueAreaPercent { get; set; }
        [Display(Name = "Rows layout", GroupName = "01. Anchor and calculation", Order = 1)]
        public TatankaAavpRows RowsLayout { get; set; }
        [Range(1, 5000), Display(Name = "Row size", Description = "Target row count in NumberOfRows mode, or ticks per row in TicksPerRow mode.", GroupName = "01. Anchor and calculation", Order = 2)]
        public int RowSize { get; set; }
        [Display(Name = "Value area method", GroupName = "01. Anchor and calculation", Order = 5)]
        public TatankaAavpValueArea ValueAreaMethod { get; set; }

        [Display(Name = "Volume profile", GroupName = "02. Profile style", Order = 0)]
        public bool ShowProfile { get; set; }
        [Display(Name = "Values on rows", GroupName = "02. Profile style", Order = 1)]
        public bool ShowRowValues { get; set; }
        [Range(1, 100), Display(Name = "Width (% of anchored range / chart)", GroupName = "02. Profile style", Order = 2)]
        public double WidthPercent { get; set; }
        [Display(Name = "Placement", GroupName = "02. Profile style", Order = 3)]
        public TatankaAavpPlacement Placement { get; set; }
        [Display(Name = "Pin profile to visible chart edge", Description = "Keeps the histogram visible at the selected left/right chart edge. The calculation still starts at the anchor.", GroupName = "02. Profile style", Order = 4)]
        public bool PinToVisibleEdge { get; set; }
        [Range(0, 500), Display(Name = "Pinned edge padding (pixels)", GroupName = "02. Profile style", Order = 5)]
        public int EdgePaddingPixels { get; set; }
        [Range(0, 5), Display(Name = "Row gap (pixels)", GroupName = "02. Profile style", Order = 6)]
        public int RowGapPixels { get; set; }
        [Display(Name = "Incomplete history notice", GroupName = "02. Profile style", Order = 7)]
        public bool ShowHistoryNotice { get; set; }

        [Display(Name = "VAH", GroupName = "03. Lines and labels", Order = 0)]
        public bool ShowVah { get; set; }
        [Display(Name = "VAL", GroupName = "03. Lines and labels", Order = 1)]
        public bool ShowVal { get; set; }
        [Display(Name = "POC", GroupName = "03. Lines and labels", Order = 2)]
        public bool ShowPoc { get; set; }
        [Range(1, 5), Display(Name = "Line width", GroupName = "03. Lines and labels", Order = 7)]
        public int LineWidth { get; set; }
        [Display(Name = "Level name labels", GroupName = "03. Lines and labels", Order = 8)]
        public bool ShowLevelLabels { get; set; }
        [Display(Name = "Inputs in chart label", GroupName = "03. Lines and labels", Order = 9)]
        public bool ShowInputsInLabel { get; set; }
        [Display(Name = "Values in chart label", GroupName = "03. Lines and labels", Order = 10)]
        public bool ShowValuesInLabel { get; set; }

        [XmlIgnore, Display(Name = "Up / total volume", GroupName = "04. Colors", Order = 0)]
        public WpfBrush UpVolumeBrush { get; set; }
        [Browsable(false)] public string UpVolumeBrushSerializable { get { return Serialize.BrushToString(UpVolumeBrush); } set { UpVolumeBrush = Serialize.StringToBrush(value); } }
        [XmlIgnore, Display(Name = "Down volume", GroupName = "04. Colors", Order = 1)]
        public WpfBrush DownVolumeBrush { get; set; }
        [Browsable(false)] public string DownVolumeBrushSerializable { get { return Serialize.BrushToString(DownVolumeBrush); } set { DownVolumeBrush = Serialize.StringToBrush(value); } }
        [XmlIgnore, Display(Name = "Value area up / total", GroupName = "04. Colors", Order = 2)]
        public WpfBrush ValueAreaUpBrush { get; set; }
        [Browsable(false)] public string ValueAreaUpBrushSerializable { get { return Serialize.BrushToString(ValueAreaUpBrush); } set { ValueAreaUpBrush = Serialize.StringToBrush(value); } }
        [XmlIgnore, Display(Name = "Value area down", GroupName = "04. Colors", Order = 3)]
        public WpfBrush ValueAreaDownBrush { get; set; }
        [Browsable(false)] public string ValueAreaDownBrushSerializable { get { return Serialize.BrushToString(ValueAreaDownBrush); } set { ValueAreaDownBrush = Serialize.StringToBrush(value); } }
        [XmlIgnore, Display(Name = "VAH / VAL", GroupName = "04. Colors", Order = 4)]
        public WpfBrush ValueAreaLineBrush { get; set; }
        [Browsable(false)] public string ValueAreaLineBrushSerializable { get { return Serialize.BrushToString(ValueAreaLineBrush); } set { ValueAreaLineBrush = Serialize.StringToBrush(value); } }
        [XmlIgnore, Display(Name = "POC", GroupName = "04. Colors", Order = 5)]
        public WpfBrush PocLineBrush { get; set; }
        [Browsable(false)] public string PocLineBrushSerializable { get { return Serialize.BrushToString(PocLineBrush); } set { PocLineBrush = Serialize.StringToBrush(value); } }
        [XmlIgnore, Display(Name = "Text", GroupName = "04. Colors", Order = 6)]
        public WpfBrush TextBrush { get; set; }
        [Browsable(false)] public string TextBrushSerializable { get { return Serialize.BrushToString(TextBrush); } set { TextBrush = Serialize.StringToBrush(value); } }
        [XmlIgnore, Display(Name = "Histogram box", GroupName = "04. Colors", Order = 7)]
        public WpfBrush HistogramBackgroundBrush { get; set; }
        [Browsable(false)] public string HistogramBackgroundBrushSerializable { get { return Serialize.BrushToString(HistogramBackgroundBrush); } set { HistogramBackgroundBrush = Serialize.StringToBrush(value); } }
        [Browsable(false), XmlIgnore] public Series<double> POC { get { return Values[0]; } }
        [Browsable(false), XmlIgnore] public Series<double> VAH { get { return Values[1]; } }
        [Browsable(false), XmlIgnore] public Series<double> VAL { get { return Values[2]; } }
        #endregion
    }
}

// BEGIN STANDALONE AUTO ANCHORED PROFILE CORE
namespace NinjaTrader.NinjaScript
{
    public enum TatankaAavpAnchor { Session, Week, Month, Quarter, Year, Decade, Century }
    public enum TatankaAavpRows { NumberOfRows, TicksPerRow }
    public enum TatankaAavpVolume { Total, UpDown, Delta }
    public enum TatankaAavpPlacement { Left, Right }
    public enum TatankaAavpValueArea { TradingViewStyle, AtLeastTarget }
}

namespace TatankaAavpCore
{
    internal struct Window
    {
        internal DateTime Start, End;
    }

    internal static class AnchorClock
    {
        internal static Window Get(DateTime tradingDay, NinjaTrader.NinjaScript.TatankaAavpAnchor anchor)
        {
            DateTime date = tradingDay.Date;
            DateTime start, end;
            switch (anchor)
            {
                case NinjaTrader.NinjaScript.TatankaAavpAnchor.Session:
                    start = date; end = start.AddDays(1); break;
                case NinjaTrader.NinjaScript.TatankaAavpAnchor.Week:
                    // An anchored week remains a whole Monday-Sunday week at New Year.
                    start = date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
                    end = start.AddDays(7);
                    break;
                case NinjaTrader.NinjaScript.TatankaAavpAnchor.Month:
                    start = new DateTime(date.Year, date.Month, 1); end = start.AddMonths(1); break;
                case NinjaTrader.NinjaScript.TatankaAavpAnchor.Quarter:
                    start = new DateTime(date.Year, (date.Month - 1) / 3 * 3 + 1, 1); end = start.AddMonths(3); break;
                case NinjaTrader.NinjaScript.TatankaAavpAnchor.Year:
                    start = new DateTime(date.Year, 1, 1); end = start.AddYears(1); break;
                case NinjaTrader.NinjaScript.TatankaAavpAnchor.Decade:
                    start = new DateTime(Math.Max(1, date.Year / 10 * 10), 1, 1); end = start.AddYears(10); break;
                case NinjaTrader.NinjaScript.TatankaAavpAnchor.Century:
                    start = new DateTime(Math.Max(1, date.Year / 100 * 100), 1, 1); end = start.AddYears(100); break;
                default: throw new ArgumentOutOfRangeException("anchor");
            }
            return new Window { Start = start, End = end };
        }
    }

    internal struct ProfileLayout
    {
        internal float Left, Right, Width;
        internal static ProfileLayout Get(float anchorX, float latestX, float panelLeft, float panelRight,
            double widthPercent, bool pin, int padding)
        {
            float left = anchorX, right = Math.Max(anchorX + 2, latestX);
            if (pin)
            {
                float gap = Math.Min(Math.Max(0, padding), Math.Max(0, (panelRight - panelLeft - 2) / 2));
                left = panelLeft + gap;
                right = Math.Max(left + 1, panelRight - gap);
            }
            return new ProfileLayout { Left = left, Right = right,
                Width = (right - left) * (float)Math.Max(1, Math.Min(100, widthPercent)) / 100f };
        }
    }

    // Daily chart bars are stamped with an exchange trading date, not the local
    // timestamp of their opening/closing minute. Find only bars inside the loaded
    // source range; do not clamp an unrelated date onto the first/last chart bar.
    internal static class DailyBarRange
    {
        internal static bool TryGet(int count, Func<int, DateTime> timeAt,
            DateTime firstTradingDay, DateTime lastTradingDay, out int first, out int last)
        {
            first = last = -1;
            if (count <= 0 || firstTradingDay.Date > lastTradingDay.Date) return false;
            int lo = 0, hi = count;
            while (lo < hi)
            {
                int mid = lo + (hi - lo) / 2;
                if (timeAt(mid).Date < firstTradingDay.Date) lo = mid + 1;
                else hi = mid;
            }
            int start = lo;
            lo = start; hi = count;
            while (lo < hi)
            {
                int mid = lo + (hi - lo) / 2;
                if (timeAt(mid).Date <= lastTradingDay.Date) lo = mid + 1;
                else hi = mid;
            }
            if (start >= lo) return false;
            first = start; last = lo - 1;
            return true;
        }

        internal static ProfileLayout Layout(float firstCenter, float lastCenter, float barSpacing,
            float panelLeft, float panelRight, double widthPercent, bool pin, int padding)
        {
            // Include a full daily slot even when the active anchor has just one bar.
            float half = Math.Max(2, Math.Abs(barSpacing)) / 2;
            return ProfileLayout.Get(firstCenter - half, lastCenter + half,
                panelLeft, panelRight, widthPercent, pin, padding);
        }
    }

    // Sole owner of the active profile. Old anchors are discarded at rollover, so an
    // auto-anchored indicator never becomes a series of historical period profiles.
    internal sealed class AnchorSeries
    {
        private readonly NinjaTrader.NinjaScript.TatankaAavpAnchor anchor;
        private readonly NinjaTrader.NinjaScript.TatankaAavpRows layout;
        private readonly double tickSize, percent;
        private readonly int rowSize;
        private readonly bool atLeast;
        private int lastIndex = -1;
        internal Profile Current { get; private set; }
        internal DateTime FirstTradingDay { get; private set; }
        internal DateTime LastTradingDay { get; private set; }
        internal AnchorSeries(NinjaTrader.NinjaScript.TatankaAavpAnchor anchor, double tickSize,
            NinjaTrader.NinjaScript.TatankaAavpRows layout, int rowSize, double percent, bool atLeast)
        {
            this.anchor = anchor; this.tickSize = tickSize; this.layout = layout;
            this.rowSize = rowSize; this.percent = percent; this.atLeast = atLeast;
        }
        internal bool Upsert(DateTime tradingDay, DateTime sessionBegin, int index, double low, double high,
            double volume, bool up, DateTime sourceTime)
        {
            if (index < 0 || index < lastIndex || volume < 0 || double.IsNaN(volume) || double.IsInfinity(volume)
                || double.IsNaN(low) || double.IsNaN(high) || double.IsInfinity(low) || double.IsInfinity(high) || high < low)
                return false;
            Window window = AnchorClock.Get(tradingDay, anchor);
            if (Current != null && window.Start < Current.Window.Start) return false;
            if (Current != null && window.Start != Current.Window.Start && index == lastIndex) return false;
            if (Current == null || window.Start != Current.Window.Start)
            {
                Current = new Profile(window, sessionBegin, Current == null, tickSize, layout, rowSize, percent, atLeast);
                FirstTradingDay = tradingDay.Date;
            }
            lastIndex = index;
            bool changed = Current.Upsert(index, low, high, volume, up, sourceTime);
            if (changed) LastTradingDay = tradingDay.Date;
            return changed;
        }
    }

    internal sealed class Row
    {
        internal long LowTick, HighTick;
        internal double Up, Down;
        internal double Total { get { return Up + Down; } }
    }

    internal sealed class Snapshot
    {
        internal Row[] Rows;
        internal double TotalVolume, ValueAreaVolume, MaxVolume, MaxAbsDelta, Poc, Vah, Val, LowEdge, HighEdge;
        internal int PocIndex, VahIndex, ValIndex;
        internal long TicksPerRow;
        internal bool WasCapped;
    }

    internal sealed class Density
    {
        internal double Up, Down;
    }

    internal sealed class SourceBar
    {
        internal int Index;
        internal long Low, High;
        internal double Volume;
        internal bool Up;
        internal DateTime Time;
    }

    internal sealed class Profile
    {
        internal readonly Window Window;
        internal readonly DateTime StartChart;
        internal readonly bool FirstLoadedProfile;
        internal DateTime LastChart { get; private set; }
        internal bool Closed { get; private set; }
        private readonly double tickSize;
        private readonly NinjaTrader.NinjaScript.TatankaAavpRows layout;
        private readonly int rowSize;
        private readonly double percent;
        private readonly bool atLeast;
        private readonly Dictionary<long, Density> committed = new Dictionary<long, Density>();
        private SourceBar pending;
        private double committedUp, committedDown;
        private long version, cachedVersion = -1;
        private Snapshot cached;
        private DateTime lastBuild;

        internal Profile(Window window, DateTime startChart, bool firstLoadedProfile, double tickSize,
            NinjaTrader.NinjaScript.TatankaAavpRows layout, int rowSize, double percent, bool atLeast)
        {
            if (tickSize <= 0 || rowSize < 1 || percent < 1 || percent > 100)
                throw new ArgumentException("Invalid profile settings.");
            Window = window; StartChart = startChart; FirstLoadedProfile = firstLoadedProfile;
            this.tickSize = tickSize; this.layout = layout; this.rowSize = rowSize;
            this.percent = percent; this.atLeast = atLeast;
        }

        internal bool Upsert(int sourceIndex, double low, double high, double volume, bool up, DateTime time)
        {
            if (Closed || volume < 0 || double.IsNaN(volume) || double.IsInfinity(volume)
                || double.IsNaN(low) || double.IsNaN(high) || double.IsInfinity(low) || double.IsInfinity(high) || high < low)
                return false;
            long lowTick = checked((long)Math.Round(low / tickSize, MidpointRounding.AwayFromZero));
            long highTick = checked((long)Math.Round(high / tickSize, MidpointRounding.AwayFromZero));
            if (pending != null)
            {
                if (sourceIndex < pending.Index) return false;
                if (sourceIndex == pending.Index && lowTick == pending.Low && highTick == pending.High
                    && volume == pending.Volume && up == pending.Up) return false;
                if (sourceIndex > pending.Index)
                {
                    AddRange(committed, pending);
                    if (pending.Up) committedUp += pending.Volume; else committedDown += pending.Volume;
                }
            }
            // Replacing the current source bar prevents double counting as its cumulative
            // volume/range/direction changes in realtime. Completed bars remain immutable.
            pending = new SourceBar { Index = sourceIndex, Low = lowTick, High = highTick, Volume = volume, Up = up, Time = time };
            LastChart = time;
            version++;
            return true;
        }

        internal void Complete()
        {
            GetSnapshot(true);
            Closed = true;
            committed.Clear();
            pending = null;
        }

        private static void AddRange(Dictionary<long, Density> map, SourceBar bar)
        {
            if (bar == null || bar.Volume <= 0) return;
            double density = bar.Volume / (bar.High - (double)bar.Low + 1);
            AddEvent(map, bar.Low, bar.Up ? density : 0, bar.Up ? 0 : density);
            AddEvent(map, checked(bar.High + 1), bar.Up ? -density : 0, bar.Up ? 0 : -density);
        }

        private static void AddEvent(Dictionary<long, Density> map, long key, double up, double down)
        {
            Density value;
            if (!map.TryGetValue(key, out value)) { value = new Density(); map.Add(key, value); }
            value.Up += up; value.Down += down;
        }

        internal Snapshot GetSnapshot(bool force)
        {
            if (cached != null && (Closed || cachedVersion == version ||
                (!force && (DateTime.UtcNow - lastBuild).TotalMilliseconds < 250))) return cached;
            Dictionary<long, Density> map = new Dictionary<long, Density>(committed.Count + 2);
            foreach (KeyValuePair<long, Density> entry in committed)
                map.Add(entry.Key, new Density { Up = entry.Value.Up, Down = entry.Value.Down });
            AddRange(map, pending);
            List<long> keys = new List<long>(map.Keys);
            keys.Sort();
            if (keys.Count < 2) return new Snapshot { Rows = new Row[0] };
            long low = keys[0], high = keys[keys.Count - 1] - 1;
            long span = checked(high - low + 1);
            long ticks = rowSize;
            if (layout == NinjaTrader.NinjaScript.TatankaAavpRows.NumberOfRows)
                ticks = ChooseTicksPerRow(span, rowSize);
            const int maxRows = 20000;
            bool capped = Math.Ceiling(span / (double)ticks) > maxRows;
            if (capped) ticks = (long)Math.Ceiling(span / (double)maxRows);
            int count = (int)Math.Ceiling(span / (double)ticks);
            Row[] rows = new Row[count];
            for (int i = 0; i < count; i++)
            {
                long rowLow = low + i * ticks;
                rows[i] = new Row { LowTick = rowLow, HighTick = Math.Min(high, rowLow + ticks - 1) };
            }
            double runningUp = 0, runningDown = 0;
            // Integrate constant-density spans over display rows. Runtime depends on event
            // boundaries + rows, NOT millions of individual ticks in a year's price range.
            for (int i = 0; i < keys.Count - 1; i++)
            {
                runningUp += map[keys[i]].Up; runningDown += map[keys[i]].Down;
                long cursor = keys[i], end = keys[i + 1];
                while (cursor < end)
                {
                    int rowIndex = (int)((cursor - low) / ticks);
                    long stop = Math.Min(end, rows[rowIndex].HighTick + 1);
                    rows[rowIndex].Up += Math.Max(0, runningUp) * (stop - cursor);
                    rows[rowIndex].Down += Math.Max(0, runningDown) * (stop - cursor);
                    cursor = stop;
                }
            }
            double expectedUp = committedUp + (pending != null && pending.Up ? pending.Volume : 0);
            double expectedDown = committedDown + (pending != null && !pending.Up ? pending.Volume : 0);
            Normalize(rows, expectedUp, expectedDown);
            Snapshot result = Calculate(rows, tickSize, percent, atLeast);
            result.TicksPerRow = ticks;
            result.WasCapped = capped;
            cached = result; cachedVersion = version; lastBuild = DateTime.UtcNow;
            return result;
        }

        internal static long ChooseTicksPerRow(long span, int target)
        {
            long smaller = Math.Max(1, (long)Math.Floor(span / (double)target));
            long larger = Math.Max(1, (long)Math.Ceiling(span / (double)target));
            double smallerError = Math.Abs(Math.Ceiling(span / (double)smaller) - target);
            double largerError = Math.Abs(Math.Ceiling(span / (double)larger) - target);
            return smallerError <= largerError ? smaller : larger;
        }

        private static void Normalize(Row[] rows, double expectedUp, double expectedDown)
        {
            double up = 0, down = 0;
            foreach (Row row in rows) { up += row.Up; down += row.Down; }
            double upFactor = up > 0 ? expectedUp / up : 0;
            double downFactor = down > 0 ? expectedDown / down : 0;
            foreach (Row row in rows) { row.Up *= upFactor; row.Down *= downFactor; }
        }

        internal static Snapshot Calculate(Row[] rows, double tickSize, double percent, bool atLeast)
        {
            Snapshot result = new Snapshot { Rows = rows };
            if (rows.Length == 0) return result;
            double middle = (rows.Length - 1) / 2.0;
            int poc = 0;
            for (int i = 0; i < rows.Length; i++)
            {
                result.TotalVolume += rows[i].Total;
                result.MaxAbsDelta = Math.Max(result.MaxAbsDelta, Math.Abs(rows[i].Up - rows[i].Down));
                if (rows[i].Total > result.MaxVolume || (rows[i].Total == result.MaxVolume && Math.Abs(i - middle) < Math.Abs(poc - middle)))
                { result.MaxVolume = rows[i].Total; poc = i; }
            }
            int lo = poc, hi = poc;
            double included = rows[poc].Total, target = result.TotalVolume * percent / 100;
            while (included < target && (lo > 0 || hi < rows.Length - 1))
            {
                double above = hi < rows.Length - 1 ? rows[hi + 1].Total : -1;
                double below = lo > 0 ? rows[lo - 1].Total : -1;
                bool takeAbove = above > below || (above == below && hi + 1 - poc <= poc - (lo - 1));
                double candidate = takeAbove ? above : below;
                if (!atLeast && included + candidate > target + result.TotalVolume * 1e-12) break;
                included += candidate;
                if (takeAbove) hi++; else lo--;
            }
            result.PocIndex = poc; result.ValIndex = lo; result.VahIndex = hi;
            result.ValueAreaVolume = included;
            result.Poc = (rows[poc].LowTick + (rows[poc].HighTick - rows[poc].LowTick) / 2) * tickSize;
            result.Val = rows[lo].LowTick * tickSize;
            result.Vah = rows[hi].HighTick * tickSize;
            result.LowEdge = (rows[0].LowTick - 0.5) * tickSize;
            result.HighEdge = (rows[rows.Length - 1].HighTick + 0.5) * tickSize;
            return result;
        }
    }
}
// END STANDALONE AUTO ANCHORED PROFILE CORE
