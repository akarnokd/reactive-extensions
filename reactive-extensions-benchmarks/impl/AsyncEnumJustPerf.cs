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
    public class AsyncEnumJustPerf
    {

        int field;

        readonly IAsyncEnumerable<int> JustCached = AsyncEnumerable.Just(1);


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
        public async Task NewSource_MoveNextAsync()
        {
            await MoveNextAsyncOn(AsyncEnumerable.Just(1).GetAsyncEnumerator());
        }

        [Benchmark]
        public async Task CachedSource_MoveNextAsync()
        {
            await MoveNextAsyncOn(JustCached.GetAsyncEnumerator());
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
        public async Task NewSource_TryPoll()
        {
            await TryPollOn(AsyncEnumerable.Just(1).GetAsyncEnumerator());
        }

        [Benchmark]
        public async Task CachedSource_TryPoll()
        {
            await TryPollOn(JustCached.GetAsyncEnumerator());
        }
    }
}
