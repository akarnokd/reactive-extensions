using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceConcatEnumerable<T> : IObservableSource<T>
    {
        readonly IEnumerable<IObservableSource<T>> sources;

        readonly bool delayErrors;

        public ObservableSourceConcatEnumerable(IEnumerable<IObservableSource<T>> sources, bool delayErrors)
        {
            this.sources = sources;
            this.delayErrors = delayErrors;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            var en = default(IEnumerator<IObservableSource<T>>);

            try
            {
                en = ValidationHelper.RequireNonNullRef(sources.GetEnumerator(), "The GetEnumerator returned a null IEnumerator");
            }
            catch (Exception ex)
            {
                DisposableHelper.Error(observer, ex);
                return;
            }

            var parent = new ConcatObserver(observer, en, delayErrors);
            observer.OnSubscribe(parent);

            parent.Next();
        }

        sealed class ConcatObserver : ISignalObserver<T>, IDisposable
        {
            readonly ISignalObserver<T> downstream;

            IEnumerator<IObservableSource<T>> sources;

            readonly bool delayErrors;

            IDisposable upstream;

            Exception errors;

            int wip;

            public ConcatObserver(ISignalObserver<T> downstream, IEnumerator<IObservableSource<T>> sources, bool delayErrors)
            {
                this.downstream = downstream;
                this.sources = sources;
                this.delayErrors = delayErrors;
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
                Next();
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
                        if (DisposableHelper.IsDisposed(ref upstream))
                        {
                            sources?.Dispose();
                            sources = null;
                        }
                        else
                        {
                            var b = false;
                            var src = default(IObservableSource<T>);

                            try
                            {
                                b = sources.MoveNext();
                                if (b)
                                {
                                    src = ValidationHelper.RequireNonNullRef(sources.Current, "The enumerator returned a null IObservableSource");
                                }
                            }
                            catch (Exception ex)
                            {
                                DisposableHelper.WeakDispose(ref upstream);
                                downstream.OnError(ex);
                                continue;
                            }

                            if (b)
                            {
                                src.Subscribe(this);
                            }
                            else
                            {
                                DisposableHelper.WeakDispose(ref upstream);
                                var ex = Volatile.Read(ref errors);
                                if (ex != null)
                                {
                                    downstream.OnError(ex);
                                }
                                else
                                {
                                    downstream.OnCompleted();
                                }
                                continue;
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
