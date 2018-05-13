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
    internal abstract class RedoObserver<T> : BaseObserver<T, T>
    {
        readonly IObservable<T> source;

        int wip;

        internal RedoObserver(IObserver<T> downstream, IObservable<T> source) : base(downstream)
        {
            this.source = source;
        }

        public override void OnNext(T value)
        {
            downstream.OnNext(value);
        }

        internal void Next()
        {
            if (Interlocked.Increment(ref wip) != 1)
            {
                return;
            }

            for (; ; )
            {
                var sad = new SingleAssignmentDisposable();
                if (Interlocked.CompareExchange(ref upstream, sad, null) == null)
                {
                    sad.Disposable = source.Subscribe(this);
                }

                if (Interlocked.Decrement(ref wip) == 0)
                {
                    break;
                }
            }
        }
    }
}
