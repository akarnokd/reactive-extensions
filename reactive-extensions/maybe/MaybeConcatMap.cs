using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using static akarnokd.reactive_extensions.ValidationHelper;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Maps the upstream observable sequence items into maybe sources, runs them one
    /// after the other relaying their success item in order and
    /// optionally delays errors from all sources until all of them terminate.
    /// </summary>
    /// <typeparam name="T">The element type of the upstream observable sequence.</typeparam>
    /// <typeparam name="R">The success value type of the inner maybe sources.</typeparam>
    /// <returns>The new observable instance.</returns>
    /// <remarks>Since 0.0.13</remarks>
    internal sealed class MaybeConcatMap<T, R> : IObservable<R>
    {
        readonly IObservable<T> source;

        readonly Func<T, IMaybeSource<R>> mapper;

        readonly bool delayErrors;

        public MaybeConcatMap(IObservable<T> source, Func<T, IMaybeSource<R>> mapper, bool delayErrors)
        {
            this.source = source;
            this.mapper = mapper;
            this.delayErrors = delayErrors;
        }

        public IDisposable Subscribe(IObserver<R> observer)
        {
            var parent = new ConcatMapObserver(observer, mapper, delayErrors);

            parent.OnSubscribe(source.Subscribe(parent));

            return parent;
        }

        sealed class ConcatMapObserver : IObserver<T>, IDisposable
        {
            readonly IObserver<R> downstream;

            readonly Func<T, IMaybeSource<R>> mapper;

            readonly bool delayErrors;

            readonly InnerObserver inner;

            readonly ConcurrentQueue<T> queue;

            bool done;
            Exception errors;

            IDisposable upstream;

            volatile bool active;

            int wip;

            bool disposed;

            public ConcatMapObserver(IObserver<R> downstream, Func<T, IMaybeSource<R>> mapper, bool delayErrors)
            {
                this.downstream = downstream;
                this.mapper = mapper;
                this.delayErrors = delayErrors;
                this.queue = new ConcurrentQueue<T>();
                this.inner = new InnerObserver(this);
            }

            internal void OnSubscribe(IDisposable d)
            {
                DisposableHelper.SetOnce(ref upstream, d);
            }

            public void Dispose()
            {
                Volatile.Write(ref disposed, true);
                DisposableHelper.Dispose(ref upstream);
                inner.Dispose();
                Drain();
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
                }
                else
                {
                    Interlocked.CompareExchange(ref errors, error, null);
                }
                Volatile.Write(ref done, true);
                Drain();
            }

            public void OnNext(T value)
            {
                queue.Enqueue(value);
                Drain();
            }

            void InnerSuccess(R item)
            {
                downstream.OnNext(item);
                active = false;
                Drain();
            }

            void InnerError(Exception ex)
            {
                if (delayErrors)
                {
                    ExceptionHelper.AddException(ref errors, ex);
                }
                else
                {
                    Interlocked.CompareExchange(ref errors, ex, null);
                }
                active = false;
                Drain();
            }

            void InnerCompleted()
            {
                active = false;
                Drain();
            }

            void Drain()
            {
                if (Interlocked.Increment(ref wip) == 1)
                {
                    var q = queue;

                    for (; ; )
                    {
                        if (Volatile.Read(ref disposed))
                        {
                            while (q.TryDequeue(out var _)) ;
                        }
                        else
                        {
                            if (!active)
                            {
                                if (!delayErrors)
                                {
                                    var ex = Volatile.Read(ref errors);
                                    if (ex != null)
                                    {
                                        downstream.OnError(ex);
                                        Volatile.Write(ref disposed, true);
                                        DisposableHelper.Dispose(ref upstream);
                                        continue;
                                    }
                                }

                                var d = Volatile.Read(ref done);

                                var empty = !q.TryDequeue(out var v);

                                if (d && empty)
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

                                    Volatile.Write(ref disposed, true);
                                    DisposableHelper.Dispose(ref upstream);
                                    continue;
                                }
                                else
                                if (!empty)
                                {
                                    var src = default(IMaybeSource<R>);

                                    try
                                    {
                                        src = RequireNonNullRef(mapper(v), "The mapper returned a null IMaybeSource");
                                    }
                                    catch (Exception ex)
                                    {
                                        DisposableHelper.Dispose(ref upstream);
                                        if (delayErrors)
                                        {
                                            ExceptionHelper.AddException(ref errors, ex);
                                        }
                                        else
                                        {
                                            Interlocked.CompareExchange(ref errors, ex, null);
                                        }
                                        Volatile.Write(ref done, true);
                                        continue;
                                    }

                                    active = true;
                                    src.Subscribe(inner);
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

            sealed class InnerObserver : IMaybeObserver<R>, IDisposable
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

                public void OnSuccess(R item)
                {
                    parent.InnerSuccess(item);
                }
            }
        }
    }
}
