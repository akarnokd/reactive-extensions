using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Terminates when either the main or the other source terminates,
    /// disposing the other sequence.
    /// </summary>
    /// <typeparam name="T">The success type of the main source.</typeparam>
    /// <typeparam name="U">The success value of the other source.</typeparam>
    /// <remarks>Since 0.0.11</remarks>
    internal sealed class SingleTakeUntil<T, U> : ISingleSource<T>
    {
        readonly ISingleSource<T> source;

        readonly ISingleSource<U> other;

        public SingleTakeUntil(ISingleSource<T> source, ISingleSource<U> other)
        {
            this.source = source;
            this.other = other;
        }

        public void Subscribe(ISingleObserver<T> observer)
        {
            var parent = new TakeUntilObserver(observer);
            observer.OnSubscribe(parent);

            other.Subscribe(parent.other);
            source.Subscribe(parent);
        }

        sealed class TakeUntilObserver : ISingleObserver<T>, IDisposable
        {
            readonly ISingleObserver<T> downstream;

            internal readonly OtherObserver other;

            IDisposable upstream;

            int once;

            public TakeUntilObserver(ISingleObserver<T> downstream)
            {
                this.downstream = downstream;
                this.other = new OtherObserver(this);
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
                other.Dispose();
            }

            public void OnError(Exception error)
            {
                if (Interlocked.CompareExchange(ref once, 1, 0) == 0)
                {
                    other.Dispose();
                    downstream.OnError(error);
                }
            }

            public void OnSuccess(T item)
            {
                if (Interlocked.CompareExchange(ref once, 1, 0) == 0)
                {
                    other.Dispose();
                    downstream.OnSuccess(item);
                }
            }

            void OtherCompleted()
            {
                if (Interlocked.CompareExchange(ref once, 1, 0) == 0)
                {
                    DisposableHelper.Dispose(ref upstream);
                    downstream.OnError(new IndexOutOfRangeException("The source is empty"));
                }
            }

            void OtherError(Exception error)
            {
                if (Interlocked.CompareExchange(ref once, 1, 0) == 0)
                {
                    DisposableHelper.Dispose(ref upstream);
                    downstream.OnError(error);
                }
            }

            public void OnSubscribe(IDisposable d)
            {
                DisposableHelper.SetOnce(ref upstream, d);
            }

            internal sealed class OtherObserver : ISingleObserver<U>, IDisposable
            {
                readonly TakeUntilObserver parent;

                IDisposable upstream;

                public OtherObserver(TakeUntilObserver parent)
                {
                    this.parent = parent;
                }

                public void Dispose()
                {
                    DisposableHelper.Dispose(ref upstream);
                }

                public void OnError(Exception error)
                {
                    parent.OtherError(error);
                }

                public void OnSuccess(U item)
                {
                    parent.OtherCompleted();
                }

                public void OnSubscribe(IDisposable d)
                {
                    DisposableHelper.SetOnce(ref upstream, d);
                }

            }
        }
    }
    /// <summary>
    /// Terminates when either the main or the other source terminates,
    /// disposing the other sequence.
    /// </summary>
    /// <typeparam name="T">The success type of the main source.</typeparam>
    /// <typeparam name="U">The success value of the other source.</typeparam>
    /// <remarks>Since 0.0.11</remarks>
    internal sealed class SingleTakeUntilObservable<T, U> : ISingleSource<T>
    {
        readonly ISingleSource<T> source;

        readonly IObservable<U> other;

        public SingleTakeUntilObservable(ISingleSource<T> source, IObservable<U> other)
        {
            this.source = source;
            this.other = other;
        }

        public void Subscribe(ISingleObserver<T> observer)
        {
            var parent = new TakeUntilObserver(observer);
            observer.OnSubscribe(parent);

            parent.other.OnSubscribe(other.Subscribe(parent.other));
            source.Subscribe(parent);
        }

        sealed class TakeUntilObserver : ISingleObserver<T>, IDisposable
        {
            readonly ISingleObserver<T> downstream;

            internal readonly OtherObserver other;

            IDisposable upstream;

            int once;

            public TakeUntilObserver(ISingleObserver<T> downstream)
            {
                this.downstream = downstream;
                this.other = new OtherObserver(this);
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
                other.Dispose();
            }

            public void OnError(Exception error)
            {
                if (Interlocked.CompareExchange(ref once, 1, 0) == 0)
                {
                    other.Dispose();
                    downstream.OnError(error);
                }
            }

            public void OnSuccess(T item)
            {
                if (Interlocked.CompareExchange(ref once, 1, 0) == 0)
                {
                    other.Dispose();
                    downstream.OnSuccess(item);
                }
            }

            void OtherCompleted()
            {
                if (Interlocked.CompareExchange(ref once, 1, 0) == 0)
                {
                    DisposableHelper.Dispose(ref upstream);
                    downstream.OnError(new IndexOutOfRangeException("The source is empty"));
                }
            }

            void OtherError(Exception error)
            {
                if (Interlocked.CompareExchange(ref once, 1, 0) == 0)
                {
                    DisposableHelper.Dispose(ref upstream);
                    downstream.OnError(error);
                }
            }

            public void OnSubscribe(IDisposable d)
            {
                DisposableHelper.SetOnce(ref upstream, d);
            }

            internal sealed class OtherObserver : IObserver<U>, IDisposable
            {
                readonly TakeUntilObserver parent;

                IDisposable upstream;

                public OtherObserver(TakeUntilObserver parent)
                {
                    this.parent = parent;
                }

                public void Dispose()
                {
                    DisposableHelper.Dispose(ref upstream);
                }

                public void OnCompleted()
                {
                    parent.OtherCompleted();
                    Dispose();
                }

                public void OnError(Exception error)
                {
                    parent.OtherError(error);
                    Dispose();
                }

                public void OnNext(U value)
                {
                    parent.OtherCompleted();
                    Dispose();
                }

                public void OnSubscribe(IDisposable d)
                {
                    DisposableHelper.SetOnce(ref upstream, d);
                }

            }
        }
    }
}
