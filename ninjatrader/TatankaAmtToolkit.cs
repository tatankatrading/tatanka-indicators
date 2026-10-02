// Copyright (c) 2026 Tatanka Trading LLC.
// This Source Code Form is subject to the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using System.Threading;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using TatankaTrading.AmtToolkit;
using Dx = SharpDX;
using D2 = SharpDX.Direct2D1;
using DW = SharpDX.DirectWrite;
using Bar = TatankaTrading.AmtToolkit.Bar;


namespace TatankaTrading.AmtToolkit
{
    // One name, version and site on every platform. Bump Version on each release;
    // it appears in the dashboard header, the About settings group and fault text.
    public static class AmtBrand
    {
        public const string Name = "Tatanka AMT Toolkit";
        public const string Short = "Tatanka AMT";
        public const string Version = "1.0.0";
        public const string Website = "tatankatrading.com";
    }

    // TradingView's named colors (Pine v6 docs) so NinjaTrader and TradingView
    // render one color scheme. Fixed hex values from the Pine source stay as-is.
    public static class AmtPalette
    {
        public const uint Red = 0xFFF23645u, Green = 0xFF4CAF50u, Lime = 0xFF00E676u, Yellow = 0xFFFDD835u,
            Orange = 0xFFFF9800u, Gray = 0xFF787B86u, Blue = 0xFF2196F3u, Purple = 0xFF9C27B0u,
            Maroon = 0xFF880E4Fu, Aqua = 0xFF00BCD4u, White = 0xFFFFFFFFu,
            AlertRed = 0xFFFF1744u, Pink = 0xFFEA80FCu,
            // color.new(green/red, 30): 70% opaque.
            GreenSoft = 0xB34CAF50u, RedSoft = 0xB3F23645u;
    }

    public sealed class Settings
    {
        public bool tzguard_enable = true;
        public string row_scale = "Auto";
        public bool tick_auto = true;
        public double tick_manual_val = 0.25;
        public string layer_top_pick = "Daily Levels (All)";
        public bool alertcap_enable = true;
        public int alertcap_max = 30;
        public int alertcap_window_min = 60;
        public string lvl_text = "";
        public uint lvl_ip_color = 0xFFFDD835u;
        public uint lvl_bull_color = 0xFFF23645u;
        public uint lvl_bear_color = 0xFF4CAF50u;
        public int lvl_thickness = 1;
        public string lvl_style_in = "Dotted";
        public string lvl_zone_style_in = "Solid";
        public string lvl_text_size_opt = "large";
        public int lvl_x_offset = 40;
        public bool lvl_showLine = true;
        public bool lvl_showZones = true;
        public string lvl_zone_mode = "ATR %";
        public double lvl_zone_atr_pct = 0.02;
        public int lvl_zoneWidthTicks = 8;
        public int lvl_zoneOpacity = 85;
        public bool lvl_color_flip = true;
        public double lvl_flip_atr = 0.05;
        public uint lvl_txt_c = 0x00FFFFFFu;
        public bool lvl_alerts = false;
        public bool lvl_alert_ip = true;
        public bool lvl_alert_bull = true;
        public bool lvl_alert_bear = true;
        public bool mvp_master = true;
        public string mvp_tf_mode = "Weekly";
        public string mvp_sess_time = "0830-1500";
        public double mvp_sess_dur = 6.5;
        public string mvp_calc_tf = "1";
        public double mvp_row_size = 0.25;
        public double mvp_val_pct = 68.0;
        public int mvp_profile_width = 50;
        public bool mvp_align_right = true;
        public int mvp_offset = 50;
        public bool mvp_show_walls = false;
        public bool mvp_use_auto_scale = true;
        public double mvp_scale_start = 2.0;
        public double mvp_scale_end = 1.5;
        public bool mvp_extend_walls = true;
        public int mvp_smooth_len = 2;
        public double mvp_vol_mult = 2;
        public int mvp_min_wall_dist = 40;
        public int mvp_w_wall = 1;
        public uint mvp_c_wall_base = 0xFFE6B300u;
        public int mvp_wall_transp = 25;
        public uint mvp_c_poc = 0xFF78909Cu;
        public int mvp_poc_width = 2;
        public uint mvp_c_levels = 0xFF787B86u;
        public int mvp_lvl_width = 2;
        public uint mvp_c_hist_va = 0xFF78909Cu;
        public int mvp_hist_va_transp = 75;
        public uint mvp_c_hist_out = 0xFF787B86u;
        public int mvp_hist_out_transp = 90;
        public bool mvp_show_poc = true;
        public bool mvp_show_vah = true;
        public bool mvp_show_val = true;
        public string mvp_poc_style = "Solid";
        public string mvp_lvl_style = "Solid";
        public string mvp_wall_style = "Solid";
        public string mvp_lb_size = "Normal";
        public uint mvp_txt_c = 0x00FFFFFFu;
        public bool mvp_use_cur_alerts = true;
        public string mvp_alert_time = "0830-1500";
        public bool macro_show_month = true;
        public bool macro_show_poc = true;
        public bool macro_show_hl = true;
        public bool macro_alert_lvl = true;
        public uint macro_color = 0xFFFFFFFFu;
        public uint macro_poc_color = 0xFFFFFFFFu;
        public int macro_width = 3;
        public int macro_poc_width = 3;
        public string macro_style = "Solid";
        public string macro_poc_style = "Solid";
        public int macro_offset = 2;
        public string macro_text_size = "Normal";
        public uint macro_txt_c = 0x00FFFFFFu;
        public bool ib_enable = true;
        public bool ib_alert_ibh = true;
        public bool ib_alert_ibl = true;
        public string ib_session = "0830-0930";
        public string ib_alert_time = "0830-1500";
        public bool ib_show_mid = false;
        public bool ib_show_labels = true;
        public string ib_label_loc = "Right";
        public string ib_label_size = "Normal";
        public int ib_lbl_offset = 0;
        public int ib_lvl_width = 1;
        public uint ib_col_hi = 0xFFFFCC80u;
        public uint ib_col_lo = 0xFFFFCC80u;
        public uint ib_col_mid = 0xFFFFCC80u;
        public string ib_extend = "None";
        public bool ib_limit_ext = true;
        public string ib_stop_time = "0830-1515";
        public string ib_style_main = "Solid";
        public string ib_style_int = "Dotted";
        public uint ib_txt_c = 0x00FFFFFFu;
        public bool vwap_enable = false;
        public uint vwap_col = 0xFFFFFFFFu;
        public int vwap_width = 2;
        public string vwap_style = "Solid";
        public int vwap_start_h = 8;
        public int vwap_start_m = 30;
        public int vwap_end_h = 15;
        public int vwap_end_m = 0;
        public bool vwap_alert = false;
        public bool cc_showOpen = false;
        public uint cc_c_open = 0xFF2196F3u;
        public int cc_open_width = 1;
        public int cc_open_offset = 3;
        public string cc_open_style = "Solid";
        public string cc_open_lb_size = "Normal";
        public bool wk_showOpen = true;
        public uint wk_c_open = 0xFFFFFFFFu;
        public int wk_open_width = 2;
        public int wk_open_offset = 2;
        public string wk_open_style = "Solid";
        public string wk_open_lb_size = "Normal";
        public uint opens_txt_c = 0x00FFFFFFu;
        public bool wk_alert_open = true;
        public bool amt_enable = true;
        public string amt_sess = "1700-0830:123456";
        public string amt_del_sess = "1500-1505";
        public bool amt_showLb = true;
        public int amt_width = 1;
        public string amt_style_in = "Solid";
        public uint amt_c_hi = 0xFFFFFFFFu;
        public uint amt_c_txt_hi = 0xFFFFFFFFu;
        public uint amt_c_lo = 0xFFFFFFFFu;
        public uint amt_c_txt_lo = 0xFFFFFFFFu;
        public string amt_lb_size_in = "Normal";
        public int amt_offset = 2;
        public bool amt_alert_ovn = true;
        public bool amt_vauto = true;
        public bool amt_vres = false;
        public double amt_res_val = 0.25;
        public bool pd_enable = true;
        public bool pd_use_rth = true;
        public string pd_sess_time = "0830-1500";
        public bool pd_showH = true;
        public bool pd_showL = true;
        public bool pd_showOpen = false;
        public uint pd_high_c = 0xFFFFFFFFu;
        public uint pd_low_c = 0xFFFFFFFFu;
        public uint pd_open_c = 0xFF2196F3u;
        public int pd_high_width = 1;
        public int pd_low_width = 1;
        public int pd_open_width = 1;
        public string pd_style_in = "Solid";
        public string pd_lb_size_in = "Normal";
        public int pd_offset_val = 2;
        public uint pd_txt_c = 0x00FFFFFFu;
        public bool pd_alert_hl = true;
        public bool cc_enable = true;
        public bool cc_show_poc = true;
        public bool cc_show_vah = true;
        public bool cc_show_val = true;
        public bool cc_alert_va = false;
        public bool cc_alert_80 = true;
        public uint pd_poc_c = 0xFFFFFFFFu;
        public uint pd_vah_c = 0x809C27B0u;
        public uint pd_val_c = 0x809C27B0u;
        public int cc_poc_width = 1;
        public int cc_vah_width = 1;
        public int cc_val_width = 1;
        public string cc_poc_style_in = "Solid";
        public int cc_lb_offset = 2;
        public string cc_lb_size_in = "Normal";
        public uint cc_txt_c = 0x00FFFFFFu;
        public bool cc_showFill = true;
        public uint cc_col_active = 0x129C27B0u;
        public uint cc_col_conf = 0x12EA80FCu;
        public double cc_va_pct = 0.68;
        public string cc_sess_def = "0830-1500:23456";
        public bool pw_enable = true;
        public bool pw_showH = true;
        public bool pw_showL = true;
        public uint pw_col = 0xFFFFFFFFu;
        public uint pw_c_txt = 0xFFFFFFFFu;
        public int pw_width = 2;
        public string pw_style_in = "Solid";
        public int pw_offset = 2;
        public string pw_lb_size_in = "Normal";
        public bool pw_alert_hl = true;
        public string sp_rth_session = "0830-1500";
        public int sp_tpo_minutes = 30;
        public int sp_ticks_per_block = 1;
        public uint sp_color_hist = 0x19FF9800u;
        public uint sp_border_c = 0xFFFF9800u;
        public int sp_border_w = 0;
        public string sp_border_style = "Solid";
        public int sp_max_days = 10;
        public bool sp_filter_tails = true;
        public int sp_tail_max = 15;
        public int sp_min_ticks = 1;
        public bool sp_show_poor = true;
        public int sp_poor_width = 2;
        public string sp_poor_style_in = "Dotted";
        public uint sp_col_poor_h = 0xFFF23645u;
        public uint sp_col_poor_l = 0xFF4CAF50u;
        public string sp_poor_lb_size = "Small";
        public uint sp_poor_txt_c = 0x00FFFFFFu;
        public bool showSpike = true;
        public string spikeTimeRange = "1430-1500";
        public int spike_width = 1;
        public string spike_style = "Solid";
        public uint spike_c_sup = 0xFF4CAF50u;
        public uint spike_c_res = 0xFFF23645u;
        public int spike_offset = 15;
        public string spike_lbl_size = "Normal";
        public uint spike_txt_c = 0x00FFFFFFu;
        public bool spike_alert = true;
        public bool inv_enable = true;
        public bool inv_show_line = true;
        public int inv_start_h = 17;
        public int inv_end_h = 8;
        public int inv_end_m = 30;
        public uint inv_c_trap = 0xFF00FF00u;
        public uint inv_c_settle = 0xFF787B86u;
        public string inv_txt_trap = "TRAP ACTIVE";
        public int inv_width = 1;
        public string inv_style_in = "Dashed";
        public int inv_settle_width = 1;
        public string inv_settle_style = "Dashed";
        public string inv_lb_size_in = "Normal";
        public int inv_offset = 40;
        public uint inv_txt_c = 0x00FFFFFFu;
        public bool inv_alert = true;
        public bool npoc_enable = true;
        public uint npoc_c = 0xFFFFC0CBu;
        public uint npoc_c_w = 0xFFFFC0CBu;
        public uint npoc_c_m = 0xFFFFC0CBu;
        public bool npoc_d_show = true;
        public bool npoc_w_show = true;
        public bool npoc_m_show = true;
        public string npoc_style_in = "Solid";
        public int npoc_w_d = 1;
        public int npoc_w_w = 2;
        public int npoc_w_m = 3;
        public string npoc_lb_size = "Small";
        public int npoc_lb_offset = 2;
        public uint npoc_txt_c = 0x00FFFFFFu;
        public bool npoc_alert = true;
        public string context_session = "0815-1500";
        public int daily_atr_length = 14;
        public bool tbl_show = true;
        public string tbl_session = "0830-1500";
        public string tbl_pos = "Top Right";
        public string tbl_size = "Normal";
        public bool HideAllLabels = false;
        public bool LabelsLevels = true;
        public bool LabelsProfiles = true;
        public bool LabelsPriorDay = true;
        public bool LabelsPriorValue = true;
        public bool LabelsOpens = true;
        public bool LabelsPriorWeek = true;
        public bool LabelsPriorMonth = true;
        public bool LabelsInventory = true;
        public bool LabelsSpike = true;
        public bool LabelsNakedPoc = true;
        public bool LabelsPoor = true;
        public bool ShowPriceTags = true;
        public string LevelAlertMode = "Cross";
        public int AlertZoneTicks = 4;
        public int MaxProfileRows = 100000;
        public int MaxStructures = 2000;
    }
}







namespace TatankaTrading.AmtToolkit
{
    public sealed class Bar
    {
        public DateTime Start, End;
        public int Index;
        public double Open, High, Low, Close, Volume;
    }
    public static class MathEx
    {
        public static bool Valid(double x) { return !double.IsNaN(x) && !double.IsInfinity(x); }
        // Pine rounds ties toward positive infinity, unlike Math.Round's banker's rounding.
        public static long Bin(double x, double step) { return (long)Math.Floor(x / step + .5); }
        public static DateTime TradingDate(DateTime ct) { return ct.TimeOfDay >= TimeSpan.FromHours(17) ? ct.Date.AddDays(1) : ct.Date; }
        public static DateTime Week(DateTime td) { return td.Date.AddDays(-(((int)td.DayOfWeek + 6) % 7)); }
        public static DateTime Month(DateTime td) { return new DateTime(td.Year, td.Month, 1); }
        public static int Minute(DateTime t) { return t.Hour * 60 + t.Minute; }
        public static bool InSession(DateTime t, string session)
        {
            string[] parts = session.Split(':'); string[] range = parts[0].Split('-');
            if (range.Length != 2 || range[0].Length != 4 || range[1].Length != 4) throw new ArgumentException("Invalid CT session: " + session);
            int a = int.Parse(range[0].Substring(0, 2)) * 60 + int.Parse(range[0].Substring(2));
            int b = int.Parse(range[1].Substring(0, 2)) * 60 + int.Parse(range[1].Substring(2));
            if (a > 1439 || b > 1440 || int.Parse(range[0].Substring(2)) > 59 || int.Parse(range[1].Substring(2)) > 59) throw new ArgumentException("Invalid CT session: " + session);
            int m = Minute(t);
            bool day = parts.Length < 2 || parts[1].Contains(((int)t.DayOfWeek + 1).ToString());
            return day && (a == b || (a < b ? m >= a && m < b : m >= a || m < b));
        }
        // Micro contracts trade at the parent's prices, so they load the parent's levels
        // ({root} in the levels path and the levels cache use this root).
        public static string LevelsRoot(string root)
        {
            switch ((root ?? "").ToUpperInvariant())
            {
                case "MES": return "ES";
                case "MNQ": return "NQ";
                case "MCL": return "CL";
                case "MGC": return "GC";
                default: return root;
            }
        }
        // Profile row scale. Auto keeps rows near 0.0035% of price, rounded to 1, 2 or 5 times a
        // power of ten in ticks, so a bar touches about as many rows on NQ, YM or BTC as on ES.
        // Same rule as the Pine version.
        public static int RowMult(string scale, double price, double tick)
        {
            int m = 1;
            if (scale != "Auto") { if (!int.TryParse((scale ?? "1x").TrimEnd('x'), out m)) m = 1; }
            else if (Valid(price) && price > 0 && tick > 0)
            {
                double raw = price * 0.000035 / tick;
                if (raw >= Math.Sqrt(2))
                {
                    double dec = Math.Pow(10, Math.Floor(Math.Log10(raw))), f = raw / dec;
                    double n = f < Math.Sqrt(2) ? 1 : f < Math.Sqrt(10) ? 2 : f < Math.Sqrt(50) ? 5 : 10;
                    m = (int)Math.Round(n * dec, MidpointRounding.AwayFromZero);
                }
            }
            return Math.Max(1, m);
        }
        public static double AutoTick(string root, double tick, double close)
        {
            switch (root.ToUpperInvariant())
            {
                case "CL": case "MCL": return .05;
                case "NG": case "QG": return .005;
                case "GC": case "MGC": return .5;
                case "SI": case "SIL": return .05;
                case "HG": case "QC": return .005;
                case "ZB": case "ZN": return .015625;
                case "6E": case "M6E": return .0005;
                case "6J": case "M6J": return .0000005;
                default: return close > 1000 && tick < .1 ? .25 : tick;
            }
        }
        public static string Price(double price, string root, double tick)
        {
            if (!Valid(price)) return "---";
            double snap = Bin(price, tick) * tick;
            if (new[] { "ZN", "ZB", "ZF", "ZT", "UB" }.Contains(root))
            {
                int whole = (int)Math.Floor(snap), quarters = (int)Math.Round((snap - whole) * 128);
                if (quarters >= 128) { whole++; quarters = 0; }
                return whole + "'" + (quarters / 4).ToString("00") + new[] { "0", "2", "5", "7" }[quarters % 4];
            }
            int decimals = 0; double q = tick;
            while (decimals < 8 && Math.Abs(q - Math.Round(q)) > 1e-8) { q *= 10; decimals++; }
            return snap.ToString("F" + decimals, CultureInfo.InvariantCulture);
        }
    }
    public sealed class ProfileStats
    {
        public double Poc = double.NaN, Vah = double.NaN, Val = double.NaN, Total;
    }
    public sealed class Profile
    {
        public readonly double Step;
        public readonly int Limit;
        public Dictionary<long, double> Rows = new Dictionary<long, double>();
        private Queue<long> insertion = new Queue<long>();
        public long PrunedRows;
        public Profile(double step, int limit) { if (step <= 0) throw new ArgumentException("Profile row size must be positive"); Step = step; Limit = Math.Max(1, limit); }
        public Profile Clone() { var p = new Profile(Step, Limit); p.Rows = new Dictionary<long, double>(Rows); p.insertion = new Queue<long>(insertion); p.PrunedRows = PrunedRows; return p; }
        public void Add(Bar b, bool capped)
        {
            long lo = MathEx.Bin(b.Low, Step), hi = MathEx.Bin(b.High, Step);
            long count = Math.Max(1, hi - lo); if (capped) count = Math.Min(200, count);
            double per = Math.Max(0, b.Volume) / count;
            long skip = Math.Max(0, count - Limit); PrunedRows += skip;
            for (long k = lo + skip; k < lo + count; k++)
            {
                double v;
                if (!Rows.TryGetValue(k, out v)) { while (Rows.Count >= Limit) { Rows.Remove(insertion.Dequeue()); PrunedRows++; } insertion.Enqueue(k); }
                Rows[k] = v + per;
            }
        }
        public ProfileStats Stats(double pct)
        {
            var s = new ProfileStats(); if (Rows.Count == 0) return s;
            double max = -1; long poc = 0;
            foreach (long k in insertion) { double v = Rows[k]; s.Total += v; if (v > max) { max = v; poc = k; } }
            if (s.Total <= 0) return s;
            long[] keys = Rows.Keys.OrderBy(k => k).ToArray(); int up = Array.IndexOf(keys, poc), dn = up;
            double area = max, target = s.Total * (pct > 1 ? pct / 100 : pct);
            while (area < target && (up < keys.Length - 1 || dn > 0))
            {
                double vu = up < keys.Length - 1 ? Rows[keys[up + 1]] : 0, vd = dn > 0 ? Rows[keys[dn - 1]] : 0;
                if ((vu >= vd && up < keys.Length - 1) || dn <= 0) area += Rows[keys[++up]];
                else area += Rows[keys[--dn]];
            }
            s.Poc = poc * Step; s.Vah = keys[up] * Step; s.Val = keys[dn] * Step; return s;
        }
    }
    public sealed class Rma
    {
        public readonly int Period; private int count; private double sum; public double Value = double.NaN;
        public Rma(int n) { Period = Math.Max(1, n); }
        public double Add(double x) { if (!MathEx.Valid(x)) return Value; if (++count <= Period) { sum += x; if (count == Period) Value = sum / Period; } else Value = (Value * (Period - 1) + x) / Period; return Value; }
        public Rma Clone() { return (Rma)MemberwiseClone(); }
    }
    public sealed class DailyValue
    {
        public DateTime Date; public double Close, High, Low, Atr14, Atr;
    }
    public sealed class DailyHistory
    {
        private List<DailyValue> data = new List<DailyValue>(); private Rma r14 = new Rma(14), ra; private double prev = double.NaN;
        public DailyHistory(int atr) { ra = new Rma(atr); }
        public void Add(DateTime date, double h, double l, double c)
        {
            if (data.Count > 0 && date.Date <= data[data.Count - 1].Date) return;
            double tr = MathEx.Valid(prev) ? Math.Max(h - l, Math.Max(Math.Abs(h - prev), Math.Abs(l - prev))) : h - l;
            data.Add(new DailyValue { Date = date.Date, Close = c, High = h, Low = l, Atr14 = r14.Add(tr), Atr = ra.Add(tr) }); prev = c;
        }
        public DailyValue Before(DateTime date) { for (int i = data.Count - 1; i >= 0; i--) if (data[i].Date < date.Date) return data[i]; return null; }
    }
    public sealed class Level
    {
        public string Name; public double Price; public int Kind; public bool Flipped;
        public Level Clone() { return (Level)MemberwiseClone(); }
        public static List<Level> Parse(string text) { return LevelsDocument.Parse(text, true).Levels; }
    }

    public sealed class LineItem
    {
        public string Name, Group; public double Price; public DateTime Start, End; public uint Color; public int Width = 1, Offset = 2; public string Style = "Solid", Size = "Normal", Extend = "None"; public bool Label = true; public uint TextColor;
    }
    public sealed class ZoneItem
    {
        public double Top, Bottom; public DateTime Start, Created; public uint Color; public bool Live;
        public uint BorderColor; public int BorderWidth; public string BorderStyle = "Solid";
    }
    public sealed class Naked { public string Kind; public double Price; public DateTime Start; public int Width; }
    public sealed class Poor { public double Price; public bool High; public DateTime Start, Created; }
    public sealed class TpoRow { public int Hits, Last; public DateTime First; public TpoRow Clone() { return (TpoRow)MemberwiseClone(); } }
    public sealed class PastProfile { public Profile Profile; public ProfileStats Stats; public DateTime Start, End; public int EndIndex; }
    public sealed class Notice { public string Key, Text; }
    public sealed class Frame
    {
        public Bar Bar; public double RthVwap, DailyAtr;
        public List<LevelWatch> Watches = new List<LevelWatch>(); public List<Notice> Notices = new List<Notice>();
        public List<LineItem> Lines = new List<LineItem>(); public List<ZoneItem> Zones = new List<ZoneItem>();
        public List<PastProfile> Profiles = new List<PastProfile>(); public string[] Labels, Values; public uint[] Colors;
        public string Warning = ""; public bool RuleActive, RuleConfirmed; public double VaHigh, VaLow;
    }

    // A completed chart bar is committed once. Live previews run on Clone(), mirroring Pine's rollback.
    public sealed class Engine
    {
        public readonly Settings S; public readonly string Root; public readonly double Tick, Step; public readonly int Minutes;
        public List<Level> Levels;
        private Profile day, main, macro, nd, nw, nm;
        private ProfileStats prevDay = new ProfileStats(), prevMonth = new ProfileStats();
        private List<PastProfile> profiles = new List<PastProfile>(); private List<Naked> naked = new List<Naked>();
        private List<ZoneItem> single = new List<ZoneItem>(); private List<Poor> poor = new List<Poor>();
        private Dictionary<long, TpoRow> tpo = new Dictionary<long, TpoRow>();
        private Queue<long> tpoOrder = new Queue<long>();
        private bool rowsPruned, structuresPruned;
        private Bar previous;
        private DateTime td, week, month, mainKey, mainStart, weekStart, monthStart, dayStart, rthStart, ibStart, settleStart, spikeStart, macroKey;
        private double tradeH = double.NaN, tradeL = double.NaN, weekH = double.NaN, weekL = double.NaN, monthH = double.NaN, monthL = double.NaN;
        private double pdH = double.NaN, pdL = double.NaN, pdO = double.NaN, pwH = double.NaN, pwL = double.NaN, pmH = double.NaN, pmL = double.NaN, weekOpen = double.NaN;
        private double rthH = double.NaN, rthL = double.NaN, rthO = double.NaN, rthPreviousOpen = double.NaN, ibH = double.NaN, ibL = double.NaN, ibWorkH, ibWorkL;
        private double ovnH = double.NaN, ovnL = double.NaN, settle = double.NaN, spike = double.NaN, preH = double.NaN, preL = double.NaN, frozenH, frozenL;
        private double dayH = double.NaN, dayL = double.NaN, tableH, tableL, vSrc, vVol, contextHlc, contextVol;
        private int above, below, total, otfUp, otfDn, tpoPeriod = -1;
        private bool trap, resolved, trapAlert, bullSpike, auction, isTrapDay, outsideAbove, outsideBelow, gapUp, gapDown, rule, dayFull, weekFull, monthFull, macroFull;
        private DateTime? vaEntry;
        private string inventoryFrozen = "---", dayType = "DEVELOPING", pivotBias = "---";
        private uint dayColor = AmtPalette.Gray;
        private double spStep;
        // Row scale in effect (set from the last trade before each month starts). Each profile keeps
        // the step it was created with, so a scale change never mixes row sizes inside a profile.
        private int rowMult = 1;
        private double lastMacroClose = double.NaN;
        public int RowMult { get { return rowMult; } }
        private double RowStep(int mult) { return Math.Max(Step * mult, S.mvp_row_size); }
        // Daily and prior-month profiles keep the plain Row Size at 1x, as before.
        private double ValueStep(int mult) { return mult > 1 ? Math.Max(S.mvp_row_size, Step * mult) : S.mvp_row_size; }
        private double NakedStep(int mult) { return (S.amt_vauto ? Step : S.amt_vres ? S.amt_res_val : Step) * mult; }
        private double BlockStep(int mult) { return Step * Math.Max(1, S.sp_ticks_per_block) * mult; }
        public Engine(Settings s, string root, double tick, double initialClose, int minutes)
        {
            S = s; Root = root; Tick = tick; Minutes = minutes; Step = s.tick_auto ? MathEx.AutoTick(root, tick, initialClose) : s.tick_manual_val;
            rowMult = MathEx.RowMult(s.row_scale, initialClose, Step);
            main = new Profile(RowStep(rowMult), s.MaxProfileRows); day = new Profile(ValueStep(rowMult), s.MaxProfileRows);
            macro = new Profile(ValueStep(rowMult), s.MaxProfileRows);
            nd = new Profile(NakedStep(rowMult), s.MaxProfileRows); nw = new Profile(NakedStep(rowMult), s.MaxProfileRows); nm = new Profile(NakedStep(rowMult), s.MaxProfileRows);
            spStep = BlockStep(rowMult);
            Levels = Level.Parse(s.lvl_text);
        }
        public Engine Clone()
        {
            var e = (Engine)MemberwiseClone(); e.day = day.Clone(); e.main = main.Clone(); e.macro = macro.Clone(); e.nd = nd.Clone(); e.nw = nw.Clone(); e.nm = nm.Clone();
            e.Levels = Levels.Select(x => x.Clone()).ToList(); e.naked = new List<Naked>(naked); e.single = new List<ZoneItem>(single); e.poor = new List<Poor>(poor);
            e.tpo = tpo.ToDictionary(x => x.Key, x => x.Value.Clone()); e.profiles = new List<PastProfile>(profiles); e.tpoOrder = new Queue<long>(tpoOrder);
            return e;
        }
        public void ReplaceLevels(List<Level> levels) { Levels = levels.Select(x => x.Clone()).ToList(); pivotBias = "---"; }
        private bool Was(string session) { return previous != null && MathEx.InSession(previous.Start, session); }
        private static double Max(double a, double b) { return MathEx.Valid(a) ? Math.Max(a, b) : b; }
        private static double Min(double a, double b) { return MathEx.Valid(a) ? Math.Min(a, b) : b; }
        private string Price(double v) { return MathEx.Price(v, Root, Tick); }
        private void Event(Frame f, string key, string text) { f.Notices.Add(new Notice { Key = key, Text = text }); }
        private void Cross(Frame f, string name, double p, bool enabled, bool moving, double halfWidth = -1)
        {
            if (MathEx.Valid(p)) f.Watches.Add(new LevelWatch { Name = name, Price = p, Enabled = enabled, Moving = moving, HalfWidth = halfWidth >= 0 ? halfWidth : S.AlertZoneTicks * Tick });
        }
        private string Inventory()
        {
            if (!S.inv_enable || total == 0) return "---";
            double l = 100.0 * above / total, s = 100.0 * below / total;
            return l == 100 ? "TRAP LONG (100%)" : s == 100 ? "TRAP SHORT (100%)" : l > 60 ? "LONG (" + l.ToString("0") + "%)" : s > 60 ? "SHORT (" + s.ToString("0") + "%)" : "BALANCED";
        }
        private void NakedRoll(Profile p, string kind, bool complete, bool show, int width, DateTime start)
        {
            double poc = p.Stats(.68).Poc;
            if (S.npoc_enable && complete && show && MathEx.Valid(poc)) naked.Add(new Naked { Kind = kind, Price = poc, Start = start, Width = width });
        }
        private DateTime ProfileKey(Bar b)
        {
            DateTime d = MathEx.TradingDate(b.Start);
            return S.mvp_tf_mode == "Weekly" ? MathEx.Week(d) : S.mvp_tf_mode == "Monthly" ? MathEx.Month(d) : S.mvp_tf_mode == "Yearly" ? new DateTime(d.Year, 1, 1) : b.Start.Date;
        }
        private List<Bar> Aggregate(IList<Bar> input, int size)
        {
            var result = new List<Bar>(); Bar cur = null; DateTime bucket = DateTime.MinValue;
            foreach (Bar b in input)
            {
                DateTime k = b.Start.Date.AddMinutes((MathEx.Minute(b.Start) / size) * size);
                if (cur == null || k != bucket) { cur = new Bar { Start = b.Start, End = b.End, Open = b.Open, High = b.High, Low = b.Low, Close = b.Close, Volume = b.Volume }; result.Add(cur); bucket = k; }
                else { cur.High = Math.Max(cur.High, b.High); cur.Low = Math.Min(cur.Low, b.Low); cur.Close = b.Close; cur.Volume += b.Volume; cur.End = b.End; }
            }
            return result;
        }
        // Previous-month profile consumes independently completed 30/60-minute bars; never a future HTF close.
        public void AddMacro(Bar b)
        {
            DateTime key = MathEx.Month(MathEx.TradingDate(b.Start));
            RollMacro(key, MathEx.Valid(lastMacroClose) ? lastMacroClose : b.Close);
            macro.Add(b, true);
            lastMacroClose = b.Close;
        }
        private void RollMacro(DateTime key, double lastPrice)
        {
            if (macroKey != key)
            {
                if (macroKey != DateTime.MinValue && macroFull) prevMonth = macro.Stats(S.mvp_val_pct);
                macroFull = macroKey != DateTime.MinValue; macro = new Profile(ValueStep(MathEx.RowMult(S.row_scale, lastPrice, Step)), S.MaxProfileRows); macroKey = key;
            }
        }
        public Frame Process(Bar b, IList<Bar> subbars, DailyValue daily)
        {
            var f = new Frame { Bar = b }; DateTime d = MathEx.TradingDate(b.Start), w = MathEx.Week(d), m = MathEx.Month(d);
            bool newDay = td != d, newWeek = week != w, newMonth = month != m;
            double lastPrice = previous != null ? previous.Close : b.Close;
            if (newMonth) rowMult = MathEx.RowMult(S.row_scale, lastPrice, Step);
            RollMacro(m, lastPrice);
            bool rth = MathEx.InSession(b.Start, "0830-1500"), ib = MathEx.InSession(b.Start, S.ib_session), ovn = MathEx.InSession(b.Start, S.amt_sess);
            bool pdSession = MathEx.InSession(b.Start, S.pd_sess_time), track = MathEx.InSession(b.Start, S.context_session), table = MathEx.InSession(b.Start, S.tbl_session);
            bool spikeWindow = MathEx.InSession(b.Start, S.spikeTimeRange), spSession = MathEx.InSession(b.Start, S.sp_rth_session);
            if (newDay)
            {
                if (td != DateTime.MinValue && dayFull) prevDay = day.Stats(S.cc_va_pct);
                if (!S.pd_use_rth && td != DateTime.MinValue) { pdH = tradeH; pdL = tradeL; }
                NakedRoll(nd, "Daily", dayFull, S.npoc_d_show, S.npoc_w_d, b.Start);
                dayFull = td != DateTime.MinValue || MathEx.Minute(b.Start) == 1020;
                day = new Profile(ValueStep(rowMult), S.MaxProfileRows); nd = new Profile(NakedStep(rowMult), S.MaxProfileRows);
                td = d; dayStart = b.Start; tradeH = b.High; tradeL = b.Low; rule = false; vaEntry = null; rthO = double.NaN;
                contextHlc = contextVol = 0;
                above = below = total = 0; trapAlert = false;
            }
            else { tradeH = Max(tradeH, b.High); tradeL = Min(tradeL, b.Low); }
            if (newWeek)
            {
                if (weekFull) { pwH = weekH; pwL = weekL; }
                NakedRoll(nw, "Weekly", weekFull, S.npoc_w_show, S.npoc_w_w, b.Start);
                weekFull = week != DateTime.MinValue || (b.Start.DayOfWeek == DayOfWeek.Sunday && MathEx.Minute(b.Start) == 1020);
                week = w; weekStart = b.Start; weekH = b.High; weekL = b.Low; nw = new Profile(NakedStep(rowMult), S.MaxProfileRows);
                weekOpen = b.Start.DayOfWeek == DayOfWeek.Sunday && MathEx.Minute(b.Start) == 1020 ? b.Open : double.NaN;
            }
            else { weekH = Max(weekH, b.High); weekL = Min(weekL, b.Low); }
            if (newMonth)
            {
                if (monthFull) { pmH = monthH; pmL = monthL; }
                NakedRoll(nm, "Monthly", monthFull, S.npoc_m_show, S.npoc_w_m, b.Start);
                monthFull = month != DateTime.MinValue; month = m; monthStart = b.Start; monthH = b.High; monthL = b.Low; nm = new Profile(NakedStep(rowMult), S.MaxProfileRows);
            }
            else { monthH = Max(monthH, b.High); monthL = Min(monthL, b.Low); }
            // Naked POC profiles use the 1-minute bars inside this chart bar, so they don't move
            // with the chart timeframe. The chart bar is the fallback when 1-minute data is missing.
            if (S.npoc_enable)
                foreach (Bar one in subbars != null && subbars.Count > 0 ? subbars : (IList<Bar>)new[] { b })
                { if (S.npoc_d_show) nd.Add(one, false); if (S.npoc_w_show) nw.Add(one, false); if (S.npoc_m_show) nm.Add(one, false); }
            for (int i = naked.Count - 1; i >= 0; i--) if (b.Low <= naked[i].Price && b.High >= naked[i].Price) { if (S.npoc_alert) Event(f, "nPOC" + naked[i].Kind + Price(naked[i].Price), naked[i].Kind + " nPOC Touch @ " + Price(naked[i].Price)); naked.RemoveAt(i); }
            if (ovn && S.amt_enable)
            {
                if (!Was(S.amt_sess) || newDay) { ovnH = b.High; ovnL = b.Low; }
                else { ovnH = Max(ovnH, b.High); ovnL = Min(ovnL, b.Low); }
            }
            if (pdSession)
            {
                if (!Was(S.pd_sess_time)) { rthH = b.High; rthL = b.Low; rthPreviousOpen = b.Open; }
                else { rthH = Max(rthH, b.High); rthL = Min(rthL, b.Low); }
            }
            if (!pdSession && Was(S.pd_sess_time)) { if (S.pd_use_rth) { pdH = rthH; pdL = rthL; } pdO = rthPreviousOpen; }
            if (daily != null) { if (!MathEx.Valid(pdH)) pdH = daily.High; if (!MathEx.Valid(pdL)) pdL = daily.Low; }
            if (!rth && Was("0830-1500"))
            {
                settle = previous.Close; settleStart = b.Start; trap = resolved = false;
            }
            bool globex = MathEx.Minute(b.Start) >= S.inv_start_h * 60 || MathEx.Minute(b.Start) < S.inv_end_h * 60 + S.inv_end_m;
            if (globex && MathEx.Valid(settle)) { if (b.Low >= settle) above++; else if (b.High <= settle) below++; total++; }
            bool imbalance = total > 0 && (above == total || below == total);
            if (imbalance && !resolved) trap = true;
            if (trap && b.Low <= settle && b.High >= settle) { trap = false; resolved = true; }
            if (S.inv_alert && S.inv_enable && trap && !trapAlert && MathEx.Minute(b.Start) >= S.inv_end_h * 60 + S.inv_end_m && MathEx.Minute(b.Start) < S.inv_end_h * 60 + S.inv_end_m + 30)
            { Event(f, "Inventory", "Inventory Trap Active @ " + Price(settle)); trapAlert = true; }
            if (ib)
            {
                if (!Was(S.ib_session) || newDay) { ibH = ibL = double.NaN; ibStart = b.Start; ibWorkH = b.High; ibWorkL = b.Low; }
                else { ibWorkH = Math.Max(ibWorkH, b.High); ibWorkL = Math.Min(ibWorkL, b.Low); }
            }
            if (!ib && Was(S.ib_session) && S.ib_enable) { ibH = ibWorkH; ibL = ibWorkL; }
            if (track && (!Was(S.context_session) || newDay)) inventoryFrozen = Inventory();
            if (MathEx.InSession(b.Start, S.cc_sess_def) && (!Was(S.cc_sess_def) || newDay)) rthO = b.Open;
            if (rth && (!Was("0830-1500") || newDay))
            {
                rthStart = b.Start; preH = preL = double.NaN; dayH = b.High; dayL = b.Low; auction = false; otfUp = otfDn = 0;
                double openPrice = MathEx.Valid(rthO) ? rthO : b.Open;
                isTrapDay = S.inv_enable && imbalance; gapUp = MathEx.Valid(pdH) && openPrice > pdH; gapDown = MathEx.Valid(pdL) && openPrice < pdL;
                outsideAbove = MathEx.Valid(prevDay.Vah) && openPrice > prevDay.Vah; outsideBelow = MathEx.Valid(prevDay.Val) && openPrice < prevDay.Val;
            }
            if (rth && !spikeWindow) { preH = Max(preH, b.High); preL = Min(preL, b.Low); }
            if (spikeWindow && !Was(S.spikeTimeRange)) { frozenH = preH; frozenL = preL; spikeStart = b.Start; }
            if (!spikeWindow && Was(S.spikeTimeRange))
            {
                spike = double.NaN;
                if (S.showSpike && previous != null) { bullSpike = previous.Close > frozenH; if (bullSpike || previous.Close < frozenL) spike = bullSpike ? frozenH : frozenL; }
            }
            if (!spikeWindow && MathEx.Valid(spike) && b.Low <= spike && b.High >= spike) { if (S.spike_alert) Event(f, "Spike", "Spike Base Touch @ " + Price(spike)); spike = double.NaN; }
            // All profile inputs are available sub-bars only. Source's upper-exclusive, 200-row cap is retained.
            List<Bar> raw = subbars != null && subbars.Count > 0 ? subbars.ToList() : new List<Bar> { b };
            int shared = 1;
            int precision; if (!int.TryParse(S.mvp_calc_tf, out precision) || precision < 1) precision = 1;
            int calc = precision != shared && Minutes > precision ? precision : shared;
            DateTime pk = ProfileKey(b); bool mainWindow = S.mvp_tf_mode != "Session" || MathEx.InSession(b.Start, S.mvp_sess_time);
            if (mainWindow && mainKey != pk)
            {
                if (main.Rows.Count > 0) { profiles.Add(new PastProfile { Profile = main, Stats = main.Stats(S.mvp_val_pct), Start = mainStart, End = b.Start, EndIndex = b.Index }); if (profiles.Count > 12) profiles.RemoveAt(0); }
                main = new Profile(RowStep(rowMult), S.MaxProfileRows); mainKey = pk; mainStart = b.Start;
            }
            foreach (Bar q in Aggregate(raw, calc)) { if (mainWindow) main.Add(q, true); if (S.cc_enable) day.Add(q, true); }
            ProfileStats dev = main.Stats(S.mvp_val_pct);
            UpdateTpo(b, spSession);
            contextHlc += (b.High + b.Low + b.Close) / 3 * b.Volume; contextVol += b.Volume;
            double dailyVwap = contextVol > 0 ? contextHlc / contextVol : double.NaN;
            int minute = MathEx.Minute(b.Start); bool vwin = minute >= S.vwap_start_h * 60 + S.vwap_start_m && minute < S.vwap_end_h * 60 + S.vwap_end_m;
            int pm = previous == null ? -1 : MathEx.Minute(previous.Start); bool pvwin = pm >= S.vwap_start_h * 60 + S.vwap_start_m && pm < S.vwap_end_h * 60 + S.vwap_end_m;
            if (vwin) { if (!pvwin || newDay) vSrc = vVol = 0; vSrc += (b.High + b.Low + b.Close) / 3 * b.Volume; vVol += b.Volume; }
            else vSrc = vVol = 0;
            f.RthVwap = S.vwap_enable && vwin && vVol > 0 ? vSrc / vVol : double.NaN;
            double dtr = daily == null ? double.NaN : Math.Max(tradeH - tradeL, Math.Max(Math.Abs(tradeH - daily.Close), Math.Abs(tradeL - daily.Close)));
            f.DailyAtr = daily != null && MathEx.Valid(daily.Atr) ? (daily.Atr * (S.daily_atr_length - 1) + dtr) / S.daily_atr_length : double.NaN;
            double zoneAtr = daily != null && MathEx.Valid(daily.Atr14) ? (daily.Atr14 * 13 + dtr) / 14 : double.NaN;
            if (rth) Classify(b, dailyVwap, f.DailyAtr);
            if (track)
            {
                Level pivot = Levels.FirstOrDefault(x => x.Kind == 0);
                if (pivot != null) { double z = (MathEx.Valid(f.DailyAtr) ? f.DailyAtr : 1) * .005; pivotBias = b.Close > pivot.Price + z ? "BULLISH" : b.Close < pivot.Price - z ? "BEARISH" : "AT PIVOT"; }
            }
            f.RuleActive = S.cc_enable && S.cc_showFill && MathEx.Valid(rthO) && (rthO > prevDay.Vah || rthO < prevDay.Val);
            if (f.RuleActive && pdSession)
            {
                if (b.Close < prevDay.Vah && b.Close > prevDay.Val)
                {
                    if (!vaEntry.HasValue) vaEntry = b.Start;
                    if (!rule && (b.Start - vaEntry.Value).TotalMinutes >= 60) { rule = true; if (S.cc_alert_80) Event(f, "Rule80", "80% Rule Confirmed"); }
                }
                else if (!rule) vaEntry = null;
            }
            if (!pdSession && !rule) vaEntry = null;
            f.RuleConfirmed = rule; f.VaHigh = prevDay.Vah; f.VaLow = prevDay.Val;
            double flip = MathEx.Valid(zoneAtr) ? zoneAtr * S.lvl_flip_atr : 0;
            foreach (Level l in Levels)
            {
                if (S.lvl_color_flip && l.Kind != 0)
                {
                    if (l.Kind == -1) { if (!l.Flipped && b.Close > l.Price + flip) l.Flipped = true; else if (l.Flipped && b.Close < l.Price - flip) l.Flipped = false; }
                    else { if (!l.Flipped && b.Close < l.Price - flip) l.Flipped = true; else if (l.Flipped && b.Close > l.Price + flip) l.Flipped = false; }
                }
                Cross(f, l.Name, l.Price, S.lvl_alerts && (l.Kind == 0 ? S.lvl_alert_ip : l.Kind == -1 ? S.lvl_alert_bull : S.lvl_alert_bear), false, LevelZoneHalfWidth(zoneAtr));
            }
            Cross(f, "IBH", ibH, S.ib_enable && S.ib_alert_ibh && !ib && MathEx.InSession(b.Start, S.ib_alert_time), false);
            Cross(f, "IBL", ibL, S.ib_enable && S.ib_alert_ibl && !ib && MathEx.InSession(b.Start, S.ib_alert_time), false);
            Cross(f, "OVN High", ovnH, S.amt_enable && S.amt_alert_ovn && !ovn, false); Cross(f, "OVN Low", ovnL, S.amt_enable && S.amt_alert_ovn && !ovn, false);
            Cross(f, "RTH VWAP", f.RthVwap, S.vwap_alert && vwin && pvwin, true);
            Cross(f, "PDH", pdH, S.pd_alert_hl, false); Cross(f, "PDL", pdL, S.pd_alert_hl, false);
            Cross(f, "pwHigh", pwH, S.pw_alert_hl, false); Cross(f, "pwLow", pwL, S.pw_alert_hl, false); Cross(f, "Weekly Open", weekOpen, S.wk_alert_open, false);
            Cross(f, "pdPOC", prevDay.Poc, S.cc_alert_va, false); Cross(f, "pdVAH", prevDay.Vah, S.cc_alert_va, false); Cross(f, "pdVAL", prevDay.Val, S.cc_alert_va, false);
            bool da = S.mvp_use_cur_alerts && MathEx.InSession(b.Start, S.mvp_alert_time);
            Cross(f, "Developing POC", dev.Poc, da, true); Cross(f, "Developing VAH", dev.Vah, da, true); Cross(f, "Developing VAL", dev.Val, da, true);
            Cross(f, "pmPOC", prevMonth.Poc, S.macro_alert_lvl, false); Cross(f, "pmVAH", prevMonth.Vah, S.macro_alert_lvl, false); Cross(f, "pmVAL", prevMonth.Val, S.macro_alert_lvl, false);
            Cross(f, "pmHigh", pmH, S.macro_alert_lvl, false); Cross(f, "pmLow", pmL, S.macro_alert_lvl, false);
            if (table && !Was(S.tbl_session)) { tableH = b.High; tableL = b.Low; } else if (table) { tableH = Math.Max(tableH, b.High); tableL = Math.Min(tableL, b.Low); }
            PruneStructures();
            rowsPruned |= new[] { day, main, macro, nd, nw, nm }.Any(p => p.PrunedRows > 0);
            BuildVisuals(f, dev, zoneAtr, table, ovn, ib);
            if (rowsPruned) f.Warning += " Row limit reached: oldest rows pruned; profile/TPO values use retained rows.";
            if (structuresPruned) f.Warning += " Structure limit reached: oldest structures pruned.";
            previous = b;
            return f;
        }
        private void UpdateTpo(Bar b, bool active)
        {
            bool was = Was(S.sp_rth_session), ended = !active && was;
            if (active && !was) { tpo.Clear(); tpoOrder.Clear(); single.RemoveAll(x => x.Live); tpoPeriod = -1; spStep = BlockStep(rowMult); }
            int pid = MathEx.Minute(b.Start) / Math.Max(1, S.sp_tpo_minutes);
            bool newPeriod = active && pid != tpoPeriod;
            if (active)
            {
                long lo = (long)Math.Floor(b.Low / spStep), hi = (long)Math.Floor(b.High / spStep);
                if (hi - lo + 1 > S.MaxProfileRows) { lo = hi - S.MaxProfileRows + 1; rowsPruned = true; }
                for (long k = lo; k <= hi; k++)
                {
                    TpoRow row;
                    if (!tpo.TryGetValue(k, out row)) { while (tpo.Count >= S.MaxProfileRows) { tpo.Remove(tpoOrder.Dequeue()); rowsPruned = true; } row = new TpoRow { Last = -1, First = b.Start }; tpo.Add(k, row); tpoOrder.Enqueue(k); }
                    if (row.Last != pid) { row.Hits++; row.Last = pid; }
                }
                tpoPeriod = pid;
            }
            if ((newPeriod || ended) && tpo.Count > 0)
            {
                single.RemoveAll(x => x.Live);
                long high = tpo.Keys.Max(), low = tpo.Keys.Min();
                var candidates = tpo.Where(x => x.Value.Hits == 1).Select(x => x.Key).OrderBy(x => x).ToList();
                int start = 0;
                for (int i = 0; i < candidates.Count; i++)
                {
                    if (i + 1 < candidates.Count && candidates[i + 1] <= candidates[i] + 2) continue;
                    long a = candidates[start], z = candidates[i]; double ht = (z - a) * spStep / Step;
                    bool tail = a <= low || z >= high;
                    if (ht + 1e-8 >= S.sp_min_ticks && !(S.sp_filter_tails && tail && ht < S.sp_tail_max))
                        for (int j = start; j <= i; j++) single.Add(new ZoneItem { Bottom = candidates[j] * spStep, Top = (candidates[j] + 1) * spStep, Start = tpo[candidates[j]].First, Created = b.Start, Color = S.sp_color_hist, BorderColor = S.sp_border_c, BorderWidth = S.sp_border_w, BorderStyle = S.sp_border_style, Live = !ended });
                    start = i + 1;
                }
                if (S.sp_show_poor)
                {
                    double hp = (high + 1) * spStep, lp = low * spStep;
                    if (tpo[high].Hits >= 2 && !poor.Any(x => x.High && Math.Abs(x.Price - hp) < Step * 2)) poor.Add(new Poor { High = true, Price = hp, Start = tpo[high].First, Created = b.Start });
                    if (tpo[low].Hits >= 2 && !poor.Any(x => !x.High && Math.Abs(x.Price - lp) < Step * 2)) poor.Add(new Poor { High = false, Price = lp, Start = tpo[low].First, Created = b.Start });
                }
            }
            single.RemoveAll(x => (!x.Live && (b.Start - x.Created).TotalDays > S.sp_max_days) || (x.Top > b.Low && x.Bottom < b.High));
            poor.RemoveAll(x => (b.Start - x.Created).TotalDays > S.sp_max_days || (x.High ? b.High > x.Price : b.Low < x.Price));
        }
        private void Classify(Bar b, double vwap, double dailyAtr)
        {
            double h = MathEx.Valid(ibH) ? ibH : ovnH, l = MathEx.Valid(ibL) ? ibL : ovnL;
            dayH = Max(dayH, b.High); dayL = Min(dayL, b.Low);
            if (previous != null) { otfUp = b.Low > previous.Low ? otfUp + 1 : 0; otfDn = b.High < previous.High ? otfDn + 1 : 0; }
            string candidate = "DEVELOPING"; uint color = AmtPalette.Gray; bool real = false;
            if (isTrapDay) { candidate = "TRAP"; color = AmtPalette.Orange; real = true; }
            else if (MathEx.Valid(h) && MathEx.Valid(l))
            {
                double range = dayH - dayL, ibRange = h - l, ext = ibRange > 0 ? range / ibRange : 0, pos = range > 0 ? (b.Close - dayL) / range : .5;
                bool bu = dayH > h, bd = dayL < l, cvh = b.Close > prevDay.Vah, cvl = b.Close < prevDay.Val;
                bool iu = dayH > pdH || (dayH > ovnH && cvh), id = dayL < pdL || (dayL < ovnL && cvl);
                bool tu = bu && pos > .75 && vwap > (h + l) / 2 && (otfUp >= 5 || iu) && ext > 1.5;
                bool tl = bd && pos < .25 && vwap < (h + l) / 2 && (otfDn >= 5 || id) && ext > 1.5;
                tu |= iu && pos > .8 && ext > 2; tl |= id && pos < .2 && ext > 2;
                real = true;
                if (tl) { candidate = "Trend Day (Down)"; color = AmtPalette.Red; }
                else if (tu) { candidate = "Trend Day (Up)"; color = AmtPalette.Green; }
                else if (bu && bd) { candidate = pos >= .65 ? "Neutral Extreme (Up)" : pos <= .35 ? "Neutral Extreme (Down)" : "Neutral Day (Center)"; color = pos >= .65 ? AmtPalette.GreenSoft : pos <= .35 ? AmtPalette.RedSoft : AmtPalette.Gray; }
                else if (dailyAtr > 0 && ibRange / dailyAtr > .5 && !bu && !bd) candidate = "Normal Day";
                else if (bu) { candidate = iu || ext > 1.2 || cvh ? "Normal Variation (Up)" : "Normal Day (Balance)"; color = candidate.Contains("Up") ? AmtPalette.Lime : AmtPalette.Gray; }
                else if (bd) { candidate = id || ext > 1.2 || cvl ? "Normal Variation (Down)" : "Normal Day (Balance)"; color = candidate.Contains("Down") ? AmtPalette.Maroon : AmtPalette.Gray; }
                else if (!MathEx.InSession(b.Start, "0830-0930") || MathEx.Valid(ibH)) candidate = "Non-Trend Day";
                else real = false;
            }
            if (real) { auction = true; dayType = candidate; dayColor = color; }
            else if (!auction) { dayType = gapUp ? "GAP UP" : gapDown ? "GAP DOWN" : "DEVELOPING"; dayColor = gapUp ? AmtPalette.Lime : gapDown ? AmtPalette.AlertRed : AmtPalette.Gray; }
        }
        // text: label text color; fully transparent (the default) means "use the line color".
        private void Line(Frame f, string name, double price, DateTime start, uint color, int width, string style, int offset, string size, bool label, string extend, uint text = 0)
        {
            if (MathEx.Valid(price)) f.Lines.Add(new LineItem { Name = name, Group = LabelGroup(name, extend), Price = price, Start = start, End = f.Bar.End, Color = color, TextColor = (text >> 24) == 0 ? color : text, Width = width, Style = style, Offset = offset, Size = size, Label = label && ShowGroupLabel(LabelGroup(name, extend)), Extend = extend });
        }
        private double LevelZoneHalfWidth(double zoneAtr) { return S.lvl_zone_mode == "ATR %" && MathEx.Valid(zoneAtr) ? zoneAtr * S.lvl_zone_atr_pct : S.lvl_zoneWidthTicks * Step; }
        private void PruneStructures()
        {
            int excess = naked.Count + poor.Count + single.Count - S.MaxStructures;
            if (excess <= 0) return;
            structuresPruned = true;
            // Across all three collections, chronological age determines eviction.
            var discard = new HashSet<object>(naked.Select(x => new { Item = (object)x, Time = x.Start })
                .Concat(poor.Select(x => new { Item = (object)x, Time = x.Created }))
                .Concat(single.Select(x => new { Item = (object)x, Time = x.Created }))
                .OrderBy(x => x.Time).Take(excess).Select(x => x.Item));
            naked.RemoveAll(x => discard.Contains(x)); poor.RemoveAll(x => discard.Contains(x)); single.RemoveAll(x => discard.Contains(x));
        }
        private static string LabelGroup(string name, string extend)
        {
            if (extend == "Both") return "Levels";
            if (name.StartsWith("IB ")) return "IB";
            if (name.StartsWith("OVN ")) return "OVN";
            if (name == "pdHigh" || name == "pdLow" || name == "pdOpen") return "PriorDay";
            if (name.StartsWith("pd")) return "PriorValue";
            if (name == "dOpen" || name == "wkOpen") return "Opens";
            if (name.StartsWith("pw")) return "PriorWeek";
            if (name.StartsWith("pm")) return "PriorMonth";
            if (name.StartsWith("Spike")) return "Spike";
            if (name.Contains("nPOC")) return "NakedPoc";
            if (name.StartsWith("Poor")) return "Poor";
            if (name.StartsWith("Developing") || name == "HVN") return "Profiles";
            return "Inventory";
        }
        private bool ShowGroupLabel(string group)
        {
            if (S.HideAllLabels) return false;
            switch (group)
            {
                case "Levels": return S.LabelsLevels;
                case "Profiles": return S.LabelsProfiles;
                case "PriorDay": return S.LabelsPriorDay;
                case "PriorValue": return S.LabelsPriorValue;
                case "Opens": return S.LabelsOpens;
                case "PriorWeek": return S.LabelsPriorWeek;
                case "PriorMonth": return S.LabelsPriorMonth;
                case "Inventory": return S.LabelsInventory;
                case "Spike": return S.LabelsSpike;
                case "NakedPoc": return S.LabelsNakedPoc;
                case "Poor": return S.LabelsPoor;
                case "IB": return S.ib_show_labels;
                case "OVN": return S.amt_showLb;
                default: return true;
            }
        }
        // Inventory and open-location colors, matching the TradingView dashboard.
        private static uint StatusColor(string s)
        {
            if (s.StartsWith("TRAP LONG") || s == "Below pdLow") return AmtPalette.AlertRed;
            if (s.StartsWith("TRAP SHORT") || s == "Above pdHigh") return AmtPalette.Lime;
            if (s.StartsWith("LONG") || s == "Above pdVAH") return AmtPalette.GreenSoft;
            if (s.StartsWith("SHORT") || s == "Below pdVAL") return AmtPalette.RedSoft;
            return AmtPalette.Gray;
        }
        private string OpenLocation()
        {
            if (!MathEx.Valid(rthO) || !MathEx.Valid(pdH) || !MathEx.Valid(pdL)) return "---";
            return rthO > pdH ? "Above pdHigh" : rthO < pdL ? "Below pdLow" : rthO > prevDay.Vah ? "Above pdVAH" : rthO < prevDay.Val ? "Below pdVAL" : MathEx.Valid(prevDay.Vah) ? "Inside Value" : "---";
        }
        private void BuildVisuals(Frame f, ProfileStats dev, double zoneAtr, bool table, bool ovn, bool ib)
        {
            DateTime now = f.Bar.Start;
            if (S.ib_enable && MathEx.Valid(ibH))
            {
                DateTime end = f.Bar.End;
                if (S.ib_limit_ext && !MathEx.InSession(now, S.ib_stop_time))
                {
                    string t = S.ib_stop_time.Split(':')[0].Split('-')[1]; end = ibStart.Date.AddHours(int.Parse(t.Substring(0, 2))).AddMinutes(int.Parse(t.Substring(2)));
                }
                int n = f.Lines.Count;
                Line(f, "IB High", ibH, ibStart, S.ib_col_hi, S.ib_lvl_width, S.ib_style_main, S.ib_lbl_offset, S.ib_label_size, S.ib_show_labels, S.ib_extend, S.ib_txt_c);
                Line(f, "IB Low", ibL, ibStart, S.ib_col_lo, S.ib_lvl_width, S.ib_style_main, S.ib_lbl_offset, S.ib_label_size, S.ib_show_labels, S.ib_extend, S.ib_txt_c);
                if (S.ib_show_mid) Line(f, "IB Midpoint", (ibH + ibL) / 2, ibStart, S.ib_col_mid, S.ib_lvl_width, S.ib_style_int, S.ib_lbl_offset, S.ib_label_size, S.ib_show_labels, S.ib_extend, S.ib_txt_c);
                for (int i = n; i < f.Lines.Count; i++) f.Lines[i].End = end;
            }
            if (S.amt_enable && !ovn && MathEx.Minute(now) < int.Parse(S.amt_del_sess.Substring(0, 2)) * 60 + int.Parse(S.amt_del_sess.Substring(2, 2)))
            {
                Line(f, "OVN High", ovnH, rthStart, S.amt_c_hi, S.amt_width, S.amt_style_in, S.amt_offset, S.amt_lb_size_in, S.amt_showLb, "None");
                Line(f, "OVN Low", ovnL, rthStart, S.amt_c_lo, S.amt_width, S.amt_style_in, S.amt_offset, S.amt_lb_size_in, S.amt_showLb, "None");
                foreach (LineItem l in f.Lines.Where(x => x.Name.StartsWith("OVN"))) l.TextColor = l.Name.EndsWith("High") ? S.amt_c_txt_hi : S.amt_c_txt_lo;
            }
            if (S.pd_enable)
            {
                if (S.pd_showH) Line(f, "pdHigh", pdH, dayStart, S.pd_high_c, S.pd_high_width, S.pd_style_in, S.pd_offset_val, S.pd_lb_size_in, true, "None", S.pd_txt_c);
                if (S.pd_showL) Line(f, "pdLow", pdL, dayStart, S.pd_low_c, S.pd_low_width, S.pd_style_in, S.pd_offset_val, S.pd_lb_size_in, true, "None", S.pd_txt_c);
                if (S.pd_showOpen) Line(f, "pdOpen", pdO, dayStart, S.pd_open_c, S.pd_open_width, S.pd_style_in, S.pd_offset_val, S.pd_lb_size_in, true, "None", S.pd_txt_c);
            }
            if (S.cc_enable)
            {
                if (S.cc_show_poc) Line(f, "pdPOC", prevDay.Poc, dayStart, S.pd_poc_c, S.cc_poc_width, S.cc_poc_style_in, S.cc_lb_offset, S.cc_lb_size_in, true, "None", S.cc_txt_c);
                if (S.cc_show_vah) Line(f, "pdVAH", prevDay.Vah, dayStart, S.pd_vah_c, S.cc_vah_width, S.cc_poc_style_in, S.cc_lb_offset, S.cc_lb_size_in, true, "None", S.cc_txt_c);
                if (S.cc_show_val) Line(f, "pdVAL", prevDay.Val, dayStart, S.pd_val_c, S.cc_val_width, S.cc_poc_style_in, S.cc_lb_offset, S.cc_lb_size_in, true, "None", S.cc_txt_c);
            }
            if (S.cc_showOpen) Line(f, "dOpen", rthO, rthStart, S.cc_c_open, S.cc_open_width, S.cc_open_style, S.cc_open_offset, S.cc_open_lb_size, true, "None", S.opens_txt_c);
            if (S.wk_showOpen) Line(f, "wkOpen", weekOpen, weekStart, S.wk_c_open, S.wk_open_width, S.wk_open_style, S.wk_open_offset, S.wk_open_lb_size, true, "None", S.opens_txt_c);
            if (S.pw_enable)
            {
                if (S.pw_showH) Line(f, "pwHigh", pwH, weekStart, S.pw_col, S.pw_width, S.pw_style_in, S.pw_offset, S.pw_lb_size_in, true, "None");
                if (S.pw_showL) Line(f, "pwLow", pwL, weekStart, S.pw_col, S.pw_width, S.pw_style_in, S.pw_offset, S.pw_lb_size_in, true, "None");
                foreach (LineItem l in f.Lines.Where(x => x.Name.StartsWith("pw"))) l.TextColor = S.pw_c_txt;
            }
            if (S.macro_show_month) { Line(f, "pmVAH", prevMonth.Vah, monthStart, S.macro_color, S.macro_width, S.macro_style, S.macro_offset, S.macro_text_size, true, "None", S.macro_txt_c); Line(f, "pmVAL", prevMonth.Val, monthStart, S.macro_color, S.macro_width, S.macro_style, S.macro_offset, S.macro_text_size, true, "None", S.macro_txt_c); }
            if (S.macro_show_poc) Line(f, "pmPOC", prevMonth.Poc, monthStart, S.macro_poc_color, S.macro_poc_width, S.macro_poc_style, S.macro_offset, S.macro_text_size, true, "None", S.macro_txt_c);
            if (S.macro_show_hl) { Line(f, "pmHigh", pmH, monthStart, S.macro_color, S.macro_width, S.macro_style, S.macro_offset, S.macro_text_size, true, "None", S.macro_txt_c); Line(f, "pmLow", pmL, monthStart, S.macro_color, S.macro_width, S.macro_style, S.macro_offset, S.macro_text_size, true, "None", S.macro_txt_c); }
            if (S.inv_enable && S.inv_show_line) Line(f, trap ? S.inv_txt_trap : "SETTLE", settle, settleStart, trap ? S.inv_c_trap : S.inv_c_settle, trap ? S.inv_width : S.inv_settle_width, trap ? S.inv_style_in : S.inv_settle_style, S.inv_offset, S.inv_lb_size_in, true, "Right", S.inv_txt_c);
            if (S.showSpike) Line(f, bullSpike ? "Spike Base (Supp)" : "Spike Base (Res)", spike, spikeStart, bullSpike ? S.spike_c_sup : S.spike_c_res, S.spike_width, S.spike_style, S.spike_offset, S.spike_lbl_size, true, "None", S.spike_txt_c);
            foreach (Naked n in naked) Line(f, n.Kind + " nPOC", n.Price, n.Start, n.Kind == "Weekly" ? S.npoc_c_w : n.Kind == "Monthly" ? S.npoc_c_m : S.npoc_c, n.Width, S.npoc_style_in, S.npoc_lb_offset, S.npoc_lb_size, true, "Right", S.npoc_txt_c);
            foreach (Poor p in poor) Line(f, p.High ? "Poor High" : "Poor Low", p.Price, p.Start, p.High ? S.sp_col_poor_h : S.sp_col_poor_l, S.sp_poor_width, S.sp_poor_style_in, 2, S.sp_poor_lb_size, true, "None", S.sp_poor_txt_c);
            f.Zones.AddRange(single);
            double hz = LevelZoneHalfWidth(zoneAtr);
            foreach (Level l in Levels)
            {
                uint color = l.Kind == 0 ? S.lvl_ip_color : (l.Kind == 1 ^ (S.lvl_color_flip && l.Flipped)) ? S.lvl_bear_color : S.lvl_bull_color;
                string name = (S.lvl_color_flip && l.Flipped ? "FLIPPED " : "") + l.Name;
                Line(f, name, l.Price, DateTime.MinValue, color, S.lvl_showLine ? S.lvl_thickness : 0, S.lvl_style_in, S.lvl_x_offset, S.lvl_text_size_opt, true, "Both", S.lvl_txt_c);
                if (S.lvl_showZones)
                {
                    f.Zones.Add(new ZoneItem { Top = l.Price + hz, Bottom = l.Price - hz, Start = DateTime.MinValue, Color = (color & 0xffffff) | ((uint)(255 * (100 - S.lvl_zoneOpacity) / 100) << 24) });
                    Line(f, "", l.Price + hz, DateTime.MinValue, color, 1, S.lvl_zone_style_in, 0, "Small", false, "Both");
                    Line(f, "", l.Price - hz, DateTime.MinValue, color, 1, S.lvl_zone_style_in, 0, "Small", false, "Both");
                }
            }
            if (S.mvp_master)
            {
                f.Profiles.AddRange(profiles.Where(x => f.Bar.Index - x.EndIndex < 4500));
                f.Profiles.Add(new PastProfile { Profile = main, Stats = dev, Start = mainStart, End = f.Bar.End, EndIndex = f.Bar.Index });
                if (S.mvp_show_poc) Line(f, "Developing POC", dev.Poc, mainStart, S.mvp_c_poc, S.mvp_poc_width, S.mvp_poc_style, S.mvp_offset, S.mvp_lb_size, true, "None", S.mvp_txt_c);
                if (S.mvp_show_vah) Line(f, "Developing VAH", dev.Vah, mainStart, S.mvp_c_levels, S.mvp_lvl_width, S.mvp_lvl_style, S.mvp_offset, S.mvp_lb_size, true, "None", S.mvp_txt_c);
                if (S.mvp_show_val) Line(f, "Developing VAL", dev.Val, mainStart, S.mvp_c_levels, S.mvp_lvl_width, S.mvp_lvl_style, S.mvp_offset, S.mvp_lb_size, true, "None", S.mvp_txt_c);
                if (S.mvp_show_walls && main.Rows.Count > S.mvp_smooth_len + 1)
                {
                    var keys = main.Rows.Keys.OrderBy(x => x).ToArray(); double avg = main.Rows.Values.Average();
                    double duration = S.mvp_tf_mode == "Session" ? S.mvp_sess_dur / 24 : S.mvp_tf_mode == "Weekly" ? 7 : S.mvp_tf_mode == "Yearly" ? 365.25 : 30.44;
                    double progress = Math.Min(1, Math.Max(0, (now - mainStart).TotalDays / Math.Max(duration, .001)));
                    double mult = S.mvp_use_auto_scale ? S.mvp_scale_start + progress * (S.mvp_scale_end - S.mvp_scale_start) : S.mvp_vol_mult;
                    bool inside = false; var walls = new List<double>();
                    for (int i = 0; i < keys.Length; i++)
                    {
                        double sum = 0; int count = 0, half = S.mvp_smooth_len / 2;
                        for (int j = Math.Max(0, i - half); j <= Math.Min(keys.Length - 1, i + half); j++) { sum += main.Rows[keys[j]]; count++; }
                        bool structure = sum / count > avg * mult;
                        if (structure != inside)
                        {
                            double p = keys[structure ? i : Math.Max(0, i - 1)] * main.Step;
                            if (!walls.Any(x => Math.Abs(x - p) < S.mvp_min_wall_dist * main.Step)) { walls.Add(p); Line(f, "HVN", p, mainStart, (S.mvp_c_wall_base & 0xffffff) | ((uint)(255 * (100 - S.mvp_wall_transp) / 100) << 24), S.mvp_w_wall, S.mvp_wall_style, 0, "Small", false, S.mvp_extend_walls ? "Right" : "None"); }
                            inside = structure;
                        }
                    }
                }
            }
            string loc = OpenLocation(); double range = table ? tableH - tableL : 0, used = f.DailyAtr > 0 ? range / f.DailyAtr * 100 : 0;
            f.Labels = new[] { "SESSION", "DAY TYPE", "PIVOT", "OVN INVENTORY", "OPEN", "ATR(" + S.daily_atr_length + ")", "ATR USED", "80% RULE" };
            f.Values = new[] { table ? "RTH" : ovn ? "OVN" : "CLOSED", table ? dayType : "---", table ? pivotBias : "---", table ? inventoryFrozen : Inventory(), table ? loc : "---", table && MathEx.Valid(f.DailyAtr) ? f.DailyAtr.ToString("0.00") : "---", table ? range.ToString("0.00") + " (" + used.ToString("0") + "%)" : "---", table && S.cc_enable && (outsideAbove || outsideBelow) ? rule ? "CONFIRMED" : f.RuleActive ? "ACTIVE" : "SETUP" : "---" };
            // Text colors mirror the TradingView dashboard cell by cell.
            f.Colors = new[] { table ? AmtPalette.Lime : ovn ? AmtPalette.Aqua : AmtPalette.Gray, table ? dayColor : AmtPalette.Gray,
                f.Values[2] == "BULLISH" ? AmtPalette.Green : f.Values[2] == "BEARISH" ? AmtPalette.Red : f.Values[2] == "AT PIVOT" ? AmtPalette.Yellow : AmtPalette.Gray,
                StatusColor(f.Values[3]), StatusColor(f.Values[4]), AmtPalette.Gray,
                !table ? AmtPalette.Gray : used >= 100 ? AmtPalette.AlertRed : used >= 75 ? AmtPalette.Orange : AmtPalette.Gray,
                f.Values[7] == "CONFIRMED" ? AmtPalette.Pink : f.Values[7] == "ACTIVE" ? AmtPalette.Orange : f.Values[7] == "SETUP" ? AmtPalette.Yellow : AmtPalette.Gray };
            if (!MathEx.Valid(f.DailyAtr)) f.Warning = "Loading daily ATR: load at least 30 trading days.";
            if (!dayFull || !weekFull || !monthFull || !macroFull) f.Warning += " First partial profile period is warming up.";
        }
    }
}





namespace TatankaTrading.AmtToolkit
{
    // Single producer; published arrays are never modified. Intrabar previews reuse
    // the last completed-bar array instead of copying the entire chart history.
    public sealed class RenderHistory<T>
    {
        private readonly int limit;
        private readonly Queue<T> items = new Queue<T>();
        private T[] published;
        public RenderHistory(int capacity) { if (capacity < 1) throw new ArgumentOutOfRangeException("capacity"); limit = capacity; }
        public void Add(T item)
        {
            if (items.Count == limit) items.Dequeue();
            items.Enqueue(item); published = null;
        }
        public T[] Snapshot() { return published ?? (published = items.ToArray()); }
    }

    public sealed class ProfileHistogram
    {
        public readonly double[] Prices, Volumes;
        public readonly double Step, Vah, Val, Max;
        public readonly DateTime End;
        public readonly bool Current;
        public ProfileHistogram(PastProfile source, bool current)
        {
            long[] keys = source.Profile.Rows.Keys.ToArray(); Array.Sort(keys);
            Prices = new double[keys.Length]; Volumes = new double[keys.Length];
            Step = source.Profile.Step; Vah = source.Stats.Vah; Val = source.Stats.Val;
            End = source.End; Current = current;
            for (int i = 0; i < keys.Length; i++)
            {
                Prices[i] = keys[i] * Step; Volumes[i] = source.Profile.Rows[keys[i]];
                Max = Math.Max(Max, Volumes[i]);
            }
        }
    }

    // Engine never edits completed PastProfile objects. Cache only the completed
    // profiles in the current frame (at most 12); the developing one is rebuilt.
    public sealed class ProfileHistogramCache
    {
        private Dictionary<PastProfile, ProfileHistogram> completed = new Dictionary<PastProfile, ProfileHistogram>();
        public int Count { get { return completed.Count; } }
        public ProfileHistogram[] Build(IList<PastProfile> profiles)
        {
            var next = new Dictionary<PastProfile, ProfileHistogram>();
            var result = new ProfileHistogram[profiles.Count];
            for (int i = 0; i < profiles.Count; i++)
            {
                bool current = i == profiles.Count - 1;
                ProfileHistogram histogram;
                if (current || !completed.TryGetValue(profiles[i], out histogram))
                    histogram = new ProfileHistogram(profiles[i], current);
                result[i] = histogram;
                if (!current) next[profiles[i]] = histogram;
            }
            completed = next;
            return result;
        }
    }
}










namespace TatankaTrading.AmtToolkit
{
    public sealed class LevelsDocument
    {
        public List<Level> Levels = new List<Level>();
        public DateTime? Date;
        public string Symbol;
        // Discord paste line, Sierra Chart's own format: "YYYY-MM-DD, pivot, prices...".
        private static readonly Regex PasteLine = new Regex(@"^(\d{4}-\d{2}-\d{2})\s*,(.*)$");
        // If/Then band lines belong to the band parser, never to the levels parser.
        private static readonly Regex BandLine = new Regex(@"^[UVND]\|");
        public static LevelsDocument Parse(string text, bool allowEmpty)
        {
            var result = new LevelsDocument();
            if (string.IsNullOrWhiteSpace(text))
            {
                if (!allowEmpty) throw new ArgumentException("The levels file is empty.");
                return result;
            }
            if (text.Length > 1048576) throw new ArgumentException("Levels input exceeds 1 MB.");
            var found = new HashSet<int>(); var labeled = new List<Level>();
            DateTime? headerDate = null; string paste = null, labeledError = null;
            // Split order: real line breaks, then "||" (the If/Then one-liner).
            foreach (string raw in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n').SelectMany(x => x.Split(new[] { "||" }, StringSplitOptions.None)))
            {
                // A Discord mobile copy brings the whole message: title, code fences, bold markers.
                string line = raw.Trim().Trim('\uFEFF', '`', '*', ' ', '\t');
                if (line.Length == 0 || line.StartsWith("#") || BandLine.IsMatch(line)) continue;
                if (PasteLine.IsMatch(line))
                {
                    if (paste != null) throw new ArgumentException("Use one paste line per symbol.");
                    paste = line; continue;
                }
                Match date = Regex.Match(line, @"^(?:Date|Trading Date)\s*:\s*(.*)$", RegexOptions.IgnoreCase);
                if (date.Success)
                {
                    DateTime parsed;
                    if (headerDate.HasValue || !DateTime.TryParseExact(date.Groups[1].Value.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                        throw new ArgumentException("Use one Date: YYYY-MM-DD line.");
                    headerDate = parsed.Date; continue;
                }
                Match symbol = Regex.Match(line, @"^Symbol\s*:\s*([A-Za-z0-9]+)\s*$", RegexOptions.IgnoreCase);
                if (symbol.Success)
                {
                    if (result.Symbol != null) throw new ArgumentException("Use one Symbol: line.");
                    result.Symbol = symbol.Groups[1].Value.ToUpperInvariant(); continue;
                }
                Match m = Regex.Match(line, @"^(Pivot Level|Resistance Levels|Support Levels)\s*:\s*(.*)$", RegexOptions.IgnoreCase);
                if (!m.Success) continue; // Titles, report headings and commentary are deliberately ignored.
                // Labeled-block problems only matter when there is no paste line (the paste line wins).
                if (labeledError != null) continue;
                labeledError = ParseLabeled(m, found, labeled);
            }
            if (paste != null)
            {
                ParsePaste(paste, result);
                if (headerDate.HasValue && headerDate.Value != result.Date.Value) throw new ArgumentException("The Date: line and the paste-line date differ.");
            }
            else
            {
                if (labeledError != null) throw new ArgumentException(labeledError);
                if (found.Count == 0) throw new ArgumentException("No levels found. Paste the Discord line (2026-09-30, pivot, resistance..., support...) or the Pivot Level / Resistance Levels / Support Levels block.");
                if (found.Count != 3) throw new ArgumentException("Include Pivot Level:, Resistance Levels:, and Support Levels:.");
                result.Levels = labeled; result.Date = headerDate;
            }
            // A thousands separator splits one price into two, e.g. "7,745.25" becomes 7 and 745.25.
            Level pivotLevel = result.Levels.First(x => x.Kind == 0);
            foreach (Level l in result.Levels)
                if (Math.Abs(l.Price - pivotLevel.Price) > Math.Abs(pivotLevel.Price) * .2)
                    throw new ArgumentException("Price " + l.Price.ToString(CultureInfo.InvariantCulture) + " is more than 20% from the pivot. Write prices without thousands separators.");
            return result;
        }
        private static string ParseLabeled(Match m, HashSet<int> found, List<Level> labeled)
        {
            int kind = m.Groups[1].Value.StartsWith("Pivot", StringComparison.OrdinalIgnoreCase) ? 0 : m.Groups[1].Value.StartsWith("Resistance", StringComparison.OrdinalIgnoreCase) ? -1 : 1;
            if (!found.Add(kind)) return "Duplicate levels section: " + m.Groups[1].Value;
            string[] values = m.Groups[2].Value.Split(',');
            if (kind == 0 && values.Length != 1) return "Pivot requires one price without thousands separators.";
            if (kind != 0 && values.Length > 50) return "Maximum 50 prices in each support/resistance section.";
            for (int i = 0; i < values.Length; i++)
            {
                double price;
                if (!double.TryParse(values[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out price) || !MathEx.Valid(price))
                    return "Invalid price in " + m.Groups[1].Value + ". Use decimal prices separated by commas.";
                labeled.Add(new Level { Kind = kind, Price = price, Name = kind == 0 ? "PIVOT" : (kind == 1 ? "SUPP" : "RES") + (i + 1) });
            }
            return null;
        }
        // First price is the pivot; above it is resistance, below it support, numbered outward.
        private static void ParsePaste(string line, LevelsDocument result)
        {
            Match p = PasteLine.Match(line);
            DateTime date;
            if (!DateTime.TryParseExact(p.Groups[1].Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                throw new ArgumentException("The paste line must start with a YYYY-MM-DD date.");
            var prices = new List<double>();
            foreach (string item in p.Groups[2].Value.Split(','))
            {
                double price;
                if (!double.TryParse(item.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out price) || !MathEx.Valid(price))
                    throw new ArgumentException("Invalid price in the paste line: '" + item.Trim() + "'. Use decimal prices separated by commas.");
                prices.Add(price);
            }
            double pivot = prices[0];
            if (prices.Skip(1).Any(x => Math.Abs(x - pivot) > Math.Abs(pivot) * .2))
                throw new ArgumentException("A paste-line price is more than 20% from the pivot. Write prices without thousands separators.");
            if (prices.Skip(1).Any(x => x == pivot)) throw new ArgumentException("A paste-line price equals the pivot.");
            List<double> res = prices.Skip(1).Where(x => x > pivot).OrderBy(x => x).ToList(), sup = prices.Skip(1).Where(x => x < pivot).OrderByDescending(x => x).ToList();
            if (res.Count == 0 || sup.Count == 0) throw new ArgumentException("The paste line needs at least one price above and one below the pivot.");
            if (res.Count > 50 || sup.Count > 50) throw new ArgumentException("Maximum 50 prices on each side of the pivot.");
            result.Date = date.Date;
            result.Levels.Add(new Level { Kind = 0, Price = pivot, Name = "PIVOT" });
            for (int i = 0; i < res.Count; i++) result.Levels.Add(new Level { Kind = -1, Price = res[i], Name = "RES" + (i + 1) });
            for (int i = 0; i < sup.Count; i++) result.Levels.Add(new Level { Kind = 1, Price = sup[i], Name = "SUPP" + (i + 1) });
        }
        public string Serialize()
        {
            if (Levels.Count == 0) return "";
            string result = (Symbol != null ? "Symbol: " + Symbol + "\n" : "") + (Date.HasValue ? "Date: " + Date.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "\n" : "");
            foreach (int kind in new[] { 0, -1, 1 })
                result += (kind == 0 ? "Pivot Level: " : kind == -1 ? "Resistance Levels: " : "Support Levels: ") + string.Join(", ", Levels.Where(x => x.Kind == kind).Select(x => x.Price.ToString("R", CultureInfo.InvariantCulture))) + "\n";
            return result;
        }
    }

    // No parse/read error escapes into the indicator's fatal error handler.
    // Each resolved instrument/path has a separate last-good cache.
    public sealed class LevelFileSource
    {
        public LevelsDocument Current = new LevelsDocument();
        public string ResolvedPath = "", Error = "", CacheWarning = "";
        public int Revision { get; private set; }
        // Chart price used to catch another symbol's levels; NaN skips the check.
        public double ReferencePrice = double.NaN;
        private string cachePath = "", signature = "", root = "";
        private TimeZoneInfo central;
        public static string ResolvePath(string template, string root)
        {
            if (!Regex.IsMatch(root ?? "", @"^[A-Za-z0-9_-]+$")) throw new ArgumentException("Unsupported instrument root in levels path.");
            string value = Environment.ExpandEnvironmentVariables((template ?? "").Trim().Trim('"'));
            value = Regex.Replace(value, @"\{root\}", delegate(Match m) { return root; }, RegexOptions.IgnoreCase);
            if (!Path.IsPathRooted(value)) throw new ArgumentException("Use a full path for the levels file.");
            return Path.GetFullPath(value);
        }
        public void Initialize(string template, string root, string paste, string cacheDirectory, TimeZoneInfo ct)
        {
            central = ct; this.root = root ?? "";
            if (string.IsNullOrWhiteSpace(template))
            {
                try { Accept(LevelsDocument.Parse(paste, true)); Error = ""; }
                catch (Exception ex) { Error = "Pasted levels rejected; no daily levels loaded. " + ex.Message; }
                return;
            }
            // A valid pasted block is a fallback when no file/cache has ever loaded.
            try { Accept(LevelsDocument.Parse(paste, true)); } catch (ArgumentException) { }
            try
            {
                ResolvedPath = ResolvePath(template, root);
                using (var sha = SHA256.Create())
                    cachePath = Path.Combine(cacheDirectory, BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(root.ToUpperInvariant() + "|" + ResolvedPath.ToUpperInvariant()))).Replace("-", "") + ".txt");
                if (File.Exists(cachePath))
                {
                    try { Accept(LevelsDocument.Parse(ReadLimited(cachePath), false)); }
                    catch (Exception ex) { CacheWarning = "Last-good cache not used: " + ex.Message; }
                }
                Refresh();
            }
            catch (Exception ex) { Error = "Levels file unavailable; " + Retained() + " " + ex.Message; }
        }
        private string Retained() { return Current.Levels.Count > 0 ? "last valid levels retained." : "structure features remain active without daily levels."; }
        private static string ReadLimited(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                if (stream.Length > 1048576) throw new ArgumentException("Levels file exceeds 1 MB.");
                using (var reader = new StreamReader(stream, Encoding.UTF8, true))
                {
                    var text = new StringBuilder(); char[] data = new char[4096]; int count;
                    while ((count = reader.Read(data, 0, data.Length)) > 0)
                    {
                        // Keep enforcing the limit if another process grows the file
                        // after the initial length check.
                        if (text.Length > 1048576 - count) throw new ArgumentException("Levels file exceeds 1 MB.");
                        text.Append(data, 0, count);
                    }
                    return text.ToString();
                }
            }
        }
        private void Accept(LevelsDocument document)
        {
            Validate(document);
            string next = document.Serialize();
            if (next == signature) return;
            Current = document; signature = next; Revision++;
        }
        // Wrong-symbol guards: an explicit Symbol: line, then the pivot's distance from price.
        private void Validate(LevelsDocument document)
        {
            if (document.Symbol != null && root.Length != 0 && !string.Equals(MathEx.LevelsRoot(document.Symbol), root, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Levels are for " + document.Symbol + "; this chart reads " + root + " levels.");
            Level pivot = document.Levels.FirstOrDefault(x => x.Kind == 0);
            if (pivot != null && MathEx.Valid(ReferencePrice) && ReferencePrice > 0 && Math.Abs(pivot.Price / ReferencePrice - 1) > .2)
                throw new ArgumentException("Pivot " + pivot.Price.ToString(CultureInfo.InvariantCulture) + " is more than 20% from the chart price " + ReferencePrice.ToString(CultureInfo.InvariantCulture) + "; these look like another symbol's levels.");
        }
        public bool Refresh()
        {
            if (ResolvedPath.Length == 0) return false;
            int before = Revision;
            try
            {
                DateTime write = File.GetLastWriteTimeUtc(ResolvedPath);
                var document = LevelsDocument.Parse(ReadLimited(ResolvedPath), false);
                if (write != File.GetLastWriteTimeUtc(ResolvedPath)) throw new IOException("File changed while being read; retrying on the next refresh.");
                if (!document.Date.HasValue) document.Date = MathEx.TradingDate(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(write, DateTimeKind.Utc), central));
                Accept(document); Error = "";
                if (before != Revision || !File.Exists(cachePath)) SaveCache();
            }
            catch (Exception ex) { Error = "Levels file rejected; " + Retained() + " " + ex.Message; }
            return Revision != before;
        }
        private void SaveCache()
        {
            string temp = cachePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(cachePath));
                File.WriteAllText(temp, Current.Serialize(), new UTF8Encoding(false));
                if (File.Exists(cachePath)) File.Replace(temp, cachePath, null); else File.Move(temp, cachePath);
                CacheWarning = "";
            }
            catch (Exception) { CacheWarning = "Levels loaded; last-good cache could not be saved."; }
            finally { try { if (File.Exists(temp)) File.Delete(temp); } catch (IOException) { } }
        }
        public string Warning(DateTime tradingDate)
        {
            // A later date is normal: the 4:45 PM Globex publish carries the next trading date.
            string stale = Current.Date.HasValue && Current.Date.Value.Date < tradingDate.Date ?
                "STALE LEVELS: levels date " + Current.Date.Value.ToString("yyyy-MM-dd") + "; chart trading date " + tradingDate.ToString("yyyy-MM-dd") + "." : "";
            return (stale + " " + Error + " " + CacheWarning).Trim();
        }
    }
}





namespace TatankaTrading.AmtToolkit
{
    public sealed class LevelWatch
    {
        public string Name;
        public double Price, HalfWidth;
        public bool Enabled, Moving;
    }
    // Live observations, independent of the engine's speculative bar clones.
    public sealed class LevelAlertTracker
    {
        private sealed class Point { public double Close, Center, Width; public bool Enabled; }
        private readonly Dictionary<string, Point> points = new Dictionary<string, Point>();
        public void Reset() { points.Clear(); }
        public List<Notice> Observe(IEnumerable<LevelWatch> levels, double close, string mode, string root, double tick)
        {
            var result = new List<Notice>(); var present = new HashSet<string>();
            foreach (LevelWatch l in levels)
            {
                present.Add(l.Name); Point p;
                if (points.TryGetValue(l.Name, out p) && p.Enabled && l.Enabled && (l.Moving || Math.Abs(p.Center - l.Price) < tick * .001))
                {
                    if (mode == "Cross" || mode == "Both")
                    {
                        if (p.Close <= p.Center && close > l.Price) result.Add(new Notice { Key = l.Name + "Up", Text = l.Name + " Cross Up @ " + MathEx.Price(l.Price, root, tick) });
                        if (p.Close >= p.Center && close < l.Price) result.Add(new Notice { Key = l.Name + "Down", Text = l.Name + " Cross Down @ " + MathEx.Price(l.Price, root, tick) });
                    }
                    bool wasInside = Math.Abs(p.Close - p.Center) <= p.Width;
                    bool inside = Math.Abs(close - l.Price) <= l.HalfWidth;
                    if ((mode == "Zone Entry" || mode == "Both") && !wasInside && inside)
                        result.Add(new Notice { Key = l.Name + "Zone", Text = l.Name + " Zone Entry @ " + MathEx.Price(l.Price, root, tick) + " (±" + MathEx.Price(l.HalfWidth, root, tick) + ")" });
                }
                points[l.Name] = new Point { Close = close, Center = l.Price, Width = l.HalfWidth, Enabled = l.Enabled };
            }
            foreach (string key in points.Keys.Where(k => !present.Contains(k)).ToArray()) points.Remove(key);
            return result;
        }
    }
}










namespace TatankaTrading.AmtToolkit
{
    // Optional notification transport only. Never called from historical calculation.
    // Background I/O, bounded backlog, shared per-webhook pacing, no account/order data.
    public sealed class DiscordAlerts : IDisposable
    {
        public sealed class Result { public int Status; public double RetrySeconds; }
        private sealed class Gate { public int Users; public DateTime Next; }
        private static readonly object gatesLock = new object();
        private static readonly Dictionary<string, Gate> gates = new Dictionary<string, Gate>();
        private readonly object sync = new object();
        private readonly Queue<string> queue = new Queue<string>();
        private readonly ManualResetEventSlim stop = new ManualResetEventSlim(false);
        private readonly string endpoint;
        private readonly Gate gate;
        private readonly Func<string, string, Result> transport;
        private bool running, disposed;
        private HttpWebRequest active;
        private volatile string warning = "";
        public string Warning { get { return warning; } }
        public DiscordAlerts(string url) : this(url, null) { }
        public DiscordAlerts(string url, Func<string, string, Result> testTransport)
        {
            endpoint = ValidateEndpoint(url); transport = testTransport ?? Send;
            lock (gatesLock)
            {
                if (!gates.TryGetValue(endpoint, out gate)) { gate = new Gate(); gates[endpoint] = gate; }
                gate.Users++;
            }
        }
        public static string ValidateEndpoint(string value)
        {
            Uri uri;
            if (!Uri.TryCreate((value ?? "").Trim(), UriKind.Absolute, out uri) || uri.Scheme != "https" || uri.Host != "discord.com" || !uri.IsDefaultPort || uri.UserInfo.Length != 0 || uri.Fragment.Length != 0 ||
                !Regex.IsMatch(uri.AbsolutePath, @"^/api/(?:v\d+/)?webhooks/\d+/[A-Za-z0-9_-]+$"))
                throw new ArgumentException("Use an HTTPS discord.com webhook URL from the channel's Integrations settings.");
            Match thread = Regex.Match(uri.Query, @"(?:^\?|&)thread_id=(\d+)(?:&|$)");
            return uri.GetLeftPart(UriPartial.Path) + "?wait=true" + (thread.Success ? "&thread_id=" + thread.Groups[1].Value : "");
        }
        public static string Payload(string text)
        {
            text = text ?? ""; if (text.Length > 1900) text = text.Substring(0, 1900);
            // Do not allow alert labels to ping Discord users, roles or everyone.
            var b = new StringBuilder("{\"allowed_mentions\":{\"parse\":[]},\"content\":\"");
            foreach (char c in text)
            {
                if (c == '"' || c == '\\') b.Append('\\').Append(c);
                else if (c < 32 || char.IsSurrogate(c)) b.Append("\\u").Append(((int)c).ToString("x4"));
                else b.Append(c);
            }
            return b.Append("\"}").ToString();
        }
        public void Enqueue(string text)
        {
            lock (sync)
            {
                if (disposed) return;
                if (queue.Count >= 32) { warning = "Discord queue full; notification dropped."; return; }
                queue.Enqueue(Payload(text));
                if (!running) { running = true; ThreadPool.QueueUserWorkItem(delegate { Drain(); }); }
            }
        }
        private bool WaitForSlot()
        {
            while (!stop.IsSet)
            {
                int wait;
                lock (gatesLock)
                {
                    wait = (int)Math.Min(60000, Math.Max(0, (gate.Next - DateTime.UtcNow).TotalMilliseconds));
                    if (wait == 0) { gate.Next = DateTime.UtcNow.AddMilliseconds(500); return true; }
                }
                if (stop.Wait(Math.Max(1, wait))) return false;
            }
            return false;
        }
        private void Drain()
        {
            try
            {
                while (!stop.IsSet)
                {
                    string payload;
                    lock (sync)
                    {
                        if (disposed || queue.Count == 0) return;
                        payload = queue.Dequeue();
                    }
                    for (int attempt = 0; attempt < 2; attempt++)
                    {
                        if (!WaitForSlot()) return;
                        Result result = transport(endpoint, payload);
                        if (result.Status >= 200 && result.Status < 300) { warning = ""; break; }
                        if (result.Status == 429)
                        {
                            double delay = Math.Max(1, Math.Min(60, result.RetrySeconds));
                            lock (gatesLock) { DateTime until = DateTime.UtcNow.AddSeconds(delay); if (until > gate.Next) gate.Next = until; }
                            warning = "Discord rate limited; notifications delayed.";
                            if (attempt == 0 && result.RetrySeconds <= 60) continue;
                        }
                        warning = "Discord notification not delivered (" + (result.Status == 0 ? "network timeout or connection error" : "HTTP " + result.Status) + "). Native alerts remain active.";
                        break; // Do not retry ambiguous failures that could duplicate a message.
                    }
                }
            }
            catch (Exception) { warning = "Discord notification failed. Native alerts remain active."; }
            finally
            {
                lock (sync)
                {
                    running = false;
                    if (!disposed && queue.Count > 0) { running = true; ThreadPool.QueueUserWorkItem(delegate { Drain(); }); }
                }
            }
        }
        private Result Send(string url, string payload)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST"; request.ContentType = "application/json"; request.UserAgent = "TatankaAMTToolkit/" + AmtBrand.Version;
            request.Timeout = request.ReadWriteTimeout = 5000; request.AllowAutoRedirect = false;
            byte[] data = Encoding.UTF8.GetBytes(payload); request.ContentLength = data.Length;
            lock (sync) { if (disposed) return new Result(); active = request; }
            try
            {
                using (Stream stream = request.GetRequestStream()) stream.Write(data, 0, data.Length);
                using (var response = (HttpWebResponse)request.GetResponse()) return ReadResult(response);
            }
            catch (WebException ex)
            {
                using (var response = ex.Response as HttpWebResponse) return response == null ? new Result() : ReadResult(response);
            }
            finally { lock (sync) { active = null; } }
        }
        private static Result ReadResult(HttpWebResponse response)
        {
            double delay;
            if (!double.TryParse(response.Headers["Retry-After"], NumberStyles.Float, CultureInfo.InvariantCulture, out delay)) delay = 2;
            return new Result { Status = (int)response.StatusCode, RetrySeconds = delay };
        }
        public void Dispose()
        {
            lock (sync)
            {
                if (disposed) return; disposed = true; queue.Clear(); stop.Set(); if (active != null) active.Abort();
            }
            lock (gatesLock) { if (--gate.Users == 0) gates.Remove(endpoint); }
        }
    }
}






















namespace NinjaTrader.NinjaScript.Indicators
{
    public partial class TatankaAmtToolkit : Indicator
    {
        private object sync = new object();
        private Settings cfg;
        private Engine engine;
        private DailyHistory daily;
        private TimeZoneInfo central, appZone;
        private SortedDictionary<DateTime, Bar> minuteBars;
        private SortedDictionary<DateTime, Bar> macroBars;
        private RenderHistory<Sample> samples;
        private ProfileHistogramCache histogramCache;
        private LineItem[] publishedHistory;
        private volatile bool terminated;
        private List<LineItem> historicalLines;
        private HashSet<string> sent;
        private Queue<DateTime> alertTimes;
        private bool capWarned;
        private LevelFileSource levelsSource;
        private LevelAlertTracker levelAlerts;
        private DiscordAlerts discord;
        private TatankaAmtToolkit runtimeOwner;
        private string discordSetupWarning = "";
        private static readonly string[] markerNames = { "Developing POC", "pmPOC", "pmVAH", "pmVAL", "pmHigh", "pmLow", "pwHigh", "pwLow", "pdHigh", "pdLow", "IB High", "IB Low" };
        private string fault = "", settingsWarning = "";
        private DateTime lastPreview = DateTime.MinValue, lastMacro = DateTime.MinValue, lastLevelsRead = DateTime.MinValue;
        private Bar pending;
        private Frame previousFrame;
        private int committed = -1, firstLiveBar = int.MaxValue;
        private volatile Snapshot snapshot;
        private sealed class Sample
        {
            public Bar Bar; public double Vwap, VaHigh, VaLow; public bool Fill, Confirmed;
        }
        private sealed class Snapshot
        {
            public Frame Frame; public Sample[] Samples; public Sample Preview;
            public LineItem[] History; public ProfileHistogram[] Histograms; public string Warning;
            public int SampleCount { get { return Samples.Length + (Preview == null ? 0 : 1); } }
            public Sample SampleAt(int index) { return index < Samples.Length ? Samples[index] : Preview; }
        }
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = AmtBrand.Name;
                Description = AmtBrand.Name + " v" + AmtBrand.Version + ": a complete Auction Market Theory read on one chart. Value, balance vs trend, trapped inventory, unfinished business and reference prices. Free and open source (MPL 2.0). " + AmtBrand.Website;
                IsOverlay = true; IsChartOnly = true; DisplayInDataBox = false; IsAutoScale = false;
                Calculate = Calculate.OnEachTick; IsSuspendedWhileInactive = false; BarsRequiredToPlot = 0;
                InitializeSettings();
                LevelsFile = ""; AlertSound = "Alert1.wav"; EnableSound = true;
                EnableDiscord = false; DiscordWebhook = "";
                PaintPriceMarkers = true; ArePlotsConfigurable = false;
                foreach (string marker in markerNames) AddPlot(Brushes.Gray, marker);
            }
            else if (State == State.Configure)
            {
                // Price-tag colors are fixed settings. Assign frozen brushes once,
                // before rendering starts; never replace plot brushes on price ticks.
                Brush[] markerBrushes = { mvp_c_poc, macro_poc_color, macro_color, macro_color,
                    macro_color, macro_color, pw_col, pw_col, pd_high_c, pd_low_c, ib_col_hi, ib_col_lo };
                for (int i = 0; i < markerBrushes.Length; i++)
                    Plots[i].Brush = BrushFrom(BrushTo(markerBrushes[i]));
                // Hardcoded series comply with NT's Configure contract. All inherit the chart's ETH template.
                AddDataSeries(BarsPeriodType.Minute, 1);
                AddDataSeries(BarsPeriodType.Day, 1);
                AddDataSeries(BarsPeriodType.Minute, 30);
            }
            else if (State == State.DataLoaded)
            {
                try
                {
                    // NT may clone a configured indicator. Allocate per-instance runtime state here.
                    sync = new object(); minuteBars = new SortedDictionary<DateTime, Bar>(); macroBars = new SortedDictionary<DateTime, Bar>();
                    samples = new RenderHistory<Sample>(50000); histogramCache = new ProfileHistogramCache();
                    historicalLines = new List<LineItem>(); publishedHistory = null; terminated = false;
                    sent = new HashSet<string>(); alertTimes = new Queue<DateTime>();
                    engine = null; pending = null; previousFrame = null; snapshot = null; committed = -1; firstLiveBar = int.MaxValue;
                    capWarned = false; fault = settingsWarning = discordSetupWarning = "";
                    levelsSource = new LevelFileSource(); levelAlerts = new LevelAlertTracker();
                    // A property-grid clone may share copied fields before DataLoaded.
                    // Only an instance's own runtime sender may be disposed by that instance.
                    runtimeOwner = this; discord = null;
                    lastMacro = lastPreview = lastLevelsRead = DateTime.MinValue;
                    cfg = ReadSettings(); ValidateSettings(cfg);
                    central = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
                    appZone = NinjaTrader.Core.Globals.GeneralOptions.TimeZoneInfo;
                    daily = new DailyHistory(cfg.daily_atr_length);
                    if (BarsPeriod.BarsPeriodType != BarsPeriodType.Minute || BarsPeriod.Value < 1 || BarsPeriod.Value > 30)
                        throw new ArgumentException("Use a standard 1–30 minute chart. Tick, range, Renko, Heiken Ashi and daily bars are not supported by this port.");
                    if (IsTickReplays[0] == true) throw new ArgumentException("Turn Tick Replay off. Use Market Replay / Playback to validate live intrabar behavior.");
                    if (Bars.TradingHours != null && Bars.TradingHours.Name.IndexOf("RTH", StringComparison.OrdinalIgnoreCase) >= 0 && Bars.TradingHours.Name.IndexOf("ETH", StringComparison.OrdinalIgnoreCase) < 0)
                        throw new ArgumentException("Select the instrument's ETH trading-hours template. Overnight ranges, full-day profiles and inventory require overnight data.");
                    if (Calculate != Calculate.OnEachTick) throw new ArgumentException("Keep Calculate set to On each tick for level alerts and automatic file refresh.");
                    levelsSource.ReferencePrice = Bars != null && Bars.Count > 0 ? Bars.GetClose(Bars.Count - 1) : double.NaN;
                    levelsSource.Initialize(LevelsFile, MathEx.LevelsRoot(Instrument.MasterInstrument.Name), cfg.lvl_text,
                        Path.Combine(NinjaTrader.Core.Globals.UserDataDir, "Tatanka", "LevelsCache"), central);
                    cfg.lvl_text = levelsSource.Current.Serialize();
                    if (EnableDiscord)
                    {
                        try { discord = new DiscordAlerts(DiscordWebhook); }
                        catch (ArgumentException ex) { discordSetupWarning = "Discord disabled: " + ex.Message; }
                    }
                }
                catch (Exception ex) { Fail(ex); }
            }
            else if (State == State.Terminated)
            {
                if (ReferenceEquals(runtimeOwner, this))
                {
                    lock (sync)
                    {
                        terminated = true;
                        if (discord != null) discord.Dispose();
                        // NinjaTrader can retain a terminated indicator instance. Release
                        // its histories and profiles without touching property-grid clones.
                        snapshot = null; previousFrame = null; pending = null; engine = null; daily = null;
                        samples = null; histogramCache = null; historicalLines = null; publishedHistory = null;
                        minuteBars = null; macroBars = null; levelsSource = null; levelAlerts = null;
                        sent = null; alertTimes = null; runtimeOwner = null;
                    }
                }
            }
            else if (State == State.Transition)
            {
                lock (sync) { if (pending != null && engine != null && fault.Length == 0) { try { Preview(true); } catch (Exception ex) { Fail(ex); } } }
            }
        }
        private void Fail(Exception ex)
        {
            fault = AmtBrand.Short + " v" + AmtBrand.Version + ": " + ex.Message; Print(fault); Log(fault, LogLevel.Error);
            if (ChartControl != null) ForceRefresh();
        }
        private DateTime CT(DateTime t) { return TimeZoneInfo.ConvertTime(DateTime.SpecifyKind(t, DateTimeKind.Unspecified), appZone, central); }
        private DateTime Local(DateTime t) { return TimeZoneInfo.ConvertTime(DateTime.SpecifyKind(t, DateTimeKind.Unspecified), central, appZone); }
        private Bar ReadBar(int series, int ago, int minutes)
        {
            DateTime end = CT(Times[series][ago]);
            return new Bar { Start = end.AddMinutes(-minutes), End = end, Index = CurrentBars[series] - ago, Open = Opens[series][ago], High = Highs[series][ago], Low = Lows[series][ago], Close = Closes[series][ago], Volume = Volumes[series][ago] };
        }
        protected override void OnBarUpdate()
        {
            if (terminated || fault.Length != 0 || cfg == null) return;
            lock (sync)
            {
                if (terminated) return;
                try
                {
                    int bip = BarsInProgress;
                    bool historical = State == State.Historical;
                    if (bip == 1)
                    {
                        if (!historical && IsFirstTickOfBar && CurrentBars[1] > 0) { Bar prev = ReadBar(1, 1, 1); minuteBars[prev.End] = prev; }
                        Bar one = ReadBar(1, 0, 1); minuteBars[one.End] = one;
                        // Keep more than the maximum primary bar plus session gaps; no whole-history duplication.
                        while (minuteBars.Count > 180) minuteBars.Remove(minuteBars.Keys.First());
                        return;
                    }
                    if (bip == 2)
                    {
                        int ago = historical ? 0 : IsFirstTickOfBar ? 1 : -1;
                        if (ago >= 0 && CurrentBars[2] >= ago) daily.Add(Times[2][ago].Date, Highs[2][ago], Lows[2][ago], Closes[2][ago]);
                        return;
                    }
                    if (bip == 3)
                    {
                        int ago = historical ? 0 : IsFirstTickOfBar ? 1 : -1;
                        if (ago >= 0 && CurrentBars[bip] >= ago) { Bar mb = ReadBar(bip, ago, 30); macroBars[mb.End] = mb; }
                        return;
                    }
                    if (bip != 0) return;
                    Bar current = ReadBar(0, 0, BarsPeriod.Value);
                    if (State == State.Realtime) firstLiveBar = Math.Min(firstLiveBar, current.Index);
                    if (engine == null) engine = new Engine(cfg, Instrument.MasterInstrument.Name, TickSize, current.Close, BarsPeriod.Value);
                    if (pending != null && current.Index != pending.Index)
                    {
                        // On a shared timestamp NT updates the primary first. Waiting until the next primary
                        // bar guarantees its completed secondary data has arrived, in historical and live modes.
                        if (CurrentBar > 0) pending = ReadBar(0, 1, BarsPeriod.Value);
                        Commit();
                        sent.Clear();
                    }
                    pending = current;
                    if (!historical)
                    {
                        ReadLevelsFile();
                        if ((DateTime.UtcNow - lastPreview).TotalMilliseconds >= 100 || IsFirstTickOfBar) { Preview(false); lastPreview = DateTime.UtcNow; }
                        else if (snapshot != null)
                        {
                            // Observe every price tick even while expensive profile rendering is throttled.
                            EmitAlerts(new Frame { Bar = current, Watches = snapshot.Frame.Watches }, false);
                        }
                    }
                    else if (CurrentBar >= Bars.Count - 2) Preview(true);
                }
                catch (Exception ex) { Fail(ex); }
            }
        }
        private List<Bar> Subbars(Bar b)
        {
            return minuteBars.Values.Where(x => x.Start >= b.Start && x.End <= b.End).ToList();
        }
        private void DrainMacro(DateTime through)
        {
            var done = macroBars.Keys.Where(x => x <= through).ToArray();
            foreach (DateTime key in done) { if (key > lastMacro) { engine.AddMacro(macroBars[key]); lastMacro = key; } macroBars.Remove(key); }
        }
        private Frame CalculateFrame(Engine target, Bar b)
        {
            List<Bar> subs = Subbars(b);
            Frame f = target.Process(b, subs, daily.Before(MathEx.TradingDate(b.Start)));
            if (subs.Count == 0) f.Warning += " One-minute data unavailable; using chart-bar profile fallback.";
            return f;
        }
        private void Commit()
        {
            if (pending.Index <= committed) return;
            DrainMacro(pending.End);
            Frame f = CalculateFrame(engine, pending);
            if (previousFrame != null && MathEx.TradingDate(previousFrame.Bar.Start) != MathEx.TradingDate(f.Bar.Start))
            {
                historicalLines.AddRange(previousFrame.Lines.Where(x => x.Name.StartsWith("IB ") || x.Name.StartsWith("pd") || x.Name == "dOpen").Select(x => new LineItem { Name = x.Name, Price = x.Price, Start = x.Start, End = x.End, Color = x.Color, Width = x.Width, Style = x.Style, Label = false }));
                historicalLines.RemoveAll(x => (f.Bar.Start - x.End).TotalDays > 20);
                publishedHistory = null;
            }
            samples.Add(ToSample(f)); previousFrame = f; committed = pending.Index;
            if (State == State.Realtime) EmitAlerts(f, true);
            if (State != State.Historical || CurrentBar >= Bars.Count - 2) Publish(f, false);
        }
        private void Preview(bool historical)
        {
            DrainMacro(pending.Start);
            Engine preview = engine.Clone(); Frame f = CalculateFrame(preview, pending);
            if (!historical) EmitAlerts(f, false);
            Publish(f, true);
        }
        private Sample ToSample(Frame f) { return new Sample { Bar = f.Bar, Vwap = f.RthVwap, VaHigh = f.VaHigh, VaLow = f.VaLow, Fill = f.RuleActive || f.RuleConfirmed, Confirmed = f.RuleConfirmed }; }
        private void Publish(Frame f, bool preview)
        {
            // Render thread receives immutable arrays, never the live dictionaries updated on the data thread.
            ProfileHistogram[] hist = histogramCache.Build(f.Profiles);
            // Rendering uses the detached histogram arrays, not the preview engine's
            // large dictionaries. Do not keep those dictionaries alive in the snapshot.
            f.Profiles.Clear();
            if (publishedHistory == null) publishedHistory = historicalLines.ToArray();
            UpdatePriceMarkers(f);
            snapshot = new Snapshot { Frame = f, Samples = samples.Snapshot(), Preview = preview && f.Bar.Index > committed ? ToSample(f) : null,
                History = publishedHistory, Histograms = hist, Warning = (settingsWarning + " " + levelsSource.Warning(MathEx.TradingDate(f.Bar.Start)) + " " + f.Warning).Trim() };
        }
        private void EmitAlerts(Frame f, bool confirmed)
        {
            if (State != State.Realtime || f.Bar.Index < firstLiveBar) return;
            if (!confirmed) f.Notices.AddRange(levelAlerts.Observe(f.Watches, f.Bar.Close, cfg.LevelAlertMode, Instrument.MasterInstrument.Name, TickSize));
            foreach (Notice n in f.Notices)
            {
                if (n.Key == "Rule80" && !confirmed) continue;
                string id = f.Bar.Index + ":" + n.Key; if (!sent.Add(id)) continue;
                DateTime now = f.Bar.Start;
                while (alertTimes.Count > 0 && alertTimes.Peek() < now.AddMinutes(-cfg.alertcap_window_min)) alertTimes.Dequeue();
                if (alertTimes.Count < cfg.alertcap_max) capWarned = false;
                if (cfg.alertcap_enable && alertTimes.Count >= cfg.alertcap_max)
                {
                    if (!capWarned) { NativeAlert("Cap:" + f.Bar.Index, "Alert storm cap reached (" + cfg.alertcap_max + "/" + cfg.alertcap_window_min + "m). Further alerts suppressed."); capWarned = true; }
                    continue;
                }
                if (cfg.alertcap_enable) alertTimes.Enqueue(now);
                NativeAlert(id, n.Text + " | close " + MathEx.Price(f.Bar.Close, Instrument.MasterInstrument.Name, TickSize));
            }
        }
        private void NativeAlert(string id, string text)
        {
            string sound = EnableSound && !string.IsNullOrWhiteSpace(AlertSound) ? Path.Combine(NinjaTrader.Core.Globals.InstallDir, "sounds", Path.GetFileName(AlertSound)) : "";
            Alert("TatankaAMT:" + GetHashCode() + ":" + id, Priority.Medium, "[" + Instrument.FullName + "] " + text, sound, 0, Brushes.Black, Brushes.White);
            // Playback is useful for local testing but must not send old market events to members.
            if (discord != null && NinjaTrader.Cbi.Connection.PlaybackConnection == null)
                discord.Enqueue("[" + Instrument.FullName + "] " + text + " | " + CT(Time[0]).ToString("yyyy-MM-dd HH:mm:ss") + " CT");
        }
        private void ReadLevelsFile()
        {
            if (levelsSource == null || string.IsNullOrWhiteSpace(LevelsFile) || (DateTime.UtcNow - lastLevelsRead).TotalSeconds < 5) return;
            lastLevelsRead = DateTime.UtcNow;
            levelsSource.ReferencePrice = Closes[0][0];
            if (levelsSource.Refresh())
            {
                cfg.lvl_text = levelsSource.Current.Serialize();
                engine.ReplaceLevels(levelsSource.Current.Levels);
                levelAlerts.Reset(); // Replacing a level cannot manufacture a crossing alert.
                lastPreview = DateTime.MinValue; // Publish the new watch levels before observing another tick.
            }
        }
        private void UpdatePriceMarkers(Frame f)
        {
            int ago = CurrentBar - f.Bar.Index;
            if (ago < 0 || ago > 1) return;
            for (int i = 0; i < markerNames.Length; i++)
            {
                LineItem line = f.Lines.FirstOrDefault(x => x.Name == markerNames[i]);
                if (cfg.ShowPriceTags && !cfg.HideAllLabels && line != null)
                {
                    Values[i][ago] = line.Price;
                }
                else Values[i].Reset(ago);
            }
        }
        public override string FormatPriceMarker(double price)
        {
            return Instrument == null ? price.ToString("0.#####", CultureInfo.InvariantCulture) : Instrument.MasterInstrument.FormatPrice(price);
        }
        // Read-only; never saved to templates or workspaces.
        [XmlIgnore, global::System.ComponentModel.ReadOnly(true)]
        [Display(Name = "Version", Description = "Quote this version when reporting a problem.", GroupName="About", Order=277)]
        public string AboutVersion { get { return AmtBrand.Version; } set { } }
        [XmlIgnore, global::System.ComponentModel.ReadOnly(true)]
        [Display(Name = "Website", GroupName="About", Order=278)]
        public string AboutWebsite { get { return AmtBrand.Website; } set { } }
        [Display(Name="Levels File (full path; supports {root})", Description = "Read daily prices from a local text file. Leave blank to use Paste levels here. {root} becomes the chart's root symbol; micros use the parent (MES reads ES, MNQ reads NQ, MCL reads CL, MGC reads GC). This does not connect to Discord.", GroupName="2. Daily Levels", Order=8)]
        public string LevelsFile { get; set; }
        [Display(Name="Enable Alert Sound", GroupName="18. Alerts", Order=271)]
        public bool EnableSound { get; set; }
        [Display(Name="Sound File (NinjaTrader sounds folder)", GroupName="18. Alerts", Order=272)]
        public string AlertSound { get; set; }
        [Display(Name = "Send Chart Alerts to Discord", Description = "Optional. Sends enabled live chart alerts to the Discord channel below. Leave off for NinjaTrader-only alerts. Does not import levels. Historical loading and Playback are excluded.", GroupName="19. Discord Alerts (Optional)", Order=273)]
        public bool EnableDiscord { get; set; }
        [PasswordPropertyText(true)]
        [Display(Name = "Discord Webhook URL", Description = "In Discord: Server Settings > Integrations > Create Webhook. Choose the receiving channel and copy its Webhook URL here. A server invite or channel link will not work. Configure mobile notifications in Discord.", GroupName="19. Discord Alerts (Optional)", Order=274)]
        public string DiscordWebhook { get; set; }
        private static Brush BrushFrom(uint argb)
        {
            var b = new SolidColorBrush(Color.FromArgb((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb)); b.Freeze(); return b;
        }
        private static uint BrushTo(Brush b)
        {
            SolidColorBrush s = b as SolidColorBrush; if (s == null) throw new ArgumentException("Use solid colors for indicator brushes.");
            Color c = s.Color; return (uint)((byte)(c.A * s.Opacity) << 24 | c.R << 16 | c.G << 8 | c.B);
        }
        private sealed class Paint : IDisposable
        {
            private readonly D2.RenderTarget target;
            private readonly Dictionary<uint, D2.SolidColorBrush> colors = new Dictionary<uint, D2.SolidColorBrush>();
            private readonly Dictionary<float, DW.TextFormat> fonts = new Dictionary<float, DW.TextFormat>();
            private readonly Dictionary<string, D2.StrokeStyle> strokes = new Dictionary<string, D2.StrokeStyle>();
            public Paint(D2.RenderTarget rt) { target = rt; }
            public D2.Brush Color(uint c)
            {
                D2.SolidColorBrush b;
                if (!colors.TryGetValue(c, out b)) { b = new D2.SolidColorBrush(target, new Dx.Color4(((c >> 16) & 255) / 255f, ((c >> 8) & 255) / 255f, (c & 255) / 255f, (c >> 24) / 255f)); colors[c] = b; }
                return b;
            }
            public DW.TextFormat Font(float size)
            {
                DW.TextFormat f; if (!fonts.TryGetValue(size, out f)) { f = new DW.TextFormat(NinjaTrader.Core.Globals.DirectWriteFactory, "Segoe UI", size); fonts[size] = f; } return f;
            }
            public D2.StrokeStyle Stroke(string style)
            {
                if (style == "Solid") return null;
                D2.StrokeStyle s;
                if (!strokes.TryGetValue(style, out s)) { s = new D2.StrokeStyle(NinjaTrader.Core.Globals.D2DFactory, new D2.StrokeStyleProperties { DashStyle = style == "Dotted" ? D2.DashStyle.Dot : D2.DashStyle.Dash }); strokes[style] = s; }
                return s;
            }
            public void Dispose() { foreach (var b in colors.Values) b.Dispose(); foreach (var f in fonts.Values) f.Dispose(); foreach (var s in strokes.Values) s.Dispose(); }
        }
        private static float FontSize(string name) { switch (name.ToLowerInvariant()) { case "tiny": return 10; case "small": return 12; case "large": return 18; case "huge": return 24; default: return 14; } }
        private float X(ChartControl c, DateTime t) { return c.GetXByTime(Local(t)); }
        private void Text(Paint p, string text, float x, float y, float w, float h, uint color, float size)
        {
            RenderTarget.DrawText(text, p.Font(size), new Dx.RectangleF(x, y, Math.Max(1, w), Math.Max(1, h)), p.Color(color));
        }
        private void Segment(Paint p, float x1, float x2, float y, uint color, int width, string style)
        {
            if (width <= 0 || float.IsNaN(y)) return;
            RenderTarget.DrawLine(new Dx.Vector2(x1, y), new Dx.Vector2(x2, y), p.Color(color), Math.Max(1, width), p.Stroke(style));
        }
        protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
            if (terminated || RenderTarget == null || ChartBars == null || ChartPanel == null || IsInHitTest) return;
            Snapshot snap = snapshot;
            using (var paint = new Paint(RenderTarget))
            {
                if (fault.Length != 0) { Text(paint, fault, ChartPanel.X + 12, ChartPanel.Y + 12, ChartPanel.W - 24, 150, 0xffff5050, 16); return; }
                if (snap == null || cfg == null) { Text(paint, AmtBrand.Short + " v" + AmtBrand.Version + " — loading chart and secondary history…", ChartPanel.X + 12, ChartPanel.Y + 12, ChartPanel.W - 24, 50, 0xffc0c0c0, 14); return; }
                float left = ChartPanel.X, right = ChartPanel.X + ChartPanel.W, top = ChartPanel.Y, bottom = ChartPanel.Y + ChartPanel.H;
                float spacing = ChartBars.ToIndex > ChartBars.FromIndex ? Math.Abs(chartControl.GetXByBarIndex(ChartBars, ChartBars.ToIndex) - chartControl.GetXByBarIndex(ChartBars, ChartBars.ToIndex - 1)) : 8;
                spacing = Math.Max(1, spacing);
                RenderTarget.PushAxisAlignedClip(new Dx.RectangleF(left, top, ChartPanel.W, ChartPanel.H), D2.AntialiasMode.Aliased);
                try
                {
                    for (int sampleIndex = 0; sampleIndex < snap.SampleCount; sampleIndex++)
                    {
                        Sample sm = snap.SampleAt(sampleIndex);
                        if (sm.Bar.Index < ChartBars.FromIndex || sm.Bar.Index > ChartBars.ToIndex) continue;
                        float x = chartControl.GetXByBarIndex(ChartBars, sm.Bar.Index);
                        if (cfg.cc_showFill && sm.Fill && MathEx.Valid(sm.VaHigh) && MathEx.Valid(sm.VaLow))
                        {
                            float ya = chartScale.GetYByValue(sm.VaHigh), yb = chartScale.GetYByValue(sm.VaLow);
                            RenderTarget.FillRectangle(new Dx.RectangleF(x - spacing / 2, Math.Min(ya, yb), spacing, Math.Abs(yb - ya)), paint.Color(sm.Confirmed ? cfg.cc_col_conf : cfg.cc_col_active));
                        }
                    }
                    foreach (ZoneItem z in snap.Frame.Zones)
                    {
                        float xa = z.Start == DateTime.MinValue ? left : X(chartControl, z.Start), xb = z.Start == DateTime.MinValue ? right : X(chartControl, snap.Frame.Bar.End.AddMinutes(30));
                        float ya = chartScale.GetYByValue(z.Top), yb = chartScale.GetYByValue(z.Bottom);
                        if (yb < top || ya > bottom) continue;
                        var rect = new Dx.RectangleF(Math.Max(left, xa), ya, Math.Max(1, Math.Min(right, xb) - Math.Max(left, xa)), Math.Max(1, yb - ya));
                        RenderTarget.FillRectangle(rect, paint.Color(z.Color));
                        if (z.BorderWidth > 0) RenderTarget.DrawRectangle(rect, paint.Color(z.BorderColor), z.BorderWidth, paint.Stroke(z.BorderStyle));
                    }
                    foreach (ProfileHistogram h in snap.Histograms)
                    {
                        if (h.Max <= 0) continue;
                        float anchor = X(chartControl, h.End), width = cfg.mvp_profile_width * spacing;
                        if (h.Current) anchor += (cfg.mvp_offset - (cfg.mvp_align_right ? 0 : cfg.mvp_profile_width)) * spacing;
                        if (anchor + width < left || anchor - width > right) continue;
                        for (int i = 0; i < h.Prices.Length; i++)
                        {
                            float ya = chartScale.GetYByValue(h.Prices[i] + h.Step / 2), yb = chartScale.GetYByValue(h.Prices[i] - h.Step / 2);
                            if (yb < top || ya > bottom) continue;
                            float w = (float)(h.Volumes[i] / h.Max * width);
                            bool va = h.Prices[i] >= h.Val && h.Prices[i] <= h.Vah;
                            uint color = va ? cfg.mvp_c_hist_va : cfg.mvp_c_hist_out; int transparency = va ? cfg.mvp_hist_va_transp : cfg.mvp_hist_out_transp;
                            color = (color & 0xffffff) | ((uint)(255 * (100 - transparency) / 100) << 24);
                            RenderTarget.FillRectangle(new Dx.RectangleF(cfg.mvp_align_right ? anchor - w : anchor, ya, Math.Max(1, w), Math.Max(1, yb - ya)), paint.Color(color));
                        }
                    }
                    foreach (LineItem line in snap.History) RenderLine(paint, line, chartControl, chartScale, spacing, snap.Frame);
                    var lines = snap.Frame.Lines.OrderBy(x => IsTopLayer(x) ? 1 : 0);
                    foreach (LineItem line in lines) RenderLine(paint, line, chartControl, chartScale, spacing, snap.Frame);
                    Sample prev = null;
                    for (int sampleIndex = 0; sampleIndex < snap.SampleCount; sampleIndex++)
                    {
                        Sample sm = snap.SampleAt(sampleIndex);
                        if (sm.Bar.Index < ChartBars.FromIndex - 1) { prev = sm; continue; }
                        if (sm.Bar.Index > ChartBars.ToIndex) break;
                        float x = chartControl.GetXByBarIndex(ChartBars, sm.Bar.Index);
                        if (prev != null && prev.Bar.Index == sm.Bar.Index - 1)
                        {
                            float xp = chartControl.GetXByBarIndex(ChartBars, prev.Bar.Index);
                            if (cfg.vwap_enable && MathEx.Valid(sm.Vwap) && MathEx.Valid(prev.Vwap) && MathEx.TradingDate(prev.Bar.Start) == MathEx.TradingDate(sm.Bar.Start)) RenderTarget.DrawLine(new Dx.Vector2(xp, chartScale.GetYByValue(prev.Vwap)), new Dx.Vector2(x, chartScale.GetYByValue(sm.Vwap)), paint.Color(cfg.vwap_col), cfg.vwap_width, paint.Stroke(cfg.vwap_style));
                        }
                        prev = sm;
                    }
                    if (cfg.tbl_show) Dashboard(paint, snap.Frame);
                    string notices = (snap.Warning + " " + discordSetupWarning + " " + (discord == null ? "" : discord.Warning)).Trim();
                    if (notices.Length != 0) Text(paint, notices, left + 10, bottom - 64, ChartPanel.W - 20, 64, 0xffffc66b, 12);
                }
                finally { RenderTarget.PopAxisAlignedClip(); }
            }
        }
        private bool IsTopLayer(LineItem l)
        {
            switch (cfg.layer_top_pick)
            {
                case "Daily Levels (All)": return l.Extend == "Both";
                case "Profile POC": return l.Name == "Developing POC";
                case "Profile VAH": return l.Name == "Developing VAH";
                case "Profile VAL": return l.Name == "Developing VAL";
                case "Prev Day High": return l.Name == "pdHigh";
                case "Prev Day Low": return l.Name == "pdLow";
                case "Prev Month VAH": return l.Name == "pmVAH";
                case "Prev Month VAL": return l.Name == "pmVAL";
                case "Spike Base": return l.Name.StartsWith("Spike Base");
                default: return l.Name == cfg.layer_top_pick;
            }
        }
        private void RenderLine(Paint p, LineItem line, ChartControl c, ChartScale scale, float spacing, Frame frame)
        {
            if (!MathEx.Valid(line.Price)) return;
            float y = scale.GetYByValue(line.Price), left = ChartPanel.X, right = ChartPanel.X + ChartPanel.W;
            if (y < ChartPanel.Y - 25 || y > ChartPanel.Y + ChartPanel.H + 25) return;
            float x1 = line.Start == DateTime.MinValue || line.Extend == "Left" || line.Extend == "Both" ? left : X(c, line.Start);
            float x2 = line.Extend == "Right" || line.Extend == "Both" ? right : X(c, line.End) + line.Offset * spacing;
            Segment(p, Math.Max(left, x1), Math.Min(right, x2), y, line.Color, line.Width, line.Style);
            if (!line.Label) return;
            bool dailyLevel = line.Extend == "Both" && line.Name.Length > 0;
            float labelX = X(c, frame.Bar.End) + line.Offset * spacing + 4;
            if (line.Name.StartsWith("IB ") && cfg.ib_label_loc == "Left") labelX = x1;
            float size = FontSize(line.Size);
            string label = line.Name + ": " + MathEx.Price(line.Price, Instrument.MasterInstrument.Name, TickSize);
            float estimated = Math.Max(80, label.Length * size * .56f);
            labelX = Math.Max(left + 2, Math.Min(labelX, right - estimated - 4));
            if (dailyLevel && cfg.lvl_showZones)
            {
                ZoneItem zone = frame.Zones.FirstOrDefault(z => z.Start == DateTime.MinValue && Math.Abs((z.Top + z.Bottom) / 2 - line.Price) < TickSize * .01);
                if (zone != null) y = scale.GetYByValue(line.Price - (zone.Top - zone.Bottom) / 4);
            }
            Text(p, label, labelX, y - size / 2 - 2, estimated + 10, size * 2, line.TextColor == 0 ? line.Color : line.TextColor, size);
        }
        private static float TextWidth(Paint p, string text, float size)
        {
            using (var layout = new DW.TextLayout(NinjaTrader.Core.Globals.DirectWriteFactory, text, p.Font(size), 10000, 1000))
                return layout.Metrics.Width;
        }
        private void Dashboard(Paint p, Frame f)
        {
            int rows = f.Labels.Length;
            float fs = FontSize(cfg.tbl_size), rh = fs + 10, w = Math.Max(310, fs * 25), h = rh * (rows + 1);
            float x = cfg.tbl_pos.Contains("Left") ? ChartPanel.X + 10 : ChartPanel.X + ChartPanel.W - w - 10;
            float y = cfg.tbl_pos.Contains("Bottom") ? ChartPanel.Y + ChartPanel.H - h - 55 : cfg.tbl_pos.Contains("Middle") ? ChartPanel.Y + (ChartPanel.H - h) / 2 : ChartPanel.Y + 10;
            RenderTarget.FillRectangle(new Dx.RectangleF(x, y, w, h), p.Color(0xcc000000));
            // Header row: product, version and site (the site is shown on NinjaTrader only).
            RenderTarget.FillRectangle(new Dx.RectangleF(x, y, w, rh), p.Color(0xee1e222d));
            float small = fs * .85f, siteW = TextWidth(p, AmtBrand.Website, small);
            Text(p, AmtBrand.Website, x + w - 8 - siteW, y + 4 + (fs - small) * .6f, siteW + 4, rh, 0xffb2b5be, small);
            Text(p, AmtBrand.Short + " v" + AmtBrand.Version, x + 8, y + 4, Math.Max(1, w - siteW - 24), rh, 0xffffffff, fs);
            Segment(p, x, x + w, y + rh, 0x66666666, 1, "Solid");
            y += rh;
            for (int i = 0; i < rows; i++)
            {
                Segment(p, x, x + w, y + (i + 1) * rh, 0x66666666, 1, "Solid");
                Text(p, f.Labels[i], x + 8, y + i * rh + 4, w * .44f, rh, 0xffffffff, fs);
                Text(p, f.Values[i], x + w * .44f, y + i * rh + 4, w * .55f, rh, f.Colors[i], fs);
            }
        }
    }
}


namespace NinjaTrader.NinjaScript.Indicators {
[NinjaTrader.Gui.CategoryOrder("1. Global Settings", 1)]
[NinjaTrader.Gui.CategoryOrder("2. Daily Levels", 2)]
[NinjaTrader.Gui.CategoryOrder("3. Previous Day High/Low/Open", 3)]
[NinjaTrader.Gui.CategoryOrder("4. Previous Day Value & 80% Rule", 4)]
[NinjaTrader.Gui.CategoryOrder("5. Overnight High/Low", 5)]
[NinjaTrader.Gui.CategoryOrder("6. Initial Balance", 6)]
[NinjaTrader.Gui.CategoryOrder("7. RTH VWAP", 7)]
[NinjaTrader.Gui.CategoryOrder("8. Daily & Weekly Open", 8)]
[NinjaTrader.Gui.CategoryOrder("9. Previous Week", 9)]
[NinjaTrader.Gui.CategoryOrder("10. Previous Month", 10)]
[NinjaTrader.Gui.CategoryOrder("11. Volume Profile", 11)]
[NinjaTrader.Gui.CategoryOrder("12. Volume Profile Walls", 12)]
[NinjaTrader.Gui.CategoryOrder("13. TPO & Single Prints", 13)]
[NinjaTrader.Gui.CategoryOrder("14. Naked POCs", 14)]
[NinjaTrader.Gui.CategoryOrder("15. Settlement & Inventory", 15)]
[NinjaTrader.Gui.CategoryOrder("16. Late Spike", 16)]
[NinjaTrader.Gui.CategoryOrder("17. Dashboard", 17)]
[NinjaTrader.Gui.CategoryOrder("18. Alerts", 18)]
[NinjaTrader.Gui.CategoryOrder("19. Discord Alerts (Optional)", 19)]
[NinjaTrader.Gui.CategoryOrder("20. NinjaTrader Limits", 20)]
[NinjaTrader.Gui.CategoryOrder("About", 21)]
public partial class TatankaAmtToolkit {
private void InitializeSettings() {
tzguard_enable = true;
row_scale = "Auto";
tick_auto = true;
tick_manual_val = 0.25;
layer_top_pick = "Daily Levels (All)";
alertcap_enable = true;
alertcap_max = 30;
alertcap_window_min = 60;
lvl_text = "";
lvl_ip_color = BrushFrom(0xFFFDD835u);
lvl_bull_color = BrushFrom(0xFFF23645u);
lvl_bear_color = BrushFrom(0xFF4CAF50u);
lvl_thickness = 1;
lvl_style_in = "Dotted";
lvl_zone_style_in = "Solid";
lvl_text_size_opt = "large";
lvl_x_offset = 40;
lvl_showLine = true;
lvl_showZones = true;
lvl_zone_mode = "ATR %";
lvl_zone_atr_pct = 0.02;
lvl_zoneWidthTicks = 8;
lvl_zoneOpacity = 85;
lvl_color_flip = true;
lvl_flip_atr = 0.05;
lvl_alerts = false;
lvl_alert_ip = true;
lvl_alert_bull = true;
lvl_alert_bear = true;
mvp_master = true;
mvp_tf_mode = "Weekly";
mvp_sess_time = "0830-1500";
mvp_sess_dur = 6.5;
mvp_calc_tf = "1";
mvp_row_size = 0.25;
mvp_val_pct = 68.0;
mvp_profile_width = 50;
mvp_align_right = true;
mvp_offset = 50;
mvp_show_walls = false;
mvp_use_auto_scale = true;
mvp_scale_start = 2.0;
mvp_scale_end = 1.5;
mvp_extend_walls = true;
mvp_smooth_len = 2;
mvp_vol_mult = 2;
mvp_min_wall_dist = 40;
mvp_w_wall = 1;
mvp_c_wall_base = BrushFrom(0xFFE6B300u);
mvp_wall_transp = 25;
mvp_c_poc = BrushFrom(0xFF78909Cu);
mvp_poc_width = 2;
mvp_c_levels = BrushFrom(0xFF787B86u);
mvp_lvl_width = 2;
mvp_c_hist_va = BrushFrom(0xFF78909Cu);
mvp_hist_va_transp = 75;
mvp_c_hist_out = BrushFrom(0xFF787B86u);
mvp_hist_out_transp = 90;
mvp_show_poc = true;
mvp_show_vah = true;
mvp_show_val = true;
mvp_use_cur_alerts = true;
mvp_alert_time = "0830-1500";
macro_show_month = true;
macro_show_poc = true;
macro_show_hl = true;
macro_alert_lvl = true;
macro_color = BrushFrom(0xFFFFFFFFu);
macro_poc_color = BrushFrom(0xFFFFFFFFu);
macro_width = 3;
macro_poc_width = 3;
macro_style = "Solid";
macro_poc_style = "Solid";
macro_offset = 2;
macro_text_size = "Normal";
ib_enable = true;
ib_alert_ibh = true;
ib_alert_ibl = true;
ib_session = "0830-0930";
ib_alert_time = "0830-1500";
ib_show_mid = false;
ib_show_labels = true;
ib_label_loc = "Right";
ib_label_size = "Normal";
ib_lbl_offset = 0;
ib_lvl_width = 1;
ib_col_hi = BrushFrom(0xFFFFCC80u);
ib_col_lo = BrushFrom(0xFFFFCC80u);
ib_col_mid = BrushFrom(0xFFFFCC80u);
ib_extend = "None";
ib_limit_ext = true;
ib_stop_time = "0830-1515";
ib_style_main = "Solid";
ib_style_int = "Dotted";
vwap_enable = false;
vwap_col = BrushFrom(0xFFFFFFFFu);
vwap_width = 2;
vwap_start_h = 8;
vwap_start_m = 30;
vwap_end_h = 15;
vwap_end_m = 0;
vwap_alert = false;
cc_showOpen = false;
cc_c_open = BrushFrom(0xFF2196F3u);
cc_open_width = 1;
cc_open_offset = 3;
wk_showOpen = true;
wk_c_open = BrushFrom(0xFFFFFFFFu);
wk_open_width = 2;
wk_open_offset = 2;
wk_alert_open = true;
amt_enable = true;
amt_sess = "1700-0830:123456";
amt_del_sess = "1500-1505";
amt_showLb = true;
amt_width = 1;
amt_style_in = "Solid";
amt_c_hi = BrushFrom(0xFFFFFFFFu);
amt_c_txt_hi = BrushFrom(0xFFFFFFFFu);
amt_c_lo = BrushFrom(0xFFFFFFFFu);
amt_c_txt_lo = BrushFrom(0xFFFFFFFFu);
amt_lb_size_in = "Normal";
amt_offset = 2;
amt_alert_ovn = true;
amt_vauto = true;
amt_vres = false;
amt_res_val = 0.25;
pd_enable = true;
pd_use_rth = true;
pd_sess_time = "0830-1500";
pd_showH = true;
pd_showL = true;
pd_showOpen = false;
pd_high_c = BrushFrom(0xFFFFFFFFu);
pd_low_c = BrushFrom(0xFFFFFFFFu);
pd_open_c = BrushFrom(0xFF2196F3u);
pd_high_width = 1;
pd_low_width = 1;
pd_open_width = 1;
pd_style_in = "Solid";
pd_lb_size_in = "Normal";
pd_offset_val = 2;
pd_alert_hl = true;
cc_enable = true;
cc_alert_va = false;
cc_alert_80 = true;
pd_poc_c = BrushFrom(0xFFFFFFFFu);
pd_vah_c = BrushFrom(0x809C27B0u);
pd_val_c = BrushFrom(0x809C27B0u);
cc_poc_width = 1;
cc_vah_width = 1;
cc_val_width = 1;
cc_poc_style_in = "Solid";
cc_lb_offset = 2;
cc_lb_size_in = "Normal";
cc_showFill = true;
cc_col_active = BrushFrom(0x129C27B0u);
cc_col_conf = BrushFrom(0x12EA80FCu);
cc_va_pct = 0.68;
cc_sess_def = "0830-1500:23456";
pw_enable = true;
pw_col = BrushFrom(0xFFFFFFFFu);
pw_c_txt = BrushFrom(0xFFFFFFFFu);
pw_width = 2;
pw_style_in = "Solid";
pw_offset = 2;
pw_lb_size_in = "Normal";
pw_alert_hl = true;
sp_rth_session = "0830-1500";
sp_tpo_minutes = 30;
sp_ticks_per_block = 1;
sp_color_hist = BrushFrom(0x19FF9800u);
sp_max_days = 10;
sp_filter_tails = true;
sp_tail_max = 15;
sp_min_ticks = 1;
sp_show_poor = true;
sp_poor_width = 2;
sp_poor_style_in = "Dotted";
sp_col_poor_h = BrushFrom(0xFFF23645u);
sp_col_poor_l = BrushFrom(0xFF4CAF50u);
showSpike = true;
spikeTimeRange = "1430-1500";
spike_width = 1;
spike_offset = 15;
spike_lbl_size = "Normal";
spike_alert = true;
inv_enable = true;
inv_show_line = true;
inv_start_h = 17;
inv_end_h = 8;
inv_end_m = 30;
inv_c_trap = BrushFrom(0xFF00FF00u);
inv_c_settle = BrushFrom(0xFF787B86u);
inv_txt_trap = "TRAP ACTIVE";
inv_width = 1;
inv_style_in = "Dashed";
inv_lb_size_in = "Normal";
inv_offset = 40;
inv_alert = true;
npoc_enable = true;
npoc_c = BrushFrom(0xFFFFC0CBu);
npoc_d_show = true;
npoc_w_show = true;
npoc_m_show = true;
npoc_style_in = "Solid";
npoc_w_d = 1;
npoc_w_w = 2;
npoc_w_m = 3;
npoc_alert = true;
context_session = "0815-1500";
daily_atr_length = 14;
tbl_show = true;
lvl_txt_c = BrushFrom(0x00FFFFFFu);
mvp_poc_style = "Solid";
mvp_lvl_style = "Solid";
mvp_wall_style = "Solid";
mvp_lb_size = "Normal";
mvp_txt_c = BrushFrom(0x00FFFFFFu);
macro_txt_c = BrushFrom(0x00FFFFFFu);
ib_txt_c = BrushFrom(0x00FFFFFFu);
vwap_style = "Solid";
cc_open_style = "Solid";
cc_open_lb_size = "Normal";
wk_open_style = "Solid";
wk_open_lb_size = "Normal";
opens_txt_c = BrushFrom(0x00FFFFFFu);
pd_txt_c = BrushFrom(0x00FFFFFFu);
cc_show_poc = true;
cc_show_vah = true;
cc_show_val = true;
cc_txt_c = BrushFrom(0x00FFFFFFu);
pw_showH = true;
pw_showL = true;
sp_border_c = BrushFrom(0xFFFF9800u);
sp_border_w = 0;
sp_border_style = "Solid";
sp_poor_lb_size = "Small";
sp_poor_txt_c = BrushFrom(0x00FFFFFFu);
spike_style = "Solid";
spike_c_sup = BrushFrom(0xFF4CAF50u);
spike_c_res = BrushFrom(0xFFF23645u);
spike_txt_c = BrushFrom(0x00FFFFFFu);
inv_settle_width = 1;
inv_settle_style = "Dashed";
inv_txt_c = BrushFrom(0x00FFFFFFu);
npoc_c_w = BrushFrom(0xFFFFC0CBu);
npoc_c_m = BrushFrom(0xFFFFC0CBu);
npoc_lb_size = "Small";
npoc_lb_offset = 2;
npoc_txt_c = BrushFrom(0x00FFFFFFu);
tbl_session = "0830-1500";
tbl_pos = "Top Right";
tbl_size = "Normal";
HideAllLabels = false;
LabelsLevels = true;
LabelsProfiles = true;
LabelsPriorDay = true;
LabelsPriorValue = true;
LabelsOpens = true;
LabelsPriorWeek = true;
LabelsPriorMonth = true;
LabelsInventory = true;
LabelsSpike = true;
LabelsNakedPoc = true;
LabelsPoor = true;
ShowPriceTags = true;
LevelAlertMode = "Cross";
AlertZoneTicks = 4;
MaxProfileRows = 100000;
MaxStructures = 2000;
}
private Settings ReadSettings() { var s = new Settings();
s.tzguard_enable = tzguard_enable;
s.row_scale = row_scale;
s.tick_auto = tick_auto;
s.tick_manual_val = tick_manual_val;
s.layer_top_pick = layer_top_pick != null && layer_top_pick.StartsWith("Monthly ") ? "Profile " + layer_top_pick.Substring(8) : layer_top_pick; // charts saved before the rename
s.alertcap_enable = alertcap_enable;
s.alertcap_max = alertcap_max;
s.alertcap_window_min = alertcap_window_min;
s.lvl_text = lvl_text;
s.lvl_ip_color = BrushTo(lvl_ip_color);
s.lvl_bull_color = BrushTo(lvl_bull_color);
s.lvl_bear_color = BrushTo(lvl_bear_color);
s.lvl_thickness = lvl_thickness;
s.lvl_style_in = lvl_style_in;
s.lvl_zone_style_in = lvl_zone_style_in;
s.lvl_text_size_opt = lvl_text_size_opt;
s.lvl_x_offset = lvl_x_offset;
s.lvl_showLine = lvl_showLine;
s.lvl_showZones = lvl_showZones;
s.lvl_zone_mode = lvl_zone_mode;
s.lvl_zone_atr_pct = lvl_zone_atr_pct;
s.lvl_zoneWidthTicks = lvl_zoneWidthTicks;
s.lvl_zoneOpacity = lvl_zoneOpacity;
s.lvl_color_flip = lvl_color_flip;
s.lvl_flip_atr = lvl_flip_atr;
s.lvl_alerts = lvl_alerts;
s.lvl_alert_ip = lvl_alert_ip;
s.lvl_alert_bull = lvl_alert_bull;
s.lvl_alert_bear = lvl_alert_bear;
s.mvp_master = mvp_master;
s.mvp_tf_mode = mvp_tf_mode;
s.mvp_sess_time = mvp_sess_time;
s.mvp_sess_dur = mvp_sess_dur;
s.mvp_calc_tf = mvp_calc_tf;
s.mvp_row_size = mvp_row_size;
s.mvp_val_pct = mvp_val_pct;
s.mvp_profile_width = mvp_profile_width;
s.mvp_align_right = mvp_align_right;
s.mvp_offset = mvp_offset;
s.mvp_show_walls = mvp_show_walls;
s.mvp_use_auto_scale = mvp_use_auto_scale;
s.mvp_scale_start = mvp_scale_start;
s.mvp_scale_end = mvp_scale_end;
s.mvp_extend_walls = mvp_extend_walls;
s.mvp_smooth_len = mvp_smooth_len;
s.mvp_vol_mult = mvp_vol_mult;
s.mvp_min_wall_dist = mvp_min_wall_dist;
s.mvp_w_wall = mvp_w_wall;
s.mvp_c_wall_base = BrushTo(mvp_c_wall_base);
s.mvp_wall_transp = mvp_wall_transp;
s.mvp_c_poc = BrushTo(mvp_c_poc);
s.mvp_poc_width = mvp_poc_width;
s.mvp_c_levels = BrushTo(mvp_c_levels);
s.mvp_lvl_width = mvp_lvl_width;
s.mvp_c_hist_va = BrushTo(mvp_c_hist_va);
s.mvp_hist_va_transp = mvp_hist_va_transp;
s.mvp_c_hist_out = BrushTo(mvp_c_hist_out);
s.mvp_hist_out_transp = mvp_hist_out_transp;
s.mvp_show_poc = mvp_show_poc;
s.mvp_show_vah = mvp_show_vah;
s.mvp_show_val = mvp_show_val;
s.mvp_use_cur_alerts = mvp_use_cur_alerts;
s.mvp_alert_time = mvp_alert_time;
s.macro_show_month = macro_show_month;
s.macro_show_poc = macro_show_poc;
s.macro_show_hl = macro_show_hl;
s.macro_alert_lvl = macro_alert_lvl;
s.macro_color = BrushTo(macro_color);
s.macro_poc_color = BrushTo(macro_poc_color);
s.macro_width = macro_width;
s.macro_poc_width = macro_poc_width;
s.macro_style = macro_style;
s.macro_poc_style = macro_poc_style;
s.macro_offset = macro_offset;
s.macro_text_size = macro_text_size;
s.ib_enable = ib_enable;
s.ib_alert_ibh = ib_alert_ibh;
s.ib_alert_ibl = ib_alert_ibl;
s.ib_session = ib_session;
s.ib_alert_time = ib_alert_time;
s.ib_show_mid = ib_show_mid;
s.ib_show_labels = ib_show_labels;
s.ib_label_loc = ib_label_loc;
s.ib_label_size = ib_label_size;
s.ib_lbl_offset = ib_lbl_offset;
s.ib_lvl_width = ib_lvl_width;
s.ib_col_hi = BrushTo(ib_col_hi);
s.ib_col_lo = BrushTo(ib_col_lo);
s.ib_col_mid = BrushTo(ib_col_mid);
s.ib_extend = ib_extend;
s.ib_limit_ext = ib_limit_ext;
s.ib_stop_time = ib_stop_time;
s.ib_style_main = ib_style_main;
s.ib_style_int = ib_style_int;
s.vwap_enable = vwap_enable;
s.vwap_col = BrushTo(vwap_col);
s.vwap_width = vwap_width;
s.vwap_start_h = vwap_start_h;
s.vwap_start_m = vwap_start_m;
s.vwap_end_h = vwap_end_h;
s.vwap_end_m = vwap_end_m;
s.vwap_alert = vwap_alert;
s.cc_showOpen = cc_showOpen;
s.cc_c_open = BrushTo(cc_c_open);
s.cc_open_width = cc_open_width;
s.cc_open_offset = cc_open_offset;
s.wk_showOpen = wk_showOpen;
s.wk_c_open = BrushTo(wk_c_open);
s.wk_open_width = wk_open_width;
s.wk_open_offset = wk_open_offset;
s.wk_alert_open = wk_alert_open;
s.amt_enable = amt_enable;
s.amt_sess = amt_sess;
s.amt_del_sess = amt_del_sess;
s.amt_showLb = amt_showLb;
s.amt_width = amt_width;
s.amt_style_in = amt_style_in;
s.amt_c_hi = BrushTo(amt_c_hi);
s.amt_c_txt_hi = BrushTo(amt_c_txt_hi);
s.amt_c_lo = BrushTo(amt_c_lo);
s.amt_c_txt_lo = BrushTo(amt_c_txt_lo);
s.amt_lb_size_in = amt_lb_size_in;
s.amt_offset = amt_offset;
s.amt_alert_ovn = amt_alert_ovn;
s.amt_vauto = amt_vauto;
s.amt_vres = amt_vres;
s.amt_res_val = amt_res_val;
s.pd_enable = pd_enable;
s.pd_use_rth = pd_use_rth;
s.pd_sess_time = pd_sess_time;
s.pd_showH = pd_showH;
s.pd_showL = pd_showL;
s.pd_showOpen = pd_showOpen;
s.pd_high_c = BrushTo(pd_high_c);
s.pd_low_c = BrushTo(pd_low_c);
s.pd_open_c = BrushTo(pd_open_c);
s.pd_high_width = pd_high_width;
s.pd_low_width = pd_low_width;
s.pd_open_width = pd_open_width;
s.pd_style_in = pd_style_in;
s.pd_lb_size_in = pd_lb_size_in;
s.pd_offset_val = pd_offset_val;
s.pd_alert_hl = pd_alert_hl;
s.cc_enable = cc_enable;
s.cc_alert_va = cc_alert_va;
s.cc_alert_80 = cc_alert_80;
s.pd_poc_c = BrushTo(pd_poc_c);
s.pd_vah_c = BrushTo(pd_vah_c);
s.pd_val_c = BrushTo(pd_val_c);
s.cc_poc_width = cc_poc_width;
s.cc_vah_width = cc_vah_width;
s.cc_val_width = cc_val_width;
s.cc_poc_style_in = cc_poc_style_in;
s.cc_lb_offset = cc_lb_offset;
s.cc_lb_size_in = cc_lb_size_in;
s.cc_showFill = cc_showFill;
s.cc_col_active = BrushTo(cc_col_active);
s.cc_col_conf = BrushTo(cc_col_conf);
s.cc_va_pct = cc_va_pct;
s.cc_sess_def = cc_sess_def;
s.pw_enable = pw_enable;
s.pw_col = BrushTo(pw_col);
s.pw_c_txt = BrushTo(pw_c_txt);
s.pw_width = pw_width;
s.pw_style_in = pw_style_in;
s.pw_offset = pw_offset;
s.pw_lb_size_in = pw_lb_size_in;
s.pw_alert_hl = pw_alert_hl;
s.sp_rth_session = sp_rth_session;
s.sp_tpo_minutes = sp_tpo_minutes;
s.sp_ticks_per_block = sp_ticks_per_block;
s.sp_color_hist = BrushTo(sp_color_hist);
s.sp_max_days = sp_max_days;
s.sp_filter_tails = sp_filter_tails;
s.sp_tail_max = sp_tail_max;
s.sp_min_ticks = sp_min_ticks;
s.sp_show_poor = sp_show_poor;
s.sp_poor_width = sp_poor_width;
s.sp_poor_style_in = sp_poor_style_in;
s.sp_col_poor_h = BrushTo(sp_col_poor_h);
s.sp_col_poor_l = BrushTo(sp_col_poor_l);
s.showSpike = showSpike;
s.spikeTimeRange = spikeTimeRange;
s.spike_width = spike_width;
s.spike_offset = spike_offset;
s.spike_lbl_size = spike_lbl_size;
s.spike_alert = spike_alert;
s.inv_enable = inv_enable;
s.inv_show_line = inv_show_line;
s.inv_start_h = inv_start_h;
s.inv_end_h = inv_end_h;
s.inv_end_m = inv_end_m;
s.inv_c_trap = BrushTo(inv_c_trap);
s.inv_c_settle = BrushTo(inv_c_settle);
s.inv_txt_trap = inv_txt_trap;
s.inv_width = inv_width;
s.inv_style_in = inv_style_in;
s.inv_lb_size_in = inv_lb_size_in;
s.inv_offset = inv_offset;
s.inv_alert = inv_alert;
s.npoc_enable = npoc_enable;
s.npoc_c = BrushTo(npoc_c);
s.npoc_d_show = npoc_d_show;
s.npoc_w_show = npoc_w_show;
s.npoc_m_show = npoc_m_show;
s.npoc_style_in = npoc_style_in;
s.npoc_w_d = npoc_w_d;
s.npoc_w_w = npoc_w_w;
s.npoc_w_m = npoc_w_m;
s.npoc_alert = npoc_alert;
s.context_session = context_session;
s.daily_atr_length = daily_atr_length;
s.tbl_show = tbl_show;
s.lvl_txt_c = BrushTo(lvl_txt_c);
s.mvp_poc_style = mvp_poc_style;
s.mvp_lvl_style = mvp_lvl_style;
s.mvp_wall_style = mvp_wall_style;
s.mvp_lb_size = mvp_lb_size;
s.mvp_txt_c = BrushTo(mvp_txt_c);
s.macro_txt_c = BrushTo(macro_txt_c);
s.ib_txt_c = BrushTo(ib_txt_c);
s.vwap_style = vwap_style;
s.cc_open_style = cc_open_style;
s.cc_open_lb_size = cc_open_lb_size;
s.wk_open_style = wk_open_style;
s.wk_open_lb_size = wk_open_lb_size;
s.opens_txt_c = BrushTo(opens_txt_c);
s.pd_txt_c = BrushTo(pd_txt_c);
s.cc_show_poc = cc_show_poc;
s.cc_show_vah = cc_show_vah;
s.cc_show_val = cc_show_val;
s.cc_txt_c = BrushTo(cc_txt_c);
s.pw_showH = pw_showH;
s.pw_showL = pw_showL;
s.sp_border_c = BrushTo(sp_border_c);
s.sp_border_w = sp_border_w;
s.sp_border_style = sp_border_style;
s.sp_poor_lb_size = sp_poor_lb_size;
s.sp_poor_txt_c = BrushTo(sp_poor_txt_c);
s.spike_style = spike_style;
s.spike_c_sup = BrushTo(spike_c_sup);
s.spike_c_res = BrushTo(spike_c_res);
s.spike_txt_c = BrushTo(spike_txt_c);
s.inv_settle_width = inv_settle_width;
s.inv_settle_style = inv_settle_style;
s.inv_txt_c = BrushTo(inv_txt_c);
s.npoc_c_w = BrushTo(npoc_c_w);
s.npoc_c_m = BrushTo(npoc_c_m);
s.npoc_lb_size = npoc_lb_size;
s.npoc_lb_offset = npoc_lb_offset;
s.npoc_txt_c = BrushTo(npoc_txt_c);
s.tbl_session = tbl_session;
s.tbl_pos = tbl_pos;
s.tbl_size = tbl_size;
s.HideAllLabels = HideAllLabels;
s.LabelsLevels = LabelsLevels;
s.LabelsProfiles = LabelsProfiles;
s.LabelsPriorDay = LabelsPriorDay;
s.LabelsPriorValue = LabelsPriorValue;
s.LabelsOpens = LabelsOpens;
s.LabelsPriorWeek = LabelsPriorWeek;
s.LabelsPriorMonth = LabelsPriorMonth;
s.LabelsInventory = LabelsInventory;
s.LabelsSpike = LabelsSpike;
s.LabelsNakedPoc = LabelsNakedPoc;
s.LabelsPoor = LabelsPoor;
s.ShowPriceTags = ShowPriceTags;
s.LevelAlertMode = LevelAlertMode;
s.AlertZoneTicks = AlertZoneTicks;
s.MaxProfileRows = MaxProfileRows;
s.MaxStructures = MaxStructures;
return s; }
private void ValidateSettings(Settings s) {
if (s.tick_manual_val < 0.001) throw new ArgumentException("Manual Tick Size is below its minimum.");
if (!MathEx.Valid(s.tick_manual_val)) throw new ArgumentException("Manual Tick Size must be finite.");
if (!TatankaAmtToolkitOptions.Choices["layer_top_pick"].Contains(s.layer_top_pick)) throw new ArgumentException("Invalid Force on Top");
if (s.alertcap_max < 5) throw new ArgumentException("Max Alerts per Window is below its minimum.");
if (s.alertcap_max > 200) throw new ArgumentException("Max Alerts per Window exceeds its maximum.");
if (s.alertcap_window_min < 5) throw new ArgumentException("Window (minutes) is below its minimum.");
if (s.alertcap_window_min > 1440) throw new ArgumentException("Window (minutes) exceeds its maximum.");
if (!TatankaAmtToolkitOptions.Choices["lvl_style_in"].Contains(s.lvl_style_in)) throw new ArgumentException("Invalid Level Line Style");
if (!TatankaAmtToolkitOptions.Choices["lvl_zone_style_in"].Contains(s.lvl_zone_style_in)) throw new ArgumentException("Invalid Zone Border Style");
if (!TatankaAmtToolkitOptions.Choices["lvl_text_size_opt"].Contains(s.lvl_text_size_opt)) throw new ArgumentException("Invalid Text Size");
if (!TatankaAmtToolkitOptions.Choices["lvl_zone_mode"].Contains(s.lvl_zone_mode)) throw new ArgumentException("Invalid Zone Size Mode");
if (s.lvl_zone_atr_pct < 0.01) throw new ArgumentException("Zone Width (ATR %) is below its minimum.");
if (s.lvl_zone_atr_pct > 0.50) throw new ArgumentException("Zone Width (ATR %) exceeds its maximum.");
if (!MathEx.Valid(s.lvl_zone_atr_pct)) throw new ArgumentException("Zone Width (ATR %) must be finite.");
if (s.lvl_zoneWidthTicks < 1) throw new ArgumentException("Zone Width (ticks above & below) is below its minimum.");
if (s.lvl_zoneWidthTicks > 200) throw new ArgumentException("Zone Width (ticks above & below) exceeds its maximum.");
if (s.lvl_zoneOpacity < 0) throw new ArgumentException("Zone Transparency (0=solid, 100=transp) is below its minimum.");
if (s.lvl_zoneOpacity > 100) throw new ArgumentException("Zone Transparency (0=solid, 100=transp) exceeds its maximum.");
if (!MathEx.Valid(s.lvl_flip_atr)) throw new ArgumentException("Flip ATR Buffer must be finite.");
if (!TatankaAmtToolkitOptions.Choices["row_scale"].Contains(s.row_scale)) throw new ArgumentException("Invalid Profile Row Scale");
if (!TatankaAmtToolkitOptions.Choices["mvp_tf_mode"].Contains(s.mvp_tf_mode)) throw new ArgumentException("Invalid Profile Period");
MathEx.InSession(new DateTime(2026, 1, 5), s.mvp_sess_time);
if (!MathEx.Valid(s.mvp_sess_dur)) throw new ArgumentException("Session Duration (Hours) must be finite.");
if (s.mvp_row_size < 0.25) throw new ArgumentException("Row Size is below its minimum.");
if (!MathEx.Valid(s.mvp_row_size)) throw new ArgumentException("Row Size must be finite.");
if (!MathEx.Valid(s.mvp_val_pct)) throw new ArgumentException("Value Area % must be finite.");
if (s.mvp_profile_width < 10) throw new ArgumentException("Profile Width (Bars) is below its minimum.");
if (s.mvp_profile_width > 500) throw new ArgumentException("Profile Width (Bars) exceeds its maximum.");
if (s.mvp_offset < -200) throw new ArgumentException("Profile Offset (Bars) is below its minimum.");
if (s.mvp_offset > 200) throw new ArgumentException("Profile Offset (Bars) exceeds its maximum.");
if (!MathEx.Valid(s.mvp_scale_start)) throw new ArgumentException("Start Mult must be finite.");
if (!MathEx.Valid(s.mvp_scale_end)) throw new ArgumentException("End Mult must be finite.");
if (!MathEx.Valid(s.mvp_vol_mult)) throw new ArgumentException("Fixed Mult must be finite.");
MathEx.InSession(new DateTime(2026, 1, 5), s.mvp_alert_time);
if (!TatankaAmtToolkitOptions.Choices["macro_style"].Contains(s.macro_style)) throw new ArgumentException("Invalid Line Style");
if (!TatankaAmtToolkitOptions.Choices["macro_poc_style"].Contains(s.macro_poc_style)) throw new ArgumentException("Invalid POC Style");
if (!TatankaAmtToolkitOptions.Choices["macro_text_size"].Contains(s.macro_text_size)) throw new ArgumentException("Invalid Label Size");
MathEx.InSession(new DateTime(2026, 1, 5), s.ib_session);
MathEx.InSession(new DateTime(2026, 1, 5), s.ib_alert_time);
if (!TatankaAmtToolkitOptions.Choices["ib_label_loc"].Contains(s.ib_label_loc)) throw new ArgumentException("Invalid IB Label Location");
if (!TatankaAmtToolkitOptions.Choices["ib_label_size"].Contains(s.ib_label_size)) throw new ArgumentException("Invalid IB Label Size");
if (!TatankaAmtToolkitOptions.Choices["ib_extend"].Contains(s.ib_extend)) throw new ArgumentException("Invalid Extend IB Levels");
MathEx.InSession(new DateTime(2026, 1, 5), s.ib_stop_time);
if (!TatankaAmtToolkitOptions.Choices["ib_style_main"].Contains(s.ib_style_main)) throw new ArgumentException("Invalid IB Main Style");
if (!TatankaAmtToolkitOptions.Choices["ib_style_int"].Contains(s.ib_style_int)) throw new ArgumentException("Invalid IB Intermediate Style");
if (s.vwap_start_h < 0) throw new ArgumentException("VWAP Start H (CT) is below its minimum.");
if (s.vwap_start_h > 23) throw new ArgumentException("VWAP Start H (CT) exceeds its maximum.");
if (s.vwap_start_m < 0) throw new ArgumentException("VWAP Start M (CT) is below its minimum.");
if (s.vwap_start_m > 59) throw new ArgumentException("VWAP Start M (CT) exceeds its maximum.");
if (s.vwap_end_h < 0) throw new ArgumentException("VWAP End H (CT) is below its minimum.");
if (s.vwap_end_h > 23) throw new ArgumentException("VWAP End H (CT) exceeds its maximum.");
if (s.vwap_end_m < 0) throw new ArgumentException("VWAP End M (CT) is below its minimum.");
if (s.vwap_end_m > 59) throw new ArgumentException("VWAP End M (CT) exceeds its maximum.");
MathEx.InSession(new DateTime(2026, 1, 5), s.amt_sess);
MathEx.InSession(new DateTime(2026, 1, 5), s.amt_del_sess);
if (s.amt_width < 1) throw new ArgumentException("OVN Width is below its minimum.");
if (!TatankaAmtToolkitOptions.Choices["amt_style_in"].Contains(s.amt_style_in)) throw new ArgumentException("Invalid OVN Style");
if (!TatankaAmtToolkitOptions.Choices["amt_lb_size_in"].Contains(s.amt_lb_size_in)) throw new ArgumentException("Invalid OVN Label Size");
if (s.amt_res_val < 0.0001) throw new ArgumentException("OVN Manual Res Value is below its minimum.");
if (!MathEx.Valid(s.amt_res_val)) throw new ArgumentException("OVN Manual Res Value must be finite.");
MathEx.InSession(new DateTime(2026, 1, 5), s.pd_sess_time);
if (!TatankaAmtToolkitOptions.Choices["pd_style_in"].Contains(s.pd_style_in)) throw new ArgumentException("Invalid pdHigh/Low/Open Style");
if (!TatankaAmtToolkitOptions.Choices["pd_lb_size_in"].Contains(s.pd_lb_size_in)) throw new ArgumentException("Invalid pdHigh/Low/Open Label Size");
if (!TatankaAmtToolkitOptions.Choices["mvp_poc_style"].Contains(s.mvp_poc_style)) throw new ArgumentException("Invalid POC Style");
if (!TatankaAmtToolkitOptions.Choices["mvp_lvl_style"].Contains(s.mvp_lvl_style)) throw new ArgumentException("Invalid VAH/VAL Style");
if (!TatankaAmtToolkitOptions.Choices["mvp_wall_style"].Contains(s.mvp_wall_style)) throw new ArgumentException("Invalid Wall Style");
if (!TatankaAmtToolkitOptions.Choices["mvp_lb_size"].Contains(s.mvp_lb_size)) throw new ArgumentException("Invalid Label Size");
if (!TatankaAmtToolkitOptions.Choices["vwap_style"].Contains(s.vwap_style)) throw new ArgumentException("Invalid VWAP Style");
if (!TatankaAmtToolkitOptions.Choices["cc_open_style"].Contains(s.cc_open_style)) throw new ArgumentException("Invalid Open Line Style");
if (!TatankaAmtToolkitOptions.Choices["cc_open_lb_size"].Contains(s.cc_open_lb_size)) throw new ArgumentException("Invalid Open Label Size");
if (!TatankaAmtToolkitOptions.Choices["wk_open_style"].Contains(s.wk_open_style)) throw new ArgumentException("Invalid Weekly Open Style");
if (!TatankaAmtToolkitOptions.Choices["wk_open_lb_size"].Contains(s.wk_open_lb_size)) throw new ArgumentException("Invalid Weekly Open Label Size");
if (s.sp_border_w < 0 || s.sp_border_w > 4) throw new ArgumentException("Single Print Border Width must be from 0 to 4.");
if (!TatankaAmtToolkitOptions.Choices["sp_border_style"].Contains(s.sp_border_style)) throw new ArgumentException("Invalid Single Print Border Style");
if (!TatankaAmtToolkitOptions.Choices["sp_poor_lb_size"].Contains(s.sp_poor_lb_size)) throw new ArgumentException("Invalid Poor H/L Label Size");
if (!TatankaAmtToolkitOptions.Choices["spike_style"].Contains(s.spike_style)) throw new ArgumentException("Invalid Spike Style");
if (s.inv_settle_width < 1 || s.inv_settle_width > 10) throw new ArgumentException("Settle Line Width must be from 1 to 10.");
if (!TatankaAmtToolkitOptions.Choices["inv_settle_style"].Contains(s.inv_settle_style)) throw new ArgumentException("Invalid Settle Line Style");
if (!TatankaAmtToolkitOptions.Choices["npoc_lb_size"].Contains(s.npoc_lb_size)) throw new ArgumentException("Invalid nPOC Label Size");
if (s.npoc_lb_offset < -50 || s.npoc_lb_offset > 200) throw new ArgumentException("nPOC Label Offset must be from -50 to 200.");
if (!TatankaAmtToolkitOptions.Choices["cc_poc_style_in"].Contains(s.cc_poc_style_in)) throw new ArgumentException("Invalid pdPOC/VAH/VAL Style");
if (!TatankaAmtToolkitOptions.Choices["cc_lb_size_in"].Contains(s.cc_lb_size_in)) throw new ArgumentException("Invalid pdPOC/VAH/VAL Label Size");
if (s.cc_va_pct < 0.01) throw new ArgumentException("Value Area % is below its minimum.");
if (s.cc_va_pct > 1.0) throw new ArgumentException("Value Area % exceeds its maximum.");
if (!MathEx.Valid(s.cc_va_pct)) throw new ArgumentException("Value Area % must be finite.");
MathEx.InSession(new DateTime(2026, 1, 5), s.cc_sess_def);
if (!TatankaAmtToolkitOptions.Choices["pw_style_in"].Contains(s.pw_style_in)) throw new ArgumentException("Invalid Prev Week Style");
if (!TatankaAmtToolkitOptions.Choices["pw_lb_size_in"].Contains(s.pw_lb_size_in)) throw new ArgumentException("Invalid Prev Week Label Size");
MathEx.InSession(new DateTime(2026, 1, 5), s.sp_rth_session);
if (s.sp_tpo_minutes < 1) throw new ArgumentException("TPO Period is below its minimum.");
if (s.sp_ticks_per_block < 1) throw new ArgumentException("Ticks per Block is below its minimum.");
if (!TatankaAmtToolkitOptions.Choices["sp_poor_style_in"].Contains(s.sp_poor_style_in)) throw new ArgumentException("Invalid Poor H/L Style");
MathEx.InSession(new DateTime(2026, 1, 5), s.spikeTimeRange);
if (!TatankaAmtToolkitOptions.Choices["spike_lbl_size"].Contains(s.spike_lbl_size)) throw new ArgumentException("Invalid Spike Label Size");
if (s.inv_start_h < 0) throw new ArgumentException("Globex Start H (CT) is below its minimum.");
if (s.inv_start_h > 23) throw new ArgumentException("Globex Start H (CT) exceeds its maximum.");
if (s.inv_end_h < 0) throw new ArgumentException("RTH Open H (CT) is below its minimum.");
if (s.inv_end_h > 23) throw new ArgumentException("RTH Open H (CT) exceeds its maximum.");
if (s.inv_end_m < 0) throw new ArgumentException("RTH Open M (CT) is below its minimum.");
if (s.inv_end_m > 59) throw new ArgumentException("RTH Open M (CT) exceeds its maximum.");
if (!TatankaAmtToolkitOptions.Choices["inv_style_in"].Contains(s.inv_style_in)) throw new ArgumentException("Invalid Trap Style");
if (!TatankaAmtToolkitOptions.Choices["inv_lb_size_in"].Contains(s.inv_lb_size_in)) throw new ArgumentException("Invalid Trap Label Size");
if (!TatankaAmtToolkitOptions.Choices["npoc_style_in"].Contains(s.npoc_style_in)) throw new ArgumentException("Invalid nPOC Style");
MathEx.InSession(new DateTime(2026, 1, 5), s.context_session);
MathEx.InSession(new DateTime(2026, 1, 5), s.tbl_session);
if (!TatankaAmtToolkitOptions.Choices["tbl_pos"].Contains(s.tbl_pos)) throw new ArgumentException("Invalid Table Position");
if (!TatankaAmtToolkitOptions.Choices["tbl_size"].Contains(s.tbl_size)) throw new ArgumentException("Invalid Table Text Size");
if (!TatankaAmtToolkitOptions.Choices["LevelAlertMode"].Contains(s.LevelAlertMode)) throw new ArgumentException("Invalid Level alert mode");
if (s.AlertZoneTicks < 1) throw new ArgumentException("Structure alert zone half-width (ticks) is below its minimum.");
if (s.AlertZoneTicks > 1000) throw new ArgumentException("Structure alert zone half-width (ticks) exceeds its maximum.");
if (s.MaxProfileRows < 200) throw new ArgumentException("Maximum rows per profile / TPO is below its minimum.");
if (s.MaxProfileRows > 100000) throw new ArgumentException("Maximum rows per profile / TPO exceeds its maximum.");
if (s.MaxStructures < 100) throw new ArgumentException("Maximum retained structures is below its minimum.");
if (s.MaxStructures > 2000) throw new ArgumentException("Maximum retained structures exceeds its maximum.");
if (s.daily_atr_length < 1 || s.sp_tpo_minutes < 1 || s.mvp_sess_dur <= 0 || s.mvp_smooth_len < 1 || s.sp_max_days < 1) throw new ArgumentException("Periods and history lengths must be positive.");
if (s.mvp_val_pct <= 0 || s.mvp_val_pct > 100) throw new ArgumentException("Profile value area must be between 0 and 100 percent.");
int precision; if (!int.TryParse(s.mvp_calc_tf, out precision) || precision < 1 || precision > 30) throw new ArgumentException("Calc Precision must be a whole number of minutes from 1 to 30.");
if (s.tzguard_enable && (s.mvp_sess_time != "0830-1500" || s.mvp_alert_time != "0830-1500" || s.ib_session != "0830-0930" || s.ib_alert_time != "0830-1500" || s.ib_stop_time != "0830-1515" || s.amt_sess != "1700-0830:123456" || s.amt_del_sess != "1500-1505" || s.pd_sess_time != "0830-1500" || s.cc_sess_def != "0830-1500:23456" || s.sp_rth_session != "0830-1500" || s.spikeTimeRange != "1430-1500" || s.context_session != "0815-1500" || s.tbl_session != "0830-1500" || s.vwap_start_h != 8 || s.vwap_start_m != 30 || s.vwap_end_h != 15 || s.vwap_end_m != 0 || s.inv_start_h != 17 || s.inv_end_h != 8 || s.inv_end_m != 30)) settingsWarning = "Session fields differ from defaults. All session fields are Central exchange time.";
}
[Display(Name="Warn if Session Times Were Changed", GroupName="1. Global Settings", Order=7)]
public bool tzguard_enable { get; set; }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="Profile Row Scale", Description="Ticks per profile row, naked POC bin and single-print block. Auto keeps rows near 0.0035% of price: ES, RTY, CL, GC 1x; YM 2x; NQ 5x. Pick a fixed scale if a symbol loads slowly. Row Size is a floor.", GroupName="1. Global Settings", Order=1)]
public string row_scale { get; set; }
[Display(Name="Auto Tick Size", GroupName="1. Global Settings", Order=2)]
public bool tick_auto { get; set; }
[Range(0.001, double.MaxValue)]
[Display(Name="Manual Tick Size", GroupName="1. Global Settings", Order=3)]
public double tick_manual_val { get; set; }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="Force on Top", GroupName="1. Global Settings", Order=4)]
public string layer_top_pick { get; set; }
[Display(Name="Enable Alert Storm Cap", GroupName="18. Alerts", Order=266)]
public bool alertcap_enable { get; set; }
[Range(5, 200)]
[Display(Name="Max Alerts per Window", GroupName="18. Alerts", Order=267)]
public int alertcap_max { get; set; }
[Range(5, 1440)]
[Display(Name="Window (minutes)", GroupName="18. Alerts", Order=268)]
public int alertcap_window_min { get; set; }
[Display(Name="Paste Levels Here", Description="Paste daily prices here when Levels file is blank. Accepts the one-line Discord paste line (2026-09-30, pivot, resistance..., support...) or the three-line Pivot Level / Resistance Levels / Support Levels block. If both are pasted, the paste line wins. No Discord connection is needed.", GroupName="2. Daily Levels", Order=9)]
public string lvl_text { get; set; }
[XmlIgnore]
[Display(Name="Pivot Color", GroupName="2. Daily Levels", Order=11)]
public Brush lvl_ip_color { get; set; }
[Browsable(false)] public string lvl_ip_colorSerialized { get { return Serialize.BrushToString(lvl_ip_color); } set { lvl_ip_color = Serialize.StringToBrush(value); } }
[XmlIgnore]
[Display(Name="Resistance Color", GroupName="2. Daily Levels", Order=12)]
public Brush lvl_bull_color { get; set; }
[Browsable(false)] public string lvl_bull_colorSerialized { get { return Serialize.BrushToString(lvl_bull_color); } set { lvl_bull_color = Serialize.StringToBrush(value); } }
[XmlIgnore]
[Display(Name="Support Color", GroupName="2. Daily Levels", Order=13)]
public Brush lvl_bear_color { get; set; }
[Browsable(false)] public string lvl_bear_colorSerialized { get { return Serialize.BrushToString(lvl_bear_color); } set { lvl_bear_color = Serialize.StringToBrush(value); } }
[Display(Name="Line Width", GroupName="2. Daily Levels", Order=14)]
public int lvl_thickness { get; set; }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="Line Style", GroupName="2. Daily Levels", Order=15)]
public string lvl_style_in { get; set; }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="Zone Border Style", GroupName="2. Daily Levels", Order=23)]
public string lvl_zone_style_in { get; set; }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="Label Size", GroupName="2. Daily Levels", Order=25)]
public string lvl_text_size_opt { get; set; }
[Display(Name="Label Offset", GroupName="2. Daily Levels", Order=26)]
public int lvl_x_offset { get; set; }
[Display(Name="Show Level Lines", GroupName="2. Daily Levels", Order=10)]
public bool lvl_showLine { get; set; }
[Display(Name="Show Zones", GroupName="2. Daily Levels", Order=18)]
public bool lvl_showZones { get; set; }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="Zone Size Mode", GroupName="2. Daily Levels", Order=19)]
public string lvl_zone_mode { get; set; }
[Range(0.01, 0.50)]
[Display(Name="Zone Width (ATR %)", GroupName="2. Daily Levels", Order=20)]
public double lvl_zone_atr_pct { get; set; }
[Range(1, 200)]
[Display(Name="Zone Width (ticks above & below)", GroupName="2. Daily Levels", Order=21)]
public int lvl_zoneWidthTicks { get; set; }
[Range(0, 100)]
[Display(Name="Zone Transparency (0=solid, 100=clear)", GroupName="2. Daily Levels", Order=22)]
public int lvl_zoneOpacity { get; set; }
[Display(Name="Flip Colors on Break (Res <-> Supp)", GroupName="2. Daily Levels", Order=16)]
public bool lvl_color_flip { get; set; }
[Display(Name="Flip ATR Buffer", GroupName="2. Daily Levels", Order=17)]
public double lvl_flip_atr { get; set; }
[XmlIgnore]
[Display(Name="Label Text Color", Description="Transparent (the default) uses each line's own color. Pick any color to override.", GroupName="2. Daily Levels", Order=27)]
public Brush lvl_txt_c { get; set; }
[Browsable(false)] public string lvl_txt_cSerialized { get { return Serialize.BrushToString(lvl_txt_c); } set { lvl_txt_c = Serialize.StringToBrush(value); } }
[Display(Name="Enable Level Alerts", Description="Enable alerts for the pasted or file-loaded daily levels. Other structural groups have their own alert switches.", GroupName="2. Daily Levels", Order=28)]
public bool lvl_alerts { get; set; }
[Display(Name="Pivot Alerts", GroupName="2. Daily Levels", Order=29)]
public bool lvl_alert_ip { get; set; }
[Display(Name="Resistance Alerts", GroupName="2. Daily Levels", Order=30)]
public bool lvl_alert_bull { get; set; }
[Display(Name="Support Alerts", GroupName="2. Daily Levels", Order=31)]
public bool lvl_alert_bear { get; set; }
[Display(Name="Enable Volume Profile", GroupName="11. Volume Profile", Order=154)]
public bool mvp_master { get; set; }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="Profile Period", GroupName="11. Volume Profile", Order=155)]
public string mvp_tf_mode { get; set; }
[Display(Name="Session Time (CT)", GroupName="11. Volume Profile", Order=156)]
public string mvp_sess_time { get; set; }
[Display(Name="Session Duration (Hours)", GroupName="11. Volume Profile", Order=157)]
public double mvp_sess_dur { get; set; }
[Display(Name="Calc Precision", GroupName="11. Volume Profile", Order=158)]
public string mvp_calc_tf { get; set; }
[Range(0.25, double.MaxValue)]
[Display(Name="Row Size", GroupName="11. Volume Profile", Order=159)]
public double mvp_row_size { get; set; }
[Display(Name="Value Area %", GroupName="11. Volume Profile", Order=160)]
public double mvp_val_pct { get; set; }
[Range(10, 500)]
[Display(Name="Profile Width (Bars)", GroupName="11. Volume Profile", Order=161)]
public int mvp_profile_width { get; set; }
[Display(Name="Right Align", GroupName="11. Volume Profile", Order=162)]
public bool mvp_align_right { get; set; }
[Range(-200, 200)]
[Display(Name="Profile Offset (Bars)", GroupName="11. Volume Profile", Order=163)]
public int mvp_offset { get; set; }
[Display(Name="Show Walls", GroupName="12. Volume Profile Walls", Order=182)]
public bool mvp_show_walls { get; set; }
[Display(Name="Use Auto-Scale Threshold", GroupName="12. Volume Profile Walls", Order=190)]
public bool mvp_use_auto_scale { get; set; }
[Display(Name="Start Mult", GroupName="12. Volume Profile Walls", Order=191)]
public double mvp_scale_start { get; set; }
[Display(Name="End Mult", GroupName="12. Volume Profile Walls", Order=192)]
public double mvp_scale_end { get; set; }
[Display(Name="Extend Walls", GroupName="12. Volume Profile Walls", Order=187)]
public bool mvp_extend_walls { get; set; }
[Display(Name="Smoothing", GroupName="12. Volume Profile Walls", Order=188)]
public int mvp_smooth_len { get; set; }
[Display(Name="Fixed Mult", GroupName="12. Volume Profile Walls", Order=193)]
public double mvp_vol_mult { get; set; }
[Display(Name="Min Wall Distance (ticks)", GroupName="12. Volume Profile Walls", Order=189)]
public int mvp_min_wall_dist { get; set; }
[Display(Name="Wall Width", GroupName="12. Volume Profile Walls", Order=185)]
public int mvp_w_wall { get; set; }
[XmlIgnore]
[Display(Name="Wall Color", GroupName="12. Volume Profile Walls", Order=183)]
public Brush mvp_c_wall_base { get; set; }
[Browsable(false)] public string mvp_c_wall_baseSerialized { get { return Serialize.BrushToString(mvp_c_wall_base); } set { mvp_c_wall_base = Serialize.StringToBrush(value); } }
[Display(Name="Wall Transparency", GroupName="12. Volume Profile Walls", Order=184)]
public int mvp_wall_transp { get; set; }
[XmlIgnore]
[Display(Name="POC Color", GroupName="11. Volume Profile", Order=169)]
public Brush mvp_c_poc { get; set; }
[Browsable(false)] public string mvp_c_pocSerialized { get { return Serialize.BrushToString(mvp_c_poc); } set { mvp_c_poc = Serialize.StringToBrush(value); } }
[Display(Name="POC Width", GroupName="11. Volume Profile", Order=170)]
public int mvp_poc_width { get; set; }
[XmlIgnore]
[Display(Name="VAH/VAL Color", GroupName="11. Volume Profile", Order=174)]
public Brush mvp_c_levels { get; set; }
[Browsable(false)] public string mvp_c_levelsSerialized { get { return Serialize.BrushToString(mvp_c_levels); } set { mvp_c_levels = Serialize.StringToBrush(value); } }
[Display(Name="VAH/VAL Width", GroupName="11. Volume Profile", Order=175)]
public int mvp_lvl_width { get; set; }
[XmlIgnore]
[Display(Name="VA Histogram Color", GroupName="11. Volume Profile", Order=164)]
public Brush mvp_c_hist_va { get; set; }
[Browsable(false)] public string mvp_c_hist_vaSerialized { get { return Serialize.BrushToString(mvp_c_hist_va); } set { mvp_c_hist_va = Serialize.StringToBrush(value); } }
[Display(Name="VA Transparency", GroupName="11. Volume Profile", Order=165)]
public int mvp_hist_va_transp { get; set; }
[XmlIgnore]
[Display(Name="Outer Histogram Color", GroupName="11. Volume Profile", Order=166)]
public Brush mvp_c_hist_out { get; set; }
[Browsable(false)] public string mvp_c_hist_outSerialized { get { return Serialize.BrushToString(mvp_c_hist_out); } set { mvp_c_hist_out = Serialize.StringToBrush(value); } }
[Display(Name="Outer Transparency", GroupName="11. Volume Profile", Order=167)]
public int mvp_hist_out_transp { get; set; }
[Display(Name="Show POC", GroupName="11. Volume Profile", Order=168)]
public bool mvp_show_poc { get; set; }
[Display(Name="Show VAH", GroupName="11. Volume Profile", Order=172)]
public bool mvp_show_vah { get; set; }
[Display(Name="Show VAL", GroupName="11. Volume Profile", Order=173)]
public bool mvp_show_val { get; set; }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="POC Style", GroupName="11. Volume Profile", Order=171)]
public string mvp_poc_style { get; set; }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="VAH/VAL Style", GroupName="11. Volume Profile", Order=176)]
public string mvp_lvl_style { get; set; }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="Wall Style", GroupName="12. Volume Profile Walls", Order=186)]
public string mvp_wall_style { get; set; }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="Label Size", GroupName="11. Volume Profile", Order=178)]
public string mvp_lb_size { get; set; }
[XmlIgnore]
[Display(Name="Label Text Color", Description="Transparent (the default) uses each line's own color. Pick any color to override.", GroupName="11. Volume Profile", Order=179)]
public Brush mvp_txt_c { get; set; }
[Browsable(false)] public string mvp_txt_cSerialized { get { return Serialize.BrushToString(mvp_txt_c); } set { mvp_txt_c = Serialize.StringToBrush(value); } }
[Display(Name="Enable Profile Alerts", GroupName="11. Volume Profile", Order=180)]
public bool mvp_use_cur_alerts { get; set; }
[Display(Name="Alert Window (CT)", GroupName="11. Volume Profile", Order=181)]
public string mvp_alert_time { get; set; }
[Display(Name="Show pmVAH/pmVAL", GroupName="10. Previous Month", Order=140)]
public bool macro_show_month { get; set; }
[Display(Name="Show pmPOC", GroupName="10. Previous Month", Order=141)]
public bool macro_show_poc { get; set; }
[Display(Name="Show pmHigh/pmLow", GroupName="10. Previous Month", Order=142)]
public bool macro_show_hl { get; set; }
[Display(Name="Alert Prev Month Cross", GroupName="10. Previous Month", Order=153)]
public bool macro_alert_lvl { get; set; }
[XmlIgnore]
[Display(Name="Line Color", GroupName="10. Previous Month", Order=143)]
public Brush macro_color { get; set; }
[Browsable(false)] public string macro_colorSerialized { get { return Serialize.BrushToString(macro_color); } set { macro_color = Serialize.StringToBrush(value); } }
[XmlIgnore]
[Display(Name="POC Color", GroupName="10. Previous Month", Order=146)]
public Brush macro_poc_color { get; set; }
[Browsable(false)] public string macro_poc_colorSerialized { get { return Serialize.BrushToString(macro_poc_color); } set { macro_poc_color = Serialize.StringToBrush(value); } }
[Display(Name="Line Width", GroupName="10. Previous Month", Order=144)]
public int macro_width { get; set; }
[Display(Name="POC Width", GroupName="10. Previous Month", Order=147)]
public int macro_poc_width { get; set; }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="Line Style", GroupName="10. Previous Month", Order=145)]
public string macro_style { get; set; }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="POC Style", GroupName="10. Previous Month", Order=148)]
public string macro_poc_style { get; set; }
[Display(Name="Label Offset", GroupName="10. Previous Month", Order=151)]
public int macro_offset { get; set; }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="Label Size", GroupName="10. Previous Month", Order=150)]
public string macro_text_size { get; set; }
[XmlIgnore]
[Display(Name="Label Text Color", Description="Transparent (the default) uses each line's own color. Pick any color to override.", GroupName="10. Previous Month", Order=152)]
public Brush macro_txt_c { get; set; }
[Browsable(false)] public string macro_txt_cSerialized { get { return Serialize.BrushToString(macro_txt_c); } set { macro_txt_c = Serialize.StringToBrush(value); } }
[Display(Name="Enable IB", GroupName="6. Initial Balance", Order=85)]
public bool ib_enable { get; set; }
[Display(Name="Alert IB High Cross", GroupName="6. Initial Balance", Order=102)]
public bool ib_alert_ibh { get; set; }
[Display(Name="Alert IB Low Cross", GroupName="6. Initial Balance", Order=103)]
public bool ib_alert_ibl { get; set; }
[Display(Name="IB Calc Period (CT)", GroupName="6. Initial Balance", Order=86)]
public string ib_session { get; set; }
[Display(Name="Alert Window (CT)", GroupName="6. Initial Balance", Order=104)]
public string ib_alert_time { get; set; }
[Display(Name="Show IB Midpoint", GroupName="6. Initial Balance", Order=91)]
public bool ib_show_mid { get; set; }
[Display(Name="Show Labels", GroupName="6. Initial Balance", Order=97)]
public bool ib_show_labels { get; set; }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="Label Location", GroupName="6. Initial Balance", Order=98)]
public string ib_label_loc { get; set; }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="Label Size", GroupName="6. Initial Balance", Order=99)]
public string ib_label_size { get; set; }
[Display(Name="Label Offset", GroupName="6. Initial Balance", Order=100)]
public int ib_lbl_offset { get; set; }
[Display(Name="Line Width", GroupName="6. Initial Balance", Order=89)]
public int ib_lvl_width { get; set; }
[XmlIgnore]
[Display(Name="IB High Color", GroupName="6. Initial Balance", Order=87)]
public Brush ib_col_hi { get; set; }
[Browsable(false)] public string ib_col_hiSerialized { get { return Serialize.BrushToString(ib_col_hi); } set { ib_col_hi = Serialize.StringToBrush(value); } }
[XmlIgnore]
[Display(Name="IB Low Color", GroupName="6. Initial Balance", Order=88)]
public Brush ib_col_lo { get; set; }
[Browsable(false)] public string ib_col_loSerialized { get { return Serialize.BrushToString(ib_col_lo); } set { ib_col_lo = Serialize.StringToBrush(value); } }
[XmlIgnore]
[Display(Name="Midpoint Color", GroupName="6. Initial Balance", Order=92)]
public Brush ib_col_mid { get; set; }
[Browsable(false)] public string ib_col_midSerialized { get { return Serialize.BrushToString(ib_col_mid); } set { ib_col_mid = Serialize.StringToBrush(value); } }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="Extend IB Levels", GroupName="6. Initial Balance", Order=94)]
public string ib_extend { get; set; }
[Display(Name="Stop Extension at Time", GroupName="6. Initial Balance", Order=95)]
public bool ib_limit_ext { get; set; }
[Display(Name="Stop Time (CT)", GroupName="6. Initial Balance", Order=96)]
public string ib_stop_time { get; set; }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="High/Low Style", GroupName="6. Initial Balance", Order=90)]
public string ib_style_main { get; set; }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="Midpoint Style", GroupName="6. Initial Balance", Order=93)]
public string ib_style_int { get; set; }
[XmlIgnore]
[Display(Name="Label Text Color", Description="Transparent (the default) uses each line's own color. Pick any color to override.", GroupName="6. Initial Balance", Order=101)]
public Brush ib_txt_c { get; set; }
[Browsable(false)] public string ib_txt_cSerialized { get { return Serialize.BrushToString(ib_txt_c); } set { ib_txt_c = Serialize.StringToBrush(value); } }
[Display(Name="Enable RTH VWAP", GroupName="7. RTH VWAP", Order=105)]
public bool vwap_enable { get; set; }
[XmlIgnore]
[Display(Name="Color", GroupName="7. RTH VWAP", Order=110)]
public Brush vwap_col { get; set; }
[Browsable(false)] public string vwap_colSerialized { get { return Serialize.BrushToString(vwap_col); } set { vwap_col = Serialize.StringToBrush(value); } }
[Display(Name="Width", GroupName="7. RTH VWAP", Order=111)]
public int vwap_width { get; set; }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="Style", GroupName="7. RTH VWAP", Order=112)]
public string vwap_style { get; set; }
[Range(0, 23)]
[Display(Name="Start Hour (CT)", GroupName="7. RTH VWAP", Order=106)]
public int vwap_start_h { get; set; }
[Range(0, 59)]
[Display(Name="Start Minute", GroupName="7. RTH VWAP", Order=107)]
public int vwap_start_m { get; set; }
[Range(0, 23)]
[Display(Name="End Hour (CT)", GroupName="7. RTH VWAP", Order=108)]
public int vwap_end_h { get; set; }
[Range(0, 59)]
[Display(Name="End Minute", GroupName="7. RTH VWAP", Order=109)]
public int vwap_end_m { get; set; }
[Display(Name="Alert VWAP Cross", GroupName="7. RTH VWAP", Order=113)]
public bool vwap_alert { get; set; }
[Display(Name="Show RTH Open (8:30)", GroupName="8. Daily & Weekly Open", Order=114)]
public bool cc_showOpen { get; set; }
[XmlIgnore]
[Display(Name="RTH Open Color", GroupName="8. Daily & Weekly Open", Order=115)]
public Brush cc_c_open { get; set; }
[Browsable(false)] public string cc_c_openSerialized { get { return Serialize.BrushToString(cc_c_open); } set { cc_c_open = Serialize.StringToBrush(value); } }
[Display(Name="RTH Open Width", GroupName="8. Daily & Weekly Open", Order=116)]
public int cc_open_width { get; set; }
[Display(Name="RTH Open Label Offset", GroupName="8. Daily & Weekly Open", Order=124)]
public int cc_open_offset { get; set; }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="RTH Open Style", GroupName="8. Daily & Weekly Open", Order=117)]
public string cc_open_style { get; set; }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="RTH Open Label Size", GroupName="8. Daily & Weekly Open", Order=123)]
public string cc_open_lb_size { get; set; }
[Display(Name="Show Weekly Open (Globex Sunday)", GroupName="8. Daily & Weekly Open", Order=118)]
public bool wk_showOpen { get; set; }
[XmlIgnore]
[Display(Name="Weekly Open Color", GroupName="8. Daily & Weekly Open", Order=119)]
public Brush wk_c_open { get; set; }
[Browsable(false)] public string wk_c_openSerialized { get { return Serialize.BrushToString(wk_c_open); } set { wk_c_open = Serialize.StringToBrush(value); } }
[Display(Name="Weekly Open Width", GroupName="8. Daily & Weekly Open", Order=120)]
public int wk_open_width { get; set; }
[Display(Name="Weekly Open Label Offset", GroupName="8. Daily & Weekly Open", Order=126)]
public int wk_open_offset { get; set; }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="Weekly Open Style", GroupName="8. Daily & Weekly Open", Order=121)]
public string wk_open_style { get; set; }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="Weekly Open Label Size", GroupName="8. Daily & Weekly Open", Order=125)]
public string wk_open_lb_size { get; set; }
[XmlIgnore]
[Display(Name="Label Text Color", Description="Transparent (the default) uses each line's own color. Pick any color to override.", GroupName="8. Daily & Weekly Open", Order=127)]
public Brush opens_txt_c { get; set; }
[Browsable(false)] public string opens_txt_cSerialized { get { return Serialize.BrushToString(opens_txt_c); } set { opens_txt_c = Serialize.StringToBrush(value); } }
[Display(Name="Alert Weekly Open Cross", GroupName="8. Daily & Weekly Open", Order=128)]
public bool wk_alert_open { get; set; }
[Display(Name="Enable OVN High/Low", GroupName="5. Overnight High/Low", Order=72)]
public bool amt_enable { get; set; }
[Display(Name="OVN Session Time (CT)", GroupName="5. Overnight High/Low", Order=73)]
public string amt_sess { get; set; }
[Display(Name="Delete OVN Lines At (CT)", GroupName="5. Overnight High/Low", Order=74)]
public string amt_del_sess { get; set; }
[Display(Name="Show Labels", GroupName="5. Overnight High/Low", Order=79)]
public bool amt_showLb { get; set; }
[Range(1, int.MaxValue)]
[Display(Name="Line Width", GroupName="5. Overnight High/Low", Order=77)]
public int amt_width { get; set; }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="Line Style", GroupName="5. Overnight High/Low", Order=78)]
public string amt_style_in { get; set; }
[XmlIgnore]
[Display(Name="High Color", GroupName="5. Overnight High/Low", Order=75)]
public Brush amt_c_hi { get; set; }
[Browsable(false)] public string amt_c_hiSerialized { get { return Serialize.BrushToString(amt_c_hi); } set { amt_c_hi = Serialize.StringToBrush(value); } }
[XmlIgnore]
[Display(Name="High Label Text Color", GroupName="5. Overnight High/Low", Order=82)]
public Brush amt_c_txt_hi { get; set; }
[Browsable(false)] public string amt_c_txt_hiSerialized { get { return Serialize.BrushToString(amt_c_txt_hi); } set { amt_c_txt_hi = Serialize.StringToBrush(value); } }
[XmlIgnore]
[Display(Name="Low Color", GroupName="5. Overnight High/Low", Order=76)]
public Brush amt_c_lo { get; set; }
[Browsable(false)] public string amt_c_loSerialized { get { return Serialize.BrushToString(amt_c_lo); } set { amt_c_lo = Serialize.StringToBrush(value); } }
[XmlIgnore]
[Display(Name="Low Label Text Color", GroupName="5. Overnight High/Low", Order=83)]
public Brush amt_c_txt_lo { get; set; }
[Browsable(false)] public string amt_c_txt_loSerialized { get { return Serialize.BrushToString(amt_c_txt_lo); } set { amt_c_txt_lo = Serialize.StringToBrush(value); } }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="Label Size", GroupName="5. Overnight High/Low", Order=80)]
public string amt_lb_size_in { get; set; }
[Display(Name="Label Offset", GroupName="5. Overnight High/Low", Order=81)]
public int amt_offset { get; set; }
[Display(Name="Alert OVN Break", GroupName="5. Overnight High/Low", Order=84)]
public bool amt_alert_ovn { get; set; }
[Display(Name="Auto Bin Size (tick)", GroupName="14. Naked POCs", Order=224)]
public bool amt_vauto { get; set; }
[Display(Name="Use Manual Bin Size", GroupName="14. Naked POCs", Order=225)]
public bool amt_vres { get; set; }
[Range(0.0001, double.MaxValue)]
[Display(Name="Manual Bin Size", GroupName="14. Naked POCs", Order=226)]
public double amt_res_val { get; set; }
[Display(Name="Enable Prev Day Levels", GroupName="3. Previous Day High/Low/Open", Order=32)]
public bool pd_enable { get; set; }
[Display(Name="Use Custom Session for High/Low", GroupName="3. Previous Day High/Low/Open", Order=33)]
public bool pd_use_rth { get; set; }
[Display(Name="Custom Session Time (CT)", GroupName="3. Previous Day High/Low/Open", Order=34)]
public string pd_sess_time { get; set; }
[Display(Name="Show pdHigh", GroupName="3. Previous Day High/Low/Open", Order=35)]
public bool pd_showH { get; set; }
[Display(Name="Show pdLow", GroupName="3. Previous Day High/Low/Open", Order=36)]
public bool pd_showL { get; set; }
[Display(Name="Show pdOpen", GroupName="3. Previous Day High/Low/Open", Order=37)]
public bool pd_showOpen { get; set; }
[XmlIgnore]
[Display(Name="pdHigh Color", GroupName="3. Previous Day High/Low/Open", Order=38)]
public Brush pd_high_c { get; set; }
[Browsable(false)] public string pd_high_cSerialized { get { return Serialize.BrushToString(pd_high_c); } set { pd_high_c = Serialize.StringToBrush(value); } }
[XmlIgnore]
[Display(Name="pdLow Color", GroupName="3. Previous Day High/Low/Open", Order=39)]
public Brush pd_low_c { get; set; }
[Browsable(false)] public string pd_low_cSerialized { get { return Serialize.BrushToString(pd_low_c); } set { pd_low_c = Serialize.StringToBrush(value); } }
[XmlIgnore]
[Display(Name="pdOpen Color", GroupName="3. Previous Day High/Low/Open", Order=40)]
public Brush pd_open_c { get; set; }
[Browsable(false)] public string pd_open_cSerialized { get { return Serialize.BrushToString(pd_open_c); } set { pd_open_c = Serialize.StringToBrush(value); } }
[Display(Name="pdHigh Width", GroupName="3. Previous Day High/Low/Open", Order=41)]
public int pd_high_width { get; set; }
[Display(Name="pdLow Width", GroupName="3. Previous Day High/Low/Open", Order=42)]
public int pd_low_width { get; set; }
[Display(Name="pdOpen Width", GroupName="3. Previous Day High/Low/Open", Order=43)]
public int pd_open_width { get; set; }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="Line Style", GroupName="3. Previous Day High/Low/Open", Order=44)]
public string pd_style_in { get; set; }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="Label Size", GroupName="3. Previous Day High/Low/Open", Order=46)]
public string pd_lb_size_in { get; set; }
[Display(Name="Label Offset", GroupName="3. Previous Day High/Low/Open", Order=47)]
public int pd_offset_val { get; set; }
[XmlIgnore]
[Display(Name="Label Text Color", Description="Transparent (the default) uses each line's own color. Pick any color to override.", GroupName="3. Previous Day High/Low/Open", Order=48)]
public Brush pd_txt_c { get; set; }
[Browsable(false)] public string pd_txt_cSerialized { get { return Serialize.BrushToString(pd_txt_c); } set { pd_txt_c = Serialize.StringToBrush(value); } }
[Display(Name="Alert High/Low Break", GroupName="3. Previous Day High/Low/Open", Order=49)]
public bool pd_alert_hl { get; set; }
[Display(Name="Enable Prev Day Value & 80% Rule", GroupName="4. Previous Day Value & 80% Rule", Order=50)]
public bool cc_enable { get; set; }
[Display(Name="Show pdPOC", GroupName="4. Previous Day Value & 80% Rule", Order=53)]
public bool cc_show_poc { get; set; }
[Display(Name="Show pdVAH", GroupName="4. Previous Day Value & 80% Rule", Order=54)]
public bool cc_show_vah { get; set; }
[Display(Name="Show pdVAL", GroupName="4. Previous Day Value & 80% Rule", Order=55)]
public bool cc_show_val { get; set; }
[Display(Name="Alert pdVAH/pdVAL/pdPOC Cross", GroupName="4. Previous Day Value & 80% Rule", Order=70)]
public bool cc_alert_va { get; set; }
[Display(Name="Alert 80% Rule Confirmed", GroupName="4. Previous Day Value & 80% Rule", Order=71)]
public bool cc_alert_80 { get; set; }
[XmlIgnore]
[Display(Name="pdPOC Color", GroupName="4. Previous Day Value & 80% Rule", Order=56)]
public Brush pd_poc_c { get; set; }
[Browsable(false)] public string pd_poc_cSerialized { get { return Serialize.BrushToString(pd_poc_c); } set { pd_poc_c = Serialize.StringToBrush(value); } }
[XmlIgnore]
[Display(Name="pdVAH Color", GroupName="4. Previous Day Value & 80% Rule", Order=57)]
public Brush pd_vah_c { get; set; }
[Browsable(false)] public string pd_vah_cSerialized { get { return Serialize.BrushToString(pd_vah_c); } set { pd_vah_c = Serialize.StringToBrush(value); } }
[XmlIgnore]
[Display(Name="pdVAL Color", GroupName="4. Previous Day Value & 80% Rule", Order=58)]
public Brush pd_val_c { get; set; }
[Browsable(false)] public string pd_val_cSerialized { get { return Serialize.BrushToString(pd_val_c); } set { pd_val_c = Serialize.StringToBrush(value); } }
[Display(Name="pdPOC Width", GroupName="4. Previous Day Value & 80% Rule", Order=59)]
public int cc_poc_width { get; set; }
[Display(Name="pdVAH Width", GroupName="4. Previous Day Value & 80% Rule", Order=60)]
public int cc_vah_width { get; set; }
[Display(Name="pdVAL Width", GroupName="4. Previous Day Value & 80% Rule", Order=61)]
public int cc_val_width { get; set; }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="Line Style", GroupName="4. Previous Day Value & 80% Rule", Order=62)]
public string cc_poc_style_in { get; set; }
[Display(Name="Label Offset", GroupName="4. Previous Day Value & 80% Rule", Order=65)]
public int cc_lb_offset { get; set; }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="Label Size", GroupName="4. Previous Day Value & 80% Rule", Order=64)]
public string cc_lb_size_in { get; set; }
[XmlIgnore]
[Display(Name="Label Text Color", Description="Transparent (the default) uses each line's own color. Pick any color to override.", GroupName="4. Previous Day Value & 80% Rule", Order=66)]
public Brush cc_txt_c { get; set; }
[Browsable(false)] public string cc_txt_cSerialized { get { return Serialize.BrushToString(cc_txt_c); } set { cc_txt_c = Serialize.StringToBrush(value); } }
[Display(Name="Show 80% Rule Fill", GroupName="4. Previous Day Value & 80% Rule", Order=67)]
public bool cc_showFill { get; set; }
[XmlIgnore]
[Display(Name="Fill: Active (Setup)", GroupName="4. Previous Day Value & 80% Rule", Order=68)]
public Brush cc_col_active { get; set; }
[Browsable(false)] public string cc_col_activeSerialized { get { return Serialize.BrushToString(cc_col_active); } set { cc_col_active = Serialize.StringToBrush(value); } }
[XmlIgnore]
[Display(Name="Fill: Confirmed (80%)", GroupName="4. Previous Day Value & 80% Rule", Order=69)]
public Brush cc_col_conf { get; set; }
[Browsable(false)] public string cc_col_confSerialized { get { return Serialize.BrushToString(cc_col_conf); } set { cc_col_conf = Serialize.StringToBrush(value); } }
[Range(0.01, 1.0)]
[Display(Name="Value Area %", GroupName="4. Previous Day Value & 80% Rule", Order=51)]
public double cc_va_pct { get; set; }
[Display(Name="RTH Session for VA (CT)", GroupName="4. Previous Day Value & 80% Rule", Order=52)]
public string cc_sess_def { get; set; }
[Display(Name="Enable Prev Week Levels", GroupName="9. Previous Week", Order=129)]
public bool pw_enable { get; set; }
[Display(Name="Show pwHigh", GroupName="9. Previous Week", Order=130)]
public bool pw_showH { get; set; }
[Display(Name="Show pwLow", GroupName="9. Previous Week", Order=131)]
public bool pw_showL { get; set; }
[XmlIgnore]
[Display(Name="Line Color", GroupName="9. Previous Week", Order=132)]
public Brush pw_col { get; set; }
[Browsable(false)] public string pw_colSerialized { get { return Serialize.BrushToString(pw_col); } set { pw_col = Serialize.StringToBrush(value); } }
[XmlIgnore]
[Display(Name="Label Text Color", GroupName="9. Previous Week", Order=138)]
public Brush pw_c_txt { get; set; }
[Browsable(false)] public string pw_c_txtSerialized { get { return Serialize.BrushToString(pw_c_txt); } set { pw_c_txt = Serialize.StringToBrush(value); } }
[Display(Name="Line Width", GroupName="9. Previous Week", Order=133)]
public int pw_width { get; set; }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="Line Style", GroupName="9. Previous Week", Order=134)]
public string pw_style_in { get; set; }
[Display(Name="Label Offset", GroupName="9. Previous Week", Order=137)]
public int pw_offset { get; set; }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="Label Size", GroupName="9. Previous Week", Order=136)]
public string pw_lb_size_in { get; set; }
[Display(Name="Alert High/Low Break", GroupName="9. Previous Week", Order=139)]
public bool pw_alert_hl { get; set; }
[Display(Name="TPO Session (CT)", GroupName="13. TPO & Single Prints", Order=194)]
public string sp_rth_session { get; set; }
[Range(1, int.MaxValue)]
[Display(Name="TPO Period (min)", GroupName="13. TPO & Single Prints", Order=195)]
public int sp_tpo_minutes { get; set; }
[Range(1, int.MaxValue)]
[Display(Name="Ticks per Block", GroupName="13. TPO & Single Prints", Order=196)]
public int sp_ticks_per_block { get; set; }
[XmlIgnore]
[Display(Name="Single Print Color", GroupName="13. TPO & Single Prints", Order=201)]
public Brush sp_color_hist { get; set; }
[Browsable(false)] public string sp_color_histSerialized { get { return Serialize.BrushToString(sp_color_hist); } set { sp_color_hist = Serialize.StringToBrush(value); } }
[XmlIgnore]
[Display(Name="Border Color", GroupName="13. TPO & Single Prints", Order=202)]
public Brush sp_border_c { get; set; }
[Browsable(false)] public string sp_border_cSerialized { get { return Serialize.BrushToString(sp_border_c); } set { sp_border_c = Serialize.StringToBrush(value); } }
[Range(0, 4)]
[Display(Name="Border Width", Description="0 = no border.", GroupName="13. TPO & Single Prints", Order=203)]
public int sp_border_w { get; set; }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="Border Style", GroupName="13. TPO & Single Prints", Order=204)]
public string sp_border_style { get; set; }
[Display(Name="Max History Days", GroupName="13. TPO & Single Prints", Order=200)]
public int sp_max_days { get; set; }
[Display(Name="Filter Tails", GroupName="13. TPO & Single Prints", Order=198)]
public bool sp_filter_tails { get; set; }
[Display(Name="Max Tail Size", GroupName="13. TPO & Single Prints", Order=199)]
public int sp_tail_max { get; set; }
[Display(Name="Min Single Print Size (ticks)", GroupName="13. TPO & Single Prints", Order=197)]
public int sp_min_ticks { get; set; }
[Display(Name="Show Poor Highs/Lows", GroupName="13. TPO & Single Prints", Order=205)]
public bool sp_show_poor { get; set; }
[Display(Name="Poor H/L Width", GroupName="13. TPO & Single Prints", Order=208)]
public int sp_poor_width { get; set; }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="Poor H/L Style", GroupName="13. TPO & Single Prints", Order=209)]
public string sp_poor_style_in { get; set; }
[XmlIgnore]
[Display(Name="Poor High Color", GroupName="13. TPO & Single Prints", Order=206)]
public Brush sp_col_poor_h { get; set; }
[Browsable(false)] public string sp_col_poor_hSerialized { get { return Serialize.BrushToString(sp_col_poor_h); } set { sp_col_poor_h = Serialize.StringToBrush(value); } }
[XmlIgnore]
[Display(Name="Poor Low Color", GroupName="13. TPO & Single Prints", Order=207)]
public Brush sp_col_poor_l { get; set; }
[Browsable(false)] public string sp_col_poor_lSerialized { get { return Serialize.BrushToString(sp_col_poor_l); } set { sp_col_poor_l = Serialize.StringToBrush(value); } }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="Label Size", GroupName="13. TPO & Single Prints", Order=211)]
public string sp_poor_lb_size { get; set; }
[XmlIgnore]
[Display(Name="Label Text Color", Description="Transparent (the default) uses each line's own color. Pick any color to override.", GroupName="13. TPO & Single Prints", Order=212)]
public Brush sp_poor_txt_c { get; set; }
[Browsable(false)] public string sp_poor_txt_cSerialized { get { return Serialize.BrushToString(sp_poor_txt_c); } set { sp_poor_txt_c = Serialize.StringToBrush(value); } }
[Display(Name="Show Late Spike", GroupName="16. Late Spike", Order=249)]
public bool showSpike { get; set; }
[Display(Name="Spike Window (CT)", GroupName="16. Late Spike", Order=250)]
public string spikeTimeRange { get; set; }
[Display(Name="Line Width", GroupName="16. Late Spike", Order=253)]
public int spike_width { get; set; }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="Line Style", GroupName="16. Late Spike", Order=254)]
public string spike_style { get; set; }
[XmlIgnore]
[Display(Name="Support Color", GroupName="16. Late Spike", Order=251)]
public Brush spike_c_sup { get; set; }
[Browsable(false)] public string spike_c_supSerialized { get { return Serialize.BrushToString(spike_c_sup); } set { spike_c_sup = Serialize.StringToBrush(value); } }
[XmlIgnore]
[Display(Name="Resistance Color", GroupName="16. Late Spike", Order=252)]
public Brush spike_c_res { get; set; }
[Browsable(false)] public string spike_c_resSerialized { get { return Serialize.BrushToString(spike_c_res); } set { spike_c_res = Serialize.StringToBrush(value); } }
[Display(Name="Label Offset", GroupName="16. Late Spike", Order=257)]
public int spike_offset { get; set; }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="Label Size", GroupName="16. Late Spike", Order=256)]
public string spike_lbl_size { get; set; }
[XmlIgnore]
[Display(Name="Label Text Color", Description="Transparent (the default) uses each line's own color. Pick any color to override.", GroupName="16. Late Spike", Order=258)]
public Brush spike_txt_c { get; set; }
[Browsable(false)] public string spike_txt_cSerialized { get { return Serialize.BrushToString(spike_txt_c); } set { spike_txt_c = Serialize.StringToBrush(value); } }
[Display(Name="Alert Spike Base Test", GroupName="16. Late Spike", Order=259)]
public bool spike_alert { get; set; }
[Display(Name="Enable Inventory Trap Logic", GroupName="15. Settlement & Inventory", Order=232)]
public bool inv_enable { get; set; }
[Display(Name="Show Settlement Line", GroupName="15. Settlement & Inventory", Order=233)]
public bool inv_show_line { get; set; }
[Range(0, 23)]
[Display(Name="Globex Start Hour (CT)", GroupName="15. Settlement & Inventory", Order=234)]
public int inv_start_h { get; set; }
[Range(0, 23)]
[Display(Name="RTH Open Hour (CT)", GroupName="15. Settlement & Inventory", Order=235)]
public int inv_end_h { get; set; }
[Range(0, 59)]
[Display(Name="RTH Open Minute", GroupName="15. Settlement & Inventory", Order=236)]
public int inv_end_m { get; set; }
[XmlIgnore]
[Display(Name="Trap Color", GroupName="15. Settlement & Inventory", Order=240)]
public Brush inv_c_trap { get; set; }
[Browsable(false)] public string inv_c_trapSerialized { get { return Serialize.BrushToString(inv_c_trap); } set { inv_c_trap = Serialize.StringToBrush(value); } }
[XmlIgnore]
[Display(Name="Settle Color", GroupName="15. Settlement & Inventory", Order=237)]
public Brush inv_c_settle { get; set; }
[Browsable(false)] public string inv_c_settleSerialized { get { return Serialize.BrushToString(inv_c_settle); } set { inv_c_settle = Serialize.StringToBrush(value); } }
[Display(Name="Trap Text", GroupName="15. Settlement & Inventory", Order=243)]
public string inv_txt_trap { get; set; }
[Display(Name="Trap Line Width", GroupName="15. Settlement & Inventory", Order=241)]
public int inv_width { get; set; }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="Trap Line Style", GroupName="15. Settlement & Inventory", Order=242)]
public string inv_style_in { get; set; }
[Range(1, 10)]
[Display(Name="Settle Line Width", GroupName="15. Settlement & Inventory", Order=238)]
public int inv_settle_width { get; set; }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="Settle Line Style", GroupName="15. Settlement & Inventory", Order=239)]
public string inv_settle_style { get; set; }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="Label Size", GroupName="15. Settlement & Inventory", Order=245)]
public string inv_lb_size_in { get; set; }
[Display(Name="Label Offset", GroupName="15. Settlement & Inventory", Order=246)]
public int inv_offset { get; set; }
[XmlIgnore]
[Display(Name="Label Text Color", Description="Transparent (the default) uses each line's own color. Pick any color to override.", GroupName="15. Settlement & Inventory", Order=247)]
public Brush inv_txt_c { get; set; }
[Browsable(false)] public string inv_txt_cSerialized { get { return Serialize.BrushToString(inv_txt_c); } set { inv_txt_c = Serialize.StringToBrush(value); } }
[Display(Name="Alert Trap Detected", GroupName="15. Settlement & Inventory", Order=248)]
public bool inv_alert { get; set; }
[Display(Name="Enable nPOCs", GroupName="14. Naked POCs", Order=213)]
public bool npoc_enable { get; set; }
[XmlIgnore]
[Display(Name="Daily Color", GroupName="14. Naked POCs", Order=217)]
public Brush npoc_c { get; set; }
[Browsable(false)] public string npoc_cSerialized { get { return Serialize.BrushToString(npoc_c); } set { npoc_c = Serialize.StringToBrush(value); } }
[XmlIgnore]
[Display(Name="Weekly Color", GroupName="14. Naked POCs", Order=218)]
public Brush npoc_c_w { get; set; }
[Browsable(false)] public string npoc_c_wSerialized { get { return Serialize.BrushToString(npoc_c_w); } set { npoc_c_w = Serialize.StringToBrush(value); } }
[XmlIgnore]
[Display(Name="Monthly Color", GroupName="14. Naked POCs", Order=219)]
public Brush npoc_c_m { get; set; }
[Browsable(false)] public string npoc_c_mSerialized { get { return Serialize.BrushToString(npoc_c_m); } set { npoc_c_m = Serialize.StringToBrush(value); } }
[Display(Name="Show Daily nPOC", GroupName="14. Naked POCs", Order=214)]
public bool npoc_d_show { get; set; }
[Display(Name="Show Weekly nPOC", GroupName="14. Naked POCs", Order=215)]
public bool npoc_w_show { get; set; }
[Display(Name="Show Monthly nPOC", GroupName="14. Naked POCs", Order=216)]
public bool npoc_m_show { get; set; }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="Line Style", GroupName="14. Naked POCs", Order=223)]
public string npoc_style_in { get; set; }
[Display(Name="Daily Width", GroupName="14. Naked POCs", Order=220)]
public int npoc_w_d { get; set; }
[Display(Name="Weekly Width", GroupName="14. Naked POCs", Order=221)]
public int npoc_w_w { get; set; }
[Display(Name="Monthly Width", GroupName="14. Naked POCs", Order=222)]
public int npoc_w_m { get; set; }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="Label Size", GroupName="14. Naked POCs", Order=228)]
public string npoc_lb_size { get; set; }
[Range(-50, 200)]
[Display(Name="Label Offset", GroupName="14. Naked POCs", Order=229)]
public int npoc_lb_offset { get; set; }
[XmlIgnore]
[Display(Name="Label Text Color", Description="Transparent (the default) uses each line's own color. Pick any color to override.", GroupName="14. Naked POCs", Order=230)]
public Brush npoc_txt_c { get; set; }
[Browsable(false)] public string npoc_txt_cSerialized { get { return Serialize.BrushToString(npoc_txt_c); } set { npoc_txt_c = Serialize.StringToBrush(value); } }
[Display(Name="Alert nPOC Touch", GroupName="14. Naked POCs", Order=231)]
public bool npoc_alert { get; set; }
[Display(Name="Context Tracking Session (CT)", GroupName="17. Dashboard", Order=264)]
public string context_session { get; set; }
[Display(Name="Daily ATR Period", GroupName="17. Dashboard", Order=265)]
public int daily_atr_length { get; set; }
[Display(Name="Show Dashboard", GroupName="17. Dashboard", Order=260)]
public bool tbl_show { get; set; }
[Display(Name="Table RTH Session (CT)", GroupName="17. Dashboard", Order=263)]
public string tbl_session { get; set; }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="Position", GroupName="17. Dashboard", Order=261)]
public string tbl_pos { get; set; }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="Text Size", GroupName="17. Dashboard", Order=262)]
public string tbl_size { get; set; }
[Display(Name="Hide All Labels & Price Tags", GroupName="1. Global Settings", Order=5)]
public bool HideAllLabels { get; set; }
[Display(Name="Show Labels", GroupName="2. Daily Levels", Order=24)]
public bool LabelsLevels { get; set; }
[Display(Name="Show Labels", GroupName="11. Volume Profile", Order=177)]
public bool LabelsProfiles { get; set; }
[Display(Name="Show Labels", GroupName="3. Previous Day High/Low/Open", Order=45)]
public bool LabelsPriorDay { get; set; }
[Display(Name="Show Labels", GroupName="4. Previous Day Value & 80% Rule", Order=63)]
public bool LabelsPriorValue { get; set; }
[Display(Name="Show Labels", GroupName="8. Daily & Weekly Open", Order=122)]
public bool LabelsOpens { get; set; }
[Display(Name="Show Labels", GroupName="9. Previous Week", Order=135)]
public bool LabelsPriorWeek { get; set; }
[Display(Name="Show Labels", GroupName="10. Previous Month", Order=149)]
public bool LabelsPriorMonth { get; set; }
[Display(Name="Show Labels", GroupName="15. Settlement & Inventory", Order=244)]
public bool LabelsInventory { get; set; }
[Display(Name="Show Labels", GroupName="16. Late Spike", Order=255)]
public bool LabelsSpike { get; set; }
[Display(Name="Show Labels", GroupName="14. Naked POCs", Order=227)]
public bool LabelsNakedPoc { get; set; }
[Display(Name="Show Poor H/L Labels", GroupName="13. TPO & Single Prints", Order=210)]
public bool LabelsPoor { get; set; }
[Display(Name="Show Price-Axis Tags", GroupName="1. Global Settings", Order=6)]
public bool ShowPriceTags { get; set; }
[TypeConverter(typeof(TatankaAmtToolkitOptions))]
[Display(Name="Level Alert Mode", Description="Choose crosses, entries into a price band, or both. This applies to enabled level-cross alert categories. Touch, inventory and 80% rule alerts keep their own behavior.", GroupName="18. Alerts", Order=269)]
public string LevelAlertMode { get; set; }
[Range(1, 1000)]
[Display(Name="Structure Alert Zone Half-Width (ticks)", Description="Half-width for structural level alert bands in actual instrument ticks. Pasted/file daily levels use their own configured ATR/tick zone width.", GroupName="18. Alerts", Order=270)]
public int AlertZoneTicks { get; set; }
[Range(200, 100000)]
[Display(Name="Maximum Rows per Profile / TPO", GroupName="20. NinjaTrader Limits", Order=275)]
public int MaxProfileRows { get; set; }
[Range(100, 2000)]
[Display(Name="Maximum Retained Structures", GroupName="20. NinjaTrader Limits", Order=276)]
public int MaxStructures { get; set; }
}
public sealed class TatankaAmtToolkitOptions : StringConverter {
public static readonly Dictionary<string,string[]> Choices = new Dictionary<string,string[]> {
{"layer_top_pick", new string[] {"Daily Levels (All)", "Profile POC", "Profile VAH", "Profile VAL", "IB Midpoint", "IB High", "IB Low", "pdPOC", "Prev Day High", "Prev Day Low", "OVN High", "OVN Low", "Prev Month VAH", "Prev Month VAL", "Spike Base"}},
{"lvl_style_in", new string[] {"Solid", "Dashed", "Dotted"}},
{"lvl_zone_style_in", new string[] {"Solid", "Dashed", "Dotted"}},
{"lvl_text_size_opt", new string[] {"auto", "tiny", "small", "normal", "large", "huge"}},
{"lvl_zone_mode", new string[] {"ATR %", "Ticks"}},
{"row_scale", new string[] {"Auto", "1x", "2x", "5x", "10x", "20x", "50x", "100x"}},
{"mvp_tf_mode", new string[] {"Session", "Weekly", "Monthly", "Yearly"}},
{"macro_style", new string[] {"Solid", "Dashed", "Dotted"}},
{"macro_poc_style", new string[] {"Solid", "Dashed", "Dotted"}},
{"macro_text_size", new string[] {"Tiny", "Small", "Normal", "Large"}},
{"ib_label_loc", new string[] {"Left", "Right"}},
{"ib_label_size", new string[] {"Auto", "Huge", "Large", "Normal", "Small", "Tiny"}},
{"ib_extend", new string[] {"Right", "Left", "Both", "None"}},
{"ib_style_main", new string[] {"Solid", "Dashed", "Dotted"}},
{"ib_style_int", new string[] {"Solid", "Dashed", "Dotted"}},
{"amt_style_in", new string[] {"Solid", "Dashed", "Dotted"}},
{"amt_lb_size_in", new string[] {"Tiny", "Small", "Normal", "Large"}},
{"pd_style_in", new string[] {"Solid", "Dashed", "Dotted"}},
{"pd_lb_size_in", new string[] {"Tiny", "Small", "Normal", "Large"}},
{"cc_poc_style_in", new string[] {"Solid", "Dashed", "Dotted"}},
{"cc_lb_size_in", new string[] {"Tiny", "Small", "Normal", "Large"}},
{"pw_style_in", new string[] {"Solid", "Dashed", "Dotted"}},
{"pw_lb_size_in", new string[] {"Tiny", "Small", "Normal", "Large"}},
{"sp_poor_style_in", new string[] {"Solid", "Dashed", "Dotted"}},
{"spike_lbl_size", new string[] {"Tiny", "Small", "Normal", "Large"}},
{"inv_style_in", new string[] {"Solid", "Dashed", "Dotted"}},
{"inv_lb_size_in", new string[] {"Tiny", "Small", "Normal", "Large"}},
{"npoc_style_in", new string[] {"Solid", "Dashed", "Dotted"}},
{"tbl_pos", new string[] {"Top Right", "Top Left", "Bottom Right", "Bottom Left", "Middle Right", "Middle Left"}},
{"tbl_size", new string[] {"Tiny", "Small", "Normal", "Large"}},
{"mvp_poc_style", new string[] {"Solid", "Dashed", "Dotted"}},
{"mvp_lvl_style", new string[] {"Solid", "Dashed", "Dotted"}},
{"mvp_wall_style", new string[] {"Solid", "Dashed", "Dotted"}},
{"mvp_lb_size", new string[] {"Tiny", "Small", "Normal", "Large", "Huge"}},
{"vwap_style", new string[] {"Solid", "Dashed", "Dotted"}},
{"cc_open_style", new string[] {"Solid", "Dashed", "Dotted"}},
{"cc_open_lb_size", new string[] {"Tiny", "Small", "Normal", "Large", "Huge"}},
{"wk_open_style", new string[] {"Solid", "Dashed", "Dotted"}},
{"wk_open_lb_size", new string[] {"Tiny", "Small", "Normal", "Large", "Huge"}},
{"sp_border_style", new string[] {"Solid", "Dashed", "Dotted"}},
{"sp_poor_lb_size", new string[] {"Tiny", "Small", "Normal", "Large", "Huge"}},
{"spike_style", new string[] {"Solid", "Dashed", "Dotted"}},
{"inv_settle_style", new string[] {"Solid", "Dashed", "Dotted"}},
{"npoc_lb_size", new string[] {"Tiny", "Small", "Normal", "Large", "Huge"}},
{"LevelAlertMode", new string[] {"Cross", "Zone Entry", "Both"}},
};
public override bool GetStandardValuesSupported(ITypeDescriptorContext context) { return true; }
public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) { return true; }
public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) { string[] choices; return new StandardValuesCollection(context != null && context.PropertyDescriptor != null && Choices.TryGetValue(context.PropertyDescriptor.Name, out choices) ? choices : new string[0]); }
}}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private TatankaAmtToolkit[] cacheTatankaAmtToolkit;
		public TatankaAmtToolkit TatankaAmtToolkit()
		{
			return TatankaAmtToolkit(Input);
		}

		public TatankaAmtToolkit TatankaAmtToolkit(ISeries<double> input)
		{
			if (cacheTatankaAmtToolkit != null)
				for (int idx = 0; idx < cacheTatankaAmtToolkit.Length; idx++)
					if (cacheTatankaAmtToolkit[idx] != null &&  cacheTatankaAmtToolkit[idx].EqualsInput(input))
						return cacheTatankaAmtToolkit[idx];
			return CacheIndicator<TatankaAmtToolkit>(new TatankaAmtToolkit(), input, ref cacheTatankaAmtToolkit);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.TatankaAmtToolkit TatankaAmtToolkit()
		{
			return indicator.TatankaAmtToolkit(Input);
		}

		public Indicators.TatankaAmtToolkit TatankaAmtToolkit(ISeries<double> input )
		{
			return indicator.TatankaAmtToolkit(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.TatankaAmtToolkit TatankaAmtToolkit()
		{
			return indicator.TatankaAmtToolkit(Input);
		}

		public Indicators.TatankaAmtToolkit TatankaAmtToolkit(ISeries<double> input )
		{
			return indicator.TatankaAmtToolkit(input);
		}
	}
}

#endregion
