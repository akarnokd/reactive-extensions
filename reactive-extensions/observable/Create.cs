using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Creates an observable sequence by providing an emitter
    /// API to bridge the callback world with the reactive world.
    /// </summary>
    /// <typeparam name="T">The element type of the sequence.</typeparam>
    /// <remarks>Since 0.0.5</remarks>
    internal sealed class Create<T> : IObservable<T>
    {
        readonly Action<IObservableEmitter<T>> onSubscribe;

        readonly bool serialize;

        public Create(Action<IObservableEmitter<T>> onSubscribe, bool serialize)
        {
            this.onSubscribe = onSubscribe;
            this.serialize = serialize;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (serialize)
            {
                var parent = new CreateDisposableSerializedEmitter(observer);
                try
                {
                    onSubscribe(parent);
                }
                catch (Exception ex)
                {
                    parent.OnError(ex);
                }
                return parent;
            }
            else
            {
                var parent = new CreateDisposableEmitter(observer);
                try
                {
                    onSubscribe(parent);
                }
                catch (Exception ex)
                {
                    parent.OnError(ex);
                }
                return parent;
            }
        }

        internal sealed class CreateDisposableEmitter : IObservableEmitter<T>, IDisposable
        {
            readonly IObserver<T> downstream;

            IDisposable resource;

            public CreateDisposableEmitter(IObserver<T> downstream)
            {
                this.downstream = downstream;
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref resource);
            }

            public bool IsDisposed()
            {
                return DisposableHelper.IsDisposed(ref resource);
            }

            public void OnCompleted()
            {
                var d = Interlocked.Exchange(ref resource, DisposableHelper.DISPOSED);
                if (d != DisposableHelper.DISPOSED)
                {
                    downstream.OnCompleted();
                    d?.Dispose();
                }
            }

            public void OnError(Exception error)
            {
                var d = Interlocked.Exchange(ref resource, DisposableHelper.DISPOSED);
                if (d != DisposableHelper.DISPOSED)
                {
                    downstream.OnError(error);
                    d?.Dispose();
                }
            }

            public void OnNext(T value)
            {
                var d = Volatile.Read(ref resource);
                if (d != DisposableHelper.DISPOSED)
                {
                    downstream.OnNext(value);
                }
            }

            public void SetResource(IDisposable resource)
            {
                DisposableHelper.Set(ref this.resource, resource);
            }
        }

        internal sealed class CreateDisposableSerializedEmitter : IObservableEmitter<T>, IDisposable
        {
            readonly IObserver<T> downstream;

            IDisposable resource;

            bool terminated;

            ConcurrentQueue<T> queue;

            bool done;
            Exception error;

            int wip;

            public CreateDisposableSerializedEmitter(IObserver<T> downstream)
            {
                this.downstream = downstream;
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref resource);
            }

            public bool IsDisposed()
            {
                return DisposableHelper.IsDisposed(ref resource);
            }

            public void OnCompleted()
            {
                if (Interlocked.CompareExchange(ref wip, 1, 0) == 0)
                {
                    if (!terminated)
                    {
                        terminated = true;
                        Volatile.Write(ref done, true);
                        downstream.OnCompleted();
                        Dispose();
                        if (Interlocked.Decrement(ref wip) == 0)
                        {
                            return;
                        }
                    }
                }
                else
                {
                    Volatile.Write(ref done, true);
                    if (Interlocked.Increment(ref wip) != 1)
                    {
                        return;
                    }
                }
                DrainLoop();
            }

            public void OnError(Exception error)
            {
                if (Interlocked.CompareExchange(ref wip, 1, 0) == 0)
                {
                    if (!terminated)
                    {
                        terminated = true;
                        Interlocked.Exchange(ref this.error, error);
                        Volatile.Write(ref done, true);
                        downstream.OnError(error);
                        Dispose();
                        if (Interlocked.Decrement(ref wip) == 0)
                        {
                            return;
                        }
                    }
                }
                else
                {
                    if (Interlocked.CompareExchange(ref this.error, error, null) == null)
                    {
                        Volatile.Write(ref done, true);
                        if (Interlocked.Increment(ref wip) != 1)
                        {
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                }
                DrainLoop();
            }

            public void OnNext(T value)
            {
                if (Interlocked.CompareExchange(ref wip, 1, 0) == 0)
                {
                    if (!terminated)
                    {
                        downstream.OnNext(value);
                        if (Interlocked.Decrement(ref wip) == 0)
                        {
                            return;
                        }
                    }
                }
                else
                {
                    var q = GetOrCreateQueue();
                    q.Enqueue(value);
                    if (Interlocked.Increment(ref wip) != 1)
                    {
                        return;
                    }
                }
                DrainLoop();
            }

            ConcurrentQueue<T> GetQueue()
            {
                return Volatile.Read(ref queue);
            }

            ConcurrentQueue<T> GetOrCreateQueue()
            {
                var q = GetQueue();
                if (q == null)
                {
                    q = new ConcurrentQueue<T>();
                    if (Interlocked.CompareExchange(ref queue, q, null) != null)
                    {
                        q = GetQueue();
                    }
                }
                return q;
            }

            void DrainLoop()
            {
                int missed = 1;
                for (; ;)
                {
                    if (terminated)
                    {
                        var q = GetQueue();
                        if (q != null)
                        {
                            while (q.TryDequeue(out var _)) ;
                        }
                    }
                    else
                    {
                        var continueOuter = false;
                        for (; ;)
                        {
                            if (IsDisposed())
                            {
                                terminated = true;
                                continueOuter = true;
                                break;
                            }

                            var d = Volatile.Read(ref done);
                            var q = GetQueue();
                            var v = default(T);
                            var empty = q == null || !q.TryDequeue(out v);

                            if (d && empty)
                            {
                                terminated = true;
                                var ex = Volatile.Read(ref error);
                                if (ex != null)
                                {
                                    downstream.OnError(ex);
                                }
                                else
                                {
                                    downstream.OnCompleted();
                                }
                                Dispose();
                                break;
                            }

                            if (empty)
                            {
                                break;
                            }

                            downstream.OnNext(v);
                        }

                        if (continueOuter)
                        {
                            continue;
                        }
                    }

                    missed = Interlocked.Add(ref wip, -missed);
                    if (missed == 0)
                    {
                        break;
                    }
                }
            }

            public void SetResource(IDisposable resource)
            {
                DisposableHelper.Set(ref this.resource, resource);
            }
        }
    }
}
