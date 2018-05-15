using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using static akarnokd.reactive_extensions.ValidationHelper;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Maps the elements of an observable source into completable sources
    /// and runs them one after the other completes, optionally delaying
    /// errors until all sources terminate.
    /// </summary>
    /// <typeparam name="T">The element type of the upstream observable source.</typeparam>
    internal sealed class CompletableConcatMap<T> : ICompletableSource
    {
        readonly IObservable<T> source;

        readonly Func<T, ICompletableSource> mapper;

        readonly bool delayErrors;

        public CompletableConcatMap(IObservable<T> source, Func<T, ICompletableSource> mapper, bool delayErrors)
        {
            this.source = source;
            this.mapper = mapper;
            this.delayErrors = delayErrors;
        }

        public void Subscribe(ICompletableObserver observer)
        {
            var parent = new ConcatMapObserver(observer, mapper, delayErrors);
            observer.OnSubscribe(parent);
            parent.OnSubscribe(source.Subscribe(parent));
        }

        internal sealed class ConcatMapObserver : IObserver<T>, IDisposable
        {
            readonly ICompletableObserver downstream;

            readonly Func<T, ICompletableSource> mapper;

            readonly bool delayErrors;

            readonly InnerObserver inner;

            readonly ConcurrentQueue<T> queue;

            IDisposable upstream;

            int wip;

            bool done;
            Exception errors;

            volatile bool active;

            volatile bool disposed;

            public ConcatMapObserver(ICompletableObserver downstream, Func<T, ICompletableSource> mapper, bool delayErrors)
            {
                this.downstream = downstream;
                this.mapper = mapper;
                this.delayErrors = delayErrors;
                this.inner = new InnerObserver(this);
                this.queue = new ConcurrentQueue<T>();
            }

            public void Dispose()
            {
                disposed = true;
                DisposableHelper.Dispose(ref upstream);
                inner.Dispose();
                Drain();
            }

            public void OnSubscribe(IDisposable d)
            {
                DisposableHelper.SetOnce(ref upstream, d);
            }

            public void OnCompleted()
            {
                Volatile.Write(ref done, true);
                Drain();
            }

            public void OnError(Exception error)
            {
                if (delayErrors)
                {
                    ExceptionHelper.AddException(ref errors, error);
                    Volatile.Write(ref done, true);
                    Drain();
                }
                else
                {
                    if (Interlocked.CompareExchange(ref errors, error, null) == null)
                    {
                        Volatile.Write(ref done, true);
                        Drain();
                    }
                }
                DisposableHelper.Dispose(ref upstream);
            }

            public void OnNext(T value)
            {
                queue.Enqueue(value);
                Drain();
            }

            void InnerCompleted()
            {
                active = false;
                Drain();
            }

            void InnerError(Exception error)
            {
                if (delayErrors)
                {
                    ExceptionHelper.AddException(ref errors, error);
                    active = false;
                    Drain();
                }
                else
                {
                    if (Interlocked.CompareExchange(ref errors, error, null) == null)
                    {
                        DisposableHelper.Dispose(ref upstream);
                        Volatile.Write(ref done, true);
                        active = false;
                        Drain();
                    }
                }
            }

            internal void Drain()
            {
                if (Interlocked.Increment(ref wip) == 1)
                {
                    var q = queue;
                    var delayErrors = this.delayErrors;


                    for (; ; )
                    {
                        if (disposed)
                        {
                            while (q.TryDequeue(out var _)) ;
                        }
                        else
                        {
                            if (!active)
                            {
                                var d = Volatile.Read(ref done);
                                if (d && !delayErrors)
                                {
                                    var ex = Volatile.Read(ref errors);
                                    if (ex != null)
                                    {
                                        downstream.OnError(ex);
                                        disposed = true;
                                        continue;
                                    }
                                }

                                bool empty = !q.TryDequeue(out var v);

                                if (d && empty)
                                {
                                    var ex = ExceptionHelper.Terminate(ref errors);
                                    if (ex != null)
                                    {
                                        downstream.OnError(ex);
                                    }
                                    else
                                    {
                                        downstream.OnCompleted();
                                    }
                                    disposed = true;
                                    continue;
                                }

                                if (!empty)
                                {
                                    var c = default(ICompletableSource);

                                    try
                                    {
                                        c = RequireNonNullRef(mapper(v), "The mapper returned a null ICompletableSource");
                                    }
                                    catch (Exception ex)
                                    {
                                        DisposableHelper.Dispose(ref upstream);

                                        ExceptionHelper.AddException(ref errors, ex);
                                        ex = ExceptionHelper.Terminate(ref errors);

                                        downstream.OnError(ex);

                                        disposed = true;
                                        continue;
                                    }

                                    active = true;
                                    c.Subscribe(inner);
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

            sealed internal class InnerObserver : ICompletableObserver, IDisposable
            {
                readonly ConcatMapObserver parent;

                IDisposable upstream;

                public InnerObserver(ConcatMapObserver parent)
                {
                    this.parent = parent;
                }

                public void Dispose()
                {
                    DisposableHelper.Dispose(ref upstream);
                }

                public void OnCompleted()
                {
                    parent.InnerCompleted();
                }

                public void OnError(Exception error)
                {
                    parent.InnerError(error);
                }

                public void OnSubscribe(IDisposable d)
                {
                    DisposableHelper.Replace(ref upstream, d);
                }
            }
        }
    }
}
