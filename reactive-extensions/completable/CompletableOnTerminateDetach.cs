using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// When the upstream terminates or the downstream disposes,
    /// it detaches the references between the two, avoiding
    /// leaks of one or the other.
    /// </summary>
    internal sealed class CompletableOnTerminateDetach : ICompletableSource
    {
        readonly ICompletableSource source;

        public CompletableOnTerminateDetach(ICompletableSource source)
        {
            this.source = source;
        }

        public void Subscribe(ICompletableObserver observer)
        {
            source.Subscribe(new OnTerminateDetachObserver(observer));
        }

        sealed class OnTerminateDetachObserver : ICompletableObserver, IDisposable
        {
            ICompletableObserver downstream;

            IDisposable upstream;

            public OnTerminateDetachObserver(ICompletableObserver downstream)
            {
                Volatile.Write(ref this.downstream, downstream);
            }

            public void Dispose()
            {
                Volatile.Write(ref downstream, null);

                // plain read should be okay as Dispose happens after OnSubscribe
                var d = upstream;
                Volatile.Write(ref upstream, null);

                d?.Dispose();
            }

            public void OnCompleted()
            {
                var d = downstream;
                downstream = null;
                upstream = null;

                d?.OnCompleted();
            }

            public void OnError(Exception error)
            {
                var d = downstream;
                downstream = null;
                upstream = null;

                d?.OnError(error);
            }

            public void OnSubscribe(IDisposable d)
            {
                upstream = d;
                downstream.OnSubscribe(this);
            }
        }
    }
}
