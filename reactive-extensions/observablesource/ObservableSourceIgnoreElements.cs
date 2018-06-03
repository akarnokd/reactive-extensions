using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceIgnoreElements<T> : IObservableSource<T>
    {
        readonly IObservableSource<T> source;

        public ObservableSourceIgnoreElements(IObservableSource<T> source)
        {
            this.source = source;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            source.Subscribe(new IgnoreElementsObserver(observer));
        }

        sealed class IgnoreElementsObserver : BasicFuseableObserver<T, T>
        {
            public IgnoreElementsObserver(ISignalObserver<T> downstream) : base(downstream)
            {
            }

            public override void OnNext(T item)
            {
                // deliberately ignored
            }

            public override int RequestFusion(int mode)
            {
                if ((mode & FusionSupport.Async) != 0)
                {
                    fusionMode = FusionSupport.Async;
                    return FusionSupport.Async;
                }
                return FusionSupport.None;
            }

            public override T TryPoll(out bool success)
            {
                success = false;
                return default(T);
            }
        }
    }
}
