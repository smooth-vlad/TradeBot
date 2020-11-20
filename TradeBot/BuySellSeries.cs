using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeBot
{
    public class BuySellSeries
    {
        private LineSeries mainSeries;
        private ScatterSeries openPositionsCirclesSeries;
        private ScatterSeries closePositionsGreenCirclesSeries;
        private ScatterSeries closePositionsRedCirclesSeries;
        private ScatterSeries openPositionsUpShapesSeries;
        private ScatterSeries openPositionsDownShapesSeries;
        private ScatterSeries closePositionsPlusShapesSeries;
        private ScatterSeries closePositionsMinusShapesSeries;

        private ScatterSeries[] scatterSeries;

        public bool IsShort { get; private set; } = false;
        public double? OpenPrice { get; private set; }
        public bool isPositionOpened => OpenPrice.HasValue;

        public bool AreSeriesAttached { get; private set; }

        public BuySellSeries()
        {
            mainSeries = new LineSeries
            {
                Title = "Operations",
                Color = OxyColor.FromRgb(55, 55, 55),
            };
            openPositionsCirclesSeries = new ScatterSeries
            {
                Title = "Open position (arrow down - short postion)",
                MarkerType = MarkerType.Circle,
                MarkerSize = 8,
                MarkerFill = OxyColor.FromRgb(40, 40, 40),
            };
            closePositionsGreenCirclesSeries = new ScatterSeries
            {
                Title = "Profitable trade",
                MarkerType = MarkerType.Circle,
                MarkerSize = 8,
                MarkerFill = OxyColor.FromRgb(121, 229, 112),
                MarkerStroke = OxyColor.FromRgb(40, 40, 40),
                MarkerStrokeThickness = 2,
            };
            closePositionsRedCirclesSeries = new ScatterSeries
            {
                Title = "Unprofitable trade",
                MarkerType = MarkerType.Circle,
                MarkerSize = 8,
                MarkerFill = OxyColor.FromRgb(214, 107, 107),
                MarkerStroke = OxyColor.FromRgb(40, 40, 40),
                MarkerStrokeThickness = 2,
            };
            openPositionsUpShapesSeries = new ScatterSeries
            {
                MarkerType = MarkerType.Custom,
                MarkerOutline = ShapesPaths.arrowUp,
                MarkerSize = 16,
                MarkerFill = OxyColor.FromRgb(255, 255, 255),
            };
            openPositionsDownShapesSeries = new ScatterSeries
            {
                MarkerType = MarkerType.Custom,
                MarkerOutline = ShapesPaths.arrowDown,
                MarkerSize = 16,
                MarkerFill = OxyColor.FromRgb(255, 255, 255),
            };
            closePositionsPlusShapesSeries = new ScatterSeries
            {
                MarkerType = MarkerType.Custom,
                MarkerOutline = ShapesPaths.plus,
                MarkerSize = 16,
                MarkerFill = OxyColor.FromRgb(55, 55, 55),
            };
            closePositionsMinusShapesSeries = new ScatterSeries
            {
                MarkerType = MarkerType.Custom,
                MarkerOutline = ShapesPaths.minus,
                MarkerSize = 16,
                MarkerFill = OxyColor.FromRgb(55, 55, 55),
            };

            scatterSeries = new ScatterSeries[]
            {
                closePositionsGreenCirclesSeries,
                closePositionsRedCirclesSeries,
                closePositionsPlusShapesSeries,
                closePositionsMinusShapesSeries,
                openPositionsCirclesSeries,
                openPositionsUpShapesSeries,
                openPositionsDownShapesSeries,
            };
        }

        public void AttachToChart(ElementCollection<Series> chart)
        {
            if (AreSeriesAttached || chart == null)
                return;

            chart.Add(mainSeries);
            foreach (var s in scatterSeries)
                chart.Add(s);
            AreSeriesAttached = true;
        }

        public void ClearSeries()
        {
            mainSeries.Points.Clear();
            foreach (var s in scatterSeries)
                s.Points.Clear();
            OpenPrice = null;
            IsShort = false;
        }

        public void OffsetSeries(int offset)
        {
            var mainSeriesBuff = new List<DataPoint>(mainSeries.Points.Count);
            mainSeriesBuff.AddRange(mainSeries.Points.Select(
                point => new DataPoint(point.X + offset, point.Y)));
            mainSeries.Points.Clear();
            mainSeries.Points.AddRange(mainSeriesBuff);

            foreach (var s in scatterSeries)
            {
                var buff = new List<ScatterPoint>(s.Points.Count);
                buff.AddRange(s.Points.Select(
                    point => new ScatterPoint(point.X + offset, point.Y)));
                s.Points.Clear();
                s.Points.AddRange(buff);
            }
        }

        public void OpenPosition(int candleIndex, double openPrice, bool isShort)
        {
            if (this.OpenPrice.HasValue)
                throw new InvalidOperationException("can't open position if its already opened");
            this.IsShort = isShort;
            this.OpenPrice = openPrice;

            var newPoint = new DataPoint(candleIndex - 0.25, openPrice);
            var newPointScatter = new ScatterPoint(candleIndex - 0.25, openPrice);

            mainSeries.Points.Add(newPoint);
            openPositionsCirclesSeries.Points.Add(newPointScatter);
            if (isShort)
                openPositionsDownShapesSeries.Points.Add(newPointScatter);
            else
                openPositionsUpShapesSeries.Points.Add(newPointScatter);
        }

        public void ClosePosition(int candleIndex, double closePrice)
        {
            if (!this.OpenPrice.HasValue)
                throw new InvalidOperationException("can't close position because there is no opened position");

            var newPoint = new DataPoint(candleIndex + 0.25, closePrice);
            var newPointScatter = new ScatterPoint(candleIndex + 0.25, closePrice);

            mainSeries.Points.Add(newPoint);
            mainSeries.Points.Add(new DataPoint(double.NaN, double.NaN));
            var isGrowth = OpenPrice - closePrice < 0;
            if (isGrowth && IsShort
                || !isGrowth && !IsShort)
            {
                closePositionsRedCirclesSeries.Points.Add(newPointScatter);
                closePositionsMinusShapesSeries.Points.Add(newPointScatter);
            }
            else
            {
                closePositionsGreenCirclesSeries.Points.Add(newPointScatter);
                closePositionsPlusShapesSeries.Points.Add(newPointScatter);
            }

            this.OpenPrice = null;
        }
    }
}
