using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    internal sealed class SingleSingleOrDefault<T> : ISingleSource<T>
    {
        readonly IObservable<T> source;

        readonly T defaultItem;

        public SingleSingleOrDefault(IObservable<T> source, T defaultItem)
        {
            this.source = source;
            this.defaultItem = defaultItem;
        }

        public void Subscribe(ISingleObserver<T> observer)
        {
            var parent = new SingleOrDefaultObserver(observer, defaultItem);
            observer.OnSubscribe(parent);

            parent.OnSubscribe(source.Subscribe(parent));
        }

        sealed class SingleOrDefaultObserver : IObserver<T>, IDisposable
        {
            readonly ISingleObserver<T> downstream;

            readonly T defaultItem;

            IDisposable upstream;

            bool hasValue;
            T value;

            bool done;

            public SingleOrDefaultObserver(ISingleObserver<T> downstream, T defaultItem)
            {
                this.downstream = downstream;
                this.defaultItem = defaultItem;
            }

            public void OnSubscribe(IDisposable d)
            {
                DisposableHelper.SetOnce(ref upstream, d);
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
            }

            public void OnCompleted()
            {
                if (!done)
                {
                    if (hasValue)
                    {
                        downstream.OnSuccess(value);
                    }
                    else
                    {
                        downstream.OnSuccess(defaultItem);
                    }
                    Dispose();
                }
            }

            public void OnError(Exception error)
            {
                if (!done)
                {
                    downstream.OnError(error);
                    Dispose();
                }
            }

            public void OnNext(T value)
            {
                if (!done)
                {
                    if (hasValue)
                    {
                        done = true;
                        downstream.OnError(new IndexOutOfRangeException("The source has more than one item"));
                        Dispose();
                    }
                    else
                    {
                        hasValue = true;
                        this.value = value;
                    }
                }
            }
        }
    }
}
