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
    /// <remarks>Since 0.0.10</remarks>
    internal sealed class CompletableTakeUntil : ICompletableSource
    {
        readonly ICompletableSource source;

        readonly ICompletableSource other;

        public CompletableTakeUntil(ICompletableSource source, ICompletableSource other)
        {
            this.source = source;
            this.other = other;
        }

        public void Subscribe(ICompletableObserver observer)
        {
            var parent = new TakeUntilObserver(observer);
            observer.OnSubscribe(parent);

            other.Subscribe(parent.other);
            source.Subscribe(parent);
        }

        sealed class TakeUntilObserver : ICompletableObserver, IDisposable
        {
            readonly ICompletableObserver downstream;

            internal readonly OtherObserver other;

            IDisposable upstream;

            int once;

            public TakeUntilObserver(ICompletableObserver downstream)
            {
                this.downstream = downstream;
                this.other = new OtherObserver(this);
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
                other.Dispose();
            }

            public void OnCompleted()
            {
                if (Interlocked.CompareExchange(ref once, 1, 0) == 0)
                {
                    other.Dispose();
                    downstream.OnCompleted();
                }
            }

            public void OnError(Exception error)
            {
                if (Interlocked.CompareExchange(ref once, 1, 0) == 0)
                {
                    other.Dispose();
                    downstream.OnError(error);
                }
            }

            void OtherCompleted()
            {
                if (Interlocked.CompareExchange(ref once, 1, 0) == 0)
                {
                    DisposableHelper.Dispose(ref upstream);
                    downstream.OnCompleted();
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

            internal sealed class OtherObserver : ICompletableObserver, IDisposable
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
                }

                public void OnError(Exception error)
                {
                    parent.OtherError(error);
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
    /// <remarks>Since 0.0.10</remarks>
    internal sealed class CompletableTakeUntilObservable<U> : ICompletableSource
    {
        readonly ICompletableSource source;

        readonly IObservable<U> other;

        public CompletableTakeUntilObservable(ICompletableSource source, IObservable<U> other)
        {
            this.source = source;
            this.other = other;
        }

        public void Subscribe(ICompletableObserver observer)
        {
            var parent = new TakeUntilObserver(observer);
            observer.OnSubscribe(parent);

            parent.other.OnSubscribe(other.Subscribe(parent.other));
            source.Subscribe(parent);
        }

        sealed class TakeUntilObserver : ICompletableObserver, IDisposable
        {
            readonly ICompletableObserver downstream;

            internal readonly OtherObserver other;

            IDisposable upstream;

            int once;

            public TakeUntilObserver(ICompletableObserver downstream)
            {
                this.downstream = downstream;
                this.other = new OtherObserver(this);
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
                other.Dispose();
            }

            public void OnCompleted()
            {
                if (Interlocked.CompareExchange(ref once, 1, 0) == 0)
                {
                    other.Dispose();
                    downstream.OnCompleted();
                }
            }

            public void OnError(Exception error)
            {
                if (Interlocked.CompareExchange(ref once, 1, 0) == 0)
                {
                    other.Dispose();
                    downstream.OnError(error);
                }
            }

            void OtherCompleted()
            {
                if (Interlocked.CompareExchange(ref once, 1, 0) == 0)
                {
                    DisposableHelper.Dispose(ref upstream);
                    downstream.OnCompleted();
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
                }

                public void OnError(Exception error)
                {
                    parent.OtherError(error);
                }

                public void OnNext(U value)
                {
                    Dispose();
                    parent.OtherCompleted();
                }

                public void OnSubscribe(IDisposable d)
                {
                    DisposableHelper.SetOnce(ref upstream, d);
                }

            }
        }
    }
}
