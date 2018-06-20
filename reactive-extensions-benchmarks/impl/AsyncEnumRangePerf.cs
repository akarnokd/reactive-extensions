using BenchmarkDotNet.Attributes;
using System;
using akarnokd.reactive_extensions;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions_benchmarks
{
    [Config(typeof(FastAndDirtyConfig))]
    public class AsyncEnumRangePerf
    {
        [Params(1, 10, 100, 1000, 10000, 100000)]
        public int N;

        int field;

        IAsyncEnumerable<int> source;

        [GlobalSetup]
        public void Setup()
        {
            source = AsyncEnumerable.Range(1, N);
        }


        async Task MoveNextAsyncOn(IAsyncEnumerator<int> en)
        {
            try
            {
                while (await en.MoveNextAsync())
                {
                    Volatile.Write(ref field, en.Current);
                }
            }
            finally
            {
                await en.DisposeAsync();
            }
        }

        [Benchmark]
        public async Task MoveNextAsync()
        {
            await MoveNextAsyncOn(source.GetAsyncEnumerator());
        }

        async Task TryPollOn(IAsyncEnumerator<int> en)
        {
            try
            {
                var f = en as IAsyncFusedEnumerator<int>;

                for (; ; )
                {
                    var v = f.TryPoll(out var state);
                    if (state != AsyncFusedState.Ready)
                    {
                        break;
                    }
                    Volatile.Write(ref field, v);
                }
            }
            finally
            {
                await en.DisposeAsync();
            }
        }

        [Benchmark]
        public async Task TryPoll()
        {
            await TryPollOn(source.GetAsyncEnumerator());
        }
    }
}
