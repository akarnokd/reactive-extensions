using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceLast<T> : IObservableSource<T>
    {
        readonly IObservableSource<T> source;

        public ObservableSourceLast(IObservableSource<T> source)
        {
            this.source = source;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            source.Subscribe(new LastObserver(observer));
        }

        sealed class LastObserver : ObservableSourceLastObserver<T>
        {
            internal LastObserver(ISignalObserver<T> downstream) : base(downstream)
            {
            }

            public override void OnCompleted()
            {
                if (hasLast)
                {
                    var last = this.last;
                    this.last = default;
                    Complete(last);
                }
                else
                {
                    Error(new IndexOutOfRangeException());
                }
            }
        }
    }

    internal sealed class ObservableSourceLastDefault<T> : IObservableSource<T>
    {
        readonly IObservableSource<T> source;

        readonly T defaultItem;

        public ObservableSourceLastDefault(IObservableSource<T> source, T defaultItem)
        {
            this.source = source;
            this.defaultItem = defaultItem;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            source.Subscribe(new LastObserver(observer, defaultItem));
        }

        sealed class LastObserver : ObservableSourceLastObserver<T>
        {
            T defaultItem;

            internal LastObserver(ISignalObserver<T> downstream, T defaultItem) : base(downstream)
            {
                this.defaultItem = defaultItem;
            }

            public override void OnCompleted()
            {
                var item = default(T);
                if (hasLast)
                {
                    item = this.last;
                    this.last = default;
                }
                else
                {
                    item = this.defaultItem;
                    this.defaultItem = default;
                }
                Complete(item);
            }
        }
    }

    internal abstract class ObservableSourceLastObserver<T> : DeferredScalarDisposable<T>, ISignalObserver<T>
    {
        IDisposable upstream;

        protected T last;
        protected bool hasLast;

        protected ObservableSourceLastObserver(ISignalObserver<T> downstream) : base(downstream)
        {
        }

        public abstract void OnCompleted();

        public void OnError(Exception ex)
        {
            Error(ex);
        }

        public void OnNext(T item)
        {
            hasLast = true;
            last = item;
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
