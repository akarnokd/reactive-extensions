using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceWindowBoundary<T, B> : IObservableSource<IObservableSource<T>>
    {
        readonly IObservableSource<T> source;

        readonly IObservableSource<B> boundary;

        public ObservableSourceWindowBoundary(IObservableSource<T> source, IObservableSource<B> boundary)
        {
            this.source = source;
            this.boundary = boundary;
        }

        public void Subscribe(ISignalObserver<IObservableSource<T>> observer)
        {
            var parent = new WindowBoundaryMainObserver(observer);
            observer.OnSubscribe(parent);

            boundary.Subscribe(parent.boundary);
            source.Subscribe(parent);
        }

        sealed class WindowBoundaryMainObserver : ISignalObserver<T>, IDisposable
        {
            readonly ISignalObserver<IObservableSource<T>> downstream;

            internal readonly BoundaryObserver boundary;

            readonly ConcurrentQueue<(T item, bool boundary, bool done)> queue;

            readonly Action onTerminate;

            IDisposable upstream;

            bool disposed;

            int wip;

            Exception error;

            int active;

            int once;

            MonocastSubject<T> window;

            public WindowBoundaryMainObserver(ISignalObserver<IObservableSource<T>> downstream)
            {
                this.downstream = downstream;
                this.queue = new ConcurrentQueue<(T item, bool boundary, bool done)>();
                this.boundary = new BoundaryObserver(this);
                this.onTerminate = TerminateWindow;
                Volatile.Write(ref active, 1);
            }

            public void Dispose()
            {
                if (Interlocked.CompareExchange(ref once, 1, 0) == 0)
                {
                    if (Interlocked.Decrement(ref active) == 0)
                    {
                        DisposeAll();
                    }
                }
            }

            void TerminateWindow()
            {
                if (Interlocked.Decrement(ref active) == 0)
                {
                    DisposeAll();
                }
            }

            void DisposeAll()
            {
                Volatile.Write(ref disposed, true);
                DisposableHelper.Dispose(ref upstream);
                boundary.Dispose();
                Drain();
            }

            public void OnCompleted()
            {
                boundary.Dispose();
                queue.Enqueue((default, false, true));
                Drain();
            }

            public void OnError(Exception ex)
            {
                boundary.Dispose();
                Interlocked.CompareExchange(ref error, ex, null);
                Drain();
            }

            public void OnNext(T item)
            {
                queue.Enqueue((item, false, false));
                Drain();
            }

            public void OnSubscribe(IDisposable d)
            {
                DisposableHelper.SetOnce(ref upstream, d);
            }

            void BoundaryNext()
            {
                queue.Enqueue((default, true, false));
                Drain();
            }

            void BoundaryError(Exception ex)
            {
                DisposableHelper.Dispose(ref upstream);
                Interlocked.CompareExchange(ref error, ex, null);
                Drain();
            }

            void BoundaryCompleted()
            {
                DisposableHelper.Dispose(ref upstream);
                queue.Enqueue((default, false, true));
                Drain();
            }

            void Drain()
            {
                if (Interlocked.Increment(ref wip) != 1)
                {
                    return;
                }

                var missed = 1;

                for (; ; )
                {
                    if (Volatile.Read(ref disposed))
                    {
                        while (queue.TryDequeue(out var _)) ;
                        window = null;
                    }
                    else
                    {
                        var ex = Volatile.Read(ref error);
                        if (ex != null)
                        {
                            downstream.OnError(ex);
                            Volatile.Write(ref disposed, true);
                            continue;
                        }

                        if (queue.TryDequeue(out var entry))
                        {
                            var w = window;
                            if (entry.done)
                            {
                                w?.OnCompleted();
                                downstream.OnCompleted();
                                Volatile.Write(ref disposed, true);
                            }
                            else
                            if (entry.boundary)
                            {
                                w?.OnCompleted();
                                window = null;
                            }
                            else
                            {
                                if (w == null && Volatile.Read(ref once) == 0)
                                {
                                    w = new MonocastSubject<T>(onTerminate: onTerminate);
                                    window = w;
                                    Interlocked.Increment(ref active);
                                    downstream.OnNext(w);
                                }

                                w?.OnNext(entry.item);
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

            internal sealed class BoundaryObserver : ISignalObserver<B>, IDisposable
            {
                readonly WindowBoundaryMainObserver parent;

                IDisposable upstream;

                public BoundaryObserver(WindowBoundaryMainObserver parent)
                {
                    this.parent = parent;
                }

                public void Dispose()
                {
                    DisposableHelper.Dispose(ref upstream);
                }

                public void OnCompleted()
                {
                    parent.BoundaryCompleted();
                }

                public void OnError(Exception ex)
                {
                    parent.BoundaryError(ex);
                }

                public void OnNext(B item)
                {
                    parent.BoundaryNext();
                }

                public void OnSubscribe(IDisposable d)
                {
                    DisposableHelper.SetOnce(ref upstream, d);
                }
            }
        }
    }
}
