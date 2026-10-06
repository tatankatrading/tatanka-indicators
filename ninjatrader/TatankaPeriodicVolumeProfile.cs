// Copyright (c) 2026 Tatanka Trading LLC.
// This Source Code Form is subject to the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, obtain one at https://mozilla.org/MPL/2.0/.
// Tatanka Periodic Volume Profile v1.0.0 for NinjaTrader 8 - tatankatrading.com
// Chart-only. One-minute source bars; uniform estimated volume across each bar's price ticks.
// Guide (setup, calculation, TradingView differences): https://github.com/TatankaTrading/tatanka-indicators/blob/main/docs/Other-Indicators-Guide.md#periodic-volume-profile
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
    public class TatankaPeriodicVolumeProfile : Indicator
    {
        private readonly object sync = new object();
        private readonly List<TatankaPvpCore.Profile> profiles = new List<TatankaPvpCore.Profile>();
        private TatankaPvpCore.Profile current;
        private SessionIterator iterator;
        private DateTime sessionBegin, sessionEnd, tradingDay;
        private DateTime lastPublish;
        private int lastPrimary = -1;
        private string error;
        private double latestPoc = double.NaN, latestVah = double.NaN, latestVal = double.NaN;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "TatankaPeriodicVolumeProfile";
                Description = "Daily, weekly, monthly, or yearly volume profiles from 1-minute bars. Defaults: 1 Week, Total, 68% value, 200 rows, 60% width, Left, extensions until touched.";
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
                Period = TatankaPvpPeriod.Weekly;
                PeriodMultiplier = 1;
                VolumeMode = TatankaPvpVolume.Total;
                ValueAreaPercent = 68;
                ValueAreaMethod = TatankaPvpValueArea.TradingViewStyle;
                RowsLayout = TatankaPvpRows.NumberOfRows;
                RowSize = 200;
                ProfilesToKeep = 20;
                Placement = TatankaPvpPlacement.Left;
                WidthPercent = 60;
                RightPaddingPixels = 12;
                RowGapPixels = 1;
                ShowProfile = true;
                ShowRowValues = false;
                ShowPoc = ShowVah = ShowVal = true;
                ExtendPoc = ExtendVah = ExtendVal = true;
                ExtensionBehavior = TatankaPvpExtension.UntilTouched;
                ShowLevelLabels = false;
                ShowInputsInLabel = ShowValuesInLabel = true;
                ShowHistoryNotice = true;
                LineWidth = 1;
                UpVolumeBrush = ColorBrush(0x50, 0x55, 0x61);
                DownVolumeBrush = ColorBrush(0x40, 0x45, 0x51);
                ValueAreaUpBrush = ColorBrush(0x00, 0xB5, 0xEE);
                ValueAreaDownBrush = ColorBrush(0x40, 0x20, 0x70);
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
                lock (sync) { profiles.Clear(); current = null; }
                latestPoc = latestVah = latestVal = double.NaN;
                lastPrimary = -1;
                lastPublish = DateTime.MinValue;
                sessionBegin = sessionEnd = tradingDay = DateTime.MinValue;
                error = null;
                if (!Bars.BarsType.IsIntraday)
                    error = "Use an intraday chart (for example, 30 or 60 Minute). The profile Period can still be Daily, Weekly, Monthly, or Yearly.";
                else if (TickSize <= 0 || PeriodMultiplier < 1 || PeriodMultiplier > 100 || RowSize < 1 || ProfilesToKeep < 1)
                    error = "Invalid period, row, or retention setting.";
                else iterator = new SessionIterator(BarsArray[1]);
                if (error != null) Print(Name + ": " + error);
            }
            else if (State == State.Transition && error == null)
                Publish(true);
        }

        public override string DisplayName
        {
            get
            {
                string name = "Tatanka PVP";
                if (ShowInputsInLabel)
                    name += " (" + PeriodMultiplier + " " + Period + ", " + VolumeMode + ", VA "
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
            if (error != null || iterator == null || CurrentBars[BarsInProgress] < 0) return;
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
                TatankaPvpCore.Window window = TatankaPvpCore.PeriodClock.Get(tradingDay, Period, PeriodMultiplier);
                lock (sync)
                {
                    if (current == null || current.Window.Start != window.Start)
                    {
                        if (current != null && window.Start < current.Window.Start) return;
                        bool first = current == null;
                        if (current != null) current.Complete();
                        // Start at the beginning of the first available trading session in the
                        // period. Missing earlier data remains explicitly a partial-history risk.
                        current = new TatankaPvpCore.Profile(window, sessionBegin, first, TickSize, RowsLayout,
                            RowSize, ValueAreaPercent, ValueAreaMethod == TatankaPvpValueArea.AtLeastTarget);
                        profiles.Add(current);
                        while (profiles.Count > ProfilesToKeep) profiles.RemoveAt(0);
                    }
                    current.Upsert(CurrentBars[1], Lows[1][0], Highs[1][0], Volumes[1][0], Closes[1][0] >= Opens[1][0], time);
                    ObserveTouches();
                }
                // History can contain hundreds of thousands of minute bars. Build final
                // histograms at period boundaries, at render time, and after the load finishes.
                if (State != State.Historical || CurrentBars[1] == BarsArray[1].Count - 1)
                    Publish(State == State.Historical);
            }
            else if (BarsInProgress == 0)
            {
                lock (sync) { ObserveTouches(); }
                if (State == State.Historical)
                {
                    // Historical developing plots are deliberately omitted; period profiles
                    // themselves display their completed values. The price box is current only.
                    Values[0].Reset(); Values[1].Reset(); Values[2].Reset();
                }
                else Publish(CurrentBars[0] != lastPrimary);
            }
        }

        private void ObserveTouches()
        {
            if (CurrentBars[0] < 0 || ExtensionBehavior != TatankaPvpExtension.UntilTouched) return;
            DateTime time = Times[0][0];
            double low = Lows[0][0], high = Highs[0][0];
            foreach (TatankaPvpCore.Profile p in profiles)
            {
                if (!p.Closed || time <= p.LastChart) continue;
                p.Touch.Observe(low, high, time, p.GetSnapshot(false), ExtendPoc, ExtendVah, ExtendVal);
            }
        }

        private void Publish(bool force)
        {
            if (CurrentBars == null || CurrentBars.Length < 2 || CurrentBars[0] < 0) return;
            if (!force && (DateTime.UtcNow - lastPublish).TotalMilliseconds < 250) return;
            lock (sync)
            {
                if (current == null) return;
                TatankaPvpCore.Snapshot p = current.GetSnapshot(force);
                if (p.Rows.Length == 0) return;
                latestPoc = p.Poc; latestVah = p.Vah; latestVal = p.Val;
                Values[0][0] = latestPoc; Values[1][0] = latestVah; Values[2][0] = latestVal;
            }
            lastPublish = DateTime.UtcNow;
            lastPrimary = CurrentBars[0];
        }

        private sealed class RenderProfile
        {
            internal TatankaPvpCore.Snapshot Data;
            internal DateTime Start, End;
            internal DateTime? PocTouch, VahTouch, ValTouch;
            internal bool First;
        }

        protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
            if (RenderTarget == null || ChartBars == null || ChartPanel == null || IsInHitTest
                || ChartBars.FromIndex < 0 || ChartBars.ToIndex < 0) return;
            base.OnRender(chartControl, chartScale);
            List<RenderProfile> packets = new List<RenderProfile>();
            lock (sync)
            {
                foreach (TatankaPvpCore.Profile p in profiles)
                    packets.Add(new RenderProfile { Data = p.GetSnapshot(false), Start = p.StartChart, End = p.LastChart,
                        First = p.FirstLoadedProfile, PocTouch = p.Touch.Poc, VahTouch = p.Touch.Vah, ValTouch = p.Touch.Val });
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
                    if (error != null || packets.Count == 0)
                    {
                        RenderTarget.DrawText(error ?? "Tatanka PVP: waiting for 1-minute history. Check the connection, Days to load, and Trading Hours.",
                            font, new RectangleF(ChartPanel.X + 12, ChartPanel.Y + 35, Math.Max(100, ChartPanel.W - 24), 45), text);
                        return;
                    }
                    DateTime firstVisible = ChartBars.Bars.GetTime(ChartBars.FromIndex);
                    DateTime lastVisible = ChartBars.Bars.GetTime(ChartBars.ToIndex);
                    for (int n = 0; n < packets.Count; n++)
                    {
                        RenderProfile packet = packets[n];
                        TatankaPvpCore.Snapshot p = packet.Data;
                        if (p.Rows.Length == 0 || p.MaxVolume <= 0 || packet.Start > lastVisible) continue;
                        bool latest = n == packets.Count - 1;
                        bool pinned = Placement == TatankaPvpPlacement.ChartRightLatest;
                        if (pinned && !latest) continue;
                        float left = chartControl.GetXByTime(packet.Start);
                        float right = chartControl.GetXByTime(packet.End);
                        float span = Math.Max(2, right - left);
                        float width = span * (float)WidthPercent / 100f;
                        if (pinned)
                        {
                            right = rightEdge - RightPaddingPixels;
                            width = Math.Max(1, (rightEdge - ChartPanel.X) * (float)WidthPercent / 100f);
                            left = right - width;
                        }
                        bool drawHistogram = pinned || packet.End >= firstVisible;
                        if (drawHistogram)
                        {
                            float top = chartScale.GetYByValue(p.HighEdge), bottom = chartScale.GetYByValue(p.LowEdge);
                            RenderTarget.FillRectangle(new RectangleF(left, Math.Min(top, bottom), Math.Max(1, right - left), Math.Abs(bottom - top)), background);
                            if (ShowProfile)
                            {
                                for (int i = 0; i < p.Rows.Length; i++)
                                {
                                    TatankaPvpCore.Row row = p.Rows[i];
                                    if (row.Total <= 0) continue;
                                    double lo = (row.LowTick - 0.5) * TickSize, hi = (row.HighTick + 0.5) * TickSize;
                                    if (hi < chartScale.MinValue || lo > chartScale.MaxValue) continue;
                                    float y1 = chartScale.GetYByValue(hi), y2 = chartScale.GetYByValue(lo);
                                    float fullHeight = Math.Abs(y2 - y1);
                                    float height = Math.Max(0.15f, fullHeight - Math.Min(RowGapPixels, fullHeight * 0.35f));
                                    float y = Math.Min(y1, y2) + (fullHeight - height) / 2;
                                    bool va = i >= p.ValIndex && i <= p.VahIndex;
                                    bool alignLeft = Placement == TatankaPvpPlacement.Left;
                                    float firstWidth, secondWidth = 0;
                                    D2D.Brush firstBrush = va ? vaUp : up, secondBrush = va ? vaDown : down;
                                    string label;
                                    if (VolumeMode == TatankaPvpVolume.UpDown)
                                    {
                                        firstWidth = (float)(width * row.Up / p.MaxVolume);
                                        secondWidth = (float)(width * row.Down / p.MaxVolume);
                                        label = row.Up.ToString("0", CultureInfo.InvariantCulture) + " / " + row.Down.ToString("0", CultureInfo.InvariantCulture);
                                    }
                                    else if (VolumeMode == TatankaPvpVolume.Delta)
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
                        if (ShowVah) RenderLevel(p.Vah, "VAH", left, right, rightEdge, ExtendVah, packet.VahTouch, vaLine, font, chartControl, chartScale, latest);
                        if (ShowVal) RenderLevel(p.Val, "VAL", left, right, rightEdge, ExtendVal, packet.ValTouch, vaLine, font, chartControl, chartScale, latest);
                        if (ShowPoc) RenderLevel(p.Poc, "POC", left, right, rightEdge, ExtendPoc, packet.PocTouch, pocLine, font, chartControl, chartScale, latest);
                    }
                    if (ShowHistoryNotice && packets[packets.Count - 1].First)
                        RenderTarget.DrawText("PVP: first loaded period may be incomplete. Load history from before its start.", font,
                            new RectangleF(ChartPanel.X + 12, ChartPanel.Y + ChartPanel.H - 23, Math.Max(100, ChartPanel.W - 24), 20), text);
                    else if (packets[packets.Count - 1].Data.WasCapped)
                        RenderTarget.DrawText("PVP: row height increased to keep the profile within 20,000 rows.", font,
                            new RectangleF(ChartPanel.X + 12, ChartPanel.Y + ChartPanel.H - 23, Math.Max(100, ChartPanel.W - 24), 20), text);
                }
                finally { RenderTarget.PopAxisAlignedClip(); }
            }
        }

        private void RenderLevel(double price, string label, float left, float right, float edge, bool extend, DateTime? touch,
            D2D.Brush brush, SharpDX.DirectWrite.TextFormat font, ChartControl control, ChartScale scale, bool latest)
        {
            if (price < scale.MinValue || price > scale.MaxValue) return;
            float end = right;
            if (extend)
                end = ExtensionBehavior == TatankaPvpExtension.UntilTouched && touch.HasValue
                    ? Math.Max(right, control.GetXByTime(touch.Value)) : edge;
            float y = scale.GetYByValue(price);
            if (end >= ChartPanel.X && left <= edge)
                RenderTarget.DrawLine(new Vector2(Math.Max(ChartPanel.X, left), y), new Vector2(Math.Min(edge, end), y), brush, LineWidth);
            if (latest && ShowLevelLabels)
                RenderTarget.DrawText(label + " " + FormatPriceMarker(price), font,
                    new RectangleF(Math.Max(ChartPanel.X + 4, Math.Min(right + 4, edge - 110)), y - 15, 110, 15), brush);
        }

        private static WpfBrush ColorBrush(byte r, byte g, byte b)
        {
            SolidColorBrush brush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(r, g, b));
            brush.Freeze(); return brush;
        }

        #region Settings
        [Display(Name = "Period", GroupName = "01. Period and calculation", Order = 0)]
        public TatankaPvpPeriod Period { get; set; }
        [Range(1, 100), Display(Name = "Period multiplier", GroupName = "01. Period and calculation", Order = 1)]
        public int PeriodMultiplier { get; set; }
        [Display(Name = "Volume", GroupName = "01. Period and calculation", Order = 2)]
        public TatankaPvpVolume VolumeMode { get; set; }
        [Range(1, 100), Display(Name = "Value area volume (%)", GroupName = "01. Period and calculation", Order = 3)]
        public double ValueAreaPercent { get; set; }
        [Display(Name = "Rows layout", GroupName = "01. Period and calculation", Order = 4)]
        public TatankaPvpRows RowsLayout { get; set; }
        [Range(1, 5000), Display(Name = "Row size", Description = "Target row count in NumberOfRows mode, or ticks per row in TicksPerRow mode.", GroupName = "01. Period and calculation", Order = 5)]
        public int RowSize { get; set; }
        [Display(Name = "Value area method", GroupName = "01. Period and calculation", Order = 6)]
        public TatankaPvpValueArea ValueAreaMethod { get; set; }
        [Range(1, 100), Display(Name = "Profiles to retain", GroupName = "01. Period and calculation", Order = 7)]
        public int ProfilesToKeep { get; set; }

        [Display(Name = "Volume profile", GroupName = "02. Profile style", Order = 0)]
        public bool ShowProfile { get; set; }
        [Display(Name = "Values on rows", GroupName = "02. Profile style", Order = 1)]
        public bool ShowRowValues { get; set; }
        [Range(1, 100), Display(Name = "Width (% of period / chart)", GroupName = "02. Profile style", Order = 2)]
        public double WidthPercent { get; set; }
        [Display(Name = "Placement", GroupName = "02. Profile style", Order = 3)]
        public TatankaPvpPlacement Placement { get; set; }
        [Range(0, 500), Display(Name = "Chart-right padding (pixels)", GroupName = "02. Profile style", Order = 4)]
        public int RightPaddingPixels { get; set; }
        [Range(0, 5), Display(Name = "Row gap (pixels)", GroupName = "02. Profile style", Order = 5)]
        public int RowGapPixels { get; set; }
        [Display(Name = "Incomplete history notice", GroupName = "02. Profile style", Order = 6)]
        public bool ShowHistoryNotice { get; set; }

        [Display(Name = "VAH", GroupName = "03. Lines and labels", Order = 0)]
        public bool ShowVah { get; set; }
        [Display(Name = "VAL", GroupName = "03. Lines and labels", Order = 1)]
        public bool ShowVal { get; set; }
        [Display(Name = "POC", GroupName = "03. Lines and labels", Order = 2)]
        public bool ShowPoc { get; set; }
        [Display(Name = "Extend POC right", GroupName = "03. Lines and labels", Order = 3)]
        public bool ExtendPoc { get; set; }
        [Display(Name = "Extend VAH right", GroupName = "03. Lines and labels", Order = 4)]
        public bool ExtendVah { get; set; }
        [Display(Name = "Extend VAL right", GroupName = "03. Lines and labels", Order = 5)]
        public bool ExtendVal { get; set; }
        [Display(Name = "Extension behavior", GroupName = "03. Lines and labels", Order = 6)]
        public TatankaPvpExtension ExtensionBehavior { get; set; }
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

// BEGIN STANDALONE PERIODIC PROFILE CORE
namespace NinjaTrader.NinjaScript
{
    public enum TatankaPvpPeriod { Daily, Weekly, Monthly, Yearly }
    public enum TatankaPvpRows { NumberOfRows, TicksPerRow }
    public enum TatankaPvpVolume { Total, UpDown, Delta }
    public enum TatankaPvpPlacement { Left, Right, ChartRightLatest }
    public enum TatankaPvpExtension { UntilTouched, ToChartEdge }
    public enum TatankaPvpValueArea { TradingViewStyle, AtLeastTarget }
}

namespace TatankaPvpCore
{
    internal struct Window
    {
        internal DateTime Start, End;
    }

    internal static class PeriodClock
    {
        internal static Window Get(DateTime tradingDay, NinjaTrader.NinjaScript.TatankaPvpPeriod period, int multiple)
        {
            if (multiple < 1 || multiple > 100) throw new ArgumentOutOfRangeException("multiple");
            DateTime date = tradingDay.Date;
            DateTime yearStart = new DateTime(date.Year, 1, 1);
            DateTime yearEnd = yearStart.AddYears(1);
            DateTime start, end;
            switch (period)
            {
                case NinjaTrader.NinjaScript.TatankaPvpPeriod.Daily:
                    int dayIndex = (date - yearStart).Days / multiple * multiple;
                    start = yearStart.AddDays(dayIndex); end = start.AddDays(multiple); break;
                case NinjaTrader.NinjaScript.TatankaPvpPeriod.Weekly:
                    // Monday-based weeks; the first/last week can be truncated at January 1.
                    int offset = ((int)yearStart.DayOfWeek + 6) % 7;
                    DateTime monday = yearStart.AddDays(-offset);
                    int block = (date - monday).Days / (7 * multiple);
                    start = monday.AddDays(block * 7 * multiple);
                    end = start.AddDays(7 * multiple);
                    if (start < yearStart) start = yearStart;
                    break;
                case NinjaTrader.NinjaScript.TatankaPvpPeriod.Monthly:
                    int firstMonth = (date.Month - 1) / multiple * multiple + 1;
                    start = new DateTime(date.Year, firstMonth, 1);
                    end = start.AddMonths(multiple); break;
                case NinjaTrader.NinjaScript.TatankaPvpPeriod.Yearly:
                    int firstYear = 2000 + (int)Math.Floor((date.Year - 2000) / (double)multiple) * multiple;
                    start = new DateTime(Math.Max(1, firstYear), 1, 1);
                    end = start.AddYears(multiple);
                    return new Window { Start = start, End = end };
                default: throw new ArgumentOutOfRangeException("period");
            }
            if (end > yearEnd) end = yearEnd;
            return new Window { Start = start, End = end };
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

    internal sealed class Touches
    {
        internal DateTime? Poc, Vah, Val;
        internal void Observe(double low, double high, DateTime time, Snapshot levels, bool showPoc, bool showVah, bool showVal)
        {
            if (showPoc && !Poc.HasValue && low <= levels.Poc && high >= levels.Poc) Poc = time;
            if (showVah && !Vah.HasValue && low <= levels.Vah && high >= levels.Vah) Vah = time;
            if (showVal && !Val.HasValue && low <= levels.Val && high >= levels.Val) Val = time;
        }
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
        internal readonly Touches Touch = new Touches();
        internal DateTime LastChart { get; private set; }
        internal bool Closed { get; private set; }
        private readonly double tickSize;
        private readonly NinjaTrader.NinjaScript.TatankaPvpRows layout;
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
            NinjaTrader.NinjaScript.TatankaPvpRows layout, int rowSize, double percent, bool atLeast)
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
            if (layout == NinjaTrader.NinjaScript.TatankaPvpRows.NumberOfRows)
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
// END STANDALONE PERIODIC PROFILE CORE
