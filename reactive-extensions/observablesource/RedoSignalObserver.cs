using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Observer that re-subscribes to a source upon a call
    /// to <see cref="Next"/>.
    /// </summary>
    /// <typeparam name="T">The element type of the sequence.</typeparam>
    /// <remarks>Since 0.0.22</remarks>
    internal abstract class RedoSignalObserver<T> : BaseSignalObserver<T, T>
    {
        readonly IObservableSource<T> source;

        int wip;

        internal RedoSignalObserver(ISignalObserver<T> downstream, IObservableSource<T> source) : base(downstream)
        {
            this.source = source;
        }

        public override void OnNext(T value)
        {
            downstream.OnNext(value);
        }

        public override void OnSubscribe(IDisposable d)
        {
            DisposableHelper.SetOnce(ref upstream, d);
        }

        public override void Dispose()
        {
            DisposableHelper.Dispose(ref upstream);
        }

        internal void Next()
        {
            if (Interlocked.Increment(ref wip) != 1)
            {
                return;
            }

            for (; ; )
            {
                if (DisposableHelper.Replace(ref upstream, null))
                {
                    source.Subscribe(this);
                }

                if (Interlocked.Decrement(ref wip) == 0)
                {
                    break;
                }
            }
        }
    }
}
