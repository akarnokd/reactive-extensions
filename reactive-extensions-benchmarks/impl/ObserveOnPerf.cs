using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_benchmarks
{
    [Config(typeof(FastAndDirtyConfig))]
    public class ObserveOnPerf
    {
        [Params(1, 10, 100, 1000, 10000, 100000)]
        public int N;

        [Benchmark]
        public void Range_Baseline()
        {
            BlockingSubscribe(FastRange(1, N));
        }

        [Benchmark]
        public void ObserveOn_Ext()
        {
            BlockingSubscribe(FastRange(1, N)
                    .ObserveOn(ImmediateScheduler.INSTANCE, false)
                );
        }

        [Benchmark]
        public void ObserveOn_Ext_DelayError()
        {
            BlockingSubscribe(FastRange(1, N)
                    .ObserveOn(ImmediateScheduler.INSTANCE, true)
                );
        }

        IObservable<int> FastRange(int start, int count)
        {
            return null;
        }

        void BlockingSubscribe<T>(IObservable<T> source)
        {

        }

    }
}
