using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Collects upstream items into a per-observer collection object created
    /// via a function and added via a collector action and emits this collection
    /// when the upstream completes.
    /// </summary>
    /// <typeparam name="T">The upstream value type.</typeparam>
    /// <typeparam name="C">The output collection type.</typeparam>
    /// <remarks>Since 0.0.3</remarks>
    internal sealed class Collect<T, C> : IObservable<C>
    {
        readonly IObservable<T> source;
        readonly Func<C> collectionSupplier;
        readonly Action<C, T> collector;

        public Collect(IObservable<T> source, Func<C> collectionSupplier, Action<C, T> collector)
        {
            this.source = source;
            this.collectionSupplier = collectionSupplier;
            this.collector = collector;
        }

        public IDisposable Subscribe(IObserver<C> observer)
        {
            var c = default(C);
            try
            {
                c = collectionSupplier();
            }
            catch (Exception ex)
            {
                observer.OnError(ex);
                return DisposableHelper.EMPTY;
            }

            var parent = new CollectObserver(observer, c, collector);
            var d = source.Subscribe(parent);
            parent.OnSubscribe(d);
            return parent;
        }

        sealed class CollectObserver : BaseObserver<T, C>
        {
            readonly Action<C, T> collector;

            C collection;

            bool done;

            internal CollectObserver(IObserver<C> downstream, C collection, Action<C, T> collector) : base(downstream)
            {
                this.collection = collection;
                this.collector = collector;
            }

            public override void OnCompleted()
            {
                if (done)
                {
                    return;
                }
                var c = collection;
                collection = default(C);
                downstream.OnNext(c);
                downstream.OnCompleted();
                Dispose();
            }

            public override void OnError(Exception error)
            {
                if (done)
                {
                    return;
                }
                collection = default(C);
                downstream.OnError(error);
                Dispose();
            }

            public override void OnNext(T value)
            {
                if (done)
                {
                    return;
                }
                try
                {
                    collector(collection, value);
                }
                catch (Exception ex)
                {
                    done = true;
                    collection = default(C);
                    downstream.OnError(ex);
                    Dispose();
                }
            }
        }
    }
}
