using System;
using System.Reactive;
using System.Reactive.Concurrency;

namespace akarnokd.reactive_extensions
{
    public static class ReactiveExtensions
    {
        public static IObservable<T> ObserveOn<T>(
            this IObservable<T> source, 
            IScheduler scheduler, 
            bool delayError)
        {
            return null;
        }

        public static IObservable<R> ConcatMap<T, R>(
            this IObservable<T> source,
            Func<T, IObservable<R>> mapper
            )
        {
            return null;
        }
    }
}
