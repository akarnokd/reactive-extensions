using System;
using System.Collections.Generic;
using System.Threading;
using static akarnokd.reactive_extensions.ValidationHelper;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Runs some or all single sources, produced by
    /// an enumerable sequence, at once but emits
    /// their success item in order and optionally delays
    /// errors until all sources terminate.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <remarks>Since 0.0.12</remarks>
    internal sealed class SingleConcatEagerEnumerable<T> : IObservable<T>
    {
        readonly IEnumerable<ISingleSource<T>> sources;

        readonly int maxConcurrency;

        readonly bool delayErrors;

        public SingleConcatEagerEnumerable(IEnumerable<ISingleSource<T>> sources, int maxConcurrency, bool delayErrors)
        {
            this.sources = sources;
            this.maxConcurrency = maxConcurrency;
            this.delayErrors = delayErrors;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            var en = default(IEnumerator<ISingleSource<T>>);

            try
            {
                en = RequireNonNullRef(sources.GetEnumerator(), "The GetEnumerator returned a null IEnumerator");
            }
            catch (Exception ex)
            {
                observer.OnError(ex);
                return DisposableHelper.EMPTY;
            }

            if (maxConcurrency == int.MaxValue)
            {
                var parent = new SingleConcatEagerAllCoordinator<T>(observer, delayErrors);

                for (; ; )
                {
                    var b = false;
                    var src = default(ISingleSource<T>);
                    try
                    {
                        b = en.MoveNext();
                        if (!b)
                        {
                            break;
                        }
                        src = en.Current;
                    }
                    catch (Exception ex)
                    {
                        parent.SubscribeTo(SingleSource.Error<T>(ex));
                        break;
                    }

                    if (!parent.SubscribeTo(src))
                    {
                        break;
                    }
                }

                en.Dispose();

                parent.Done();

                return parent;

            }
            else
            {
                var parent = new ConcatEagerLimitedCoordinator(observer, en, maxConcurrency, delayErrors);

                parent.Drain();

                return parent;
            }
        }

        sealed class ConcatEagerLimitedCoordinator : SingleConcatEagerCoordinator<T>
        {
            IEnumerator<ISingleSource<T>> sources;

            readonly int maxConcurrency;

            int active;

            bool done;

            internal ConcatEagerLimitedCoordinator(IObserver<T> downstream, IEnumerator<ISingleSource<T>> sources, int maxConcurrency, bool delayErrors) : base(downstream, delayErrors)
            {
                this.sources = sources;
                this.maxConcurrency = maxConcurrency;
            }

            public override void Dispose()
            {
                base.Dispose();
                Drain();
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

                for (; ; )
                {
                    if (Volatile.Read(ref disposed))
                    {
                        while (observers.TryDequeue(out var inner))
                        {
                            inner.Dispose();
                        }
                        if (sources != null)
                        {
                            this.sources = null;
                            sources.Dispose();
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

                        if (active < maxConcurrency && !done)
                        {

                            var src = default(ISingleSource<T>);

                            try
                            {
                                if (sources.MoveNext())
                                {
                                    src = sources.Current;
                                }
                                else
                                {
                                    done = true;
                                }
                            }
                            catch (Exception ex)
                            {
                                done = true;
                                sources.Dispose();
                                this.sources = null;

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

                            if (!done)
                            {
                                active++;

                                SubscribeTo(src);
                                continue;
                            }
                            else
                            {
                                sources.Dispose();
                                this.sources = null;
                            }
                        }

                        var d = done;
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
