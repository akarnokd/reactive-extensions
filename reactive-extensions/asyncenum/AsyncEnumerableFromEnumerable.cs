using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions
{
    internal sealed class AsyncEnumerableFromEnumerable<T> : IAsyncEnumerable<T>
    {
        readonly IEnumerable<T> source;

        public AsyncEnumerableFromEnumerable(IEnumerable<T> source)
        {
            this.source = source;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            var en = default(IEnumerator<T>);
            try
            {
                en = ValidationHelper.RequireNonNullRef(source.GetEnumerator(), "The GetEnumerator returned a null IEnumerator");
            }
            catch (Exception ex)
            {
                return new AsyncEnumerableError<T>.ErrorAsyncEnumerator(ex);
            }
            return new EnumerableAsyncEnumerator(en);
        }

        sealed class EnumerableAsyncEnumerator : IAsyncEnumerator<T>, IAsyncFusedEnumerator<T>
        {
            IEnumerator<T> enumerator;

            public T Current => enumerator.Current;

            public EnumerableAsyncEnumerator(IEnumerator<T> enumerator)
            {
                this.enumerator = enumerator;
            }

            public Task DisposeAsync()
            {
                enumerator?.Dispose();
                enumerator = null;
                return Task.CompletedTask;
            }

            public Task<bool> MoveNextAsync()
            {
                if (enumerator.MoveNext())
                {
                    return AsyncHelper.TrueTask;
                }
                enumerator.Dispose();
                enumerator = null;
                return AsyncHelper.FalseTask;
            }

            public T TryPoll(out AsyncFusedState state)
            {
                if (enumerator.MoveNext())
                {
                    state = AsyncFusedState.Ready;
                    return enumerator.Current;
                }
                state = AsyncFusedState.Terminated;
                enumerator.Dispose();
                enumerator = null;
                return default;
            }
        }
    }
}
