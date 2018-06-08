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
    /// <remarks>Since 0.0.21</remarks>
    internal sealed class ObservableSourceWithLatestFrom<T, U, R> : IObservableSource<R>
    {
        readonly IObservableSource<T> source;

        readonly IObservableSource<U>[] others;

        readonly Func<T, U[], R> mapper;

        readonly bool delayErrors;

        readonly bool sourceFirst;

        public ObservableSourceWithLatestFrom(IObservableSource<T> source, IObservableSource<U>[] others, Func<T, U[], R> mapper, bool delayErrors, bool sourceFirst)
        {
            this.source = source;
            this.others = others;
            this.mapper = mapper;
            this.delayErrors = delayErrors;
            this.sourceFirst = sourceFirst;
        }

        public void Subscribe(ISignalObserver<R> observer)
        {
            var o = others;
            var n = o.Length;

            var parent = new WithLatestFromObserver(observer, n, mapper, delayErrors);
            observer.OnSubscribe(parent);

            if (sourceFirst)
            {
                source.Subscribe(parent);
                parent.SubscribeAll(o);
            }
            else
            {
                parent.SubscribeAll(o);
                source.Subscribe(parent);
            }
        }

        sealed class WithLatestFromObserver : BaseSignalObserver<T, R>
        {
            readonly Func<T, U[], R> mapper;

            readonly bool delayErrors;

            readonly InnerObserver[] observers;

            Exception errors;

            int wip;

            internal WithLatestFromObserver(ISignalObserver<R> downstream, int n, Func<T, U[], R> mapper, bool delayErrors) : base(downstream)
            {
                this.mapper = mapper;
                this.delayErrors = delayErrors;
                this.observers = new InnerObserver[n];
                for (int i = 0; i < n; i++)
                {
                    this.observers[i] = new InnerObserver(this, i);
                }
            }

            internal void SubscribeAll(IObservableSource<U>[] others)
            {
                var o = observers;
                var n = o.Length;

                for (int i = 0; i < n; i++)
                {
                    if (IsDisposed())
                    {
                        return;
                    }

                    InnerObserver inner = Volatile.Read(ref o[i]);
                    if (inner != null)
                    {
                        others[i].Subscribe(inner);
                    }
                }
            }

            public override void OnSubscribe(IDisposable d)
            {
                DisposableHelper.SetOnce(ref upstream, d);
            }

            public override void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
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
                }
            }

            public override void OnError(Exception error)
            {
                DisposeInners();
                if (delayErrors)
                {
                    if (!ExceptionHelper.AddException(ref errors, error))
                    {
                        return;
                    }
                }
                else
                {
                    if (Interlocked.CompareExchange(ref errors, error, null) != null)
                    {
                        return;
                    }
                }
                if (Interlocked.Increment(ref wip) == 1)
                {
                    var ex = ExceptionHelper.Terminate(ref errors);
                    downstream.OnError(ex);
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
                        Dispose();
                        OnError(ex);
                        return;
                    }

                    if (Interlocked.CompareExchange(ref wip, 1, 0) == 0)
                    {
                        downstream.OnNext(result);
                        if (Interlocked.Decrement(ref wip) != 0)
                        {
                            var ex = ExceptionHelper.Terminate(ref errors);
                            downstream.OnError(ex);
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
                            Dispose();
                        }
                    }
                }
            }

            sealed class InnerObserver : ISignalObserver<U>, IDisposable
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

                public void OnSubscribe(IDisposable d)
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
