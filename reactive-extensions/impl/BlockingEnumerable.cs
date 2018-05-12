using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Consumes the source observable in a blocking fashion through
    /// an IEnumerable.
    /// </summary>
    /// <typeparam name="T">The element type of the sequence.</typeparam>
    /// <remarks>Since 0.0.4</remarks>
    internal sealed class BlockingEnumerable<T> : IEnumerable<T>
    {
        readonly IObservable<T> source;

        public BlockingEnumerable(IObservable<T> source)
        {
            this.source = source;
        }

        public IEnumerator<T> GetEnumerator()
        {
            var consumer = new BlockingEnumerator();

            consumer.OnSubscribe(source.Subscribe(consumer));

            return consumer;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        sealed class BlockingEnumerator : IEnumerator<T>, IObserver<T>
        {
            readonly ConcurrentQueue<T> queue = new ConcurrentQueue<T>();

            bool done;
            Exception error;

            int wip;

            IDisposable upstream;

            T current;

            public T Current => current;

            object IEnumerator.Current => current;

            internal void OnSubscribe(IDisposable d)
            {
                DisposableHelper.SetOnce(ref upstream, d);
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
                Signal();
            }

            public bool MoveNext()
            {
                var q = queue;
                for (; ; )
                {
                    if (DisposableHelper.IsDisposed(ref upstream))
                    {
                        current = default(T);
                        while (q.TryDequeue(out var _)) ;
                        return false;
                    }

                    var d = Volatile.Read(ref done);
                    var empty = !q.TryDequeue(out current);

                    if (d && empty)
                    {
                        DisposableHelper.Dispose(ref upstream);
                        var ex = error;
                        if (ex != null)
                        {
                            throw ex;
                        }
                        return false;                        
                    }

                    if (!empty)
                    {
                        Interlocked.Decrement(ref wip);
                        return true;
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

            public void Reset()
            {
                throw new InvalidOperationException("Not supported");
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
        }
    }
}
