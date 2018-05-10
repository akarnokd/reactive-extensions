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
    public class ConcatMapEagerXPerf
    {
        [Params(1, 10, 100, 1000, 10000, 100000)]
        public int N;

        IObservable<int> baseline;

        IObservable<int> concat;

        IObservable<int> merge;

        IObservable<int> concatMapEager;

        IObservable<int> concatMapEager1;

        [GlobalSetup]
        public void Setup()
        {
            var outer = new int[100000 / N];
            var inner = new int[N];

            baseline = new int[100000].ToObservable();

            concat = outer.ToObservable().Select(v => inner.ToObservable()).Concat();

            merge = outer.ToObservable().Select(v => inner.ToObservable()).Merge();

            concatMapEager = outer.ToObservable().ConcatMapEager(v => inner.ToObservable());

            concatMapEager1 = outer.ToObservable().ConcatMapEager(v => inner.ToObservable(), 1);
        }

        [Benchmark]
        public int Baseline()
        {
            return baseline.Wait();
        }

        [Benchmark]
        public int Concat()
        {
            return concat.Wait();
        }

        [Benchmark]
        public int Merge()
        {
            return merge.Wait();
        }

        [Benchmark]
        public int ConcatMapEager()
        {
            return concatMapEager.Wait();
        }

        [Benchmark]
        public int ConcatMapEager_Max1()
        {
            return concatMapEager1.Wait();
        }
    }
}
