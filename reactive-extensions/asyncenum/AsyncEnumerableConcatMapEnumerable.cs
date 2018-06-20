using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions
{
    internal sealed class AsyncEnumerableConcatMapEnumerable<T, R> : IAsyncEnumerable<R>
    {
        readonly IAsyncEnumerable<T> source;

        readonly Func<T, IEnumerable<R>> mapper;

        public AsyncEnumerableConcatMapEnumerable(IAsyncEnumerable<T> source, Func<T, IEnumerable<R>> mapper)
        {
            this.source = source;
            this.mapper = mapper;
        }

        public IAsyncEnumerator<R> GetAsyncEnumerator()
        {
            return new ConcatMapAsyncEnumerator(source.GetAsyncEnumerator(), mapper);
        }

        sealed class ConcatMapAsyncEnumerator : IAsyncEnumerator<R>
        {
            readonly IAsyncEnumerator<T> enumerator;

            readonly Func<T, IEnumerable<R>> mapper;

            R current;

            IEnumerator<R> currentEnumerator;

            public R Current => current;

            public ConcatMapAsyncEnumerator(IAsyncEnumerator<T> enumerator, Func<T, IEnumerable<R>> mapper)
            {
                this.enumerator = enumerator;
                this.mapper = mapper;
            }

            public Task DisposeAsync()
            {
                currentEnumerator?.Dispose();
                currentEnumerator = null;
                return enumerator.DisposeAsync();
            }

            public async Task<bool> MoveNextAsync()
            {
                for (; ; )
                {
                    var en = currentEnumerator;
                    if (en == null)
                    {
                        if (await enumerator.MoveNextAsync())
                        {
                            en = mapper(enumerator.Current).GetEnumerator();
                            currentEnumerator = en;
                        }
                        else
                        {
                            current = default;
                            return false;
                        }
                    }

                    if (en.MoveNext())
                    {
                        current = en.Current;
                        return true;
                    }
                    en.Dispose();
                    currentEnumerator = null;
                }
            }
        }
    }
}
