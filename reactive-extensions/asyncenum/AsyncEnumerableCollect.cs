using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace akarnokd.reactive_extensions
{
    internal sealed class AsyncEnumerableCollect<T, C> : IAsyncEnumerable<C>
    {
        readonly IAsyncEnumerable<T> source;

        readonly Func<C> collectionSupplier;

        readonly Action<C, T> collector;

        public AsyncEnumerableCollect(IAsyncEnumerable<T> source, Func<C> collectionSupplier, Action<C, T> collector)
        {
            this.source = source;
            this.collectionSupplier = collectionSupplier;
            this.collector = collector;
        }

        public IAsyncEnumerator<C> GetAsyncEnumerator()
        {
            var collection = default(C);
            try
            {
                collection = collectionSupplier();
            }
            catch (Exception ex)
            {
                return new AsyncEnumerableError<C>.ErrorAsyncEnumerator(ex);
            }

            return new CollectAsyncEnumerator(source.GetAsyncEnumerator(), collection, collector);
        }

        sealed class CollectAsyncEnumerator : IAsyncEnumerator<C>
        {
            readonly IAsyncEnumerator<T> enumerator;

            readonly C collection;

            readonly Action<C, T> collector;

            bool ready;

            public CollectAsyncEnumerator(IAsyncEnumerator<T> enumerator, C collection, Action<C, T> collector)
            {
                this.enumerator = enumerator;
                this.collection = collection;
                this.collector = collector;
            }

            public C Current => ready ? collection : default;

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

                var c = collection;
                for (; ; )
                {
                    if (await enumerator.MoveNextAsync())
                    {
                        collector(c, enumerator.Current);
                    }
                    else
                    {
                        ready = true;
                        return true;
                    }
                }
            }
        }
    }
}
