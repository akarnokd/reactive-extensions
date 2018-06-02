using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceFilter<T> : IObservableSource<T>
    {
        readonly IObservableSource<T> source;

        readonly Func<T, bool> predicate;

        public ObservableSourceFilter(IObservableSource<T> source, Func<T, bool> predicate)
        {
            this.source = source;
            this.predicate = predicate;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            source.Subscribe(new FilterObserver(observer, predicate));
        }

        sealed class FilterObserver : BasicFuseableObserver<T, T>
        {
            readonly Func<T, bool> predicate;

            public FilterObserver(ISignalObserver<T> downstream, Func<T, bool> predicate) : base(downstream)
            {
                this.predicate = predicate;
            }

            public override void OnNext(T item)
            {
                if (done)
                {
                    return;
                }
                if (fusionMode == FusionSupport.None)
                {
                    try
                    {
                        if (!predicate(item))
                        {
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        OnError(ex);
                        return;
                    }
                }
                downstream.OnNext(item);
            }

            public override int RequestFusion(int mode)
            {
                return RequestBoundaryFusion(mode);
            }

            public override T TryPoll(out bool success)
            {
                var q = queue;

                for (; ;)
                {
                    var v = q.TryPoll(out var nonEmpty);

                    if (nonEmpty)
                    {
                        if (predicate(v))
                        {
                            success = true;
                            return v;
                        }
                    }
                    else
                    {
                        success = false;
                        return default(T);
                    }
                }
            }
        }

    }
}
