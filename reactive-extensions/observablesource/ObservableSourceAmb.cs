using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceAmbArray<T> : IObservableSource<T>
    {
        readonly IObservableSource<T>[] sources;

        public ObservableSourceAmbArray(IObservableSource<T>[] sources)
        {
            this.sources = sources;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            ObservableSourceAmbCoordinator<T>.Subscribe(observer, sources);
        }
    }

    internal sealed class ObservableSourceAmbCoordinator<T> : IDisposable
    {
        readonly ISignalObserver<T> downstream;

        readonly InnerObserver[] observers;

        int winner;

        public ObservableSourceAmbCoordinator(ISignalObserver<T> downstream, int n)
        {
            this.downstream = downstream;
            this.observers = new InnerObserver[n];
            for (int i = 0; i < n; i++)
            {
                observers[i] = new InnerObserver(downstream, this, i);
            }
            Volatile.Write(ref winner, -1);
        }

        internal static void Subscribe(ISignalObserver<T> observer, IObservableSource<T>[] sources)
        {
            var n = sources.Length;
            if (n == 0)
            {
                DisposableHelper.Complete(observer);
            }
            else
            if (n == 1)
            {
                sources[0].Subscribe(observer);
            } else
            {
                var parent = new ObservableSourceAmbCoordinator<T>(observer, n);
                observer.OnSubscribe(parent);

                parent.Subscribe(sources);
            }
        }

        void Subscribe(IObservableSource<T>[] sources)
        {
            for (int i = 0; i < sources.Length; i++)
            {
                if (Volatile.Read(ref winner) != -1)
                {
                    break;
                }
                var inner = Volatile.Read(ref observers[i]);
                if (inner == null)
                {
                    break;
                }
                else
                {
                    sources[i].Subscribe(inner);
                }
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < observers.Length; i++)
            {
                if (Volatile.Read(ref observers[i]) != null)
                {
                    Interlocked.Exchange(ref observers[i], null)?.Dispose();
                }
            }
        }

        bool TryWin(int index)
        {
            if (Volatile.Read(ref winner) == -1 && Interlocked.CompareExchange(ref winner, index, -1) == -1)
            {
                for (int i = 0; i < observers.Length; i++)
                {
                    if (i != index && Volatile.Read(ref observers[i]) != null)
                    {
                        Interlocked.Exchange(ref observers[i], null)?.Dispose();
                    }
                }
                return true;
            }
            return false;
        }

        sealed class InnerObserver : ISignalObserver<T>, IDisposable
        {
            readonly ISignalObserver<T> downstream;

            readonly ObservableSourceAmbCoordinator<T> parent;

            readonly int index;

            bool won;

            IDisposable upstream;

            public InnerObserver(ISignalObserver<T> downstream, ObservableSourceAmbCoordinator<T> parent, int index)
            {
                this.downstream = downstream;
                this.parent = parent;
                this.index = index;
            }

            public void Dispose()
            {
                DisposableHelper.Dispose(ref upstream);
            }

            public void OnCompleted()
            {
                if (won || parent.TryWin(index))
                {
                    downstream.OnCompleted();
                }
            }

            public void OnError(Exception ex)
            {
                if (won || parent.TryWin(index))
                {
                    downstream.OnError(ex);
                }
            }

            public void OnNext(T item)
            {
                if (won)
                {
                    downstream.OnNext(item);
                }
                else
                if (parent.TryWin(index))
                {
                    won = true;
                    downstream.OnNext(item);
                }
            }

            public void OnSubscribe(IDisposable d)
            {
                DisposableHelper.SetOnce(ref upstream, d);
            }
        }
    }

    internal sealed class ObservableSourceAmbEnumerable<T> : IObservableSource<T>
    {
        readonly IEnumerable<IObservableSource<T>> sources;

        public ObservableSourceAmbEnumerable(IEnumerable<IObservableSource<T>> sources)
        {
            this.sources = sources;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            var srcs = default(IObservableSource<T>[]);

            try
            {
                srcs = sources.ToArray();
            }
            catch (Exception ex)
            {
                DisposableHelper.Error(observer, ex);
                return;
            }

            ObservableSourceAmbCoordinator<T>.Subscribe(observer, srcs);
        }
    }
}
