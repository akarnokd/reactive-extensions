using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using static akarnokd.reactive_extensions.ValidationHelper;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Maps the upstream observable sequence into maybe sources and switches to
    /// the next inner source when it becomes mapped, disposing the previous inner
    /// source if it is still running, optionally delaying
    /// errors until all sources terminate.
    /// </summary>
    /// <typeparam name="T">The element type of the observable sequence.</typeparam>
    /// <typeparam name="R">The success value type of the inner maybe sources.</typeparam>
    /// <remarks>Since 0.0.13</remarks>
    internal sealed class MaybeSwitchMap<T, R> : IObservable<R>
    {
        readonly IObservable<T> source;

        readonly Func<T, IMaybeSource<R>> mapper;

        readonly bool delayErrors;

        public MaybeSwitchMap(IObservable<T> source, Func<T, IMaybeSource<R>> mapper, bool delayErrors)
        {
            this.source = source;
            this.mapper = mapper;
            this.delayErrors = delayErrors;
        }

        public IDisposable Subscribe(IObserver<R> observer)
        {
            var parent = new SwitchMapObserver(observer, mapper, delayErrors);

            parent.OnSubscribe(source.Subscribe(parent));

            return parent;
        }

        sealed class SwitchMapObserver : IObserver<T>, IDisposable
        {
            readonly IObserver<R> downstream;

            readonly Func<T, IMaybeSource<R>> mapper;

            readonly bool delayErrors;

            IDisposable upstream;

            int wip;

            bool done;
            Exception errors;

            InnerObserver current;

            static readonly InnerObserver DISPOSED = new InnerObserver(null);

            public SwitchMapObserver(IObserver<R> downstream, Func<T, IMaybeSource<R>> mapper, bool delayErrors)
            {
                this.downstream = downstream;
                this.mapper = mapper;
                this.delayErrors = delayErrors;
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);

                var inner = Volatile.Read(ref current);
                if (inner != DISPOSED)
                {
                    Interlocked.Exchange(ref current, DISPOSED)?.Dispose();
                }
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
                if (done)
                {
                    return;
                }
                var inner = Volatile.Read(ref current);
                if (inner == DISPOSED)
                {
                    return;
                }

                if (Interlocked.CompareExchange(ref current, null, inner) != inner)
                {
                    return;
                }
                inner?.Dispose();

                var src = default(IMaybeSource<R>);
                try
                {
                    src = RequireNonNullRef(mapper(value), "The mapper returned a null IMaybeSource");
                }
                catch (Exception ex)
                {
                    OnError(ex);
                    return;
                }

                inner = new InnerObserver(this);

                if (Interlocked.CompareExchange(ref current, inner, null) == null)
                {
                    src.Subscribe(inner);
                }
            }

            void Drain()
            {
                if (Interlocked.Increment(ref wip) == 1)
                {
                    DrainLoop();
                }
            }

            void InnerError(InnerObserver inner, Exception ex)
            {
                if (delayErrors)
                {
                    ExceptionHelper.AddException(ref errors, ex);
                    inner.SetDone();
                }
                else
                {
                    Interlocked.CompareExchange(ref errors, ex, null);
                }
                Drain();
            }

            void DrainLoop()
            {
                var missed = 1;
                var downstream = this.downstream;

                for (; ; )
                {
                    if (!DisposableHelper.IsDisposed(ref upstream))
                    {
                        if (!delayErrors)
                        {
                            var ex = Volatile.Read(ref errors);
                            if (ex != null)
                            {
                                downstream.OnError(errors);
                                Dispose();
                                continue;
                            }
                        }

                        var d = Volatile.Read(ref done);

                        var inner = Volatile.Read(ref current);

                        var empty = inner == null;

                        if (d && empty)
                        {
                            var ex = Volatile.Read(ref errors);
                            if (ex != null)
                            {
                                downstream.OnError(errors);
                            }
                            else
                            {
                                downstream.OnCompleted();
                            }
                            DisposableHelper.Dispose(ref upstream);
                        }
                        else
                        if (!empty)
                        {
                            var state = inner.GetState();
                            if (state == 1)
                            {
                                downstream.OnNext(inner.value);
                                Interlocked.CompareExchange(ref current, null, current);
                                continue;
                            }
                            else
                            if (state == 2)
                            {
                                Interlocked.CompareExchange(ref current, null, current);
                                continue;
                            }
                        }
                    }

                    missed = Interlocked.Add(ref wip, -missed);
                    if (missed == 0)
                    {
                        break;
                    }
                }
            }

            internal sealed class InnerObserver : IMaybeObserver<R>, IDisposable
            {
                readonly SwitchMapObserver parent;

                IDisposable upstream;

                internal R value;

                int state;

                public InnerObserver(SwitchMapObserver parent)
                {
                    this.parent = parent;
                }

                internal int GetState()
                {
                    return Volatile.Read(ref state);
                }
                
                internal void SetDone()
                {
                    Volatile.Write(ref state, 2);
                }

                public void Dispose()
                {
                    DisposableHelper.Dispose(ref upstream);
                }

                public void OnCompleted()
                {
                    DisposableHelper.WeakDispose(ref upstream);
                    SetDone();
                    parent.Drain();
                }

                public void OnError(Exception error)
                {
                    DisposableHelper.WeakDispose(ref upstream);
                    parent.InnerError(this, error);
                }

                public void OnSubscribe(IDisposable d)
                {
                    DisposableHelper.SetOnce(ref upstream, d);
                }

                public void OnSuccess(R item)
                {
                    DisposableHelper.WeakDispose(ref upstream);
                    value = item;
                    Volatile.Write(ref state, 1);
                    parent.Drain();
                }
            }
        }
    }
}
