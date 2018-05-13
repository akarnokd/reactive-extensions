using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static akarnokd.reactive_extensions.ValidationHelper;


namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Extension and factory methods for dealing with
    /// <see cref="ICompletableSource"/>s.
    /// </summary>
    /// <remarks>Since 0.0.5</remarks>
    public static class CompletableSource
    {
        /// <summary>
        /// Test an observable by creating a TestObserver and subscribing 
        /// it to the <paramref name="source"/> completable.
        /// </summary>
        /// <param name="source">The source completable to test.</param>
        /// <returns>The new TestObserver instance.</returns>
        public static TestObserver<object> Test(this ICompletableSource source)
        {
            RequireNonNull(source, nameof(source));
            var to = new TestObserver<object>();
            source.Subscribe(to);
            return to;
        }

        //-------------------------------------------------
        // Factory methods
        //-------------------------------------------------

        /// <summary>
        /// Creates a completable that calls the specified <paramref name="onSubscribe"/>
        /// action with a <see cref="ICompletableEmitter"/> to allow
        /// bridging the callback world with the reactive world.
        /// </summary>
        /// <param name="onSubscribe">The action that is called with an emitter
        /// that can be used for signalling a completion or error event.</param>
        /// <returns>The new completable instance</returns>
        public static ICompletableSource Create(Action<ICompletableEmitter> onSubscribe)
        {
            RequireNonNull(onSubscribe, nameof(onSubscribe));

            return new CompletableCreate(onSubscribe);
        }

        /// <summary>
        /// Creates an empty completable that completes immediately.
        /// </summary>
        /// <returns>The shared empty completable instance.</returns>
        public static ICompletableSource Empty()
        {
            return CompletableEmpty.INSTANCE;
        }

        /// <summary>
        /// Creates a failing completable that signals the specified error
        /// immediately.
        /// </summary>
        /// <param name="error">The error to signal.</param>
        /// <returns>The new completable instance.</returns>
        public static ICompletableSource Error(Exception error)
        {
            RequireNonNull(error, nameof(error));

            return new CompletableError(error);
        }

        public static ICompletableSource FromAction(Action action)
        {
            throw new NotImplementedException();
        }

        public static ICompletableSource FromTask(Task task)
        {
            return task.ToCompletable();
        }

        public static ICompletableSource FromTask<T>(Task<T> task)
        {
            return task.ToCompletable();
        }

        public static ICompletableSource ConcatAll(this ICompletableSource[] sources, bool delayErrors = false, int maxConcurrency = int.MaxValue)
        {
            throw new NotImplementedException();
        }

        public static ICompletableSource Concat(params ICompletableSource[] sources)
        {
            throw new NotImplementedException();
        }

        public static ICompletableSource Concat(IEnumerable<ICompletableSource> sources, bool delayErrors = false, int maxConcurrency = int.MaxValue)
        {
            throw new NotImplementedException();
        }

        public static ICompletableSource Concat(int maxConcurrency, params ICompletableSource[] sources)
        {
            throw new NotImplementedException();
        }

        public static ICompletableSource Concat(int maxConcurrency, bool delayErrors, params ICompletableSource[] sources)
        {
            throw new NotImplementedException();
        }

        public static ICompletableSource Concat(this IObservable<ICompletableSource> sources, bool delayErrors = false, int maxConcurrency = int.MaxValue)
        {
            throw new NotImplementedException();
        }

        public static ICompletableSource ConcatEagerAll(this ICompletableSource[] sources, bool delayErrors = false, int maxConcurrency = int.MaxValue)
        {
            throw new NotImplementedException();
        }

        public static ICompletableSource ConcatEager(params ICompletableSource[] sources)
        {
            throw new NotImplementedException();
        }

        public static ICompletableSource ConcatEager(IEnumerable<ICompletableSource> sources, bool delayErrors = false, int maxConcurrency = int.MaxValue)
        {
            throw new NotImplementedException();
        }

        public static ICompletableSource ConcatEager(int maxConcurrency, params ICompletableSource[] sources)
        {
            throw new NotImplementedException();
        }

        public static ICompletableSource ConcatEager(int maxConcurrency, bool delayErrors, params ICompletableSource[] sources)
        {
            throw new NotImplementedException();
        }

        public static ICompletableSource ConcatEager(this IObservable<ICompletableSource> sources, bool delayErrors = false, int maxConcurrency = int.MaxValue)
        {
            throw new NotImplementedException();
        }

        public static ICompletableSource Defer(Func<ICompletableSource> supplier)
        {
            throw new NotImplementedException();
        }

        public static ICompletableSource MergeAll(this ICompletableSource[] sources, bool delayErrors = false, int maxConcurrency = int.MaxValue)
        {
            throw new NotImplementedException();
        }

        public static ICompletableSource Merge(params ICompletableSource[] sources)
        {
            throw new NotImplementedException();
        }

        public static ICompletableSource Merge(IEnumerable<ICompletableSource> sources, bool delayErrors = false, int maxConcurrency = int.MaxValue)
        {
            throw new NotImplementedException();
        }

        public static ICompletableSource Merge(int maxConcurrency, params ICompletableSource[] sources)
        {
            throw new NotImplementedException();
        }

        public static ICompletableSource Merge(int maxConcurrency, bool delayErrors, params ICompletableSource[] sources)
        {
            throw new NotImplementedException();
        }

        public static ICompletableSource Merge(this IObservable<ICompletableSource> sources, bool delayErrors = false, int maxConcurrency = int.MaxValue)
        {
            throw new NotImplementedException();
        }

        public static ICompletableSource Using<S>(Func<S> stateFactory, Func<S, ICompletableSource> sourceSelector, Action<S> stateCleanup = null, bool eagerCleanup = false)
        {
            throw new NotImplementedException();
        }

        //-------------------------------------------------
        // Instance methods
        //-------------------------------------------------

        /// <summary>
        /// Applies a function to the source at assembly-time and returns the
        /// completable source returned by this function.
        /// This allows creating reusable set of operators to be applied to completable sources.
        /// </summary>
        /// <param name="source">The upstream completable source.</param>
        /// <param name="composer">The function called immediately on <paramref name="source"/>
        /// and should return a completable source.</param>
        /// <returns>The completable source returned by the <paramref name="composer"/> function.</returns>
        public static ICompletableSource Compose(this ICompletableSource source, Func<ICompletableSource, ICompletableSource> composer)
        {
            return composer(source);
        }

        public static ICompletableSource DoOnSubscribe(this ICompletableSource source, Action<IDisposable> handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            throw new NotImplementedException();
        }

        public static ICompletableSource DoOnDispose(this ICompletableSource source, Action handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            throw new NotImplementedException();
        }

        public static ICompletableSource DoOnCompleted(this ICompletableSource source, Action handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            throw new NotImplementedException();
        }

        public static ICompletableSource DoOnError(this ICompletableSource source, Action<Exception> handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            throw new NotImplementedException();
        }

        public static ICompletableSource DoOnTerminate(this ICompletableSource source, Action handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            throw new NotImplementedException();
        }

        public static ICompletableSource DoAfterTerminate(this ICompletableSource source, Action handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            throw new NotImplementedException();
        }

        public static ICompletableSource DoFinally(this ICompletableSource source, Action handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            throw new NotImplementedException();
        }

        public static ICompletableSource Timeout(this ICompletableSource source, TimeSpan time, IScheduler scheduler, ICompletableSource fallback = null)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static ICompletableSource OnErrorComplete(this ICompletableSource source)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static ICompletableSource OnErrorResumeNext(this ICompletableSource source, ICompletableSource fallback)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static ICompletableSource OnErrorResumeNext(this ICompletableSource source, Func<Exception, ICompletableSource> handler)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static ICompletableSource Repeat(this ICompletableSource source, long times = long.MaxValue)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static ICompletableSource Repeat(this ICompletableSource source, Func<bool> handler)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static ICompletableSource RepeatWhen<U>(this ICompletableSource source, Func<IObservable<object>, IObservable<U>> handler)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static ICompletableSource Retry(this ICompletableSource source, long times = long.MaxValue)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static ICompletableSource Retry(this ICompletableSource source, Func<Exception, long, bool> handler)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static ICompletableSource RetryWhen<U>(this ICompletableSource source, Func<IObservable<Exception>, IObservable<U>> handler)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static ICompletableSource SubscribeOn(this ICompletableSource source, IScheduler scheduler)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static ICompletableSource ObserveOn(this ICompletableSource source, IScheduler scheduler)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static ICompletableSource UnsubscribeOn(this ICompletableSource source, IScheduler scheduler)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static ICompletableSource OnTerminateDetach(this ICompletableSource source)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        // ------------------------------------------------
        // Leaving the reactive world
        // ------------------------------------------------

        public static void SubscribeSafe(this ICompletableSource source, ICompletableObserver observer)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static IDisposable Subscribe(this ICompletableSource source, Action onCompleted = null, Action<Exception> onError = null)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static void BlockingSubscribe(this ICompletableSource source, ICompletableObserver observer)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static void BlockingSubscribe(this ICompletableSource source, Action onCompleted = null, Action<Exception> onError = null, Action<IDisposable> onSubscribe = null)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static void Wait(this ICompletableSource source, long timeoutMillis = long.MinValue, CancellationTokenSource cts = null)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        //-------------------------------------------------
        // Interoperation with other reactive types
        //-------------------------------------------------

        public static IObservable<T> AndThen<T>(this ICompletableSource source, IObservable<T> next)
        {
            throw new NotImplementedException();
        }

        public static ICompletableSource AndThen(this ICompletableSource source, ICompletableSource next)
        {
            throw new NotImplementedException();
        }

        public static ISingleSource<T> AndThen<T>(this ICompletableSource source, ISingleSource<T> next)
        {
            throw new NotImplementedException();
        }

        public static IMaybeSource<T> AndThen<T>(this ICompletableSource source, IMaybeSource<T> next)
        {
            throw new NotImplementedException();
        }

        //-------------------------------------------------
        // Conversions from other reactive types
        //-------------------------------------------------

        public static ICompletableSource IgnoreAllElements<T>(this IObservable<T> source)
        {
            throw new NotImplementedException();
        }

        public static ICompletableSource IgnoreElement<T>(this ISingleSource<T> source)
        {
            throw new NotImplementedException();
        }

        public static ICompletableSource IgnoreElement<T>(this IMaybeSource<T> source)
        {
            throw new NotImplementedException();
        }

        public static ICompletableSource ToCompletable(this Task task)
        {
            RequireNonNull(task, nameof(task));

            throw new NotImplementedException();
        }

        public static ICompletableSource ToCompletable<T>(this Task<T> task)
        {
            RequireNonNull(task, nameof(task));

            throw new NotImplementedException();
        }

        public static IObservable<T> ToObservable<T>(this ICompletableSource source)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static ISingleSource<T> ToSingle<T>(this ICompletableSource source, T successItem)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static IMaybeSource<T> ToMaybe<T>(this ICompletableSource source)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static IMaybeSource<T> ToMaybe<T>(this ICompletableSource source, T successItem)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static Task ToTask(this ICompletableSource source, CancellationTokenSource cts = null)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static ICompletableSource ConcatMap<T>(this IObservable<T> source, Func<T, ICompletableSource> mapper, bool delayErrors = false)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));

            throw new NotImplementedException();
        }

        public static ICompletableSource FlatMap<T>(this IObservable<T> source, Func<T, ICompletableSource> mapper, bool delayErrors = false, int maxConcurrency = int.MaxValue)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));

            throw new NotImplementedException();
        }

        public static ICompletableSource FlatMap<T>(this ISingleSource<T> source, Func<T, ICompletableSource> mapper)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));

            throw new NotImplementedException();
        }

        public static ICompletableSource FlatMap<T>(this IMaybeSource<T> source, Func<T, ICompletableSource> mapper)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));

            throw new NotImplementedException();
        }

        public static ICompletableSource SwitchMap<T>(this IObservable<T> source, Func<T, ICompletableSource> mapper, bool delayErrors = false)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));

            throw new NotImplementedException();
        }
    }
}
