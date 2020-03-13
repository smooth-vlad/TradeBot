using LiveCharts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinkoff.Trading.OpenApi.Models;

namespace TradeBot
{
    interface IIndicator
    {
        List<CandlePayload> candles { get; set; }
        int candlesSpan { get; set; }
        bool areGraphsInitialized { get; set; }
        void UpdateState();
        bool IsBuySignal();
        bool IsSellSignal();

        void UpdateGraphs();
        void InitializeGraphs(SeriesCollection series);
    }
}
