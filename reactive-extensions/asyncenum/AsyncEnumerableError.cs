using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions
{
    internal sealed class AsyncEnumerableError<T> : IAsyncEnumerable<T>
    {
        readonly Exception error;

        public AsyncEnumerableError(Exception error)
        {
            this.error = error;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new ErrorAsyncEnumerator(error);
        }

        internal sealed class ErrorAsyncEnumerator : IAsyncEnumerator<T>, IAsyncFusedEnumerator<T>
        {
            Exception error;

            public T Current => default;

            public ErrorAsyncEnumerator(Exception error)
            {
                this.error = error;
            }

            public Task DisposeAsync()
            {
                return Task.CompletedTask;
            }

            public Task<bool> MoveNextAsync()
            {
                var ex = error;
                if (ex == null)
                {
                    return AsyncHelper.FalseTask;
                }
                error = null;
                return Task.FromException<bool>(ex);
            }

            public T TryPoll(out AsyncFusedState state)
            {
                var ex = error;
                if (ex == null)
                {
                    state = AsyncFusedState.Terminated;
                    return default;
                }
                error = null;
                throw ex;
            }
        }
    }
}
