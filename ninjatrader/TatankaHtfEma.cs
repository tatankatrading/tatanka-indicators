// Copyright (c) 2026 Tatanka Trading LLC.
// This Source Code Form is subject to the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, obtain one at https://mozilla.org/MPL/2.0/.
// Tatanka 1hr 21 EMA (HTF EMA) v1.0.0 for NinjaTrader 8 - tatankatrading.com

#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

public enum TatankaHtfEmaMode
{
	LiveHTF,
	ExactHTF,
	SmoothScaled
}

namespace NinjaTrader.NinjaScript.Indicators
{
	public class TatankaHtfEma : Indicator
	{
		private EMA htfEmaInd;
		private EMA smoothEmaInd;
		private int scaledLen;
		private double alpha;
		private double lastClosedHtfEma;
		private bool htfEmaReady;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= @"HTF EMA (1H 21 by default). Live / Exact / Smooth modes ported from TradingView Pine v6 with lookahead_off.";
				Name						= "HTF EMA (1H 21 by default)";
				Calculate					= Calculate.OnPriceChange;
				IsOverlay					= true;
				DisplayInDataBox			= true;
				DrawOnPricePanel			= true;
				DrawHorizontalGridLines		= true;
				DrawVerticalGridLines		= true;
				PaintPriceMarkers			= true;
				IsAutoScale					= true;
				ScaleJustification			= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive	= true;
				MaximumBarsLookBack			= MaximumBarsLookBack.Infinite;

				HtfPeriodType				= BarsPeriodType.Minute;
				HtfPeriod					= 60;
				EmaLength					= 21;
				Mode						= TatankaHtfEmaMode.SmoothScaled;

				AddPlot(new Stroke(Brushes.Gray, 2), PlotStyle.Line, "HTF EMA");
			}
			else if (State == State.Configure)
			{
				AddDataSeries(HtfPeriodType, HtfPeriod);
			}
			else if (State == State.DataLoaded)
			{
				alpha				= 2.0 / (EmaLength + 1.0);
				lastClosedHtfEma	= double.NaN;
				htfEmaReady			= false;

				htfEmaInd			= EMA(BarsArray[1], EmaLength);
				scaledLen			= GetScaledLength();
				smoothEmaInd		= EMA(Closes[0], scaledLen);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBars[0] < 0 || CurrentBars[1] < 0)
				return;

			if (BarsInProgress == 1)
			{
				if (CurrentBars[1] >= EmaLength - 1)
				{
					lastClosedHtfEma = htfEmaInd[0];
					htfEmaReady = true;
				}
				return;
			}

			if (BarsInProgress != 0)
				return;

			double plotVal = double.NaN;

			if (Mode == TatankaHtfEmaMode.ExactHTF)
			{
				if (htfEmaReady)
					plotVal = lastClosedHtfEma;
			}
			else if (Mode == TatankaHtfEmaMode.SmoothScaled)
			{
				if (CurrentBars[0] >= scaledLen - 1)
					plotVal = smoothEmaInd[0];
			}
			else
			{
				if (htfEmaReady)
					plotVal = alpha * Close[0] + (1.0 - alpha) * lastClosedHtfEma;
			}

			if (!double.IsNaN(plotVal))
				Values[0][0] = plotVal;
		}

		private int GetScaledLength()
		{
			double htfSec	= GetPeriodSeconds(HtfPeriodType, HtfPeriod);
			double chartSec	= GetPeriodSeconds(BarsPeriod.BarsPeriodType, BarsPeriod.Value);

			if (htfSec <= 0 || chartSec <= 0)
				return Math.Max(1, EmaLength);

			int scaled = (int)Math.Round(EmaLength * (htfSec / chartSec));
			return Math.Max(1, scaled);
		}

		private static double GetPeriodSeconds(BarsPeriodType type, int value)
		{
			if (type == BarsPeriodType.Second)
				return value;
			if (type == BarsPeriodType.Minute)
				return value * 60.0;
			if (type == BarsPeriodType.Day)
				return value * 86400.0;
			if (type == BarsPeriodType.Week)
				return value * 604800.0;
			if (type == BarsPeriodType.Month)
				return value * 2592000.0;
			if (type == BarsPeriodType.Year)
				return value * 31536000.0;
			return 0;
		}

		public override string DisplayName
		{
			get
			{
				string modeTag = Mode == TatankaHtfEmaMode.ExactHTF ? "Exact"
								: Mode == TatankaHtfEmaMode.SmoothScaled ? "Smooth"
								: "Live";
				return string.Format("HTF EMA({0} {1}, {2}, {3})", HtfPeriod, HtfPeriodType, EmaLength, modeTag);
			}
		}

		#region Properties
		[NinjaScriptProperty]
		[Display(Name = "Source timeframe type", Description = "Pine input.timeframe type. Default Minute + 60 = 1 hour.", Order = 1, GroupName = "Parameters")]
		public BarsPeriodType HtfPeriodType { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Source timeframe", Description = "Pine input.timeframe value. 60 with Minute = 1 hour.", Order = 2, GroupName = "Parameters")]
		public int HtfPeriod { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "EMA length", Description = "Pine i_len. Default 21.", Order = 3, GroupName = "Parameters")]
		public int EmaLength { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Mode", Description = "LiveHTF = developing HTF EMA. ExactHTF = last closed HTF EMA. SmoothScaled = chart-TF EMA with scaled length.", Order = 4, GroupName = "Parameters")]
		public TatankaHtfEmaMode Mode { get; set; }
		#endregion
	}
}
