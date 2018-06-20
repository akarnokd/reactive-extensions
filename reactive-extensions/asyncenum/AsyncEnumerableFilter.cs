using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions
{
    internal sealed class AsyncEnumerableFilter<T> : IAsyncEnumerable<T>
    {

        readonly IAsyncEnumerable<T> source;

        readonly Func<T, bool> predicate;

        public AsyncEnumerableFilter(IAsyncEnumerable<T> source, Func<T, bool> predicate)
        {
            this.source = source;
            this.predicate = predicate;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new FilterAsyncEnumerable(source.GetAsyncEnumerator(), predicate);
        }

        sealed class FilterAsyncEnumerable : IAsyncEnumerator<T>
        {
            readonly IAsyncEnumerator<T> enumerator;

            readonly Func<T, bool> predicate;

            T current;

            public FilterAsyncEnumerable(IAsyncEnumerator<T> enumerator, Func<T, bool> predicate)
            {
                this.enumerator = enumerator;
                this.predicate = predicate;
            }

            public T Current => current;

            public Task DisposeAsync()
            {
                return enumerator.DisposeAsync();
            }

            public async Task<bool> MoveNextAsync()
            {
                for (; ; )
                {
                    if (await enumerator.MoveNextAsync())
                    {
                        var v = enumerator.Current;

                        if (predicate(v))
                        {
                            current = v;
                            return true;
                        }
                        continue;
                    }
                    current = default;
                    return false;
                }
            }
        }
    }
}
