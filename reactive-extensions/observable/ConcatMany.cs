﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Concatenates a sequence of observables.
    /// </summary>
    /// <typeparam name="T">The element type of the inner sequence of observables.</typeparam>
    internal sealed class ConcatMany<T> : IObservable<T>
    {
        readonly IObservable<IObservable<T>> sources;

        internal ConcatMany(IObservable<IObservable<T>> sources)
        {
            this.sources = sources;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            var parent = new ConcatManyOuterObserver(observer);
            var d = sources.Subscribe(parent);
            parent.OnSubscribe(d);
            return parent;
        }

        internal sealed class ConcatManyOuterObserver : IObserver<IObservable<T>>, IDisposable
        {
            readonly IObserver<T> downstream;

            readonly ConcurrentQueue<IObservable<T>> queue;

            readonly InnerObserver innerObserver;

            IDisposable upstream;

            int trampoline;

            Exception error;

            bool done;

            int active;

            internal ConcatManyOuterObserver(IObserver<T> downstream)
            {
                this.downstream = downstream;
                this.queue = new ConcurrentQueue<IObservable<T>>();
                this.innerObserver = new InnerObserver(this);
            }

            internal void OnSubscribe(IDisposable d)
            {
                if (Interlocked.CompareExchange(ref upstream, d, null) != null)
                {
                    d?.Dispose();
                }
            }

            public void Dispose()
            {
                innerObserver.Dispose();
                DisposeMain();
            }

            void DisposeMain()
            {
                Interlocked.Exchange(ref upstream, DisposableHelper.DISPOSED)?.Dispose();
            }

            bool IsDisposed()
            {
                return Volatile.Read(ref upstream) == DisposableHelper.DISPOSED;
            }

            public void OnCompleted()
            {
                Volatile.Write(ref done, true);
                Drain();
            }

            public void OnError(Exception error)
            {
                if (Interlocked.CompareExchange(ref error, error, null) == null)
                {
                    Volatile.Write(ref done, true);
                    Drain();
                }
            }

            public void OnNext(IObservable<T> value)
            {
                queue.Enqueue(value);
                Drain();
            }

            void InnerNext(T item)
            {
                downstream.OnNext(item);
            }

            void InnerError(Exception error)
            {
                if (innerObserver.Finish())
                {
                    if (Interlocked.CompareExchange(ref error, error, null) == null)
                    {
                        Volatile.Write(ref done, true);
                        Volatile.Write(ref active, 0);
                        Drain();
                    }
                }
            }

            void InnerComplete()
            {
                if (innerObserver.Finish())
                {
                    Volatile.Write(ref active, 0);
                    Drain();
                }
            }

            void Drain()
            {
                if (Interlocked.Increment(ref trampoline) != 1)
                {
                    return;
                }

                do
                {
                    if (IsDisposed())
                    {
                        while (queue.TryDequeue(out var _)) ;
                    }
                    else
                    {
                        if (Volatile.Read(ref active) == 0)
                        {
                            var isDone = Volatile.Read(ref done);

                            if (isDone)
                            {
                                var ex = Volatile.Read(ref error);
                                if (ex != null)
                                {
                                    downstream.OnError(ex);
                                    DisposeMain();
                                    continue;
                                }
                            }

                            if (queue.TryDequeue(out var source))
                            {
                                var sad = new SingleAssignmentDisposable();
                                if (innerObserver.SetDisposable(sad))
                                {
                                    Interlocked.Exchange(ref active, 1);
                                    sad.Disposable = source.Subscribe(innerObserver);
                                }
                            }
                            else
                            {
                                if (isDone)
                                {
                                    downstream.OnCompleted();
                                    DisposeMain();
                                }
                            }
                        }
                    }
                } while (Interlocked.Decrement(ref trampoline) != 0);
            }

            internal sealed class InnerObserver : IObserver<T>, IDisposable
            {
                readonly ConcatManyOuterObserver parent;

                internal SingleAssignmentDisposable upstream;

                static readonly SingleAssignmentDisposable DISPOSED;

                static InnerObserver()
                {
                    DISPOSED = new SingleAssignmentDisposable();
                    DISPOSED.Dispose();
                }

                internal InnerObserver(ConcatManyOuterObserver parent)
                {
                    this.parent = parent;
                }

                internal bool SetDisposable(SingleAssignmentDisposable sad)
                {
                    return Interlocked.CompareExchange(ref upstream, sad, null) == null;
                }

                internal bool Finish()
                {
                    var sad = Volatile.Read(ref upstream);
                    if (sad != DISPOSED)
                    {
                        if (Interlocked.CompareExchange(ref upstream, null, sad) == sad)
                        {
                            sad.Dispose();
                            return true;
                        }
                    }
                    return false;
                }

                public void Dispose()
                {
                    Interlocked.Exchange(ref upstream, DISPOSED)?.Dispose();
                }

                public void OnCompleted()
                {
                    parent.InnerComplete();
                }

                public void OnError(Exception error)
                {
                    parent.InnerError(error);
                }

                public void OnNext(T value)
                {
                    parent.InnerNext(value);
                }
            }
        }
    }
}
