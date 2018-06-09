using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Base observer for consuming an observable in a blocking fashion
    /// from the thread which calls <see cref="Run"/>.
    /// </summary>
    /// <typeparam name="T">The element type of the sequence.</typeparam>
    /// <remarks>Since 0.0.22</remarks>
    internal abstract class BaseBlockingSignalObserver<T> : ISignalObserver<T>, IDisposable
    {
        readonly ConcurrentQueue<T> queue = new ConcurrentQueue<T>();

        bool done;
        Exception error;

        int wip;

        protected IDisposable upstream;

        public void Dispose()
        {
            DisposableHelper.Dispose(ref upstream);
            Signal();
        }

        public abstract void OnSubscribe(IDisposable d);

        public void OnCompleted()
        {
            Volatile.Write(ref done, true);
            Signal();
        }

        public void OnError(Exception error)
        {
            this.error = error;
            Volatile.Write(ref done, true);
            Signal();
        }

        public void OnNext(T value)
        {
            queue.Enqueue(value);
            Signal();
        }

        void Signal()
        {
            if (Interlocked.Increment(ref wip) == 1)
            {
                lock (this)
                {
                    Monitor.PulseAll(this);
                }
            }
        }

        /// <summary>
        /// Process the next item.
        /// </summary>
        /// <param name="item">The item to process.</param>
        /// <returns>True if the flow should continue</returns>
        protected abstract bool Next(T item);

        protected abstract void Error(Exception ex);

        protected abstract void Completed();

        internal void Run()
        {
            var q = queue;
            for (; ;)
            {
                if (DisposableHelper.IsDisposed(ref upstream))
                {
                    while (q.TryDequeue(out var _)) ;
                    return;
                }

                var d = Volatile.Read(ref done);
                var empty = !q.TryDequeue(out var v);

                if (d && empty)
                {
                    var ex = error;
                    try
                    {
                        if (ex != null)
                        {
                            Error(ex);
                        }
                        else
                        {
                            Completed();
                        }
                    }
                    finally
                    {
                        Dispose();
                    }
                    return;
                }

                if (!empty)
                {
                    Interlocked.Decrement(ref wip);
                    var b = false;

                    try
                    {
                        b = Next(v);
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            Error(ex);
                        }
                        finally
                        {
                            Dispose();
                        }
                    }

                    if (b)
                    {
                        continue;
                    }
                    else
                    {
                        try
                        {
                            Completed();
                        }
                        finally
                        {
                            Dispose();
                        }
                    }
                }

                if (Volatile.Read(ref wip) == 0)
                {
                    lock (this)
                    {
                        while (Volatile.Read(ref wip) == 0)
                        {
                            Monitor.Wait(this);
                        }
                    }
                }
            }
        }
    }

    internal abstract class BaseBlockingSubscribeSignalAction<T> : BaseBlockingSignalObserver<T>
    {
        readonly Action<Exception> onError;

        readonly Action onCompleted;

        static readonly Action<Exception> ERROR_IGNORE = e => { };

        static readonly Action COMPLETED_IGNORE = () => { };

        public BaseBlockingSubscribeSignalAction(Action<Exception> onError, Action onCompleted)
        {
            this.onError = onError ?? ERROR_IGNORE;
            this.onCompleted = onCompleted ?? COMPLETED_IGNORE;
        }

        public override void OnSubscribe(IDisposable d)
        {
            DisposableHelper.SetOnce(ref upstream, d);
        }

        protected override void Completed()
        {
            onCompleted.Invoke();
        }

        protected override void Error(Exception ex)
        {
            onError.Invoke(ex);
        }
    }

    internal sealed class BlockingSubscribeSignalAction<T> : BaseBlockingSubscribeSignalAction<T>
    {
        readonly Action<T> onNext;

        public BlockingSubscribeSignalAction(Action<T> onNext, Action<Exception> onError, Action onCompleted) : base(onError, onCompleted)
        {
            this.onNext = onNext;
        }

        protected override bool Next(T item)
        {
            onNext.Invoke(item);
            return true;
        }
    }

    internal sealed class BlockingSubscribeSignalPredicate<T> : BaseBlockingSubscribeSignalAction<T>
    {
        readonly Func<T, bool> onNext;

        public BlockingSubscribeSignalPredicate(Func<T, bool> onNext, Action<Exception> onError, Action onCompleted) : base(onError, onCompleted)
        {
            this.onNext = onNext;
        }

        protected override bool Next(T item)
        {
            return onNext.Invoke(item);
        }
    }

    internal sealed class BlockingSubscribeSignalObserver<T> : BaseBlockingSignalObserver<T>
    {
        readonly ISignalObserver<T> observer;

        public BlockingSubscribeSignalObserver(ISignalObserver<T> observer)
        {
            this.observer = observer;
        }

        protected override void Completed()
        {
            observer.OnCompleted();
        }

        protected override void Error(Exception ex)
        {
            observer.OnError(ex);
        }

        protected override bool Next(T item)
        {
            observer.OnNext(item);
            return true;
        }

        public override void OnSubscribe(IDisposable d)
        {
            if (DisposableHelper.SetOnce(ref upstream, d))
            {
                observer.OnSubscribe(this);
            }
        }
    }

}
