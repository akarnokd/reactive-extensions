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
    public class MergeMapXPerf
    {
        [Params(1, 10, 100, 1000, 10000, 100000)]
        public int N;

        IObservable<int> merge;

        IObservable<int> merge1;

        IObservable<int> mergeMany;

        IObservable<int> mergeMany1;

        [GlobalSetup]
        public void Setup()
        {
            var outer = new int[100000 / N];
            var inner = new int[N];

            merge = outer.ToObservable().Select(v => inner.ToObservable()).Merge();

            merge1 = outer.ToObservable().Select(v => inner.ToObservable()).Merge(1);

            mergeMany = outer.ToObservable().Select(v => inner.ToObservable()).MergeMany();

            mergeMany1 = outer.ToObservable().Select(v => inner.ToObservable()).MergeMany(maxConcurrency: 1);
        }

        [Benchmark]
        public int Merge()
        {
            return merge.Wait();
        }

        [Benchmark]
        public int Merge_Max1()
        {
            return merge1.Wait();
        }

        [Benchmark]
        public int MergeMany()
        {
            return mergeMany.Wait();
        }

        [Benchmark]
        public int MergeMany_Max1()
        {
            return mergeMany1.Wait();
        }
    }
}
