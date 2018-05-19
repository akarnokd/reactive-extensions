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
    /// <see cref="ISingleSource{T}"/>s.
    /// </summary>
    /// <remarks>Since 0.0.5</remarks>
    public static class SingleSource
    {
        /// <summary>
        /// Test an observable by creating a TestObserver and subscribing 
        /// it to the <paramref name="source"/> single.
        /// </summary>
        /// <typeparam name="T">The value type of the source single.</typeparam>
        /// <param name="source">The source single to test.</param>
        /// <param name="dispose">Dispose the TestObserver before the subscription happens</param>
        /// <returns>The new TestObserver instance.</returns>
        public static TestObserver<T> Test<T>(this ISingleSource<T> source, bool dispose = false)
        {
            RequireNonNull(source, nameof(source));
            var to = new TestObserver<T>();
            if (dispose)
            {
                to.Dispose();
            }
            source.Subscribe(to);
            return to;
        }
        //-------------------------------------------------
        // Factory methods
        //-------------------------------------------------

        /// <summary>
        /// Creates a single that calls the specified <paramref name="onSubscribe"/>
        /// action with a <see cref="ISingleEmitter{T}"/> to allow
        /// bridging the callback world with the reactive world.
        /// </summary>
        /// <param name="onSubscribe">The action that is called with an emitter
        /// that can be used for signalling an item or error event.</param>
        /// <returns>The new single source instance</returns>
        public static ISingleSource<T> Create<T>(Action<ISingleEmitter<T>> onSubscribe)
        {
            RequireNonNull(onSubscribe, nameof(onSubscribe));

            return new SingleCreate<T>(onSubscribe);
        }

        /// <summary>
        /// Creates a failing completable that signals the specified error
        /// immediately.
        /// </summary>
        /// <typeparam name="T">The element type of the maybe.</typeparam>
        /// <param name="error">The error to signal.</param>
        /// <returns>The new single source instance.</returns>
        public static ISingleSource<T> Error<T>(Exception error)
        {
            RequireNonNull(error, nameof(error));

            return new SingleError<T>(error);
        }

        /// <summary>
        /// Creates a single that never terminates.
        /// </summary>
        /// <typeparam name="T">The element type of the single.</typeparam>
        /// <returns>The shared never-terminating single instance.</returns>
        public static ISingleSource<T> Never<T>()
        {
            return SingleNever<T>.INSTANCE;
        }

        /// <summary>
        /// Creates a single that succeeds with the given <paramref name="item"/>.
        /// </summary>
        /// <typeparam name="T">The type of the single item.</typeparam>
        /// <param name="item">The item to succeed with.</param>
        /// <returns>The new single source instance.</returns>
        /// <remarks>Since 0.0.9</remarks>
        public static ISingleSource<T> Just<T>(T item)
        {
            return new SingleJust<T>(item);
        }


        public static ISingleSource<T> FromFunc<T>(Func<T> action)
        {
            throw new NotImplementedException();
        }

        public static ISingleSource<T> FromTask<T>(Task<T> task)
        {
            throw new NotImplementedException();
        }

        public static ISingleSource<T> AmbAll<T>(this ISingleSource<T>[] sources)
        {
            throw new NotImplementedException();
        }

        public static ISingleSource<T> Amb<T>(params ISingleSource<T>[] sources)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> ConcatAll<T>(this ISingleSource<T>[] sources, bool delayErrors = false, int maxConcurrency = int.MaxValue)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> Concat<T>(params ISingleSource<T>[] sources)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> Concat<T>(IEnumerable<ISingleSource<T>> sources, bool delayErrors = false, int maxConcurrency = int.MaxValue)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> Concat<T>(int maxConcurrency, params ISingleSource<T>[] sources)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> Concat<T>(int maxConcurrency, bool delayErrors, params ISingleSource<T>[] sources)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> Concat<T>(this IObservable<ISingleSource<T>> sources, bool delayErrors = false, int maxConcurrency = int.MaxValue)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> ConcatEagerAll<T>(this ISingleSource<T>[] sources, bool delayErrors = false, int maxConcurrency = int.MaxValue)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> ConcatEager<T>(params ISingleSource<T>[] sources)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> ConcatEager<T>(IEnumerable<ISingleSource<T>> sources, bool delayErrors = false, int maxConcurrency = int.MaxValue)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> ConcatEager<T>(int maxConcurrency, params ISingleSource<T>[] sources)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> ConcatEager<T>(int maxConcurrency, bool delayErrors, params ISingleSource<T>[] sources)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> ConcatEager<T>(this IObservable<ISingleSource<T>> sources, bool delayErrors = false, int maxConcurrency = int.MaxValue)
        {
            throw new NotImplementedException();
        }

        public static ISingleSource<T> Defer<T>(Func<ISingleSource<T>> supplier)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> MergeAll<T>(this ISingleSource<T>[] sources, bool delayErrors = false, int maxConcurrency = int.MaxValue)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> Merge<T>(params ISingleSource<T>[] sources)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> Merge<T>(IEnumerable<ISingleSource<T>> sources, bool delayErrors = false, int maxConcurrency = int.MaxValue)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> Merge<T>(int maxConcurrency, params ISingleSource<T>[] sources)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> Merge<T>(int maxConcurrency, bool delayErrors, params ISingleSource<T>[] sources)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> Merge<T>(this IObservable<ISingleSource<T>> sources, bool delayErrors = false, int maxConcurrency = int.MaxValue)
        {
            throw new NotImplementedException();
        }

        public static ISingleSource<long> Timer(TimeSpan time, IScheduler scheduler)
        {
            throw new NotImplementedException();
        }

        public static ISingleSource<T> Using<T, S>(Func<S> stateFactory, Func<S, ISingleSource<T>> sourceSelector, Action<S> stateCleanup = null, bool eagerCleanup = false)
        {
            throw new NotImplementedException();
        }

        public static ISingleSource<R> Zip<T, R>(Func<T[], R> mapper, params ISingleSource<T>[] sources)
        {
            throw new NotImplementedException();
        }

        public static ISingleSource<R> Zip<T, R>(Func<T[], R> mapper, bool delayErrors, params ISingleSource<T>[] sources)
        {
            throw new NotImplementedException();
        }

        public static ISingleSource<R> Zip<T, R>(this ISingleSource<T>[] sources, Func<T[], R> mapper, bool delayErrors = false)
        {
            throw new NotImplementedException();
        }

        //-------------------------------------------------
        // Instance methods
        //-------------------------------------------------


        /// <summary>
        /// Applies a function to the source at assembly-time and returns the
        /// maybe source returned by this function.
        /// This allows creating reusable set of operators to be applied to maybe sources.
        /// </summary>
        /// <typeparam name="T">The upstream element type.</typeparam>
        /// <typeparam name="R">The element type of the returned maybe source.</typeparam>
        /// <param name="source">The upstream maybe source.</param>
        /// <param name="composer">The function called immediately on <paramref name="source"/>
        /// and should return a maybe source.</param>
        /// <returns>The maybe source returned by the <paramref name="composer"/> function.</returns>
        public static ISingleSource<R> Compose<T, R>(this ISingleSource<T> source, Func<ISingleSource<T>, ISingleSource<R>> composer)
        {
            return composer(source);
        }

        public static ISingleSource<T> DoOnSubscribe<T>(this ISingleSource<T> source, Action<IDisposable> handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            throw new NotImplementedException();
        }

        public static ISingleSource<T> DoOnDispose<T>(this ISingleSource<T> source, Action handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            throw new NotImplementedException();
        }

        public static ISingleSource<T> DoOnSuccess<T>(this ISingleSource<T> source, Action<T> handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            throw new NotImplementedException();
        }

        public static ISingleSource<T> DoOnError<T>(this ISingleSource<T> source, Action<Exception> handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            throw new NotImplementedException();
        }

        public static ISingleSource<T> DoOnTerminate<T>(this ISingleSource<T> source, Action handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            throw new NotImplementedException();
        }

        public static ISingleSource<T> DoAfterTerminate<T>(this ISingleSource<T> source, Action handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            throw new NotImplementedException();
        }

        public static ISingleSource<T> DoFinally<T>(this ISingleSource<T> source, Action handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            throw new NotImplementedException();
        }

        public static ISingleSource<T> Timeout<T>(this ISingleSource<T> source, TimeSpan time, IScheduler scheduler, ISingleSource<T> fallback = null)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static ISingleSource<T> OnErrorResumeNext<T>(this ISingleSource<T> source, ISingleSource<T> fallback)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static ISingleSource<T> OnErrorResumeNext<T>(this ISingleSource<T> source, Func<Exception, ISingleSource<T>> handler)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static IObservable<T> Repeat<T>(this ISingleSource<T> source, long times = long.MaxValue)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static IObservable<T> Repeat<T>(this ISingleSource<T> source, Func<bool> handler)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static IObservable<T> RepeatWhen<T, U>(this ISingleSource<T> source, Func<IObservable<object>, IObservable<U>> handler)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static ISingleSource<T> Retry<T>(this ISingleSource<T> source, long times = long.MaxValue)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static ISingleSource<T> Retry<T>(this ISingleSource<T> source, Func<Exception, long, bool> handler)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static ISingleSource<T> RetryWhen<T, U>(this ISingleSource<T> source, Func<IObservable<Exception>, IObservable<U>> handler)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static ISingleSource<T> SubscribeOn<T>(this ISingleSource<T> source, IScheduler scheduler)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static ISingleSource<T> ObserveOn<T>(this ISingleSource<T> source, IScheduler scheduler)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static ISingleSource<T> UnsubscribeOn<T>(this ISingleSource<T> source, IScheduler scheduler)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static ISingleSource<T> OnTerminateDetach<T>(this ISingleSource<T> source)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static ISingleSource<T> Cache<T>(this ISingleSource<T> source, Action<IDisposable> cancel = null)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static ISingleSource<T> Delay<T>(this ISingleSource<T> source, TimeSpan time, IScheduler scheduler)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static ISingleSource<T> DelaySubscription<T>(this ISingleSource<T> source, TimeSpan time, IScheduler scheduler)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static ISingleSource<T> DelaySubscription<T>(this ISingleSource<T> source, ICompletableSource other)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static ISingleSource<T> TakeUntil<T>(this ISingleSource<T> source, ICompletableSource other)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static ISingleSource<T> TakeUntil<T, U>(this ISingleSource<T> source, IObservable<U> other)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static ISingleSource<R> Map<T, R>(this ISingleSource<T> source, Func<T, R> mapper)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));

            throw new NotImplementedException();
        }


        public static ISingleSource<R> FlatMap<T, R>(this ISingleSource<T> source, Func<T, ISingleSource<R>> mapper)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));

            throw new NotImplementedException();
        }

        public static IObservable<R> FlatMap<T, R>(this ISingleSource<T> source, Func<T, IEnumerable<R>> mapper)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));

            throw new NotImplementedException();
        }

        public static IObservable<R> FlatMap<T, R>(this ISingleSource<T> source, Func<T, IObservable<R>> mapper)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));

            throw new NotImplementedException();
        }

        public static IMaybeSource<R> FlatMap<T, R>(this ISingleSource<T> source, Func<T, IMaybeSource<R>> mapper)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));

            throw new NotImplementedException();
        }

        public static Task<T> ToTask<T>(this ISingleSource<T> source, CancellationTokenSource cts = null)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        /// <summary>
        /// Hides the identity and disposable of the upstream from
        /// the downstream.
        /// </summary>
        /// <param name="source">The single source to hide.</param>
        /// <returns>The new single source instance.</returns>
        /// <remarks>Since 0.0.9</remarks>
        public static ISingleSource<T> Hide<T>(this ISingleSource<T> source)
        {
            RequireNonNull(source, nameof(source));

            return new SingleHide<T>(source);
        }

        // ------------------------------------------------
        // Leaving the reactive world
        // ------------------------------------------------

        public static void SubscribeSafe<T>(this ISingleSource<T> source, ISingleObserver<T> observer)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }


        /// <summary>
        /// Subscribe to this maybe source and call the
        /// appropriate action depending on the success or terminal signal received.
        /// </summary>
        /// <param name="source">The maybee source to observe.</param>
        /// <param name="onSuccess">Called with the success item when the maybe source succeeds.</param>
        /// <param name="onError">Called with the exception when the maybe source terminates with an error.</param>
        /// <returns>The disposable that allows cancelling the source.</returns>
        /// <remarks>Since 0.0.9</remarks>
        public static IDisposable Subscribe<T>(this ISingleSource<T> source, Action<T> onSuccess = null, Action<Exception> onError = null)
        {
            RequireNonNull(source, nameof(source));

            var parent = new SingleLambdaObserver<T>(onSuccess, onError);
            source.Subscribe(parent);
            return parent;
        }

        public static void BlockingSubscribe<T>(this ISingleSource<T> source, ISingleObserver<T> observer)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static void BlockingSubscribe<T>(this ISingleSource<T> source, Action<T> onSuccess, Action<Exception> onError = null, Action<IDisposable> onSubscribe = null)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static T Wait<T>(this ISingleSource<T> source, long timeoutMillis = long.MinValue, CancellationTokenSource cts = null)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        /// <summary>
        /// Subscribes a single observer (subclass) to the single
        /// source and returns this observer instance as well.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <typeparam name="U">The observer type.</typeparam>
        /// <param name="source">The single source to subscribe to.</param>
        /// <param name="observer">The single observer (subclass) to subscribe with.</param>
        /// <returns>The <paramref name="observer"/> provided as parameter.</returns>
        /// <remarks>Since 0.0.9</remarks>
        public static U SubscribeWith<T, U>(this ISingleSource<T> source, U observer) where U : ISingleObserver<T>
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(observer, nameof(observer));

            source.Subscribe(observer);
            return observer;
        }

        //-------------------------------------------------
        // Interoperation with other reactive types
        //-------------------------------------------------


        public static IObservable<R> ConcatMap<T, R>(this IObservable<T> source, Func<T, ISingleSource<T>> mapper, bool delayErrors = false)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));

            throw new NotImplementedException();
        }

        public static IObservable<R> FlatMap<T, R>(this IObservable<T> source, Func<T, ISingleSource<R>> mapper, bool delayErrors = false, int maxConcurrency = int.MaxValue)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));

            throw new NotImplementedException();
        }

        public static IMaybeSource<R> FlatMap<T, R>(this ISingleSource<T> source, Func<T, IMaybeSource<T>> mapper)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));

            throw new NotImplementedException();
        }

        /// <summary>
        /// Maps the success value of the upstream single source
        /// into a completable source and signals its terminal
        /// events to the downstream.
        /// </summary>
        /// <typeparam name="T">The element type of the single source.</typeparam>
        /// <param name="source">The single source to map into a completable source.</param>
        /// <param name="mapper">The function that takes the success value from the upstream
        /// and returns a completable source to subscribe to and relay terminal events of.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.10</remarks>
        public static ICompletableSource FlatMap<T>(this ISingleSource<T> source, Func<T, ICompletableSource> mapper)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));

            return new CompletableFlatMapSingle<T>(source, mapper);
        }

        public static IObservable<R> SwitchMap<T, R>(this IObservable<T> source, Func<T, ISingleSource<T>> mapper, bool delayErrors = false)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));

            throw new NotImplementedException();
        }

        /// <summary>
        /// Ignores the success signal of the single source and
        /// completes the downstream completable observer instead.
        /// </summary>
        /// <typeparam name="T">The success value type of the source.</typeparam>
        /// <param name="source">The source to ignore the success value of.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.9</remarks>
        public static ICompletableSource IgnoreElement<T>(this ISingleSource<T> source)
        {
            RequireNonNull(source, nameof(source));

            return new CompletableIgnoreElementSingle<T>(source);
        }

        public static ISingleSource<T> FirstOrError<T>(this IObservable<T> source)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static ISingleSource<T> SingleOrError<T>(this IObservable<T> source)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static ISingleSource<T> LastOrError<T>(this IObservable<T> source)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static ISingleSource<T> ElementAtOrError<T>(this IObservable<T> source, long index)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static ISingleSource<T> FirstOrDefault<T>(this IObservable<T> source, T defaultItem)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static ISingleSource<T> SingleOrDefault<T>(this IObservable<T> source, T defaultItem)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static ISingleSource<T> LastOrDefault<T>(this IObservable<T> source, T defaultItem)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static ISingleSource<T> ElementAtOrDefault<T>(this IObservable<T> source, long index, T defaultItem)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

    }
}
