using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceSwitchIfEmpty<T> : IObservableSource<T>
    {
        readonly IObservableSource<T> source;

        readonly IObservableSource<T>[] fallbacks;

        public ObservableSourceSwitchIfEmpty(IObservableSource<T> source, IObservableSource<T>[] fallbacks)
        {
            this.source = source;
            this.fallbacks = fallbacks;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            var parent = new SwitchIfEmptyObserver(observer, fallbacks);
            observer.OnSubscribe(parent);

            parent.Run(source);
        }

        sealed class SwitchIfEmptyObserver : ISignalObserver<T>, IDisposable
        {
            readonly ISignalObserver<T> downstream;

            readonly IObservableSource<T>[] fallbacks;

            IDisposable upstream;

            int index;

            bool hasValue;

            int wip;

            public SwitchIfEmptyObserver(ISignalObserver<T> downstream, IObservableSource<T>[] fallbacks)
            {
                this.downstream = downstream;
                this.fallbacks = fallbacks;
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
            }

            public void OnCompleted()
            {
                if (hasValue)
                {
                    downstream.OnCompleted();
                }
                else
                {
                    Run(null);
                }
            }

            public void OnError(Exception ex)
            {
                downstream.OnError(ex);
            }

            public void OnNext(T item)
            {
                hasValue = true;
                downstream.OnNext(item);
            }

            public void OnSubscribe(IDisposable d)
            {
                DisposableHelper.Replace(ref upstream, d);
            }

            internal void Run(IObservableSource<T> first)
            {
                if (Interlocked.Increment(ref wip) != 1)
                {
                    return;
                }

                for (; ; )
                {
                    if (first != null)
                    {
                        first.Subscribe(this);
                        first = null;
                    }
                    else
                    {
                        var idx = index;
                        if (idx == fallbacks.Length)
                        {
                            downstream.OnCompleted();
                        }
                        else
                        {
                            var src = fallbacks[idx];
                            if (src == null)
                            {
                                downstream.OnError(new NullReferenceException("The fallbacks[" + idx + "] is null"));
                            }
                            else
                            {
                                index = idx + 1;
                                src.Subscribe(this);
                            }
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
