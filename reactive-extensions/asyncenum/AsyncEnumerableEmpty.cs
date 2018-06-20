using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions
{
    internal sealed class AsyncEnumerableEmpty<T> : IAsyncEnumerable<T>, IAsyncEnumerator<T>, IAsyncFusedEnumerator<T>
    {
        public T Current => default;

        internal static readonly IAsyncEnumerable<T> Instance = new AsyncEnumerableEmpty<T>();

        // Singleton
        private AsyncEnumerableEmpty()
        {

        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return this;
        }

        public Task<bool> MoveNextAsync()
        {
            return AsyncHelper.FalseTask;
        }

        public T TryPoll(out AsyncFusedState state)
        {
            state = AsyncFusedState.Terminated;
            return default;
        }
    }
}
