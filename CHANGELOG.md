# Changelog

All notable changes to the Tatanka Trading free indicators. Versions match the Toolkit's dashboard header and the version line at the top of each `.cs` file.

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
