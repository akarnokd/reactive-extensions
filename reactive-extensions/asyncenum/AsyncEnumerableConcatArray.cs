using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions
{
    internal sealed class AsyncEnumerableConcatArray<T> : IAsyncEnumerable<T>
    {
        readonly IAsyncEnumerable<T>[] sources;

        public AsyncEnumerableConcatArray(IAsyncEnumerable<T>[] sources)
        {
            this.sources = sources;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new ConcatArrayEnumerator(sources);
        }

        sealed class ConcatArrayEnumerator : IAsyncEnumerator<T>
        {

            readonly IAsyncEnumerable<T>[] sources;

            int index;

            IAsyncEnumerator<T> enumerator;

            T current;

            public T Current => current;

            public ConcatArrayEnumerator(IAsyncEnumerable<T>[] sources)
            {
                this.sources = sources;
            }

            public Task DisposeAsync()
            {
                var en = enumerator;
                if (en != null)
                {
                    return en.DisposeAsync();
                }
                return Task.CompletedTask;
            }

            public async Task<bool> MoveNextAsync()
            {
                for (; ; )
                {
                    var en = enumerator;
                    if (en == null)
                    {
                        var idx = index;
                        if (idx == sources.Length)
                        {
                            current = default;
                            return false;
                        }

                        en = sources[idx].GetAsyncEnumerator();
                        enumerator = en;
                        index = idx + 1;
                    }

                    if (await en.MoveNextAsync())
                    {
                        current = en.Current;
                        return true;
                    }
                    enumerator = null;
                    await en.DisposeAsync();
                }
            }
        }
    }
}
