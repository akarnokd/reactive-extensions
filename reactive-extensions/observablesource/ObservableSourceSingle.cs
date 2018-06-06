using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceSingle<T> : IObservableSource<T>
    {
        readonly IObservableSource<T> source;

        public ObservableSourceSingle(IObservableSource<T> source)
        {
            this.source = source;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            source.Subscribe(new SingleObserver(observer));
        }

        sealed class SingleObserver : ObservableSourceSingleObserver<T>
        {
            internal SingleObserver(ISignalObserver<T> downstream) : base(downstream)
            {
            }

            public override void OnCompleted()
            {
                if (done)
                {
                    return;
                }
                if (hasItem)
                {
                    var item = this.item;
                    this.item = default;
                    Complete(item);
                }
                else
                {
                    Error(new IndexOutOfRangeException());
                }
            }
        }
    }

    internal sealed class ObservableSourceSingleDefault<T> : IObservableSource<T>
    {
        readonly IObservableSource<T> source;

        readonly T defaultItem;

        public ObservableSourceSingleDefault(IObservableSource<T> source, T defaultItem)
        {
            this.source = source;
            this.defaultItem = defaultItem;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            source.Subscribe(new SingleObserver(observer, defaultItem));
        }

        sealed class SingleObserver : ObservableSourceSingleObserver<T>
        {
            T defaultItem;

            internal SingleObserver(ISignalObserver<T> downstream, T defaultItem) : base(downstream)
            {
                this.defaultItem = defaultItem;
            }

            public override void OnCompleted()
            {
                if (done)
                {
                    return;
                }
                var item = default(T);
                if (hasItem)
                {
                    item = this.item;
                    this.item = default;
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

    internal abstract class ObservableSourceSingleObserver<T> : DeferredScalarDisposable<T>, ISignalObserver<T>
    {
        protected bool hasItem;
        protected T item;
        protected bool done;

        IDisposable upstream;

        protected ObservableSourceSingleObserver(ISignalObserver<T> downstream) : base(downstream)
        {
        }

        public abstract void OnCompleted();

        public void OnError(Exception ex)
        {
            if (done)
            {
                return;
            }
            Error(ex);
        }

        public void OnNext(T item)
        {
            if (done)
            {
                return;
            }

            if (hasItem)
            {
                done = true;
                item = default;
                upstream.Dispose();
                Error(new IndexOutOfRangeException("The source has more than one element"));
            }
            else
            {
                this.item = item;
                this.hasItem = true;
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
