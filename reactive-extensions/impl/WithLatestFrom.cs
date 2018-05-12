using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Combines the latest values of multiple alternate observables with 
    /// the value of the main observable sequence through a function.
    /// </summary>
    /// <typeparam name="T">The element type of the main source.</typeparam>
    /// <typeparam name="U">The common type of the alternate observables.</typeparam>
    /// <typeparam name="R">The result type of the flow.</typeparam>
    internal sealed class WithLatestFrom<T, U, R> : IObservable<R>
    {
        readonly IObservable<T> source;

        readonly IObservable<U>[] others;

        readonly Func<T, U[], R> mapper;

        readonly bool delayErrors;

        readonly bool sourceFirst;

        public WithLatestFrom(IObservable<T> source, IObservable<U>[] others, Func<T, U[], R> mapper, bool delayErrors, bool sourceFirst)
        {
            this.source = source;
            this.others = others;
            this.mapper = mapper;
            this.delayErrors = delayErrors;
            this.sourceFirst = sourceFirst;
        }

        public IDisposable Subscribe(IObserver<R> observer)
        {
            var o = others;
            var n = o.Length;

            var parent = new WithLatestFromObserver(observer, n, mapper, delayErrors);

            if (sourceFirst)
            {
                parent.OnSubscribe(source.Subscribe(parent));
                parent.SubscribeAll(o);
            }
            else
            {
                parent.SubscribeAll(o);
                parent.OnSubscribe(source.Subscribe(parent));
            }

            return parent;
        }

        sealed class WithLatestFromObserver : BaseObserver<T, R>
        {
            readonly Func<T, U[], R> mapper;

            readonly bool delayErrors;

            readonly InnerObserver[] observers;

            Exception errors;

            int wip;

            internal WithLatestFromObserver(IObserver<R> downstream, int n, Func<T, U[], R> mapper, bool delayErrors) : base(downstream)
            {
                this.mapper = mapper;
                this.delayErrors = delayErrors;
                this.observers = new InnerObserver[n];
                for (int i = 0; i < n; i++)
                {
                    this.observers[i] = new InnerObserver(this, i);
                }
            }

            internal void SubscribeAll(IObservable<U>[] others)
            {
                var o = observers;
                var n = o.Length;

                for (int i = 0; i < n; i++)
                {
                    if (IsDisposed())
                    {
                        return;
                    }

                    InnerObserver inner = o[i];
                    inner.OnSubscribe(others[i].Subscribe(o[i]));
                }
            }

            public override void Dispose()
            {
                base.Dispose();
                DisposeInners();
            }

            void DisposeInners()
            {
                var o = observers;
                var n = o.Length;
                for (int i = 0; i < n; i++)
                {
                    if (Volatile.Read(ref o[i]) != null)
                    {
                        Interlocked.Exchange(ref o[i], null)?.Dispose();
                    }
                }
            }

            public override void OnCompleted()
            {
                DisposeInners();
                if (Interlocked.Increment(ref wip) == 1)
                {
                    var ex = ExceptionHelper.Terminate(ref errors);
                    if (ex == null)
                    {
                        downstream.OnCompleted();
                    }
                    else
                    {
                        downstream.OnError(ex);
                    }
                    base.Dispose();
                }
            }

            public override void OnError(Exception error)
            {
                DisposeInners();
                if (delayErrors)
                {
                    if (!ExceptionHelper.AddException(ref errors, error))
                    {
                        base.Dispose();
                        return;
                    }
                }
                else
                {
                    if (Interlocked.CompareExchange(ref errors, error, null) != null)
                    {
                        base.Dispose();
                        return;
                    }
                }
                if (Interlocked.Increment(ref wip) == 1)
                {
                    var ex = ExceptionHelper.Terminate(ref errors);
                    downstream.OnError(ex);
                    base.Dispose();
                }
            }

            public override void OnNext(T value)
            {
                var o = observers;
                var n = o.Length;

                var otherValues = new U[n];

                var found = true;
                for (int i = 0; i < n; i++)
                {
                    InnerObserver inner = Volatile.Read(ref o[i]);
                    if (inner == null)
                    {
                        return;
                    }
                    if (!inner.TryLatest(out otherValues[i]))
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                {
                    var result = default(R);

                    try
                    {
                        result = mapper(value, otherValues);
                    }
                    catch (Exception ex)
                    {
                        OnError(ex);
                        base.Dispose();
                        return;
                    }

                    if (Interlocked.CompareExchange(ref wip, 1, 0) == 0)
                    {
                        downstream.OnNext(result);
                        if (Interlocked.Decrement(ref wip) != 0)
                        {
                            var ex = ExceptionHelper.Terminate(ref errors);
                            downstream.OnError(ex);
                            base.Dispose();
                        }
                    }
                }
            }

            void InnerError(Exception ex)
            {
                if (delayErrors)
                {
                    ExceptionHelper.AddException(ref errors, ex);
                }
                else
                {
                    if (Interlocked.CompareExchange(ref errors, ex, null) == null)
                    {
                        if (Interlocked.Increment(ref wip) == 1)
                        {
                            ex = ExceptionHelper.Terminate(ref errors);
                            downstream.OnError(ex);
                            base.Dispose();
                        }
                    }
                }
            }

            sealed class InnerObserver : IObserver<U>, IDisposable
            {
                readonly WithLatestFromObserver parent;

                readonly int index;

                bool hasLatest;
                U latest;

                IDisposable upstream;

                public InnerObserver(WithLatestFromObserver parent, int index)
                {
                    this.parent = parent;
                    this.index = index;
                }

                internal void OnSubscribe(IDisposable d)
                {
                    DisposableHelper.SetOnce(ref upstream, d);
                }

                public void Dispose()
                {
                    DisposableHelper.Dispose(ref upstream);
                }

                public void OnCompleted()
                {
                    Dispose();
                }

                public void OnError(Exception error)
                {
                    parent.InnerError(error);
                    Dispose();
                }

                public void OnNext(U value)
                {
                    lock (this)
                    {
                        latest = value;
                        hasLatest = true;
                    }
                }

                internal bool TryLatest(out U value)
                {
                    lock (this)
                    {
                        if (hasLatest)
                        {
                            value = latest;
                            return true;
                        }
                        else
                        {
                            value = default(U);
                            return false;
                        }
                    }
                }
            }
        }
    }
}
