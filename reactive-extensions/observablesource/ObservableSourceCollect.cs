using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceCollect<T, C> : IObservableSource<C>
    {
        readonly IObservableSource<T> source;

        readonly Func<C> collectionSupplier;

        readonly Action<C, T> collector;

        public ObservableSourceCollect(IObservableSource<T> source, Func<C> collectionSupplier, Action<C, T> collector)
        {
            this.source = source;
            this.collectionSupplier = collectionSupplier;
            this.collector = collector;
        }

        public void Subscribe(ISignalObserver<C> observer)
        {
            var coll = default(C);

            try
            {
                coll = collectionSupplier();
            }
            catch (Exception ex)
            {
                DisposableHelper.Error(observer, ex);
                return;
            }

            source.Subscribe(new CollectObserver(observer, collector, coll));
        }

        sealed class CollectObserver : DeferredScalarDisposable<C>, ISignalObserver<T>
        {
            readonly Action<C, T> collector;

            IDisposable upstream;

            C collection;

            bool done;

            public CollectObserver(ISignalObserver<C> downstream, Action<C, T> collector, C collection) : base(downstream)
            {
                this.collector = collector;
                this.collection = collection;
            }

            public void OnCompleted()
            {
                if (done)
                {
                    return;
                }
                var c = collection;
                collection = default;

                Complete(c);
            }

            public void OnError(Exception ex)
            {
                if (done)
                {
                    return;
                }
                collection = default;
                Error(ex);
            }

            public void OnNext(T item)
            {
                if (done)
                {
                    return;
                }
                try
                {
                    collector(collection, item);
                }
                catch (Exception ex)
                {
                    upstream.Dispose();
                    collection = default;
                    done = true;
                    Error(ex);
                }
            }

            public void OnSubscribe(IDisposable d)
            {
                upstream = d;
                downstream.OnSubscribe(this);
            }

            public override void Dispose()
            {
                base.Dispose();
                upstream.Dispose();
            }
        }
    }
}
