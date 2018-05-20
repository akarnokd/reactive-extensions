using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Switches the fallback a single source if the main source is empty.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <remarks>Since 0.0.11</remarks>
    internal sealed class MaybeSwitchIfEmpty<T> : ISingleSource<T>
    {
        readonly IMaybeSource<T> source;

        readonly ISingleSource<T> fallback;

        public MaybeSwitchIfEmpty(IMaybeSource<T> source, ISingleSource<T> fallback)
        {
            this.source = source;
            this.fallback = fallback;
        }

        public void Subscribe(ISingleObserver<T> observer)
        {
            var parent = new SwitchIfEmptyObserver(observer, fallback);
            observer.OnSubscribe(parent);

            source.Subscribe(parent);
        }

        sealed class SwitchIfEmptyObserver : IMaybeObserver<T>, ISingleObserver<T>, IDisposable
        {
            readonly ISingleObserver<T> downstream;

            ISingleSource<T> fallback;

            IDisposable upstream;

            public SwitchIfEmptyObserver(ISingleObserver<T> downstream, ISingleSource<T> fallback)
            {
                this.downstream = downstream;
                this.fallback = fallback;
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
            }

            public void OnCompleted()
            {
                var src = fallback;
                fallback = null;

                src.Subscribe(this);
            }

            public void OnError(Exception error)
            {
                downstream.OnError(error);
            }

            public void OnSubscribe(IDisposable d)
            {
                DisposableHelper.Replace(ref upstream, d);
            }

            public void OnSuccess(T item)
            {
                downstream.OnSuccess(item);
            }
        }
    }

    /// <summary>
    /// Switches to the fallbacks if the main source or
    /// the previous fallback is empty.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <remarks>Since 0.0.11</remarks>
    internal sealed class MaybeSwitchIfEmptyMany<T> : IMaybeSource<T>
    {
        readonly IMaybeSource<T> source;

        readonly IMaybeSource<T>[] fallback;

        public MaybeSwitchIfEmptyMany(IMaybeSource<T> source, IMaybeSource<T>[] fallback)
        {
            this.source = source;
            this.fallback = fallback;
        }

        public void Subscribe(IMaybeObserver<T> observer)
        {
            var parent = new SwitchIfEmptyObserver(observer, fallback);
            observer.OnSubscribe(parent);

            parent.Drain(source);
        }

        sealed class SwitchIfEmptyObserver : IMaybeObserver<T>, IDisposable
        {
            readonly IMaybeObserver<T> downstream;

            readonly IMaybeSource<T>[] fallbacks;

            IDisposable upstream;

            int index;

            int wip;

            public SwitchIfEmptyObserver(IMaybeObserver<T> downstream, IMaybeSource<T>[] fallbacks)
            {
                this.downstream = downstream;
                this.fallbacks = fallbacks;
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
            }

            public void OnCompleted()
            {
                Drain(null);
            }

            public void OnError(Exception error)
            {
                downstream.OnError(error);
            }

            public void OnSubscribe(IDisposable d)
            {
                DisposableHelper.Replace(ref upstream, d);
            }

            public void OnSuccess(T item)
            {
                downstream.OnSuccess(item);
            }

            internal void Drain(IMaybeSource<T> source)
            {
                if (Interlocked.Increment(ref wip) == 1)
                {
                    for (; ; )
                    {
                        if (!DisposableHelper.IsDisposed(ref upstream))
                        {
                            if (source != null)
                            {
                                source.Subscribe(this);
                                source = null;
                            }
                            else
                            {
                                var idx = index;

                                if (idx == fallbacks.Length)
                                {
                                    DisposableHelper.WeakDispose(ref upstream);
                                    downstream.OnCompleted();
                                }
                                else
                                {
                                    var src = fallbacks[idx];

                                    if (src == null)
                                    {
                                        DisposableHelper.WeakDispose(ref upstream);
                                        downstream.OnError(new NullReferenceException("The fallback IMaybeSource at index " + idx + " is null"));
                                    }
                                    else
                                    {
                                        index = idx + 1;
                                        src.Subscribe(this);
                                    }
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
}
