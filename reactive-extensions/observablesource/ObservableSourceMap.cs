using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceMap<T, R> : IObservableSource<R>
    {
        readonly IObservableSource<T> source;

        readonly Func<T, R> mapper;

        public ObservableSourceMap(IObservableSource<T> source, Func<T, R> mapper)
        {
            this.source = source;
            this.mapper = mapper;
        }

        public void Subscribe(ISignalObserver<R> observer)
        {
            source.Subscribe(new MapObserver(observer, mapper));
        }

        sealed class MapObserver : BasicFuseableObserver<T, R>
        {
            readonly Func<T, R> mapper;

            public MapObserver(ISignalObserver<R> downstream, Func<T, R> mapper) : base(downstream)
            {
                this.mapper = mapper;
            }

            public override void OnNext(T item)
            {
                if (done)
                {
                    return;
                }
                var r = default(R);

                if (fusionMode == FusionSupport.None)
                {
                    try
                    {
                        r = mapper(item);
                    }
                    catch (Exception ex)
                    {
                        OnError(ex);
                        return;
                    }
                }
                downstream.OnNext(r);
            }

            public override int RequestFusion(int mode)
            {
                return RequestBoundaryFusion(mode);
            }

            public override R TryPoll(out bool success)
            {
                var q = queue;

                var v = q.TryPoll(out success);

                if (success)
                {
                    return mapper(v);
                }
                return default(R);
            }
        }
    }
}
