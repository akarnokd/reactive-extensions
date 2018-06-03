using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceEnumerable<T> : IObservableSource<T>
    {
        readonly IEnumerable<T> source;

        public ObservableSourceEnumerable(IEnumerable<T> source)
        {
            this.source = source;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            var en = default(IEnumerator<T>);
            try
            {
                en = ValidationHelper.RequireNonNullRef(source.GetEnumerator(), "The GetEnumerator returned a null IEnumeration");
            }
            catch (Exception ex)
            {
                DisposableHelper.Error(observer, ex);
                return;
            }

            var parent = new EnumerableDisposable(observer, en);
            observer.OnSubscribe(parent);
            parent.Run();
        }

        sealed class EnumerableDisposable : IFuseableDisposable<T>
        {
            readonly ISignalObserver<T> downstream;

            IEnumerator<T> enumerator;

            bool disposed;

            bool fused;

            public EnumerableDisposable(ISignalObserver<T> downstream, IEnumerator<T> enumerator)
            {
                this.downstream = downstream;
                this.enumerator = enumerator;
            }

            internal void Run()
            {
                if (fused)
                {
                    return;
                }

                for (; ; )
                {
                    if (Volatile.Read(ref disposed))
                    {
                        enumerator?.Dispose();
                        enumerator = null;
                        break;
                    }
                    else
                    {
                        var v = default(T);
                        var has = false;

                        try
                        {
                            has = enumerator.MoveNext();
                            if (has)
                            {
                                v = enumerator.Current;
                            }
                        }
                        catch (Exception ex)
                        {
                            downstream.OnError(ex);
                            break;
                        }

                        if (has)
                        {
                            downstream.OnNext(v);
                        }
                        else
                        {
                            downstream.OnCompleted();
                            Volatile.Write(ref disposed, true);
                        }
                    }
                }
            }

            public void Clear()
            {
                enumerator?.Dispose();
                enumerator = null;
            }

            public void Dispose()
            {
                Volatile.Write(ref disposed, true);
            }

            public bool IsEmpty()
            {
                return enumerator == null;
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

            public bool TryOffer(T item)
            {
                throw new InvalidOperationException("Should not be called!");
            }

            public T TryPoll(out bool success)
            {
                var en = enumerator;
                if (en != null)
                {
                    success = en.MoveNext();
                    if (success)
                    {
                        return en.Current;
                    }
                    else
                    {
                        en.Dispose();
                        enumerator = null;
                    }
                }
                success = false;
                return default(T);
            }
        }
    }
}
