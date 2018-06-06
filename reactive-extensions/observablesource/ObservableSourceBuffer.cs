using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceBuffer<T, B> : IObservableSource<B> where B : ICollection<T>
    {
        readonly IObservableSource<T> source;

        readonly Func<B> bufferSupplier;

        readonly int size;

        readonly int skip;

        public ObservableSourceBuffer(IObservableSource<T> source, Func<B> bufferSupplier, int size, int skip)
        {
            this.source = source;
            this.bufferSupplier = bufferSupplier;
            this.size = size;
            this.skip = skip;
        }

        public void Subscribe(ISignalObserver<B> observer)
        {
            if (size == skip)
            {
                source.Subscribe(new BufferExactObserver(observer, bufferSupplier, size));
            }
            else
            if (size < skip)
            {
                source.Subscribe(new BufferSkipObserver(observer, bufferSupplier, size, skip));
            }
            else
            {
                source.Subscribe(new BufferOverlapObserver(observer, bufferSupplier, size, skip));
            }
        }

        sealed class BufferOverlapObserver : ISignalObserver<T>, IDisposable
        {
            readonly ISignalObserver<B> downstream;

            readonly Func<B> bufferSupplier;

            readonly int size;

            readonly int skip;

            readonly Queue<B> buffers;

            IDisposable upstream;

            int count;
            int index;

            bool done;

            bool disposed;

            public BufferOverlapObserver(ISignalObserver<B> downstream, Func<B> bufferSupplier, int size, int skip)
            {
                this.downstream = downstream;
                this.bufferSupplier = bufferSupplier;
                this.size = size;
                this.skip = skip;
                this.buffers = new Queue<B>();
            }

            public void Dispose()
            {
                Volatile.Write(ref disposed, true);
                upstream.Dispose();
            }

            public void OnCompleted()
            {
                if (done)
                {
                    return;
                }

                var buffers = this.buffers;

                while (buffers.Count != 0)
                {
                    if (Volatile.Read(ref disposed))
                    {
                        buffers.Clear();
                        return;
                    }
                    downstream.OnNext(buffers.Dequeue());
                }
                downstream.OnCompleted();
            }

            public void OnError(Exception ex)
            {
                if (done)
                {
                    return;
                }
                done = true;
                buffers.Clear();
                downstream.OnError(ex);
            }

            public void OnNext(T item)
            {
                if (done)
                {
                    return;
                }

                var buffers = this.buffers;
                try
                {
                    var idx = index;
                    if (idx == 0)
                    {
                        buffers.Enqueue(bufferSupplier());
                    }
                    if (++idx == skip)
                    {
                        index = 0;
                    }
                    else
                    {
                        index = idx;
                    }

                    foreach (var b in buffers)
                    {
                        b.Add(item);
                    }

                    int c = count + 1;

                    if (c == size)
                    {
                        count = size - skip;
                        downstream.OnNext(buffers.Dequeue());
                    }
                    else
                    {
                        count = c;
                    }

                }
                catch (Exception ex)
                {
                    Dispose();
                    OnError(ex);
                }
            }

            public void OnSubscribe(IDisposable d)
            {
                upstream = d;
                downstream.OnSubscribe(this);
            }
        }

        sealed class BufferSkipObserver : ISignalObserver<T>, IDisposable
        {
            readonly ISignalObserver<B> downstream;

            readonly Func<B> bufferSupplier;

            readonly int size;

            readonly int skip;

            IDisposable upstream;

            B buffer;
            int count;
            int index;

            bool done;

            public BufferSkipObserver(ISignalObserver<B> downstream, Func<B> bufferSupplier, int size, int skip)
            {
                this.downstream = downstream;
                this.bufferSupplier = bufferSupplier;
                this.size = size;
                this.skip = skip;
            }

            public void Dispose()
            {
                upstream.Dispose();
            }

            public void OnCompleted()
            {
                if (done)
                {
                    return;
                }
                var b = buffer;
                buffer = default;

                if (b != null)
                {
                    downstream.OnNext(b);
                }
                downstream.OnCompleted();
            }

            public void OnError(Exception ex)
            {
                if (done)
                {
                    return;
                }
                done = true;
                buffer = default;
                downstream.OnError(ex);
            }

            public void OnNext(T item)
            {
                if (done)
                {
                    return;
                }

                var b = buffer;
                

                try
                {
                    var idx = index;
                    if (idx == 0)
                    {
                        b = bufferSupplier();
                        buffer = b;
                    }

                    if (b != null)
                    {
                        b.Add(item);

                        var c = count + 1;
                        if (c == size)
                        {
                            count = 0;
                            buffer = default;
                            downstream.OnNext(b);
                        }
                        else
                        {
                            count = c;
                        }
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
                catch (Exception ex)
                {
                    Dispose();
                    OnError(ex);
                }
            }

            public void OnSubscribe(IDisposable d)
            {
                upstream = d;
                downstream.OnSubscribe(this);
            }
        }

        sealed class BufferExactObserver : ISignalObserver<T>, IDisposable
        {
            readonly ISignalObserver<B> downstream;

            readonly Func<B> bufferSupplier;

            readonly int size;

            IDisposable upstream;

            B buffer;
            int count;

            bool done;

            public BufferExactObserver(ISignalObserver<B> downstream, Func<B> bufferSupplier, int size)
            {
                this.downstream = downstream;
                this.bufferSupplier = bufferSupplier;
                this.size = size;
            }

            public void Dispose()
            {
                upstream.Dispose();
            }

            public void OnCompleted()
            {
                if (done)
                {
                    return;
                }
                var b = buffer;
                buffer = default;

                if (b != null)
                {
                    downstream.OnNext(b);
                }
                downstream.OnCompleted();
            }

            public void OnError(Exception ex)
            {
                if (done)
                {
                    return;
                }
                done = true;
                buffer = default;
                downstream.OnError(ex);
            }

            public void OnNext(T item)
            {
                if (done)
                {
                    return;
                }

                var b = buffer;

                try
                {
                    if (b == null)
                    {
                        b = bufferSupplier();
                        buffer = b;
                    }

                    b.Add(item);
                    var c = count + 1;

                    if (c != size)
                    {
                        count = c;
                        return;
                    }
                    count = 0;
                    buffer = default;
                }
                catch (Exception ex)
                {
                    Dispose();
                    OnError(ex);
                    return;
                }

                downstream.OnNext(b);
            }

            public void OnSubscribe(IDisposable d)
            {
                upstream = d;
                downstream.OnSubscribe(this);
            }
        }
    }
}
