using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions
{
    internal sealed class AsyncEnumerableNever<T> : IAsyncEnumerable<T>, IAsyncEnumerator<T>, IAsyncFusedEnumerator<T>
    {
        public T Current => default;

        internal static readonly IAsyncEnumerable<T> Instance = new AsyncEnumerableNever<T>();

        // Singleton
        private AsyncEnumerableNever()
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
            return new TaskCompletionSource<bool>().Task;
        }

        public T TryPoll(out AsyncFusedState state)
        {
            state = AsyncFusedState.Empty;
            return default;
        }
    }
}
