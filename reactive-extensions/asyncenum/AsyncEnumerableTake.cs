using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions
{
    internal sealed class AsyncEnumerableTake<T> : IAsyncEnumerable<T>
    {
        readonly IAsyncEnumerable<T> source;

        readonly long n;

        public AsyncEnumerableTake(IAsyncEnumerable<T> source, long n)
        {
            this.source = source;
            this.n = n;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new TakeAsyncEnumerator(source.GetAsyncEnumerator(), n);
        }

        sealed class TakeAsyncEnumerator : IAsyncEnumerator<T>
        {
            readonly IAsyncEnumerator<T> enumerator;

            long remaining;

            T current;

            public T Current => current;

            public TakeAsyncEnumerator(IAsyncEnumerator<T> enumerator, long remaining)
            {
                this.enumerator = enumerator;
                this.remaining = remaining;
            }

            public Task DisposeAsync()
            {
                return enumerator.DisposeAsync();
            }

            public async Task<bool> MoveNextAsync()
            {
                var n = remaining;
                if (n <= 0)
                {
                    return false;
                }
                remaining = n - 1;
                if (await enumerator.MoveNextAsync())
                {
                    current = enumerator.Current;
                    return true;
                }
                current = default;
                return false;
            }
        }
    }
}
