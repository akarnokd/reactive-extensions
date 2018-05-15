using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using static akarnokd.reactive_extensions.ValidationHelper;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Runs the completable sources one after the other
    /// and terminates when all terminate.
    /// </summary>
    /// <remarks>Since 0.0.7</remarks>
    internal sealed class CompletableConcat : ICompletableSource
    {
        readonly ICompletableSource[] sources;

        readonly bool delayErrors;

        public CompletableConcat(ICompletableSource[] sources, bool delayErrors)
        {
            this.sources = sources;
            this.delayErrors = delayErrors;
        }

        public void Subscribe(ICompletableObserver observer)
        {
            var srcs = sources;
            var n = srcs.Length;

            if (n == 0)
            {
                DisposableHelper.Complete(observer);
                return;
            }

            if (n == 1)
            {
                srcs[0].Subscribe(observer);
                return;
            }

            var parent = new ConcatDisposable(observer, srcs, delayErrors);
            observer.OnSubscribe(parent);
            parent.Drain();
        }

        internal sealed class ConcatDisposable : ConcatBaseDisposable
        {

            readonly ICompletableSource[] sources;

            int index;

            public ConcatDisposable(ICompletableObserver downstream, ICompletableSource[] sources, bool delayErrors) : base(downstream, delayErrors)
            {
                this.sources = sources;
            }

            internal override void Drain()
            {
                if (Interlocked.Increment(ref wip) == 1)
                {
                    for (; ; )
                    {
                        var srcs = sources;
                        var n = srcs.Length;
                        if (DisposableHelper.IsDisposed(ref upstream))
                        {
                            for (int i = index; i < n; i++)
                            {
                                srcs[i] = null;
                            }
                        }
                        else
                        {
                            var i = index;
                            if (i == n)
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
                            } else
                            {
                                var src = srcs[i];
                                if (src == null)
                                {
                                    DisposableHelper.WeakDispose(ref upstream);

                                    ExceptionHelper.AddException(ref errors, new NullReferenceException("The " + i + "th ICompletableSource is null"));
                                    var ex = Volatile.Read(ref errors);
                                    downstream.OnError(ex);
                                    continue;
                                }
                                else
                                {
                                    srcs[i] = null;
                                    index = i + 1;

                                    src.Subscribe(this);
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
        }
    }

    /// <summary>
    /// Runs the completable sources one after the other
    /// and terminates when all terminate.
    /// </summary>
    /// <remarks>Since 0.0.7</remarks>
    internal sealed class CompletableConcatEnumerable : ICompletableSource
    {
        readonly IEnumerable<ICompletableSource> sources;

        readonly bool delayErrors;

        public CompletableConcatEnumerable(IEnumerable<ICompletableSource> sources, bool delayErrors)
        {
            this.sources = sources;
            this.delayErrors = delayErrors;
        }

        public void Subscribe(ICompletableObserver observer)
        {
            var en = default(IEnumerator<ICompletableSource>);

            try
            {
                en = RequireNonNullRef(sources.GetEnumerator(), "The sources returned a null IEnumerator");
            }
            catch (Exception ex)
            {
                DisposableHelper.Error(observer, ex);
                return;
            }
            var parent = new ConcatDisposable(observer, en, delayErrors);
            observer.OnSubscribe(parent);
            parent.Drain();
        }

        internal sealed class ConcatDisposable : ConcatBaseDisposable
        {
            IEnumerator<ICompletableSource> sources;

            public ConcatDisposable(ICompletableObserver downstream, IEnumerator<ICompletableSource> sources, bool delayErrors) : base(downstream, delayErrors)
            {
                Volatile.Write(ref this.sources, sources);
            }

            internal override void Drain()
            {
                if (Interlocked.Increment(ref wip) == 1)
                {
                    for (; ; )
                    {
                        if (DisposableHelper.IsDisposed(ref upstream))
                        {
                            Interlocked.Exchange(ref sources, null)?.Dispose();
                        }
                        else
                        {
                            var hasNext = false;
                            var next = default(ICompletableSource);

                            try
                            {
                                if (sources.MoveNext())
                                {
                                    hasNext = true;
                                    next = RequireNonNullRef(sources.Current, "The enumerator returned a null ICompletableSource");
                                }
                            }
                            catch (Exception ex)
                            {
                                DisposableHelper.WeakDispose(ref upstream);
                                ExceptionHelper.AddException(ref errors, ex);
                                ex = Volatile.Read(ref errors);
                                downstream.OnError(ex);
                                continue;
                            }

                            if (hasNext)
                            {
                                next.Subscribe(this);
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

    internal abstract class ConcatBaseDisposable : ICompletableObserver, IDisposable
    {
        protected readonly ICompletableObserver downstream;

        protected readonly bool delayErrors;

        protected IDisposable upstream;

        protected int wip;

        protected Exception errors;

        protected ConcatBaseDisposable(ICompletableObserver downstream, bool delayErrors)
        {
            this.downstream = downstream;
            this.delayErrors = delayErrors;
        }
        public void Dispose()
        {
            DisposableHelper.Dispose(ref upstream);
            Drain();
        }

        public void OnCompleted()
        {
            Drain();
        }

        public void OnError(Exception error)
        {
            if (delayErrors)
            {
                ExceptionHelper.AddException(ref this.errors, error);
            }
            else
            {
                DisposableHelper.WeakDispose(ref upstream);
                downstream.OnError(error);
            }
            Drain();
        }

        public void OnSubscribe(IDisposable d)
        {
            DisposableHelper.Replace(ref upstream, d);
        }

        internal abstract void Drain();
    }
}
