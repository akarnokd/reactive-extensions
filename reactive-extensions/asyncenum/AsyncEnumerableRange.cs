using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions
{
    internal sealed class AsyncEnumerableRange : IAsyncEnumerable<int>
    {
        readonly int start;

        readonly int end;

        public AsyncEnumerableRange(int start, int end)
        {
            this.start = start;
            this.end = end;
        }

        public IAsyncEnumerator<int> GetAsyncEnumerator()
        {
            return new RangeAsyncEnumerator(start, end);
        }

        sealed class RangeAsyncEnumerator : IAsyncEnumerator<int>, IAsyncFusedEnumerator<int>
        {
            readonly int end;

            int index;

            int current;

            public RangeAsyncEnumerator(int index, int end)
            {
                this.index = index;
                this.end = end;
            }

            public int Current => current;

            public Task DisposeAsync()
            {
                return Task.CompletedTask;
            }

            public Task<bool> MoveNextAsync()
            {
                var idx = index;
                if (idx == end)
                {
                    return AsyncHelper.FalseTask;
                }
                current = idx;
                index = idx + 1;
                return AsyncHelper.TrueTask;
            }

            public int TryPoll(out AsyncFusedState state)
            {
                var idx = index;
                if (idx == end)
                {
                    state = AsyncFusedState.Terminated;
                    return default;
                }
                index = idx + 1;
                state = AsyncFusedState.Ready;
                return idx;
            }
        }
    }
}
