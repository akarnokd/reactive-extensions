using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceDelaySelector<T, U> : IObservableSource<T>
    {
        readonly IObservableSource<T> source;

        readonly Func<T, IObservableSource<U>> delaySelector;

        readonly bool delayErrors;

        public ObservableSourceDelaySelector(IObservableSource<T> source, Func<T, IObservableSource<U>> delaySelector, bool delayErrors)
        {
            this.source = source;
            this.delaySelector = delaySelector;
            this.delayErrors = delayErrors;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            source.Subscribe(new DelaySelectorObserver(observer, delaySelector, delayErrors));
        }

        sealed class DelaySelectorObserver : ISignalObserver<T>, IDisposable
        {
            readonly ISignalObserver<T> downstream;

            readonly Func<T, IObservableSource<U>> delaySelector;

            readonly bool delayErrors;

            readonly ConcurrentQueue<T> queue;

            IDisposable upstream;

            HashSet<InnerObserver> observers;

            int wip;

            bool done;
            int active;
            Exception errors;

            bool disposed;

            public DelaySelectorObserver(ISignalObserver<T> downstream, Func<T, IObservableSource<U>> delaySelector, bool delayErrors)
            {
                this.downstream = downstream;
                this.delaySelector = delaySelector;
                this.delayErrors = delayErrors;
                this.observers = new HashSet<InnerObserver>();
                this.queue = new ConcurrentQueue<T>();
            }

            bool Add(InnerObserver inner)
            {
                if (Volatile.Read(ref disposed))
                {
                    return false;
                }

                lock (this)
                {
                    if (Volatile.Read(ref disposed))
                    {
                        return false;
                    }

                    observers.Add(inner);
                    return true;
                }
            }

            void Remove(InnerObserver inner)
            {
                if (Volatile.Read(ref disposed))
                {
                    return;
                }

                lock (this)
                {
                    if (Volatile.Read(ref disposed))
                    {
                        return;
                    }

                    observers.Remove(inner);
                }
            }

            public void Dispose()
            {
                upstream.Dispose();
                DisposeAll();
                while (queue.TryDequeue(out var _)) ;
            }

            public void DisposeAll()
            {
                if (Volatile.Read(ref disposed))
                {
                    return;
                }

                var obs = default(HashSet<InnerObserver>);
                lock (this)
                {
                    if (Volatile.Read(ref disposed))
                    {
                        return;
                    }
                    obs = observers;
                    observers = null;
                    Volatile.Write(ref disposed, true);
                }

                foreach (var inner in obs)
                {
                    inner.Dispose();
                }
            }

            public void OnCompleted()
            {
                Volatile.Write(ref done, true);
                Drain();
            }

            public void OnError(Exception ex)
            {
                if (delayErrors)
                {
                    ExceptionHelper.AddException(ref errors, ex);
                }
                else
                {
                    Interlocked.CompareExchange(ref errors, ex, null);
                }
                Volatile.Write(ref done, true);
                Drain();
            }

            public void OnNext(T item)
            {
                var other = default(IObservableSource<U>);

                try
                {
                    other = ValidationHelper.RequireNonNullRef(delaySelector(item), "The delaySelector returned a null IObservableSource");
                }
                catch (Exception ex)
                {
                    upstream.Dispose();
                    OnError(ex);
                    return;
                }

                var inner = new InnerObserver(this, item);
                if (Add(inner))
                {
                    Interlocked.Increment(ref active);
                    other.Subscribe(inner);
                }
            }

            public void OnSubscribe(IDisposable d)
            {
                upstream = d;
                downstream.OnSubscribe(this);
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
                        while (queue.TryDequeue(out var _)) ;
                    }
                    else
                    {
                        if (!delayErrors)
                        {
                            var ex = Volatile.Read(ref errors);
                            if (ex != null)
                            {
                                downstream.OnError(ex);
                                DisposeAll();
                                continue;
                            }
                        }

                        var d = Volatile.Read(ref done);
                        var a = Volatile.Read(ref active);

                        var success = queue.TryDequeue(out var v);

                        if (d && a == 0 && !success)
                        {
                            var ex = Volatile.Read(ref errors);
                            if (ex != null)
                            {
                                downstream.OnError(ex);
                            }
                            else
                            {
                                downstream.OnCompleted();
                            }
                            Volatile.Write(ref disposed, true);
                        }
                        else
                        if (success)
                        {
                            downstream.OnNext(v);

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

            void Signal(InnerObserver sender, T item)
            {
                Remove(sender);
                queue.Enqueue(item);
                Interlocked.Decrement(ref active);
                Drain();
            }

            void SignalError(InnerObserver sender, Exception ex)
            {
                Remove(sender);
                if (delayErrors)
                {
                    ExceptionHelper.AddException(ref errors, ex);
                    Interlocked.Decrement(ref active);
                }
                else
                {
                    upstream.Dispose();
                    Interlocked.CompareExchange(ref errors, ex, null);
                }
                Drain();
            }

            sealed class InnerObserver : ISignalObserver<U>, IDisposable
            {
                readonly DelaySelectorObserver parent;

                readonly T item;

                IDisposable upstream;

                public InnerObserver(DelaySelectorObserver parent, T item)
                {
                    this.parent = parent;
                    this.item = item;
                }

                public void Dispose()
                {
                    DisposableHelper.Dispose(ref upstream);
                }

                public void OnCompleted()
                {
                    DisposableHelper.WeakDispose(ref upstream);
                    parent.Signal(this, item);
                }

                public void OnError(Exception ex)
                {
                    DisposableHelper.WeakDispose(ref upstream);
                    parent.SignalError(this, ex);
                }

                public void OnNext(U item)
                {
                    if (DisposableHelper.Dispose(ref upstream))
                    {
                        parent.Signal(this, this.item);
                    }
                }

                public void OnSubscribe(IDisposable d)
                {
                    DisposableHelper.SetOnce(ref upstream, d);
                }
            }
        }
    }
}
