using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions
{
    internal sealed class AsyncEnumerableReduce<T, R> : IAsyncEnumerable<R>
    {
        readonly IAsyncEnumerable<T> source;

        readonly Func<R> initialSupplier;

        readonly Func<R, T, R> reducer;

        public AsyncEnumerableReduce(IAsyncEnumerable<T> source, Func<R> initialSupplier, Func<R, T, R> reducer)
        {
            this.source = source;
            this.initialSupplier = initialSupplier;
            this.reducer = reducer;
        }

        public IAsyncEnumerator<R> GetAsyncEnumerator()
        {
            var initial = default(R);
            try
            {
                initial = initialSupplier();
            }
            catch (Exception ex)
            {
                return new AsyncEnumerableError<R>.ErrorAsyncEnumerator(ex);
            }

            return new ReduceAsyncEnumerator(source.GetAsyncEnumerator(), initial, reducer);
        }

        sealed class ReduceAsyncEnumerator : IAsyncEnumerator<R>
        {
            readonly IAsyncEnumerator<T> enumerator;

            readonly Func<R, T, R> reducer;

            bool ready;

            R accumulator;

            public ReduceAsyncEnumerator(IAsyncEnumerator<T> enumerator, R initial, Func<R, T, R> reducer)
            {
                this.enumerator = enumerator;
                this.accumulator = initial;
                this.reducer = reducer;
            }

            public R Current => ready ? accumulator : default;

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

                var c = accumulator;
                for (; ; )
                {
                    if (await enumerator.MoveNextAsync())
                    {
                        c = reducer(c, enumerator.Current);
                    }
                    else
                    {
                        accumulator = c;
                        ready = true;
                        return true;
                    }
                }
            }
        }
    }
}
