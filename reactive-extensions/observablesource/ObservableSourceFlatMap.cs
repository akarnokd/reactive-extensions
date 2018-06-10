using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceFlatMap<T, R> : IObservableSource<R>
    {
        readonly IObservableSource<T> source;

        readonly Func<T, IObservableSource<R>> mapper;

        readonly bool delayErrors;

        readonly int maxConcurrency;

        readonly int capacityHint;

        public ObservableSourceFlatMap(IObservableSource<T> source, Func<T, IObservableSource<R>> mapper, bool delayErrors, int maxConcurrency, int capacityHint)
        {
            this.source = source;
            this.mapper = mapper;
            this.delayErrors = delayErrors;
            this.maxConcurrency = maxConcurrency;
            this.capacityHint = capacityHint;
        }

        public void Subscribe(ISignalObserver<R> observer)
        {
            source.Subscribe(new FlatMapMainObserver(observer, mapper, delayErrors, maxConcurrency, capacityHint));
        }

        sealed class FlatMapMainObserver : ISignalObserver<T>, IDisposable, IInnerSignalObserverSupport<R>
        {
            readonly ISignalObserver<R> downstream;

            readonly Func<T, IObservableSource<R>> mapper;

            readonly bool delayErrors;

            readonly int maxConcurrency;

            readonly int capacityHint;

            readonly SpscLinkedArrayQueue<T> sourceQueue;

            InnerSignalObserver<R>[] observers;

            static readonly InnerSignalObserver<R>[] Empty = new InnerSignalObserver<R>[0];
            static readonly InnerSignalObserver<R>[] Terminated = new InnerSignalObserver<R>[0];

            IDisposable upstream;

            int wip;

            Exception errors;

            bool done;

            bool disposed;

            SpscLinkedArrayQueue<R> scalarQueue;

            bool noMoreSources;

            public FlatMapMainObserver(ISignalObserver<R> downstream, Func<T, IObservableSource<R>> mapper, bool delayErrors, int maxConcurrency, int capacityHint)
            {
                this.downstream = downstream;
                this.mapper = mapper;
                this.delayErrors = delayErrors;
                this.maxConcurrency = maxConcurrency;
                this.capacityHint = capacityHint;
                if (maxConcurrency == int.MaxValue)
                {
                    this.sourceQueue = null;
                }
                else
                {
                    this.sourceQueue = new SpscLinkedArrayQueue<T>(capacityHint);
                }
                Volatile.Write(ref observers, Empty);
            }

            public void Dispose()
            {
                Volatile.Write(ref disposed, true);
                upstream.Dispose();
                DisposeAll();
                Drain();
            }

            void DisposeAll()
            {
                foreach (var inner in Interlocked.Exchange(ref observers, Terminated))
                {
                    inner.Dispose();
                }
            }

            public void Drain()
            {
                if (Interlocked.Increment(ref wip) == 1)
                {
                    DrainLoop();
                }
            }

            public void InnerComplete(InnerSignalObserver<R> sender)
            {
                sender.SetDone();
                Drain();
            }

            public void InnerError(InnerSignalObserver<R> sender, Exception ex)
            {
                if (delayErrors)
                {
                    ExceptionHelper.AddException(ref errors, ex);
                }
                else
                {
                    Interlocked.CompareExchange(ref errors, ex, null);
                }
                sender.SetDone();
                Drain();
            }

            public void InnerNext(InnerSignalObserver<R> sender, R item)
            {
                if (Interlocked.CompareExchange(ref wip, 1, 0) == 0)
                {
                    downstream.OnNext(item);
                    if (Interlocked.Decrement(ref wip) == 0)
                    {
                        return;
                    }
                }
                else
                {
                    var q = sender.GetOrCreateQueue(capacityHint);
                    q.TryOffer(item);
                    if (Interlocked.Increment(ref wip) != 1)
                    {
                        return;
                    }
                }
                DrainLoop();
            }

            public void OnCompleted()
            {
                if (done)
                {
                    return;
                }
                Volatile.Write(ref done, true);
                Drain();
            }

            public void OnError(Exception ex)
            {
                if (done)
                {
                    return;
                }
                if (delayErrors)
                {
                    ExceptionHelper.AddException(ref errors, ex);
                }
                else
                {
                    Interlocked.CompareExchange(ref errors, ex, null);
                }
                Volatile.Write(ref done, true);
                Drain();
            }

            public void OnNext(T item)
            {
                if (done)
                {
                    return;
                }
                var q = sourceQueue;
                if (q == null)
                {
                    var src = default(IObservableSource<R>);

                    try
                    {
                        src = ValidationHelper.RequireNonNullRef(mapper(item), "The mapper returned a null IObservableSource");
                    }
                    catch (Exception ex)
                    {
                        upstream.Dispose();
                        OnError(ex);
                        return;
                    }

                    if (src is IDynamicValue<R> d)
                    {
                        TryEmitScalar(d);
                    }
                    else
                    {
                        var inner = new InnerSignalObserver<R>(this);
                        if (Add(inner))
                        {
                            src.Subscribe(inner);
                        }
                    }
                }
                else
                {
                    q.TryOffer(item);
                    Drain();
                }
            }

            void TryEmitScalar(IDynamicValue<R> d)
            {
                var v = default(R);
                var success = false;

                try
                {
                    v = d.GetValue(out success);
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
                    Drain();
                    return;
                }

                if (success)
                {
                    if (Interlocked.CompareExchange(ref wip, 1, 0) == 0)
                    {
                        downstream.OnNext(v);
                        if (Interlocked.Decrement(ref wip) == 0)
                        {
                            return;
                        }
                    }
                    else
                    {
                        var q = scalarQueue;
                        if (q == null)
                        {
                            q = new SpscLinkedArrayQueue<R>(capacityHint);
                            Volatile.Write(ref scalarQueue, q);
                        }
                        q.TryOffer(v);
                        if (Interlocked.Increment(ref wip) != 1)
                        {
                            return;
                        }
                    }
                    DrainLoop();
                }
            }

            public void OnSubscribe(IDisposable d)
            {
                upstream = d;
                downstream.OnSubscribe(this);
            }

            bool Add(InnerSignalObserver<R> inner)
            {
                for (; ; )
                {
                    var a = Volatile.Read(ref observers);
                    if (a == Terminated)
                    {
                        return false;
                    }
                    var n = a.Length;
                    var b = new InnerSignalObserver<R>[n + 1];
                    Array.Copy(a, 0, b, 0, n);
                    b[n] = inner;

                    if (Interlocked.CompareExchange(ref observers, b, a) == a)
                    {
                        return true;
                    }
                }
            }

            void Remove(InnerSignalObserver<R> inner)
            {
                for (; ; )
                {
                    var a = Volatile.Read(ref observers);
                    var n = a.Length;
                    if (n == 0)
                    {
                        break;
                    }

                    var j = Array.IndexOf(a, inner);
                    if (j < 0)
                    {
                        break;
                    }

                    var b = default(InnerSignalObserver<R>[]);
                    if (n == 1)
                    {
                        b = Empty;
                    }
                    else
                    {
                        b = new InnerSignalObserver<R>[n - 1];
                        Array.Copy(a, 0, b, 0, j);
                        Array.Copy(a, j + 1, b, j, n - j - 1);
                    }
                    if (Interlocked.CompareExchange(ref observers, b, a) == a)
                    {
                        break;
                    }
                }
            }

            void DrainLoop()
            {
                var missed = 1;
                var downstream = this.downstream;
                var delayErrors = this.delayErrors;
                var maxConcurrency = this.maxConcurrency;

                for (; ; )
                {
                    if (Volatile.Read(ref disposed))
                    {
                        Volatile.Read(ref scalarQueue)?.Clear();
                        sourceQueue?.Clear();
                    }
                    else
                    {
                        if (!delayErrors)
                        {
                            var ex = Volatile.Read(ref errors);
                            if (ex != null)
                            {
                                downstream.OnError(ex);

                                Volatile.Write(ref disposed, true);
                                upstream.Dispose();
                                DisposeAll();
                                continue;
                            }
                        }

                        var d = Volatile.Read(ref done);

                        var sq = Volatile.Read(ref scalarQueue);

                        if (sq != null)
                        {
                            var v = sq.TryPoll(out var success);
                            if (success)
                            {
                                downstream.OnNext(v);
                                continue;
                            }
                        }

                        var obs = Volatile.Read(ref observers);
                        var n = obs.Length;

                        var srcs = sourceQueue;
                        if (srcs != null)
                        {
                            if (!noMoreSources && n < maxConcurrency && !srcs.IsEmpty())
                            {
                                var v = srcs.TryPoll(out var success);

                                var src = default(IObservableSource<R>);

                                try
                                {
                                    src = ValidationHelper.RequireNonNullRef(mapper(v), "The mapper returned a null IObservableSource");
                                }
                                catch (Exception ex)
                                {
                                    upstream.Dispose();
                                    if (delayErrors)
                                    {
                                        ExceptionHelper.AddException(ref errors, ex);
                                    }
                                    else
                                    {
                                        Interlocked.CompareExchange(ref errors, ex, null);
                                    }
                                    success = false;
                                    noMoreSources = true;
                                    srcs?.Clear();
                                    continue;
                                }

                                if (success)
                                {
                                    if (src is IDynamicValue<R> dv)
                                    {
                                        var w = default(R);
                                        var s = false;

                                        try
                                        {
                                            w = dv.GetValue(out s);
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
                                            continue;
                                        }

                                        if (s)
                                        {
                                            downstream.OnNext(w);
                                        }
                                        continue;
                                    }
                                    else
                                    {
                                        var inner = new InnerSignalObserver<R>(this);
                                        if (Add(inner))
                                        {
                                            src.Subscribe(inner);
                                            continue;
                                        }
                                    }
                                }
                            }
                        }

                        if (d && n == 0 && (sq == null || sq.IsEmpty()) && (noMoreSources || srcs == null || srcs.IsEmpty())) 
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
                        }
                        else
                        if (n != 0)
                        {
                            var continueOuter = false;
                            var removed = false;

                            for (int j = 0; j < n; j++)
                            {
                                var inner = obs[j];

                                for (; ; )
                                { 
                                    if (Volatile.Read(ref disposed))
                                    {
                                        continueOuter = true;
                                        break;
                                    }
                                    if (!delayErrors && Volatile.Read(ref errors) != null)
                                    {
                                        continueOuter = true;
                                        break;
                                    }

                                    var innerDone = inner.IsDone();
                                    var innerQueue = inner.GetQueue();

                                    if (innerDone && (innerQueue == null || innerQueue.IsEmpty()))
                                    {
                                        Remove(inner);
                                        removed = true;
                                        break;
                                    }
                                    else
                                    if (innerQueue != null)
                                    {
                                        if (innerQueue.IsEmpty())
                                        {
                                            break;
                                        }

                                        var v = default(R);
                                        var succ = false;

                                        try
                                        {
                                            v = innerQueue.TryPoll(out succ);
                                        }
                                        catch (Exception ex)
                                        {
                                            inner.Dispose();
                                            inner.SetDone();
                                            Remove(inner);
                                            if (delayErrors)
                                            {
                                                ExceptionHelper.AddException(ref errors, ex);
                                            }
                                            else
                                            {
                                                Interlocked.CompareExchange(ref errors, ex, null);
                                            }
                                            removed = true;
                                            break;
                                        }

                                        if (succ)
                                        {
                                            downstream.OnNext(v);
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }

                                if (continueOuter)
                                {
                                    break;
                                }
                            }

                            if (continueOuter || removed)
                            {
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

        }
    }
}
