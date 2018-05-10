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
    public class ConcatMapXPerf
    {
        [Params(1, 10, 100, 1000, 10000, 100000)]
        public int N;

        IObservable<int> concat;

        IObservable<int> concatMap;

        [GlobalSetup]
        public void Setup()
        {
            var outer = new int[100000 / N];
            var inner = new int[N];

            concat = outer.ToObservable().Select(v => inner.ToObservable()).Concat();

            concatMap = outer.ToObservable().ConcatMap(v => inner);
        }

        [Benchmark]
        public int Concat()
        {
            return concat.Wait();
        }

        [Benchmark]
        public int ConcatMap()
        {
            return concatMap.Wait();
        }
    }
}
