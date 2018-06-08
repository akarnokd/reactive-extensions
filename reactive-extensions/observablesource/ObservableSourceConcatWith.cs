using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceConcatWith<T> : IObservableSource<T>
    {
        readonly IObservableSource<T> source;

        readonly IObservableSource<T> other;

        public ObservableSourceConcatWith(IObservableSource<T> source, IObservableSource<T> other)
        {
            this.source = source;
            this.other = other;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            var parent = new ConcatWithObserver(observer, other);
            observer.OnSubscribe(parent);
            parent.Subscribe(source);
        }

        sealed class ConcatWithObserver : ISignalObserver<T>, IDisposable
        {
            readonly ISignalObserver<T> downstream;

            IObservableSource<T> other;

            IDisposable upstream;

            int wip;

            public ConcatWithObserver(ISignalObserver<T> downstream, IObservableSource<T> other)
            {
                this.downstream = downstream;
                this.other = other;
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
            }

            public void OnCompleted()
            {
                if (other == null)
                {
                    downstream.OnCompleted();
                }
                else
                {
                    Subscribe(null);
                }
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
                DisposableHelper.Replace(ref upstream, d);
            }

            internal void Subscribe(IObservableSource<T> source)
            {
                if (Interlocked.Increment(ref wip) == 1)
                {
                    for (; ; )
                    {
                        if (!DisposableHelper.IsDisposed(ref upstream))
                        {
                            if (source == null)
                            {
                                var o = other;
                                other = null;

                                o.Subscribe(this);
                            }
                            else
                            {
                                var o = source;
                                source = null;

                                o.Subscribe(this);
                            }
                        }
                        if (Interlocked.Decrement(ref wip) == 0)
                        {
                            break;
                        }
                    }
                }
            }
        }
    }
}
