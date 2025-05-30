using System.Collections.Generic;
using System.Drawing;
using OsEngine.Entity;

namespace OsEngine.Indicators
{
    [Indicator("OpenInterest")]
    public class OpenInterest : Aindicator
    {
        private IndicatorDataSeries _series;

        public override void OnStateChange(IndicatorState state)
        {
            if (state == IndicatorState.Configure)
            {
                _series = CreateSeries("OpenInterest", Color.DodgerBlue, IndicatorChartPaintType.Column, true);
            }
            else if (state == IndicatorState.Dispose)
            {
                _series = null;
            }
        }

        public override void OnProcess(List<Candle> candles, int index)
        {
            _series.Values[index] = candles[index].OpenInterest;
        }
    }
}