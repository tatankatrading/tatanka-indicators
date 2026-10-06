<p align="center"><img src="assets/logo.png" width="96" alt="Tatanka Trading logo"></p>

# Tatanka Trading Free Indicators

Four free, open-source Auction Market Theory indicators for **NinjaTrader 8** and **TradingView**, from [Tatanka Trading](https://tatankatrading.com). They're the four tools behind every Tatanka session read. **Level Lines**, a NinjaTrader 8 tool for labeled range and custom lines, is here too, along with three NinjaTrader 8 volume profiles: **Session**, **Periodic** and **Auto Anchored**.

None of them give buy/sell signals or trade recommendations.

## What's here

| Indicator | What it does | NinjaTrader source |
| --- | --- | --- |
| **Tatanka AMT Toolkit** | A complete Auction Market Theory read on one chart: where value is, balanced or trending, who's trapped, and what's unfinished. Volume profiles, initial balance, overnight inventory, naked POCs, single prints, day type and a paste-your-own levels box. Works on any symbol. | `ninjatrader/TatankaAmtToolkit.cs` |
| **TPO Profile** | TPO (Market Profile) letters for each 30-minute period, with the POC, value area and a heatmap gradient. Full letter detail for recent days, outlines for older ones. | `ninjatrader/TatankaTpoProfile.cs` |
| **If/Then** | Shows the afternoon review's if/then bias for the zone price is in. Paste the review's block and it updates as price moves between zones. | `ninjatrader/TatankaIfThenBiasTable.cs` |
| **1hr 21 EMA** | The 1-hour 21-period EMA on any chart timeframe, in live, confirmed or smoothed mode. | `ninjatrader/TatankaHtfEma.cs` |
| **Level Lines** (NinjaTrader only) | A range box (top, bottom, optional midpoint and shading) plus any number of labeled lines in one large box, separated by commas, semicolons, or new lines. | `ninjatrader/TatankaLevelLines.cs` |
| **Session Volume Profile** (NinjaTrader only) | A volume profile for a custom session window (08:30–15:00 by default) built from tick data, with POC, value area and up/down volume, drawn from each session's right edge. | `ninjatrader/TatankaSessionVolumeProfile.cs` |
| **Periodic Volume Profile** (NinjaTrader only) | Daily, weekly, monthly or yearly volume profiles from 1-minute data. Each finished profile's POC, VAH and VAL extend right until price touches them. | `ninjatrader/TatankaPeriodicVolumeProfile.cs` |
| **Auto Anchored Volume Profile** (NinjaTrader only) | One developing volume profile from a calendar anchor (session, week, month, quarter, year, decade or century) to the latest bar, on intraday or 1 Day charts. | `ninjatrader/TatankaAutoAnchoredVolumeProfile.cs` |

Guides:

- [Tatanka AMT Toolkit User Guide (PDF)](docs/Tatanka-AMT-Toolkit-User-Guide.pdf): everything the Toolkit draws, how each piece is calculated and every setting
- [AMT Toolkit on NinjaTrader](docs/NinjaTrader-Guide.md)
- [AMT Toolkit on TradingView](docs/TradingView-Guide.md)
- [TPO Profile, If/Then, 1hr 21 EMA, Level Lines and the volume profiles](docs/Other-Indicators-Guide.md)

Screenshots and the full feature list: [tatankatrading.com/indicators](https://tatankatrading.com/indicators/)

## Download

NinjaTrader 8 (always the latest release):

- [TatankaAmtToolkit_NT8.zip](https://github.com/TatankaTrading/tatanka-indicators/releases/latest/download/TatankaAmtToolkit_NT8.zip)
- [TatankaTPO_NT8.zip](https://github.com/TatankaTrading/tatanka-indicators/releases/latest/download/TatankaTPO_NT8.zip)
- [TatankaIfThen_NT8.zip](https://github.com/TatankaTrading/tatanka-indicators/releases/latest/download/TatankaIfThen_NT8.zip)
- [Tatanka21EMA_NT8.zip](https://github.com/TatankaTrading/tatanka-indicators/releases/latest/download/Tatanka21EMA_NT8.zip)
- [TatankaLevelLines_NT8.zip](https://github.com/TatankaTrading/tatanka-indicators/releases/latest/download/TatankaLevelLines_NT8.zip)
- [TatankaSessionVolumeProfile_NT8.zip](https://github.com/TatankaTrading/tatanka-indicators/releases/latest/download/TatankaSessionVolumeProfile_NT8.zip)
- [TatankaPeriodicVolumeProfile_NT8.zip](https://github.com/TatankaTrading/tatanka-indicators/releases/latest/download/TatankaPeriodicVolumeProfile_NT8.zip)
- [TatankaAutoAnchoredVolumeProfile_NT8.zip](https://github.com/TatankaTrading/tatanka-indicators/releases/latest/download/TatankaAutoAnchoredVolumeProfile_NT8.zip)
- [TatankaAll_NT8.zip](https://github.com/TatankaTrading/tatanka-indicators/releases/latest/download/TatankaAll_NT8.zip) (Toolkit, TPO, If/Then and 21 EMA)

TradingView:

- [Tatanka AMT Toolkit](https://tatankatrading.com/tv/amt-toolkit)
- [TPO Profile](https://tatankatrading.com/tv/tpo)
- [If/Then Bias Table](https://tatankatrading.com/tv/ifthen)
- [HTF EMA (1hr 21 EMA)](https://tatankatrading.com/tv/21ema)

## Install on NinjaTrader 8

1. Download the zip. Don't unzip it.
2. In the Control Center: **Tools → Import → NinjaScript Add-On**, then pick the zip.
3. Open a chart, right-click → **Indicators**, and add the indicator.

## Recommended chart settings (AMT Toolkit)

- ETH trading hours, minute bars
- 90 days loaded (400 days if you use the yearly profile)
- Tick Replay off
- **Tools → Options → Market Data:** merge policy *Merge non back adjusted*, so levels sit at the prices that actually traded (the same as TradingView's default). The Toolkit follows your merge policy; *Merge back adjusted* shifts levels from before a contract roll onto the current contract.

## Updating

NinjaTrader has no auto-update.

1. Compare the version in the Toolkit's dashboard header (**Tatanka AMT v1.0.0**), or the version line at the top of each `.cs` file, with the [latest release](https://github.com/TatankaTrading/tatanka-indicators/releases/latest).
2. Download the new zip and import it the same way. Accept the overwrite.
3. Reload the chart (F5).

## Daily levels

The Toolkit's **Paste Levels Here** box takes one line per symbol:

```
YYYY-MM-DD, pivot, R1, R2, R3, R4, R5, S1, S2, S3, S4, S5
```

Prices above the pivot are drawn as resistance and prices below it as support. Anyone can paste their own levels.

The daily ML levels for ES, NQ, CL and GC come in exactly this format with the **$20/month Patreon**, along with the Globex levels and the afternoon review plans, whose if/then blocks paste into the If/Then indicator. Details at [tatankatrading.com](https://tatankatrading.com).

## Troubleshooting

- **Import fails with compile errors:** usually another broken script on your PC, because NinjaTrader compiles every script together. Open the NinjaScript Editor; the error list names the file.
- **Red text on the chart, or nothing drawn:** check the **Log** tab in the Control Center.
- **STALE warning on the levels:** the pasted date is older than the chart's trading date. Paste today's line.

More fixes are in the [NinjaTrader guide](docs/NinjaTrader-Guide.md#troubleshooting).

## License

[Mozilla Public License 2.0](LICENSE). Copyright (c) 2026 Tatanka Trading LLC.

You can use, modify and share the code. If you distribute a modified version of these files, those files stay under MPL 2.0 and keep the copyright notice.

*Trading futures involves substantial risk of loss. These indicators are analysis tools, not trading advice.*
