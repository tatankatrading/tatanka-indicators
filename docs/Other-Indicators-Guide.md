<p align="center"><img src="../assets/logo.png" width="72" alt="Tatanka Trading logo"></p>

# TPO Profile, If/Then, 1hr 21 EMA and Level Lines: Guide

Version 1.0.0 · [Tatanka Trading](https://tatankatrading.com) · Free and open source (MPL 2.0)

Free indicators that sit beside the [Tatanka AMT Toolkit](NinjaTrader-Guide.md). TPO Profile, If/Then and 1hr 21 EMA are on NinjaTrader 8 and TradingView; Level Lines is NinjaTrader 8 only.

| Indicator | TradingView script | NinjaTrader download | NinjaTrader name |
| --- | --- | --- | --- |
| TPO Profile | [TPO Profile](https://tatankatrading.com/tv/tpo) | [TatankaTPO_NT8.zip](https://tatankatrading.com/nt/tpo) | TPO Profile |
| If/Then | [If/Then Bias Table](https://tatankatrading.com/tv/ifthen) | [TatankaIfThen_NT8.zip](https://tatankatrading.com/nt/ifthen) | If/Then Bias Table |
| 1hr 21 EMA | [HTF EMA (1H 21 by default)](https://tatankatrading.com/tv/21ema) | [Tatanka21EMA_NT8.zip](https://tatankatrading.com/nt/21ema) | HTF EMA (1H 21 by default) |
| Level Lines | NinjaTrader only | [TatankaLevelLines_NT8.zip](https://github.com/TatankaTrading/tatanka-indicators/releases/latest/download/TatankaLevelLines_NT8.zip) | Level Lines |

The AMT Toolkit, TPO Profile, If/Then and 1hr 21 EMA also come in one zip: [TatankaAll_NT8.zip](https://tatankatrading.com/nt/all). Level Lines is a separate download.

## Install

**TradingView:** open the script page, click **Add to favorites**, then on your chart open **Indicators → Favorites** and click the script.

**NinjaTrader 8:** download the zip (don't unzip it), then **Tools → Import → NinjaScript Add-On** and pick the zip. Add the indicator from the chart's **Indicators** window.

## TPO Profile

TPO (Market Profile) letters for each 30-minute period of the session, with the POC, the value area and a heatmap gradient from the open to the close. Recent days get full letter detail; older days get an outline.

Settings worth knowing:

- **Session Time / Filter by Time (RTH Only):** the session the profile is built from. On NinjaTrader this is in your **chart's time zone** (0830-1500 is RTH for Central time; use 0930-1600 for Eastern).
- **Block Size (Minutes):** 30 by default, one letter per block.
- **Auto Row Height / Target Rows Per Day:** sizes the rows from the daily ATR, so ES and NQ both look right without changing settings. Turn Auto off to set rows in ticks.
- **Days with Letter Detail / Max Polyline Days:** how many days get letters and how many get outlines. Lower them if the chart is slow.
- **Show Value Area, Extend POC Through Next RTH Session, Show Developing:** the value area lines, carrying the POC into the next session, and the live session's profile.

## If/Then

Turns the afternoon review's if/then plan into a live table on the chart. Each band is a price range with its bias and plan; the band price is in right now is highlighted, and a line is drawn at each band's bottom (LO).

Paste the review's block into **If/Then block → Rows** (NinjaTrader opens a multi-line editor), one band per line, top band first:

```
BIAS|LO|HI|TEXT
```

- **LO / HI:** the band's bottom and top. `0` means no bottom; `9999999` means no top.
- **TEXT:** the plan for that band. A literal `\n` (backslash, n) starts a new line in the box. Don't use `|` inside the text.
- **BIAS:** the letter the afternoon review uses for that band; it sets the band's color.

Settings worth knowing:

- **Line/label color:** *Bias* colors each line by its band's bias; *Position* colors it by where it sits versus price (above = resistance, below = support, within a daily-ATR band = neutral).
- **Plot ATR zones around lines / Zone ATR %:** shaded zones sized by daily ATR.
- **Table position, Text size, Show table:** where the table sits and whether it shows.

The afternoon review's if/then blocks come with the $20/month Patreon, along with the daily ML levels. See [tatankatrading.com](https://tatankatrading.com).

## 1hr 21 EMA

The 21-period EMA from the 1-hour chart, drawn on any chart timeframe. Change **Source timeframe** and **EMA length** for a different higher-timeframe EMA.

**Mode:**

- **Live:** the developing 1-hour EMA, updating with each bar.
- **Exact:** the last closed 1-hour EMA, stepping once an hour.
- **Smooth:** an EMA on your chart's timeframe with a scaled length, so the line curves smoothly. It can drift slightly from the true 1-hour value. NinjaTrader's default.

## Level Lines

NinjaTrader 8 only. Labeled horizontal lines you type in: a range box plus any number of custom lines. Each label sits at the chart's right edge with its price, and labels stack instead of overlapping when two lines are close. Lines outside the visible price range aren't drawn, and they never stretch the chart's price scale.

**1. Range:** set **Range top** and **Range bottom** (0 = off). Optional midpoint line, labels and shading.

**2. Custom levels:** type them into **Levels** (NinjaTrader opens a multi-line editor), one per line or separated by semicolons:

```
price|label|color|style|width
```

- Only the price is required. Anything you leave out uses **Default line**.
- **color:** a name (Orange, Magenta, Gray) or hex (#FF00FF).
- **style:** Solid, Dash, Dot, DashDot or DashDotDot.
- Use a period for decimals. Don't put `|` or `;` inside a label.

Example:

```
7845.75|CURRENT YEARLY VAH|Magenta|Solid|2
7836.75|NAKED POC|Gray|Dash|1
```

**3. Labels:** font, price in the label, right or left edge, above or below the line, and a dark background box.

## Troubleshooting (NinjaTrader)

- **Import fails with compile errors:** usually another broken script on your PC, because NinjaTrader compiles every script together. Open **New → NinjaScript Editor**; the error list names the file.
- **Nothing drawn:** check the **Log** tab in the Control Center, and that the chart has enough days loaded (the 1hr 21 EMA needs at least a few days of history).

## License

Open source under the [Mozilla Public License 2.0](../LICENSE). Copyright (c) 2026 Tatanka Trading LLC.

*Trading futures involves substantial risk of loss. These indicators are analysis tools, not trading advice.*
