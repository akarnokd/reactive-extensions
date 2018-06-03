using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceRange : IObservableSource<int>
    {
        readonly int start;
        readonly int end;

        public ObservableSourceRange(int start, int end)
        {
            this.start = start;
            this.end = end;
        }

        public void Subscribe(ISignalObserver<int> observer)
        {
            var parent = new RangeDisposable(observer, start, end);
            observer.OnSubscribe(parent);
            parent.Run();
        }

        sealed class RangeDisposable : IFuseableDisposable<int>
        {
            readonly ISignalObserver<int> downstream;

            readonly int end;

            int index;

            bool disposed;

            bool fused;

            public RangeDisposable(ISignalObserver<int> downstream, int index, int end)
            {
                this.downstream = downstream;
                this.index = index;
                this.end = end;
            }

            internal void Run()
            {
                if (fused)
                {
                    return;
                }

                var downstream = this.downstream;
                var f = end;
                for (int i = index; i != f; i++)
                {
                    if (Volatile.Read(ref disposed))
                    {
                        return;
                    }

                    downstream.OnNext(i);
                }

                if (Volatile.Read(ref disposed))
                {
                    return;
                }
                downstream.OnCompleted();
            }

            public void Clear()
            {
                index = end;
            }

            public void Dispose()
            {
                Volatile.Write(ref disposed, true);
            }

            public bool IsEmpty()
            {
                return index == end;
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

            public int TryPoll(out bool success)
            {
                var idx = index;
                if (idx != end)
                {
                    success = true;
                    index = idx + 1;
                    return idx;
                }
                success = false;
                return default(int);
            }

            public bool TryOffer(int item)
            {
                throw new InvalidOperationException("Should not be called!");
            }
        }
    }

    internal sealed class ObservableSourceRangeLong : IObservableSource<long>
    {
        readonly long start;
        readonly long end;

        public ObservableSourceRangeLong(long start, long end)
        {
            this.start = start;
            this.end = end;
        }

        public void Subscribe(ISignalObserver<long> observer)
        {
            var parent = new RangeDisposable(observer, start, end);
            observer.OnSubscribe(parent);
            parent.Run();
        }

        sealed class RangeDisposable : IFuseableDisposable<long>
        {
            readonly ISignalObserver<long> downstream;

            readonly long end;

            long index;

            bool disposed;

            bool fused;

            public RangeDisposable(ISignalObserver<long> downstream, long index, long end)
            {
                this.downstream = downstream;
                this.index = index;
                this.end = end;
            }

            internal void Run()
            {
                if (fused)
                {
                    return;
                }

                var downstream = this.downstream;
                var f = end;
                for (var i = index; i != f; i++)
                {
                    if (Volatile.Read(ref disposed))
                    {
                        return;
                    }

                    downstream.OnNext(i);
                }

                if (Volatile.Read(ref disposed))
                {
                    return;
                }
                downstream.OnCompleted();
            }

            public void Clear()
            {
                index = end;
            }

            public void Dispose()
            {
                Volatile.Write(ref disposed, true);
            }

            public bool IsEmpty()
            {
                return index == end;
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

            public long TryPoll(out bool success)
            {
                var idx = index;
                if (idx != end)
                {
                    success = true;
                    index = idx + 1;
                    return idx;
                }
                success = false;
                return default(int);
            }

            public bool TryOffer(long item)
            {
                throw new InvalidOperationException("Should not be called!");
            }
        }
    }
}
