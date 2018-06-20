using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions
{
    internal sealed class AsyncEnumerableMap<T, R> : IAsyncEnumerable<R>
    {

        readonly IAsyncEnumerable<T> source;

        readonly Func<T, R> mapper;

        public AsyncEnumerableMap(IAsyncEnumerable<T> source, Func<T, R> mapper)
        {
            this.source = source;
            this.mapper = mapper;
        }

        public IAsyncEnumerator<R> GetAsyncEnumerator()
        {
            return new MapAsyncEnumerable(source.GetAsyncEnumerator(), mapper);
        }

        sealed class MapAsyncEnumerable : IAsyncEnumerator<R>
        {
            readonly IAsyncEnumerator<T> enumerator;

            readonly Func<T, R> mapper;

            R current;

            public MapAsyncEnumerable(IAsyncEnumerator<T> enumerator, Func<T, R> mapper)
            {
                this.enumerator = enumerator;
                this.mapper = mapper;
            }

            public R Current => current;

            public Task DisposeAsync()
            {
                return enumerator.DisposeAsync();
            }

            public async Task<bool> MoveNextAsync()
            {
                if (await enumerator.MoveNextAsync())
                {
                    current = mapper(enumerator.Current);
                    return true;
                }
                current = default;
                return false;
            }
        }
    }
}
