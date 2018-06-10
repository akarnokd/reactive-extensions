using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceWindow<T> : IObservableSource<IObservableSource<T>>
    {
        readonly IObservableSource<T> source;

        readonly int size;

        readonly int skip;

        public ObservableSourceWindow(IObservableSource<T> source, int size, int skip)
        {
            this.source = source;
            this.size = size;
            this.skip = skip;
        }

        public void Subscribe(ISignalObserver<IObservableSource<T>> observer)
        {
            if (size == skip)
            {
                source.Subscribe(new WindowExactObserver(observer, size));
            }
            else
            if (skip > size)
            {
                source.Subscribe(new WindowSkipObserver(observer, size, skip));
            }
            else
            {
                source.Subscribe(new WindowOverlapObserver(observer, size, skip));
            }
        }

        sealed class WindowExactObserver : ISignalObserver<T>, IDisposable
        {
            readonly ISignalObserver<IObservableSource<T>> downstream;

            readonly int size;

            readonly Action onTerminate;

            IDisposable upstream;

            int once;

            int active;

            MonocastSubject<T> window;

            int index;

            public WindowExactObserver(ISignalObserver<IObservableSource<T>> downstream, int size)
            {
                this.downstream = downstream;
                this.size = size;
                this.onTerminate = TerminateWindow;
                Volatile.Write(ref active, 1);
            }

            public void Dispose()
            {
                if (Interlocked.CompareExchange(ref once, 1, 0) == 0)
                {
                    if (Interlocked.Decrement(ref active) == 0)
                    {
                        upstream.Dispose();
                    }
                }
            }

            void TerminateWindow()
            {
                if (Interlocked.Decrement(ref active) == 0)
                {
                    upstream.Dispose();
                }
            }

            public void OnCompleted()
            {
                window?.OnCompleted();
                window = null;
                downstream.OnCompleted();
            }

            public void OnError(Exception ex)
            {
                window?.OnError(ex);
                window = null;
                downstream.OnError(ex);
            }

            public void OnNext(T item)
            {
                var idx = index;
                var w = window;
                if (index == 0 && Volatile.Read(ref once) == 0)
                {
                    w = new MonocastSubject<T>(size, onTerminate);
                    window = w;
                    Interlocked.Increment(ref active);
                    downstream.OnNext(w);
                }

                w?.OnNext(item);

                if (++idx == size)
                {
                    w?.OnCompleted();
                    window = null;
                    index = 0;
                }
                else
                {
                    index = idx;
                }
            }

            public void OnSubscribe(IDisposable d)
            {
                upstream = d;
                downstream.OnSubscribe(this);
            }
        }

        sealed class WindowSkipObserver : ISignalObserver<T>, IDisposable
        {
            readonly ISignalObserver<IObservableSource<T>> downstream;

            readonly int skip;

            readonly int size;

            readonly Action onTerminate;

            IDisposable upstream;

            int once;

            int active;

            MonocastSubject<T> window;

            int index;

            public WindowSkipObserver(ISignalObserver<IObservableSource<T>> downstream, int size, int skip)
            {
                this.downstream = downstream;
                this.size = size;
                this.skip = skip;
                this.onTerminate = TerminateWindow;
                Volatile.Write(ref active, 1);
            }

            public void Dispose()
            {
                if (Interlocked.CompareExchange(ref once, 1, 0) == 0)
                {
                    if (Interlocked.Decrement(ref active) == 0)
                    {
                        upstream.Dispose();
                    }
                }
            }

            void TerminateWindow()
            {
                if (Interlocked.Decrement(ref active) == 0)
                {
                    upstream.Dispose();
                }
            }

            public void OnCompleted()
            {
                window?.OnCompleted();
                window = null;
                downstream.OnCompleted();
            }

            public void OnError(Exception ex)
            {
                window?.OnError(ex);
                window = null;
                downstream.OnError(ex);
            }

            public void OnNext(T item)
            {
                var idx = index;
                var w = window;
                if (idx == 0 && Volatile.Read(ref once) == 0)
                {
                    w = new MonocastSubject<T>(size, onTerminate);
                    window = w;
                    Interlocked.Increment(ref active);
                    downstream.OnNext(w);
                }

                w?.OnNext(item);

                if (++idx == size)
                {
                    w?.OnCompleted();
                    window = null;
                }

                if (idx == skip)
                {
                    index = 0;
                }
                else
                {
                    index = idx;
                }
            }

            public void OnSubscribe(IDisposable d)
            {
                upstream = d;
                downstream.OnSubscribe(this);
            }
        }

        sealed class WindowOverlapObserver : ISignalObserver<T>, IDisposable
        {
            readonly ISignalObserver<IObservableSource<T>> downstream;

            readonly int skip;

            readonly int size;

            readonly Action onTerminate;

            readonly Queue<MonocastSubject<T>> windows;

            IDisposable upstream;

            int once;

            int active;

            int index;

            int count;

            public WindowOverlapObserver(ISignalObserver<IObservableSource<T>> downstream, int size, int skip)
            {
                this.downstream = downstream;
                this.size = size;
                this.skip = skip;
                this.onTerminate = TerminateWindow;
                this.windows = new Queue<MonocastSubject<T>>();
                Volatile.Write(ref active, 1);
            }

            public void Dispose()
            {
                if (Interlocked.CompareExchange(ref once, 1, 0) == 0)
                {
                    if (Interlocked.Decrement(ref active) == 0)
                    {
                        upstream.Dispose();
                    }
                }
            }

            void TerminateWindow()
            {
                if (Interlocked.Decrement(ref active) == 0)
                {
                    upstream.Dispose();
                }
            }

            public void OnCompleted()
            {
                while (windows.Count != 0)
                {
                    windows.Dequeue().OnCompleted();
                }
                downstream.OnCompleted();
            }

            public void OnError(Exception ex)
            {
                while (windows.Count != 0)
                {
                    windows.Dequeue().OnError(ex);
                }
                downstream.OnError(ex);
            }

            public void OnNext(T item)
            {
                var idx = index;

                if (idx == 0 && Volatile.Read(ref once) == 0)
                {
                    var w = new MonocastSubject<T>(size, onTerminate);
                    windows.Enqueue(w);
                    Interlocked.Increment(ref active);
                    downstream.OnNext(w);
                }

                foreach (var w in windows)
                {
                    w.OnNext(item);
                }

                int c = count + 1;

                if (c == size)
                {
                    windows.Dequeue().OnCompleted();
                    count = c - skip;
                }
                else
                {
                    count = c;
                }

                if (++idx == skip)
                {
                    index = 0;
                }
                else
                {
                    index = idx;
                }
            }

            public void OnSubscribe(IDisposable d)
            {
                upstream = d;
                downstream.OnSubscribe(this);
            }
        }
    }
}
