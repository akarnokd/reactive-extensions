using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Hides the identity of the upstream source and disposable.
    /// </summary>
    /// <remarks>Since 0.0.9</remarks>
    internal sealed class CompletableHide : ICompletableSource
    {
        readonly ICompletableSource source;

        public CompletableHide(ICompletableSource source)
        {
            this.source = source;
        }

        public void Subscribe(ICompletableObserver observer)
        {
            source.Subscribe(new HideObserver(observer));
        }

        sealed class HideObserver : ICompletableObserver, IDisposable
        {
            readonly ICompletableObserver downstream;

            IDisposable upstream;

            public HideObserver(ICompletableObserver downstream)
            {
                this.downstream = downstream;
            }

            public void Dispose()
            {
                upstream.Dispose();
            }

            public void OnCompleted()
            {
                downstream.OnCompleted();
            }

            public void OnError(Exception error)
            {
                downstream.OnError(error);
            }

            public void OnSubscribe(IDisposable d)
            {
                upstream = d;
                downstream.OnSubscribe(this);
            }
        }
    }
}
