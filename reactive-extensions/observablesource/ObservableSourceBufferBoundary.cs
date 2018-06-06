using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceBufferBoundary<T, B, U> : IObservableSource<B> where B : ICollection<T>
    {
        readonly IObservableSource<T> source;

        readonly IObservableSource<U> boundary;

        readonly Func<B> bufferSupplier;

        public ObservableSourceBufferBoundary(IObservableSource<T> source, IObservableSource<U> boundary, Func<B> bufferSupplier)
        {
            this.source = source;
            this.boundary = boundary;
            this.bufferSupplier = bufferSupplier;
        }

        public void Subscribe(ISignalObserver<B> observer)
        {
            var parent = new BufferBoundaryObserver(observer, bufferSupplier);
            observer.OnSubscribe(parent);

            boundary.Subscribe(parent.boundary);
            source.Subscribe(parent);
        }

        sealed class BufferBoundaryObserver : ISignalObserver<T>, IDisposable
        {
            readonly ISignalObserver<B> downstream;

            readonly Func<B> bufferSupplier;

            internal readonly BoundaryObserver boundary;

            readonly ConcurrentQueue<(T item, int state)> queue;

            IDisposable upstream;

            B buffer;
            bool hasBuffer;

            Exception error;

            bool disposed;

            int wip;

            static readonly int StateItem = 0;
            static readonly int StateDone = 1;
            static readonly int StateBoundary = 2;

            public BufferBoundaryObserver(ISignalObserver<B> downstream, Func<B> bufferSupplier)
            {
                this.downstream = downstream;
                this.bufferSupplier = bufferSupplier;
                this.boundary = new BoundaryObserver(this);
                this.queue = new ConcurrentQueue<(T item, int state)>();
            }

            public void Dispose()
            {
                Volatile.Write(ref disposed, true);
                DisposableHelper.Dispose(ref upstream);
                boundary.Dispose();
                Drain();
            }

            public void OnCompleted()
            {
                if (Interlocked.CompareExchange(ref error, ExceptionHelper.TERMINATED, null) == null)
                {
                    boundary.Dispose();
                    queue.Enqueue((default, StateDone));
                    Drain();
                }
            }

            public void OnError(Exception ex)
            {
                if (Interlocked.CompareExchange(ref error, ex, null) == null)
                {
                    boundary.Dispose();
                    queue.Enqueue((default, StateDone));
                    Drain();
                }
            }

            public void OnNext(T item)
            {
                queue.Enqueue((item, StateItem));
                Drain();
            }

            public void OnSubscribe(IDisposable d)
            {
                DisposableHelper.SetOnce(ref upstream, d);
            }

            void InnerError(Exception ex)
            {
                if (Interlocked.CompareExchange(ref error, ex, null) == null)
                {
                    DisposableHelper.Dispose(ref upstream);
                    queue.Enqueue((default, StateDone));
                    Drain();
                }
            }

            void InnerNext()
            {
                queue.Enqueue((default, StateBoundary));
                Drain();
            }

            void InnerCompleted()
            {
                if (Interlocked.CompareExchange(ref error, ExceptionHelper.TERMINATED, null) == null)
                {
                    DisposableHelper.Dispose(ref upstream);
                    queue.Enqueue((default, StateDone));
                    Drain();
                }
            }

            void Drain()
            {
                if (Interlocked.Increment(ref wip) != 1)
                {
                    return;
                }

                var missed = 1;
                var downstream = this.downstream;
                var queue = this.queue;

                for (; ; )
                {
                    if (Volatile.Read(ref disposed))
                    {
                        buffer = default;
                        while (queue.TryDequeue(out var _)) ;
                    }
                    else
                    {
                        var ex = Volatile.Read(ref error);
                        if (ex != null && ex != ExceptionHelper.TERMINATED)
                        {
                            downstream.OnError(ex);
                            Volatile.Write(ref disposed, true);
                            continue;
                        }

                        var success = queue.TryDequeue(out var command);

                        if (success)
                        {
                            if (command.state == StateDone)
                            {
                                var b = buffer;
                                if (hasBuffer)
                                {
                                    downstream.OnNext(b);
                                }
                                downstream.OnCompleted();
                                Volatile.Write(ref disposed, true);
                            }
                            else
                            if (command.state == StateBoundary)
                            {
                                var b = buffer;
                                if (hasBuffer)
                                {
                                    downstream.OnNext(b);
                                }
                                // emit an empty buffer here
                                buffer = default(B);
                                hasBuffer = false;
                            }
                            else
                            {
                                var b = buffer;
                                if (!hasBuffer)
                                {
                                    try
                                    {
                                        b = bufferSupplier();
                                    }
                                    catch (Exception exc)
                                    {
                                        Interlocked.CompareExchange(ref error, exc, null);
                                        continue;
                                    }
                                    hasBuffer = true;
                                    buffer = b;
                                }

                                b.Add(command.item);
                            }
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

            internal sealed class BoundaryObserver : ISignalObserver<U>, IDisposable
            {
                readonly BufferBoundaryObserver parent;

                IDisposable upstream;

                public BoundaryObserver(BufferBoundaryObserver parent)
                {
                    this.parent = parent;
                }

                public void Dispose()
                {
                    DisposableHelper.Dispose(ref upstream);
                }

                public void OnCompleted()
                {
                    parent.InnerCompleted();
                }

                public void OnError(Exception ex)
                {
                    parent.InnerError(ex);
                }

                public void OnNext(U item)
                {
                    parent.InnerNext();
                }

                public void OnSubscribe(IDisposable d)
                {
                    DisposableHelper.SetOnce(ref upstream, d);
                }
            }
        }
    }
}
