using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceJust<T> : IObservableSource<T>, IStaticValue<T>
    {
        readonly T item;

        public ObservableSourceJust(T item)
        {
            this.item = item;
        }

        public T GetValue(out bool success)
        {
            success = true;
            return item;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            var parent = new JustDisposable(observer, item);
            observer.OnSubscribe(parent);
            parent.Run();
        }

        sealed class JustDisposable : IFuseableDisposable<T>
        {
            readonly ISignalObserver<T> downstream;

            readonly T item;

            int state;

            static readonly int ModeFresh = 0;
            static readonly int ModeFused = 1;
            static readonly int ModeDone = 2;

            public JustDisposable(ISignalObserver<T> downstream, T item)
            {
                this.downstream = downstream;
                this.item = item;
            }

            public void Clear()
            {
                Volatile.Write(ref state, ModeDone);
            }

            public void Dispose()
            {
                Volatile.Write(ref state, ModeDone);
            }

            public bool IsEmpty()
            {
                return Volatile.Read(ref state) != ModeFused;
            }

            public int RequestFusion(int mode)
            {
                if ((mode & FusionSupport.Sync) != 0)
                {
                    Volatile.Write(ref state, ModeFused);
                    return FusionSupport.Sync;
                }
                return FusionSupport.None;
            }

            public T TryPoll(out bool success)
            {
                if (Volatile.Read(ref state) == ModeFused)
                {
                    success = true;
                    Volatile.Write(ref state, ModeDone);
                    return item;
                }
                success = false;
                return default(T);
            }

            internal void Run()
            {
                if (Volatile.Read(ref state) == ModeFresh)
                {
                    downstream.OnNext(item);
                    if (Volatile.Read(ref state) == 0)
                    {
                        Volatile.Write(ref state, ModeDone);
                        downstream.OnCompleted();
                    }
                }
            }

            public bool TryOffer(T item)
            {
                throw new InvalidOperationException("Should not be called!");
            }
        }
    }
}
