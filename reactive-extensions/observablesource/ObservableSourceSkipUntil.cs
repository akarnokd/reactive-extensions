using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceSkipUntil<T, U> : IObservableSource<T>
    {
        readonly IObservableSource<T> source;

        readonly IObservableSource<U> other;

        public ObservableSourceSkipUntil(IObservableSource<T> source, IObservableSource<U> other)
        {
            this.source = source;
            this.other = other;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            var parent = new TakeUntilMainObserver(observer);
            observer.OnSubscribe(parent);

            other.Subscribe(parent.other);
            source.Subscribe(parent);
        }

        sealed class TakeUntilMainObserver : ISignalObserver<T>, IDisposable
        {
            readonly ISignalObserver<T> downstream;

            internal readonly OtherObserver other;

            IDisposable upstream;

            int wip;

            Exception error;

            bool gate;

            public TakeUntilMainObserver(ISignalObserver<T> downstream)
            {
                this.downstream = downstream;
                this.other = new OtherObserver(this);
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
                other.Dispose();
            }

            public void OnCompleted()
            {
                other.Dispose();
                HalfSerializer.OnCompleted(downstream, ref wip, ref error);
            }

            public void OnError(Exception ex)
            {
                other.Dispose();
                HalfSerializer.OnError(downstream, ex, ref wip, ref error);
            }

            public void OnNext(T item)
            {
                if (Volatile.Read(ref gate))
                {
                    HalfSerializer.OnNext(downstream, item, ref wip, ref error);
                }
            }

            public void OnSubscribe(IDisposable d)
            {
                DisposableHelper.SetOnce(ref upstream, d);
            }

            void OtherError(Exception ex)
            {
                DisposableHelper.Dispose(ref upstream);
                HalfSerializer.OnError(downstream, ex, ref wip, ref error);
            }

            void OtherCompleted()
            {
                Volatile.Write(ref gate, true);
            }

            internal sealed class OtherObserver : ISignalObserver<U>, IDisposable
            {
                readonly TakeUntilMainObserver parent;

                IDisposable upstream;

                public OtherObserver(TakeUntilMainObserver parent)
                {
                    this.parent = parent;
                }

                public void Dispose()
                {
                    DisposableHelper.Dispose(ref upstream);
                }

                public void OnCompleted()
                {
                    parent.OtherCompleted();
                }

                public void OnError(Exception ex)
                {
                    parent.OtherError(ex);
                }

                public void OnNext(U item)
                {
                    Dispose();
                    parent.OtherCompleted();
                }

                public void OnSubscribe(IDisposable d)
                {
                    DisposableHelper.SetOnce(ref upstream, d);
                }
            }
        }
    }
}
