using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceConcatArray<T> : IObservableSource<T>
    {
        readonly IObservableSource<T>[] sources;

        readonly bool delayErrors;

        public ObservableSourceConcatArray(IObservableSource<T>[] sources, bool delayErrors)
        {
            this.sources = sources;
            this.delayErrors = delayErrors;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            var parent = new ConcatObserver(observer, sources, delayErrors);
            observer.OnSubscribe(parent);

            parent.Next();
        }

        sealed class ConcatObserver : ISignalObserver<T>, IDisposable
        {
            readonly ISignalObserver<T> downstream;

            readonly IObservableSource<T>[] sources;

            readonly bool delayErrors;

            IDisposable upstream;

            int index;

            Exception errors;

            int wip;

            public ConcatObserver(ISignalObserver<T> downstream, IObservableSource<T>[] sources, bool delayErrors)
            {
                this.downstream = downstream;
                this.sources = sources;
                this.delayErrors = delayErrors;
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
            }

            public void OnCompleted()
            {
                Next();
            }

            public void OnError(Exception ex)
            {
                if (delayErrors)
                {
                    ExceptionHelper.AddException(ref errors, ex);
                    Next();
                }
                else
                {
                    downstream.OnError(ex);
                }
            }

            public void OnNext(T item)
            {
                downstream.OnNext(item);
            }

            public void OnSubscribe(IDisposable d)
            {
                DisposableHelper.Replace(ref upstream, d);
            }

            internal void Next()
            {
                if (Interlocked.Increment(ref wip) == 1)
                {
                    for (; ; )
                    {
                        var idx = index;

                        if (idx == sources.Length)
                        {
                            var ex = Volatile.Read(ref errors);
                            if (ex != null)
                            {
                                downstream.OnError(ex);
                            }
                            else
                            {
                                downstream.OnCompleted();
                            }
                        }
                        else
                        {
                            var src = sources[idx];

                            if (src == null)
                            {
                                downstream.OnError(new NullReferenceException($"sources[{idx}] is null"));
                            }
                            else
                            {
                                index = idx + 1;

                                src.Subscribe(this);
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
