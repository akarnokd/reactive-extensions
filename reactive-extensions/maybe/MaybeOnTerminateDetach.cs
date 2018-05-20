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
    /// <typeparam name="T">The success value type.</typeparam>
    /// <remarks>Since 0.0.11</remarks>
    internal sealed class MaybeOnTerminateDetach<T> : IMaybeSource<T>
    {
        readonly IMaybeSource<T> source;

        public MaybeOnTerminateDetach(IMaybeSource<T> source)
        {
            this.source = source;
        }

        public void Subscribe(IMaybeObserver<T> observer)
        {
            source.Subscribe(new OnTerminateDetachObserver(observer));
        }

        sealed class OnTerminateDetachObserver : IMaybeObserver<T>, IDisposable
        {
            IMaybeObserver<T> downstream;

            IDisposable upstream;

            public OnTerminateDetachObserver(IMaybeObserver<T> downstream)
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

            public void OnSuccess(T item)
            {
                var d = downstream;
                downstream = null;
                upstream = null;

                d?.OnSuccess(item);
            }


            public void OnSubscribe(IDisposable d)
            {
                upstream = d;
                downstream.OnSubscribe(this);
            }
        }
    }
}
