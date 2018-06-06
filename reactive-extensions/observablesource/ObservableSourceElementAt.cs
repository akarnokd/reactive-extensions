using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceElementAt<T> : IObservableSource<T>
    {
        readonly IObservableSource<T> source;

        readonly long index;

        public ObservableSourceElementAt(IObservableSource<T> source, long index)
        {
            this.source = source;
            this.index = index;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            source.Subscribe(new ElementAtObserver(observer, index));
        }

        sealed class ElementAtObserver : ObservableSourceElementAtObserver<T>
        {

            internal ElementAtObserver(ISignalObserver<T> downstream, long index) : base(downstream, index)
            {
            }

            public override void OnCompleted()
            {
                if (index >= 0L)
                {
                    Error(new IndexOutOfRangeException());
                }
            }
        }
    }

    internal abstract class ObservableSourceElementAtObserver<T> : DeferredScalarDisposable<T>, ISignalObserver<T>
    {
        protected long index;

        protected IDisposable upstream;

        internal ObservableSourceElementAtObserver(ISignalObserver<T> downstream, long index) : base(downstream)
        {
            this.index = index;
        }

        public abstract void OnCompleted();

        public void OnError(Exception ex)
        {
            if (index >= 0L)
            {
                Error(ex);
            }
        }

        public void OnNext(T item)
        {
            var idx = index;
            if (idx == 0)
            {
                upstream.Dispose();
                Complete(item);
            }
            index = idx - 1;
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

    internal sealed class ObservableSourceElementAtDefault<T> : IObservableSource<T>
    {
        readonly IObservableSource<T> source;

        readonly long index;

        readonly T defaultItem;

        public ObservableSourceElementAtDefault(IObservableSource<T> source, long index, T defaultItem)
        {
            this.source = source;
            this.index = index;
            this.defaultItem = defaultItem;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            source.Subscribe(new ElementAtObserver(observer, index, defaultItem));
        }

        sealed class ElementAtObserver : ObservableSourceElementAtObserver<T>
        {
            T defaultItem;

            internal ElementAtObserver(ISignalObserver<T> downstream, long index, T defaultItem) : base(downstream, index)
            {
                this.defaultItem = defaultItem;
            }

            public override void OnCompleted()
            {
                if (index >= 0L)
                {
                    var item = defaultItem;
                    defaultItem = default;
                    Complete(item);
                }
            }
        }
    }
}
