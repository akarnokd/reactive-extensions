using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions
{
    internal sealed class AsyncEnumerableJust<T> : IAsyncEnumerable<T>
    {
        readonly T value;

        public AsyncEnumerableJust(T value)
        {
            this.value = value;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new JustAsyncEnumerator(value);
        }

        sealed class JustAsyncEnumerator : IAsyncEnumerator<T>, IAsyncFusedEnumerator<T>
        {
            readonly T value;

            int state;

            static readonly int FreshState = 0;
            static readonly int ReadyState = 1;
            static readonly int ConsumedState = 2;

            public T Current => state == ReadyState ? value : default;

            public JustAsyncEnumerator(T value)
            {
                this.value = value;
            }

            public Task DisposeAsync()
            {
                return Task.CompletedTask;
            }

            public Task<bool> MoveNextAsync()
            {
                if (state == FreshState)
                {
                    state = ReadyState;
                    return AsyncHelper.TrueTask;
                }

                state = ConsumedState;
                return AsyncHelper.FalseTask;
            }

            public T TryPoll(out AsyncFusedState state)
            {
                if (this.state != ConsumedState)
                {
                    this.state = ConsumedState;
                    state = AsyncFusedState.Ready;
                    return value;
                }
                state = AsyncFusedState.Terminated;
                return default;
            }
        }
    }
}
