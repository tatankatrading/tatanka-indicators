# Changelog

All notable changes to the Tatanka Trading free indicators. Versions match the Toolkit's dashboard header and the version line at the top of each `.cs` file.

## Updates to release v1.0.0

- **November 8, 2026: AMT Toolkit v1.2.1** (NinjaTrader 8). The prior day's profile now matches its pdPOC, pdVAH and pdVAL lines. Since v1.2.0 the lines came from the day's ticks while the profile drawn beside them was still built from 1-minute bars, so its shaded value area could stop a few points short of the lines and its peak sit away from pdPOC (on NQ, 26902.50 against 26880.00). With **11. Volume Profile → Profile Period** on Session and its **Session Time** on the same hours as the prior day's value (08:30-15:00 CT by default), that day's profile is now drawn from the same ticks at one-tick rows: its tallest row is pdPOC and its value area ends at pdVAH and pdVAL when the **Value Area %** in groups 4 and 11 is the same. A Weekly or longer profile shows the whole period's value area, which is not the prior day's. Also: when the prior day's ticks arrive after today's session has started, today's 80% Rule starts over from that bar on the tick values (its fill so far is cleared), so the fill, the dashboard and the lines agree. And pdHigh, pdLow and pdOpen now switch to the day just closed at the same moment as pdPOC, pdVAH and pdVAL (the 17:00 open): they switched at the 15:00 close, so for two hours each afternoon pdHigh and pdLow were today's while the value was still yesterday's (pdPOC could sit below "pdLow"). Download `TatankaAmtToolkit_NT8.zip` (or `TatankaAll_NT8.zip`).
- **November 8, 2026: Periodic Volume Profile v1.0.1** (NinjaTrader 8). An "until touched" POC, VAH or VAL line now ends on the bar whose high or low is exactly the level's price. On GC and CL (tick sizes 0.1 and 0.01) about one level in three or eight was missed that way and its line ran on to a later bar. Download `TatankaPeriodicVolumeProfile_NT8.zip`.
- **November 8, 2026: Level Lines v1.1.1** (NinjaTrader 8). A line between two ticks is labelled at its own price: a range's mid on an odd number of ticks (5027.625 was labelled 5027.75) or a level typed off the tick (7845.10 on ES was labelled 7845.00). Download `TatankaLevelLines_NT8.zip`.
- **November 6, 2026: AMT Toolkit v1.2.0** (NinjaTrader 8). The prior day's value (pdPOC, pdVAH, pdVAL and the 80% Rule) now comes from **every trade** of the RTH session at one-tick rows, the way a tick volume profile builds it, instead of 1-minute bars spread over their range: on NQ on November 5 the pdPOC moves from 26902.50 to 26880.00, matching tick-based profiles. The Toolkit downloads the tick history of the last 5 sessions as the chart loads and after each close (**4. Previous Day Value & 80% Rule → Value From Ticks (Sessions)**; 0 keeps 1-minute bars). Older days, and any session whose ticks don't download, use 1-minute bars as before. Value Area % stays 0.68. Download `TatankaAmtToolkit_NT8.zip` (or `TatankaAll_NT8.zip`).
- **October 15, 2026: Level Lines v1.1.0** (NinjaTrader 8). New **Sharing mode** (Off, Source, Follow): type your levels on one chart set to Source, and every chart of the same contract set to Follow draws the same lines, labels, colors and range shading, updating when you click Apply on the Source. A micro shares with its full-size contract in the same month (ES and MES, NQ and MNQ, CL and MCL, GC and MGC). Label display settings stay per chart. Existing charts load with sharing Off. Download `TatankaLevelLines_NT8.zip`.
- **October 15, 2026: AMT Toolkit v1.1.0** (NinjaTrader 8). **2. Daily Levels → Levels File or Link** now also takes a levels link: paste it once and every publish loads on its own, with no file to manage. Tatanka Patreon members get their personal link with `/connect` in the Tatanka Discord. Each NinjaTrader asks the link once per symbol however many charts show it, asks often only around the 8:15 AM and 4:45 PM CT publishes, and keeps the last good levels if the link can't be reached. Local files work as before. Download `TatankaAmtToolkit_NT8.zip` (or `TatankaAll_NT8.zip`).
- **October 11, 2026: new Gap Analysis v1.0.0** (NinjaTrader 8; the TradingView version is already public). Each RTH opening gap versus the prior RTH close, shaded until price fills it, with partial fills, a latest-gap table with same-direction streaks, and alerts on new gaps and fills. Download `TatankaGapAnalysis_NT8.zip`.
- **October 6, 2026: new Session Volume Profile v1.0.0, Periodic Volume Profile v1.0.0 and Auto Anchored Volume Profile v1.1.0** (NinjaTrader 8 only). Session: a profile for a custom session window (08:30–15:00 by default) built from tick data. Periodic: daily, weekly, monthly or yearly profiles from 1-minute data, with POC, VAH and VAL extended until touched. Auto Anchored: one developing profile from a calendar anchor (year by default) on intraday or 1 Day charts. Downloads `TatankaSessionVolumeProfile_NT8.zip`, `TatankaPeriodicVolumeProfile_NT8.zip` and `TatankaAutoAnchoredVolumeProfile_NT8.zip`.
- **October 4, 2026: new Level Lines v1.0.0** (NinjaTrader 8 only). A range box (top, bottom, optional midpoint and shading) plus any number of labeled custom lines, typed one per line as `price|label|color|style|width`. Download `TatankaLevelLines_NT8.zip`.
- **October 3, 2026: TPO Profile (NinjaTrader).** POC ties now pick the tied row nearest the middle of the profile (the lower one if equally close), and price rows round the same way as TradingView.

## v1.0.0 (October 2026)

First public release, free and open source under MPL 2.0, on NinjaTrader 8 and TradingView: Tatanka AMT Toolkit, TPO Profile, If/Then Bias Table and HTF EMA (1hr 21 EMA).

### Tatanka AMT Toolkit

- One name on every platform: **Tatanka AMT Toolkit**, dashboard header "Tatanka AMT v1.0.0".
- Organized around the auction questions: Where is value? Balanced or trending? Who's trapped? What's unfinished? Which reference prices matter? Where are my levels?
- Volume profile for the session, week (default), month or year; prior-day and prior-month value; RTH VWAP.
- Initial balance, Dalton day-type classification, open location, 80% Rule.
- Overnight inventory versus settlement with trap detection; late-day spike base.
- Naked POCs (daily, weekly, monthly), single prints, poor highs and lows.
- Reference prices: overnight, prior day, week and month highs and lows; weekly and RTH open.
- Paste-your-own levels box that reads the one-line format `YYYY-MM-DD, pivot, prices…` (also the labeled Pivot / Resistance / Support block), with zones, flip coloring and alerts.
- Profile Row Scale (Auto) sizes profile rows to the symbol, replacing the old High Volatility Mode.
- NinjaTrader: levels file with `{root}` token, STALE and wrong-symbol checks, zone-entry alert mode, optional Discord webhook for phone alerts.
- Late spike base reads the same 1-minute close on history and live (TradingView).
- Removed: the buy/sell signal engine, scoring and playbook signals from earlier private versions.

### TPO Profile, If/Then Bias Table, HTF EMA

- NinjaTrader 8 ports of the TradingView scripts, with MPL 2.0 headers.
- NinjaTrader class names are Tatanka-prefixed (`TatankaTpoProfile`, `TatankaHtfEma`, `TatankaIfThenBiasTable`) so they can't clash with other scripts on your PC.
