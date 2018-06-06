using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceReduce<T, C> : IObservableSource<C>
    {
        readonly IObservableSource<T> source;

        readonly Func<C> initialSupplier;

        readonly Func<C, T, C> reducer;

        public ObservableSourceReduce(IObservableSource<T> source, Func<C> initialSupplier, Func<C, T, C> reducer)
        {
            this.source = source;
            this.initialSupplier = initialSupplier;
            this.reducer = reducer;
        }

        public void Subscribe(ISignalObserver<C> observer)
        {
            var coll = default(C);

            try
            {
                coll = initialSupplier();
            }
            catch (Exception ex)
            {
                DisposableHelper.Error(observer, ex);
                return;
            }

            source.Subscribe(new ReduceObserver(observer, reducer, coll));
        }

        sealed class ReduceObserver : DeferredScalarDisposable<C>, ISignalObserver<T>
        {
            readonly Func<C, T, C> reducer;

            IDisposable upstream;

            C collection;

            bool done;

            public ReduceObserver(ISignalObserver<C> downstream, Func<C, T, C> reducer, C collection) : base(downstream)
            {
                this.reducer = reducer;
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
                    collection = reducer(collection, item);
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

    internal sealed class ObservableSourceReducePlain<T> : IObservableSource<T>
    {
        readonly IObservableSource<T> source;

        readonly Func<T, T, T> reducer;

        public ObservableSourceReducePlain(IObservableSource<T> source, Func<T, T, T> reducer)
        {
            this.source = source;
            this.reducer = reducer;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            source.Subscribe(new ReduceObserver(observer, reducer));
        }

        sealed class ReduceObserver : DeferredScalarDisposable<T>, ISignalObserver<T>
        {
            readonly Func<T, T, T> reducer;

            IDisposable upstream;

            T collection;
            bool hasValue;

            bool done;

            public ReduceObserver(ISignalObserver<T> downstream, Func<T, T, T> reducer) : base(downstream)
            {
                this.reducer = reducer;
            }

            public void OnCompleted()
            {
                if (done)
                {
                    return;
                }
                var c = collection;
                collection = default;

                if (hasValue)
                {
                    Complete(c);
                }
                else
                {
                    Complete();
                }
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
                if (hasValue)
                {
                    try
                    {
                        collection = reducer(collection, item);
                    }
                    catch (Exception ex)
                    {
                        upstream.Dispose();
                        collection = default;
                        done = true;
                        Error(ex);
                    }
                }
                else
                {
                    collection = item;
                    hasValue = true;
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
