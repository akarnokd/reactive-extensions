using System;
using System.Collections.Concurrent;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Serialize the emission of OnXXX events towards the downstream
    /// observer when the upstream may call those OnXXX methods concurrently.
    /// </summary>
    /// <typeparam name="T">The value type of the flow.</typeparam>
    internal class SerializedObserver<T> : BaseObserver<T, T>
    {
        int wip;

        ConcurrentQueue<Node> queue;

        bool terminated;

        public SerializedObserver(IObserver<T> downstream) : base(downstream)
        {
        }

        ConcurrentQueue<Node> GetQueue()
        {
            for (; ; )
            {
                var q = Volatile.Read(ref queue);
                if (q != null)
                {
                    return q;
                }
                q = new ConcurrentQueue<Node>();
                if (Interlocked.CompareExchange(ref queue, q, null) == null)
                {
                    return q;
                }
            }
        }

        public override void OnCompleted()
        {
            if (Interlocked.CompareExchange(ref wip, 1, 0) == 0)
            {
                var q = Volatile.Read(ref queue);
                if (q == null)
                {
                    if (!terminated)
                    {
                        terminated = true;
                        downstream.OnCompleted();
                    }
                    if (Interlocked.Decrement(ref wip) == 0)
                    {
                        return;
                    }
                }
                else
                {
                    q.Enqueue(new Node() { item = default(T), error = null, done = true });
                }
            }
            else
            {
                var q = GetQueue();
                q.Enqueue(new Node() { item = default(T), error = null, done = true });
                if (Interlocked.Increment(ref wip) != 1)
                {
                    return;
                }
            }

            DrainLoop();
        }

        public override void OnError(Exception error)
        {
            if (Interlocked.CompareExchange(ref wip, 1, 0) == 0)
            {
                var q = Volatile.Read(ref queue);
                if (q == null)
                {
                    if (!terminated)
                    {
                        terminated = true;
                        downstream.OnError(error);
                    }
                    if (Interlocked.Decrement(ref wip) == 0)
                    {
                        return;
                    }
                }
                else
                {
                    q.Enqueue(new Node() { item = default(T), error = error, done = false });
                }
            }
            else
            {
                var q = GetQueue();
                q.Enqueue(new Node() { item = default(T), error = error, done = false });
                if (Interlocked.Increment(ref wip) != 1)
                {
                    return;
                }
            }

            DrainLoop();
        }

        public override void OnNext(T value)
        {
            if (Interlocked.CompareExchange(ref wip, 1, 0) == 0)
            {
                if (!terminated)
                {
                    downstream.OnNext(value);
                }
                if (Interlocked.Decrement(ref wip) == 0)
                {
                    return;
                }
            }
            else
            {
                var q = GetQueue();
                q.Enqueue(new Node() { item = value, error = null, done = false });
                if (Interlocked.Increment(ref wip) != 1)
                {
                    return;
                }
            }

            DrainLoop();
        }

        void Drain()
        {
            if (Interlocked.Increment(ref wip) == 1)
            {
                DrainLoop();
            }
        }

        void DrainLoop()
        {
            var missed = 1;
            for (; ; )
            {

                var q = Volatile.Read(ref queue);

                for (; ; )
                {
                    if (terminated)
                    {
                        while (q.TryDequeue(out var _)) ;
                        break;
                    }

                    if (q.TryDequeue(out var n))
                    {
                        if (n.done)
                        {
                            terminated = true;
                            downstream.OnCompleted();
                            continue;
                        }
                        if (n.error != null)
                        {
                            terminated = true;
                            downstream.OnError(n.error);
                            continue;
                        }

                        downstream.OnNext(n.item);
                    }
                    else
                    {
                        break;
                    }
                }

                missed = Interlocked.Add(ref wip, -missed);
                if (missed == 0)
                {
                    break;
                }
            }
        }

        internal struct Node
        {
            internal T item;
            internal Exception error;
            internal bool done;
        }
    }
}
