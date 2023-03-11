//
// =====================
// SUPPLY & DEMAND ZONES
// =====================
//
// Change History
// ==========================================================
// Date       Name          Desc
// ==========================================================
// 05/07/2022 JBannerman    Clone from cTrader forum & modify
// 08/03/2023 JBannerman    Attempt to salvage.
//
//

using cAlgo.API;
using cAlgo.API.Internals;
using System.Collections.Generic;

namespace cAlgo
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class MonkeySupplyandDemand : Indicator
    {
        [Parameter("H/L Periods", DefaultValue = 5)]
        public int Periods { get; set; }

        [Parameter("Higher Timeframe for H/Ls", DefaultValue = "h4")]
        public TimeFrame HighLowTimeframe { get; set; }

        [Parameter("Lower Timeframe for Zone refining", DefaultValue = "m15")]
        public TimeFrame ZoneTimeframe { get; set; }

        [Parameter("Max Zones", DefaultValue = 3)]
        public int MaxZones { get; set; }

        [Parameter("Supply Color", DefaultValue = "Red")]
        public string SupplyZoneColor { get; set; }

        [Parameter("Demand Color", DefaultValue = "Lime")]
        public string DemandZoneColor { get; set; }

        [Parameter("Opacity %", DefaultValue = 20)]
        public int ZoneOpacity { get; set; }


        private List<SDZone> supplyList = new List<SDZone>();
        private List<SDZone> demandList = new List<SDZone>();
        private Color AsColor, AdColor;
        private Bars HLSeries, ZoneSeries;


        protected override void Initialize()
        {
            AsColor = Color.FromArgb((int)(255 * 0.01 * ZoneOpacity), Color.FromName(SupplyZoneColor).R, Color.FromName(SupplyZoneColor).G, Color.FromName(SupplyZoneColor).B);
            AdColor = Color.FromArgb((int)(255 * 0.01 * ZoneOpacity), Color.FromName(DemandZoneColor).R, Color.FromName(DemandZoneColor).G, Color.FromName(DemandZoneColor).B);
            HLSeries = MarketData.GetBars(HighLowTimeframe);
            ZoneSeries = MarketData.GetBars(ZoneTimeframe);
        }

        public override void Calculate(int index)
        {
            // Find higher timeframe H/L
            bool done = false;
            int htfIndex = 0;
            while (!done)
            {
                htfIndex++;
                var index2 = htfIndex;
                var index3 = index2 + Periods;

                bool s = true;
                bool t = true;

                //SUPPLY - Local Highs
                for (int i = 1; i < Periods; i++)
                {
                    if (s == true && HLSeries.HighPrices.Last(index2 + Periods - i) > HLSeries.HighPrices.Last(index3))
                    {
                        s = false;
                        break;
                    }
                }
                for (int i = 1; i < Periods; i++)
                {
                    if (s == true && HLSeries.HighPrices.Last(index2 + Periods + i) > HLSeries.HighPrices.Last(index3))
                    {
                        s = false;
                        break;
                    }
                }
                if (s == true)
                {
                    // find index on Zone timeframe
                    int startIndex = ZoneSeries.OpenTimes.GetIndexByTime(HLSeries.OpenTimes.Last(index3));
                    int endIndex = ZoneSeries.OpenTimes.GetIndexByTime(HLSeries.OpenTimes.Last(index3 - 1));
                    int zoneIndex = 0;
                    for (int i = startIndex; i < endIndex; i++)
                    {
                        if (ZoneSeries.HighPrices[i] == HLSeries.HighPrices.Last(index3))
                        { zoneIndex = i; break; }
                    }
                    double max = ZoneSeries.HighPrices[zoneIndex];
                    double min = ZoneSeries.LowPrices[zoneIndex];
                    int newZoneIndex = zoneIndex;
                    // Fix this .................. to find sensible frame
                    for (int i = 1; i <= 2; i++)
                    {
                        if (ZoneSeries.LowPrices[zoneIndex - i] < ZoneSeries.LowPrices[zoneIndex])
                        {
                            min = ZoneSeries.LowPrices[zoneIndex - i];
                            newZoneIndex = zoneIndex - i;
                        }
                    }
                    zoneIndex = newZoneIndex;
                    //supplyList.Add(new SDZone(index3, Bars.HighPrices[index3], min));
                    supplyList.Add(new SDZone(Bars.OpenTimes.GetIndexByTime(ZoneSeries.OpenTimes[zoneIndex]), max, min));
                }

                //DEMAND - Lows
                for (int i = 1; i < Periods; i++)
                {
                    if (t == true && HLSeries.LowPrices.Last(index2 + Periods - i) < HLSeries.LowPrices.Last(index3))
                    {
                        t = false;
                        break;
                    }
                }
                for (int i = 1; i < Periods; i++)
                {
                    if (t == true && HLSeries.LowPrices.Last(index2 + Periods + i) < HLSeries.LowPrices.Last(index3))
                    {
                        t = false;
                        break;
                    }
                }
                if (t == true)
                {
                    // find index on Zone timeframe
                    int startIndex = ZoneSeries.OpenTimes.GetIndexByTime(HLSeries.OpenTimes.Last(index3));
                    int endIndex = ZoneSeries.OpenTimes.GetIndexByTime(HLSeries.OpenTimes.Last(index3 - 1));
                    int zoneIndex = 0;
                    for (int i = startIndex; i < endIndex; i++)
                    {
                        if (ZoneSeries.LowPrices[i] == HLSeries.LowPrices.Last(index3))
                        { zoneIndex = i; break; }
                    }
                    double min = ZoneSeries.LowPrices[zoneIndex];
                    double max = ZoneSeries.HighPrices[zoneIndex];
                    int newZoneIndex = zoneIndex;
                    for (int i = 1; i <= 2; i++)
                    {
                        if (ZoneSeries.HighPrices[zoneIndex - i] < max)
                        {
                            max = ZoneSeries.HighPrices[zoneIndex - i];
                            newZoneIndex = zoneIndex - i;
                        }
                    }
                    zoneIndex = newZoneIndex;
                    //demandList.Add(new SDZone(index3, max, Bars.LowPrices[index3]));
                    demandList.Add(new SDZone(Bars.OpenTimes.GetIndexByTime(ZoneSeries.OpenTimes[zoneIndex]), max, min));
                }

                if (demandList.Count >= MaxZones && supplyList.Count >= MaxZones)
                    done = true;
            }

            if (!IsLastBar)
                return;

            //DRAWING
            int count = 0;
            foreach (var zone in supplyList)
            {
                if (zone.low > Symbol.Ask)
                {
                    count++;
                    Chart.DrawRectangle("supply" + zone.index + " " + count, zone.index, zone.high, index, zone.low, AsColor, 1).IsFilled = true;
                }
                //for (int i = zone.index; i < index; i++)
                //{
                //    if (Bars.HighPrices[i] > zone.high)
                //    {
                //        Chart.DrawRectangle("supply" + zone.index + " " + i, zone.index, zone.high, i, zone.low, AsColor, 1).IsFilled = true;
                //        break;
                //    }
                //}
            }
            count = 0;
            foreach (var zone in demandList)
            {
                if (zone.high < Symbol.Ask)
                {
                    count++;
                    Chart.DrawRectangle("demand" + zone.index + " " + count, zone.index, zone.low, index, zone.high, AdColor, 1).IsFilled = true;
                }
                //for (int i = zone.index; i < index; i++)
                //{
                //    if (Bars.LowPrices[i] < zone.low)
                //    {
                //        Chart.DrawRectangle("demand" + zone.index + " " + i, zone.index, zone.low, i, zone.high, AdColor, 1).IsFilled = true;
                //        break;
                //    }
                //}
            }
        }
    }

    public class SDZone
    {
        public int index;
        public double high;
        public double low;


        public SDZone(int index, double high, double low)
        {
            this.index = index;
            this.high = high;
            this.low = low;
        }

    }



}
