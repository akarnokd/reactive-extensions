using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Continues with an completable source when the main completable
    /// source completes.
    /// </summary>
    /// <remarks>Since 0.0.6</remarks>
    internal sealed class CompletableAndThen : ICompletableSource
    {
        readonly ICompletableSource source;

        readonly ICompletableSource next;

        public CompletableAndThen(ICompletableSource source, ICompletableSource next)
        {
            this.source = source;
            this.next = next;
        }

        public void Subscribe(ICompletableObserver observer)
        {
            source.Subscribe(new AndThenObserver(observer, next));
        }

        internal sealed class AndThenObserver : ICompletableObserver, IDisposable
        {
            readonly ICompletableObserver downstream;

            ICompletableSource next;

            IDisposable upstream;

            public AndThenObserver(ICompletableObserver downstream, ICompletableSource next)
            {
                this.downstream = downstream;
                Volatile.Write(ref this.next, next);
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
            }

            public void OnCompleted()
            {
                if (next == null)
                {
                    downstream.OnCompleted();
                    Dispose();
                }
                else
                {
                    if (DisposableHelper.Replace(ref upstream, null))
                    {
                        var src = next;
                        next = null;

                        src.Subscribe(this);
                    }
                }
            }

            public void OnError(Exception error)
            {
                next = null;
                downstream.OnError(error);
            }

            public void OnSubscribe(IDisposable d)
            {
                if (DisposableHelper.SetOnce(ref upstream, d))
                {
                    if (next != null)
                    {
                        downstream.OnSubscribe(this);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Continues with an observable sequence when the main completable
    /// source completes.
    /// </summary>
    /// <typeparam name="T">The element type of the next observable sequence.</typeparam>
    /// <remarks>Since 0.0.6</remarks>
    internal sealed class CompletableAndThenObservable<T> : IObservable<T>
    {
        readonly ICompletableSource source;

        readonly IObservable<T> next;

        public CompletableAndThenObservable(ICompletableSource source, IObservable<T> next)
        {
            this.source = source;
            this.next = next;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            var parent = new AndThenObserver(observer, next);
            source.Subscribe(parent);
            return parent;
        }

        internal sealed class AndThenObserver : ICompletableObserver, IObserver<T>, IDisposable
        {
            readonly IObserver<T> downstream;

            IObservable<T> next;

            IDisposable upstream;

            public AndThenObserver(IObserver<T> downstream, IObservable<T> next)
            {
                this.downstream = downstream;
                Volatile.Write(ref this.next, next);
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
            }

            public void OnCompleted()
            {
                if (next == null)
                {
                    downstream.OnCompleted();
                    Dispose();
                }
                else
                {
                    if (DisposableHelper.Replace(ref upstream, null))
                    {
                        var src = next;
                        next = null;

                        DisposableHelper.Replace(ref upstream, src.Subscribe(this));
                    }
                }
            }

            public void OnError(Exception error)
            {
                downstream.OnError(error);
                if (next != null)
                {
                    Dispose();
                } else
                {
                    next = null;
                }
            }

            public void OnNext(T value)
            {
                downstream.OnNext(value);
            }

            public void OnSubscribe(IDisposable d)
            {
                DisposableHelper.SetOnce(ref upstream, d);
            }
        }
    }

    /// <summary>
    /// Continues with an maybe source when the main completable
    /// source completes.
    /// </summary>
    /// <typeparam name="T">The element type of the next maybe source.</typeparam>
    /// <remarks>Since 0.0.9</remarks>
    internal sealed class CompletableAndThenMaybe<T> : IMaybeSource<T>
    {
        readonly ICompletableSource source;

        readonly IMaybeSource<T> next;

        public CompletableAndThenMaybe(ICompletableSource source, IMaybeSource<T> next)
        {
            this.source = source;
            this.next = next;
        }

        public void Subscribe(IMaybeObserver<T> observer)
        {
            source.Subscribe(new AndThenObserver(observer, next));
        }

        internal sealed class AndThenObserver : ICompletableObserver, IMaybeObserver<T>, IDisposable
        {
            readonly IMaybeObserver<T> downstream;

            IMaybeSource<T> next;

            IDisposable upstream;

            public AndThenObserver(IMaybeObserver<T> downstream, IMaybeSource<T> next)
            {
                this.downstream = downstream;
                Volatile.Write(ref this.next, next);
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
            }

            public void OnCompleted()
            {
                if (next == null)
                {
                    downstream.OnCompleted();
                    Dispose();
                }
                else
                {
                    if (DisposableHelper.Replace(ref upstream, null))
                    {
                        var src = next;
                        next = null;

                        src.Subscribe(this);
                    }
                }
            }

            public void OnError(Exception error)
            {
                next = null;
                downstream.OnError(error);
            }

            public void OnSubscribe(IDisposable d)
            {
                if (DisposableHelper.SetOnce(ref upstream, d))
                {
                    if (next != null)
                    {
                        downstream.OnSubscribe(this);
                    }
                }
            }

            public void OnSuccess(T item)
            {
                downstream.OnSuccess(item);
            }
        }
    }

    /// <summary>
    /// Continues with an single source when the main completable
    /// source completes.
    /// </summary>
    /// <typeparam name="T">The element type of the next maybe source.</typeparam>
    /// <remarks>Since 0.0.9</remarks>
    internal sealed class CompletableAndThenSingle<T> : ISingleSource<T>
    {
        readonly ICompletableSource source;

        readonly ISingleSource<T> next;

        public CompletableAndThenSingle(ICompletableSource source, ISingleSource<T> next)
        {
            this.source = source;
            this.next = next;
        }

        public void Subscribe(ISingleObserver<T> observer)
        {
            source.Subscribe(new AndThenObserver(observer, next));
        }

        internal sealed class AndThenObserver : ICompletableObserver, ISingleObserver<T>, IDisposable
        {
            readonly ISingleObserver<T> downstream;

            ISingleSource<T> next;

            IDisposable upstream;

            public AndThenObserver(ISingleObserver<T> downstream, ISingleSource<T> next)
            {
                this.downstream = downstream;
                Volatile.Write(ref this.next, next);
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
            }

            public void OnCompleted()
            {
                if (DisposableHelper.Replace(ref upstream, null))
                {
                    var src = next;
                    next = null;

                    src.Subscribe(this);
                }
            }

            public void OnError(Exception error)
            {
                next = null;
                downstream.OnError(error);
            }

            public void OnSubscribe(IDisposable d)
            {
                if (DisposableHelper.SetOnce(ref upstream, d))
                {
                    if (next != null)
                    {
                        downstream.OnSubscribe(this);
                    }
                }
            }

            public void OnSuccess(T item)
            {
                downstream.OnSuccess(item);
            }
        }
    }
}
