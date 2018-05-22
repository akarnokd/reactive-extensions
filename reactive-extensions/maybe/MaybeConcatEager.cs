using System;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{

    /// <summary>
    /// Runs some or all maybe sources at once but emits
    /// their success item in order and optionally delays
    /// errors until all sources terminate.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <remarks>Since 0.0.12</remarks>
    internal sealed class MaybeConcatEager<T> : IObservable<T>
    {
        readonly IMaybeSource<T>[] sources;

        readonly int maxConcurrency;

        readonly bool delayErrors;

        public MaybeConcatEager(IMaybeSource<T>[] sources, int maxConcurrency, bool delayErrors)
        {
            this.sources = sources;
            this.maxConcurrency = maxConcurrency;
            this.delayErrors = delayErrors;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (maxConcurrency == int.MaxValue)
            {
                var parent = new MaybeConcatEagerAllCoordinator<T>(observer, delayErrors);

                foreach (var src in sources)
                {
                    if (!parent.SubscribeTo(src))
                    {
                        break;
                    }
                }

                parent.Done();

                return parent;
            }
            else
            {
                var parent = new ConcatEagerLimitedCoordinator(observer, sources, maxConcurrency, delayErrors);

                parent.Drain();

                return parent;
            }
        }

        sealed class ConcatEagerLimitedCoordinator : MaybeConcatEagerCoordinator<T>
        {
            readonly IMaybeSource<T>[] sources;

            readonly int maxConcurrency;

            int index;

            int active;

            internal ConcatEagerLimitedCoordinator(IObserver<T> downstream, IMaybeSource<T>[] sources, int maxConcurrency, bool delayErrors) : base(downstream, delayErrors)
            {
                this.sources = sources;
                this.maxConcurrency = maxConcurrency;
            }

            internal override void Drain()
            {
                if (Interlocked.Increment(ref wip) != 1)
                {
                    return;
                }

                var missed = 1;
                var observers = this.observers;
                var downstream = this.downstream;
                var delayErrors = this.delayErrors;
                var sources = this.sources;
                var n = sources.Length;

                for (; ; )
                {
                    if (Volatile.Read(ref disposed))
                    {
                        while (observers.TryDequeue(out var inner))
                        {
                            inner.Dispose();
                        }
                    }
                    else
                    {
                        if (!delayErrors)
                        {
                            var ex = Volatile.Read(ref errors);
                            if (ex != null)
                            {
                                Volatile.Write(ref disposed, true);
                                downstream.OnError(ex);
                                continue;
                            }
                        }

                        var idx = index;
                        if (active < maxConcurrency && idx < n)
                        {
                            var src = sources[idx];

                            index = idx + 1;
                            active++;

                            SubscribeTo(src);
                            continue;
                        }

                        var d = index == n;
                        var empty = !observers.TryPeek(out var inner);

                        if (d && empty)
                        {
                            Volatile.Write(ref disposed, true);
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
                        if (!empty)
                        {
                            var state = inner.GetState();

                            if (state == 1)
                            {
                                downstream.OnNext(inner.value);
                                observers.TryDequeue(out var _);
                                active--;
                                continue;
                            }
                            else
                            if (state == 2)
                            {
                                observers.TryDequeue(out var _);
                                active--;
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
