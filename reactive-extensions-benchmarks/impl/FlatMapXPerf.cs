using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Linq;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_benchmarks
{

    [Config(typeof(FastAndDirtyConfig))]
    public class FlatMapXPerf
    {
        [Params(1, 10, 100, 1000, 10000, 100000)]
        public int N;

        IObservable<int> selectMany;

        //IObservable<int> flatMap;

        [GlobalSetup]
        public void Setup()
        {
            var outer = new int[100000 / N];
            var inner = new int[N];

            selectMany = outer.ToObservable().SelectMany(v => inner.ToObservable());

            // flatMap = outer.ToObservable().FlatMap(v => inner.ToObservable());
        }

        [Benchmark]
        public int SelectMany()
        {
            return selectMany.Wait();
        }

        /*
        [Benchmark]
        public int FlatMap()
        {
            return flatMap.Wait();
        }
        */
    }
}
