using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions
{
    internal sealed class AsyncEnumerableReducePlain<T> : IAsyncEnumerable<T>
    {
        readonly IAsyncEnumerable<T> source;

        readonly Func<T, T, T> reducer;

        public AsyncEnumerableReducePlain(IAsyncEnumerable<T> source, Func<T, T, T> reducer)
        {
            this.source = source;
            this.reducer = reducer;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new ReduceAsyncEnumerator(source.GetAsyncEnumerator(), reducer);
        }

        sealed class ReduceAsyncEnumerator : IAsyncEnumerator<T>
        {
            readonly IAsyncEnumerator<T> enumerator;

            readonly Func<T, T, T> reducer;

            bool ready;

            T accumulator;
            bool hasValue;

            public ReduceAsyncEnumerator(IAsyncEnumerator<T> enumerator, Func<T, T, T> reducer)
            {
                this.enumerator = enumerator;
                this.reducer = reducer;
            }

            public T Current => ready && hasValue ? accumulator : default;

            public Task DisposeAsync()
            {
                return enumerator.DisposeAsync();
            }

            public async Task<bool> MoveNextAsync()
            {
                if (ready)
                {
                    return false;
                }

                for (; ; )
                {
                    if (await enumerator.MoveNextAsync())
                    {
                        if (hasValue)
                        {
                            accumulator = reducer(accumulator, enumerator.Current);
                        }
                        else
                        {
                            hasValue = true;
                            accumulator = enumerator.Current;
                        }
                    }
                    else
                    {
                        if (hasValue)
                        {
                            ready = true;
                            return true;
                        }
                        return false;
                    }
                }
            }
        }
    }
}
