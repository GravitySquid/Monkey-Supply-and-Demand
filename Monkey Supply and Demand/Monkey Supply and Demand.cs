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
// 11/03/2023 JBannerman    Sort zones by price. Load more histrical bars.
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

        [Parameter("Historical Bars to Load", DefaultValue = 1000, Step = 1000)]
        public int PreferredNumHistoricalBars { get; set; }

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


        //private List<SDZone> supplyList = new List<SDZone>();
        //private List<SDZone> demandList = new List<SDZone>();
        private SortedList<double,SDZone> supplyListSorted = new SortedList<double, SDZone>();
        private SortedList<double,SDZone> demandListSorted = new SortedList<double, SDZone>();
        private Color AsColor, AdColor;
        private Bars HLSeries, ZoneSeries;
        private int ExtraBarsHL = 0, ExtraBarsZ = 0;

        protected override void Initialize()
        {
            AsColor = Color.FromArgb((int)(255 * 0.01 * ZoneOpacity), Color.FromName(SupplyZoneColor).R, Color.FromName(SupplyZoneColor).G, Color.FromName(SupplyZoneColor).B);
            AdColor = Color.FromArgb((int)(255 * 0.01 * ZoneOpacity), Color.FromName(DemandZoneColor).R, Color.FromName(DemandZoneColor).G, Color.FromName(DemandZoneColor).B);
            InitializeDataSeries();
        }

        private void InitializeDataSeries()
        {
            ExtraBarsHL = 0;
            ExtraBarsZ = 0;
            HLSeries = MarketData.GetBars(HighLowTimeframe);
            ZoneSeries = MarketData.GetBars(ZoneTimeframe);
            while (HLSeries.Count + ExtraBarsHL < PreferredNumHistoricalBars || ZoneSeries.Count + ExtraBarsZ < PreferredNumHistoricalBars)
            {
                ExtraBarsHL += HLSeries.LoadMoreHistory();
                ExtraBarsZ += ZoneSeries.LoadMoreHistory();
            }
        }

        public override void Calculate(int index)
        {
            // No need to run every chart candle
            if (!IsLastBar)
                return;

            InitializeDataSeries();
            // Find higher timeframe H/L
            for (int htfIndex = 2 * Periods; htfIndex < HLSeries.Count + ExtraBarsHL - Periods - 2; htfIndex++)
            {
                var index2 = htfIndex;
                var index3 = index2 - Periods;

                bool s = true;
                bool t = true;

                //SUPPLY - Local Highs
                for (int i = 1; i < Periods; i++)
                {
                    if (s == true && HLSeries.HighPrices[index2 - Periods + i] > HLSeries.HighPrices[index3])
                    {
                        s = false;
                        break;
                    }
                }
                for (int i = 1; i < Periods; i++)
                {
                    if (s == true && HLSeries.HighPrices[index2 - Periods - i] > HLSeries.HighPrices[index3])
                    {
                        s = false;
                        break;
                    }
                }
                if (s == true)
                {
                    // find index on Zone timeframe
                    int startIndex = ZoneSeries.OpenTimes.GetIndexByTime(HLSeries.OpenTimes[index3]);
                    int endIndex = ZoneSeries.OpenTimes.GetIndexByTime(HLSeries.OpenTimes[index3 + 1]);
                    int zoneIndex = 0;
                    for (int i = startIndex; i < endIndex; i++)
                    {
                        if (ZoneSeries.HighPrices[i] == HLSeries.HighPrices[index3])
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
                    int idx = Bars.OpenTimes.GetIndexByTime(ZoneSeries.OpenTimes[zoneIndex]);
                    if (!supplyListSorted.ContainsKey(min))
                        supplyListSorted.Add(min, new SDZone(idx, max, min));
                }

                //DEMAND - Lows
                for (int i = 1; i < Periods; i++)
                {
                    if (t == true && HLSeries.LowPrices[index2 - Periods + i] < HLSeries.LowPrices[index3])
                    {
                        t = false;
                        break;
                    }
                }
                for (int i = 1; i < Periods; i++)
                {
                    if (t == true && HLSeries.LowPrices[index2 - Periods - i] < HLSeries.LowPrices[index3])
                    {
                        t = false;
                        break;
                    }
                }
                if (t == true)
                {
                    // find index on Zone timeframe
                    int startIndex = ZoneSeries.OpenTimes.GetIndexByTime(HLSeries.OpenTimes[index3]);
                    int endIndex = ZoneSeries.OpenTimes.GetIndexByTime(HLSeries.OpenTimes[index3 + 1]);
                    int zoneIndex = 0;
                    for (int i = startIndex; i < endIndex; i++)
                    {
                        if (ZoneSeries.LowPrices[i] == HLSeries.LowPrices[index3])
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
                    int idx = Bars.OpenTimes.GetIndexByTime(ZoneSeries.OpenTimes[zoneIndex]);
                    if (!demandListSorted.ContainsKey(-1 * max)) 
                    demandListSorted.Add(-1 * max, new SDZone(idx, max, min));
                }
            }

            //DRAWING
            int count = 0;
            foreach (var zone in supplyListSorted)
            {
                if (zone.Value.low > Symbol.Ask)
                {
                    count++;
                    Chart.DrawRectangle("supply" + zone.Value.index + " " + count, zone.Value.index, zone.Value.high, index, zone.Value.low, AsColor, 1).IsFilled = true;
                }
                if (count >= MaxZones) break;
            }
            count = 0;
            foreach (var zone in demandListSorted)
            {
                if (zone.Value.high < Symbol.Ask)
                {
                    count++;
                    Chart.DrawRectangle("demand" + zone.Value.index + " " + count, zone.Value.index, zone.Value.low, index, zone.Value.high, AdColor, 1).IsFilled = true;
                }
                if (count >= MaxZones) break;
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
