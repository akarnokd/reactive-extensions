using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Switches to a new observable mapped via a function in response to
    /// an new upstream item, disposing the previous active observable.
    /// </summary>
    /// <typeparam name="T">The value type of the upstream sequence.</typeparam>
    /// <typeparam name="R">The result type of the sequence</typeparam>
    /// <remarks>Since 0.0.21</remarks>
    internal sealed class ObservableSourceSwitchMap<T, R> : IObservableSource<R>
    {
        readonly IObservableSource<T> source;

        readonly Func<T, IObservableSource<R>> mapper;

        readonly bool delayErrors;

        readonly int capacityHint;

        public ObservableSourceSwitchMap(IObservableSource<T> source, 
            Func<T, IObservableSource<R>> mapper, bool delayErrors, int capacityHint)
        {
            this.source = source;
            this.mapper = mapper;
            this.delayErrors = delayErrors;
            this.capacityHint = capacityHint;
        }

        public void Subscribe(ISignalObserver<R> observer)
        {
            source.Subscribe(new SwitchMapObserver(observer, mapper, delayErrors, capacityHint));
        }

        sealed class SwitchMapObserver : BaseSignalObserver<T, R>, IInnerSignalObserverSupport<R>
        {
            readonly Func<T, IObservableSource<R>> mapper;

            readonly bool delayErrors;

            readonly int capacityHint;

            InnerSignalObserver<R> active;

            bool done;
            Exception errors;

            int wip;

            static readonly InnerSignalObserver<R> DISPOSED = new InnerSignalObserver<R>(null);

            internal SwitchMapObserver(ISignalObserver<R> downstream, Func<T, IObservableSource<R>> mapper, bool delayErrors, int capacityHint) : base(downstream)
            {
                this.mapper = mapper;
                this.delayErrors = delayErrors;
                this.capacityHint = capacityHint;
            }

            public void InnerComplete(InnerSignalObserver<R> sender)
            {
                sender.Dispose();
                if (Volatile.Read(ref active) == sender)
                {
                    sender.SetDone();
                    Drain();
                }
            }

            public void InnerError(InnerSignalObserver<R> sender, Exception error)
            {
                sender.Dispose();
                if (Volatile.Read(ref active) == sender)
                {
                    if (delayErrors)
                    {
                        ExceptionHelper.AddException(ref errors, error);
                        sender.SetDone();
                        Drain();
                    }
                    else
                    {
                        if (Interlocked.CompareExchange(ref errors, error, null) == null)
                        {
                            sender.SetDone();
                            Drain();
                        }
                    }
                }
            }

            public void InnerNext(InnerSignalObserver<R> sender, R item)
            {
                if (Interlocked.CompareExchange(ref wip, 1, 0) == 0)
                {
                    var a = Volatile.Read(ref active);
                    if (a == sender)
                    {
                        downstream.OnNext(item);
                    }
                    if (Interlocked.Decrement(ref wip) == 0)
                    {
                        return;
                    }
                }
                else
                {
                    var a = Volatile.Read(ref active);
                    if (a == sender)
                    {
                        var q = sender.GetOrCreateQueue(capacityHint);
                        q.TryOffer(item);
                        if (Interlocked.Increment(ref wip) != 1)
                        {
                            return;
                        }
                    }
                }
                DrainLoop();
            }

            public override void OnCompleted()
            {
                if (done)
                {
                    return;
                }
                Volatile.Write(ref done, true);
                Drain();
                base.Dispose();
            }

            public override void OnError(Exception error)
            {
                if (done)
                {
                    return;
                }
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
                base.Dispose();
            }

            public override void OnNext(T value)
            {
                if (done)
                {
                    return;
                }

                var o = default(IObservableSource<R>);

                try
                {
                    o = ValidationHelper.RequireNonNullRef(mapper(value), "The mapper returned a null IObservableSource");
                }
                catch (Exception ex)
                {
                    OnError(ex);
                    return;
                }

                var inner = new InnerSignalObserver<R>(this);

                for (; ; )
                {
                    var a = Volatile.Read(ref active);
                    if (a == DISPOSED)
                    {
                        break;
                    }

                    if (Interlocked.CompareExchange(ref active, inner, a) == a)
                    {
                        a?.Dispose();

                        o.Subscribe(inner);
                        break;
                    }
                }
            }

            public override void Dispose()
            {
                base.Dispose();

                var a = Volatile.Read(ref active);
                if (a != null && a != DISPOSED)
                {
                    Interlocked.Exchange(ref active, DISPOSED)?.Dispose();
                }
            }

            public void Drain()
            {
                if (Interlocked.Increment(ref wip) == 1)
                {
                    DrainLoop();
                }
            }

            void DrainLoop()
            {
                var missed = 1;
                var delayErrors = this.delayErrors;
                var downstream = this.downstream;


                for (; ;)
                {
                    for (; ; )
                    {
                        var a = Volatile.Read(ref active);

                        if (a == DISPOSED)
                        {
                            break;
                        }

                        if (!delayErrors)
                        {
                            var ex = Volatile.Read(ref errors);
                            if (ex != null)
                            {
                                downstream.OnError(ex);
                                Dispose();
                                break;
                            }
                        }

                        bool d = Volatile.Read(ref done);

                        if (d && a == null)
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
                            Dispose();
                            break;
                        }

                        if (a == null)
                        {
                            break;
                        }

                        var innerDone = a.IsDone();
                        var q = a.GetQueue();
                        var v = default(R);
                        var empty = true;

                        if (q != null)
                        {
                            try
                            {
                                v = q.TryPoll(out var success);
                                empty = !success;
                            }
                            catch (Exception ex)
                            {
                                if (delayErrors)
                                {
                                    ExceptionHelper.AddException(ref errors, ex);
                                }
                                else
                                {
                                    Interlocked.CompareExchange(ref errors, ex, null);
                                }
                                innerDone = true;
                            }
                        }

                        if (innerDone && empty)
                        {
                            Interlocked.CompareExchange(ref active, null, a);
                        }
                        else
                        if (empty)
                        {
                            break;
                        }
                        else
                        {
                            downstream.OnNext(v);
                        }
                    }

                    missed = Interlocked.Add(ref wip, -missed);
                    if (missed == 0)
                    {
                        break;
                    }
                }
            }
        }
    }
}
