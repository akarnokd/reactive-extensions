using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceDelaySubscription<T, U> : IObservableSource<T>
    {
        internal readonly IObservableSource<T> source;

        internal readonly IObservableSource<U> other;

        public ObservableSourceDelaySubscription(IObservableSource<T> source, IObservableSource<U> other)
        {
            this.source = source;
            this.other = other;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            var parent = new MainObserver(observer, source);
            observer.OnSubscribe(parent);

            other.Subscribe(parent.other);
        }

        sealed class MainObserver : ISignalObserver<T>, IDisposable
        {
            readonly ISignalObserver<T> downstream;

            internal readonly OtherObserver other;

            IDisposable upstream;

            public MainObserver(ISignalObserver<T> downstream, IObservableSource<T> source)
            {
                this.downstream = downstream;
                this.other = new OtherObserver(this, source);
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
                other.Dispose();
            }

            public void OnCompleted()
            {
                downstream.OnCompleted();
            }

            public void OnError(Exception ex)
            {
                downstream.OnError(ex);
            }

            public void OnNext(T item)
            {
                downstream.OnNext(item);
            }

            public void OnSubscribe(IDisposable d)
            {
                DisposableHelper.SetOnce(ref upstream, d);
            }

            internal sealed class OtherObserver : ISignalObserver<U>, IDisposable
            {
                IObservableSource<T> source;

                MainObserver parent;

                IDisposable upstream;

                public OtherObserver(MainObserver parent, IObservableSource<T> source)
                {
                    this.parent = parent;
                    Volatile.Write(ref this.source, source);
                }

                public void Dispose()
                {
                    DisposableHelper.Dispose(ref upstream);
                    Volatile.Write(ref parent, null);
                }

                public void OnCompleted()
                {
                    var p = Volatile.Read(ref parent);
                    if (p != null)
                    {
                        var src = source;
                        parent = null;
                        source = null;
                        DisposableHelper.WeakDispose(ref upstream);
                        src.Subscribe(p);
                    }
                }

                public void OnError(Exception ex)
                {
                    var p = Volatile.Read(ref parent);
                    if (p != null)
                    {
                        var src = source;
                        parent = null;
                        source = null;
                        p.OnError(ex);
                    }
                }

                public void OnNext(U item)
                {
                    var p = Volatile.Read(ref parent);
                    if (p != null)
                    {
                        var src = source;
                        parent = null;
                        source = null;
                        DisposableHelper.Dispose(ref upstream);
                        src.Subscribe(p);
                    }
                }

                public void OnSubscribe(IDisposable d)
                {
                    DisposableHelper.SetOnce(ref upstream, d);
                }
            }
        }
    }
}
