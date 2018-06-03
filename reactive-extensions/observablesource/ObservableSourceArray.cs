using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceArray<T> : IObservableSource<T>
    {
        readonly T[] array;

        public ObservableSourceArray(T[] array)
        {
            this.array = array;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            var parent = new ArrayDisposable(observer, array);
            observer.OnSubscribe(parent);
            parent.Run();
        }

        sealed class ArrayDisposable : IFuseableDisposable<T>
        {
            readonly ISignalObserver<T> downstream;

            readonly T[] array;
            
            int index;

            bool disposed;

            bool fused;

            public ArrayDisposable(ISignalObserver<T> downstream, T[] array)
            {
                this.downstream = downstream;
                this.array = array;
            }

            public void Clear()
            {
                index = array.Length;
            }

            public void Dispose()
            {
                Volatile.Write(ref disposed, true);
            }

            public bool IsEmpty()
            {
                return index == array.Length;
            }

            public int RequestFusion(int mode)
            {
                if ((mode & FusionSupport.Sync) != 0)
                {
                    fused = true;
                    return FusionSupport.Sync;
                }
                return FusionSupport.None;
            }

            public T TryPoll(out bool success)
            {
                var array = this.array;
                var n = array.Length;
                var idx = index;
                if (idx != n)
                {
                    success = true;
                    index = idx + 1;
                    return array[idx];
                }
                success = false;
                return default(T);
            }

            internal void Run()
            {
                if (fused)
                {
                    return;
                }

                var array = this.array;
                var n = array.Length;
                var downstream = this.downstream;

                for (var i = 0; i < n; i++)
                {
                    if (Volatile.Read(ref disposed))
                    {
                        return;
                    }
                    downstream.OnNext(array[i]);
                }
                if (Volatile.Read(ref disposed))
                {
                    return;
                }
                downstream.OnCompleted();
            }


            public bool TryOffer(T item)
            {
                throw new InvalidOperationException("Should not be called!");
            }
        }
    }
}
