using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Completes after a specified time elapsed on the given scheduler.
    /// </summary>
    /// <remarks>Since 0.0.6</remarks>
    internal sealed class CompletableTimer : ICompletableSource
    {
        readonly TimeSpan time;

        readonly IScheduler scheduler;

        static readonly Func<IScheduler, ICompletableObserver, IDisposable> COMPLETE =
            (s, o) => { o.OnCompleted(); return DisposableHelper.EMPTY; };

        public CompletableTimer(TimeSpan time, IScheduler scheduler)
        {
            this.time = time;
            this.scheduler = scheduler;
        }

        public void Subscribe(ICompletableObserver observer)
        {
            var sad = new SingleAssignmentDisposable();
            observer.OnSubscribe(sad);

            sad.Disposable = scheduler.Schedule(observer, time, COMPLETE);
        }
    }
}
