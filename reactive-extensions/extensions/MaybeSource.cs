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
    /// <see cref="IMaybeSource{T}"/>s.
    /// </summary>
    /// <remarks>Since 0.0.5</remarks>
    public static class MaybeSource
    {
        /// <summary>
        /// Test an observable by creating a TestObserver and subscribing 
        /// it to the <paramref name="source"/> maybe.
        /// </summary>
        /// <typeparam name="T">The value type of the source maybe.</typeparam>
        /// <param name="source">The source maybe to test.</param>
        /// <param name="dispose">Dispose the TestObserver before the subscription happens</param>
        /// <returns>The new TestObserver instance.</returns>
        public static TestObserver<T> Test<T>(this IMaybeSource<T> source, bool dispose = false)
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
        /// Creates a maybe that calls the specified <paramref name="onSubscribe"/>
        /// action with a <see cref="IMaybeEmitter{T}"/> to allow
        /// bridging the callback world with the reactive world.
        /// </summary>
        /// <param name="onSubscribe">The action that is called with an emitter
        /// that can be used for signaling an item, completion or error event.</param>
        /// <returns>The new maybe instance</returns>
        public static IMaybeSource<T> Create<T>(Action<IMaybeEmitter<T>> onSubscribe)
        {
            RequireNonNull(onSubscribe, nameof(onSubscribe));

            return new MaybeCreate<T>(onSubscribe);
        }

        /// <summary>
        /// Creates an empty completable that completes immediately.
        /// </summary>
        /// <typeparam name="T">The element type of the maybe.</typeparam>
        /// <returns>The shared empty maybe source instance.</returns>
        public static IMaybeSource<T> Empty<T>()
        {
            return MaybeEmpty<T>.INSTANCE;
        }

        /// <summary>
        /// Creates a failing completable that signals the specified error
        /// immediately.
        /// </summary>
        /// <typeparam name="T">The element type of the maybe.</typeparam>
        /// <param name="error">The error to signal.</param>
        /// <returns>The new maybe source instance.</returns>
        public static IMaybeSource<T> Error<T>(Exception error)
        {
            RequireNonNull(error, nameof(error));

            return new MaybeError<T>(error);
        }

        /// <summary>
        /// Creates a maybe that never terminates.
        /// </summary>
        /// <typeparam name="T">The element type of the maybe.</typeparam>
        /// <returns>The shared never-terminating maybe instance.</returns>
        public static IMaybeSource<T> Never<T>()
        {
            return MaybeNever<T>.INSTANCE;
        }

        /// <summary>
        /// Creates a maybe that succeeds with the given <paramref name="item"/>.
        /// </summary>
        /// <typeparam name="T">The type of the single item.</typeparam>
        /// <param name="item">The item to succeed with.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.9</remarks>
        public static IMaybeSource<T> Just<T>(T item)
        {
            return new MaybeJust<T>(item);
        }

        /// <summary>
        /// Wraps and calls the given action for each individual
        /// maybe observer then completes or fails the observer
        /// depending on the action completes normally or threw an exception.
        /// </summary>
        /// <param name="action">The action to invoke for each individual completable observer.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IMaybeSource<T> FromAction<T>(Action action)
        {
            RequireNonNull(action, nameof(action));

            return new MaybeFromAction<T>(action);
        }

        /// <summary>
        /// Wraps and runs a function for each incoming
        /// maybe observer and signals the value returned
        /// by the function as the success event.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="func">The function to call for each observer.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IMaybeSource<T> FromFunc<T>(Func<T> func)
        {
            RequireNonNull(func, nameof(func));

            return new MaybeFromFunc<T>(func);
        }

        /// <summary>
        /// Creates a maybe source that completes or fails
        /// its observers when the given (possibly still ongoing)
        /// task terminates.
        /// </summary>
        /// <param name="task">The task to wrap.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IMaybeSource<T> FromTask<T>(Task task)
        {
            return task.ToMaybe<T>();
        }

        /// <summary>
        /// Creates a maybe source that succeeds or fails
        /// its observers when the given (possibly still ongoing)
        /// task terminates.
        /// </summary>
        /// <param name="task">The task to wrap.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IMaybeSource<T> FromTask<T>(Task<T> task)
        {
            return task.ToMaybe();
        }

        public static IMaybeSource<T> AmbAll<T>(this IMaybeSource<T>[] sources)
        {
            throw new NotImplementedException();
        }

        public static IMaybeSource<T> Amb<T>(params IMaybeSource<T>[] sources)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> ConcatAll<T>(this IMaybeSource<T>[] sources, bool delayErrors = false, int maxConcurrency = int.MaxValue)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> Concat<T>(params IMaybeSource<T>[] sources)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> Concat<T>(IEnumerable<IMaybeSource<T>> sources, bool delayErrors = false, int maxConcurrency = int.MaxValue)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> Concat<T>(int maxConcurrency, params IMaybeSource<T>[] sources)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> Concat<T>(int maxConcurrency, bool delayErrors, params IMaybeSource<T>[] sources)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> Concat<T>(this IObservable<IMaybeSource<T>> sources, bool delayErrors = false, int maxConcurrency = int.MaxValue)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> ConcatEagerAll<T>(this IMaybeSource<T>[] sources, bool delayErrors = false, int maxConcurrency = int.MaxValue)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> ConcatEager<T>(params IMaybeSource<T>[] sources)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> ConcatEager<T>(IEnumerable<IMaybeSource<T>> sources, bool delayErrors = false, int maxConcurrency = int.MaxValue)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> ConcatEager<T>(int maxConcurrency, params IMaybeSource<T>[] sources)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> ConcatEager<T>(int maxConcurrency, bool delayErrors, params IMaybeSource<T>[] sources)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> ConcatEager<T>(this IObservable<IMaybeSource<T>> sources, bool delayErrors = false, int maxConcurrency = int.MaxValue)
        {
            throw new NotImplementedException();
        }

        public static IMaybeSource<T> Defer<T>(Func<IMaybeSource<T>> supplier)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> MergeAll<T>(this IMaybeSource<T>[] sources, bool delayErrors = false, int maxConcurrency = int.MaxValue)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> Merge<T>(params IMaybeSource<T>[] sources)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> Merge<T>(IEnumerable<IMaybeSource<T>> sources, bool delayErrors = false, int maxConcurrency = int.MaxValue)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> Merge<T>(int maxConcurrency, params IMaybeSource<T>[] sources)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> Merge<T>(int maxConcurrency, bool delayErrors, params IMaybeSource<T>[] sources)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> Merge<T>(this IObservable<IMaybeSource<T>> sources, bool delayErrors = false, int maxConcurrency = int.MaxValue)
        {
            throw new NotImplementedException();
        }

        public static IMaybeSource<long> Timer(TimeSpan time, IScheduler scheduler)
        {
            throw new NotImplementedException();
        }

        public static IMaybeSource<T> Using<T, S>(Func<S> stateFactory, Func<S, IMaybeSource<T>> sourceSelector, Action<S> stateCleanup = null, bool eagerCleanup = false)
        {
            throw new NotImplementedException();
        }

        public static IMaybeSource<R> Zip<T, R>(Func<T[], R> mapper, params IMaybeSource<T>[] sources)
        {
            throw new NotImplementedException();
        }

        public static IMaybeSource<R> Zip<T, R>(Func<T[], R> mapper, bool delayErrors, params IMaybeSource<T>[] sources)
        {
            throw new NotImplementedException();
        }

        public static IMaybeSource<R> Zip<T, R>(this IMaybeSource<T>[] sources, Func<T[], R> mapper, bool delayErrors = false)
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
        public static IMaybeSource<R> Compose<T, R>(this IMaybeSource<T> source, Func<IMaybeSource<T>, IMaybeSource<R>> composer)
        {
            return composer(source);
        }

        public static IMaybeSource<T> DoOnSubscribe<T>(this IMaybeSource<T> source, Action<IDisposable> handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            throw new NotImplementedException();
        }

        public static IMaybeSource<T> DoOnDispose<T>(this IMaybeSource<T> source, Action handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            throw new NotImplementedException();
        }

        public static IMaybeSource<T> DoOnCompleted<T>(this IMaybeSource<T> source, Action handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            throw new NotImplementedException();
        }

        public static IMaybeSource<T> DoOnSuccess<T>(this IMaybeSource<T> source, Action<T> handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            throw new NotImplementedException();
        }

        public static IMaybeSource<T> DoOnError<T>(this IMaybeSource<T> source, Action<Exception> handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            throw new NotImplementedException();
        }

        public static IMaybeSource<T> DoOnTerminate<T>(this IMaybeSource<T> source, Action handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            throw new NotImplementedException();
        }

        public static IMaybeSource<T> DoAfterTerminate<T>(this IMaybeSource<T> source, Action handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            throw new NotImplementedException();
        }

        public static IMaybeSource<T> DoFinally<T>(this IMaybeSource<T> source, Action handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            throw new NotImplementedException();
        }

        public static IMaybeSource<T> Timeout<T>(this IMaybeSource<T> source, TimeSpan time, IScheduler scheduler, IMaybeSource<T> fallback = null)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static IMaybeSource<T> OnErrorComplete<T>(this IMaybeSource<T> source)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static IMaybeSource<T> OnErrorResumeNext<T>(this IMaybeSource<T> source, IMaybeSource<T> fallback)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static IMaybeSource<T> OnErrorResumeNext<T>(this IMaybeSource<T> source, Func<Exception, IMaybeSource<T>> handler)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static IObservable<T> Repeat<T>(this IMaybeSource<T> source, long times = long.MaxValue)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static IObservable<T> Repeat<T>(this IMaybeSource<T> source, Func<bool> handler)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static IObservable<T> RepeatWhen<T, U>(this IMaybeSource<T> source, Func<IObservable<object>, IObservable<U>> handler)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static IMaybeSource<T> Retry<T>(this IMaybeSource<T> source, long times = long.MaxValue)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static IMaybeSource<T> Retry<T>(this IMaybeSource<T> source, Func<Exception, long, bool> handler)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static IMaybeSource<T> RetryWhen<T, U>(this IMaybeSource<T> source, Func<IObservable<Exception>, IObservable<U>> handler)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static IMaybeSource<T> SubscribeOn<T>(this IMaybeSource<T> source, IScheduler scheduler)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static IMaybeSource<T> ObserveOn<T>(this IMaybeSource<T> source, IScheduler scheduler)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static IMaybeSource<T> UnsubscribeOn<T>(this IMaybeSource<T> source, IScheduler scheduler)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static IMaybeSource<T> OnTerminateDetach<T>(this IMaybeSource<T> source)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static IMaybeSource<T> Cache<T>(this IMaybeSource<T> source, Action<IDisposable> cancel = null)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static IMaybeSource<T> Delay<T>(this IMaybeSource<T> source, TimeSpan time, IScheduler scheduler)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static IMaybeSource<T> DelaySubscription<T>(this IMaybeSource<T> source, TimeSpan time, IScheduler scheduler)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static IMaybeSource<T> DelaySubscription<T>(this IMaybeSource<T> source, ICompletableSource other)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static IMaybeSource<T> TakeUntil<T>(this IMaybeSource<T> source, ICompletableSource other)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static IMaybeSource<T> TakeUntil<T, U>(this IMaybeSource<T> source, IObservable<U> other)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        /// <summary>
        /// Maps the success value of the upstream maybe source
        /// into another value.
        /// </summary>
        /// <typeparam name="T">The upstream value type.</typeparam>
        /// <typeparam name="R">The result value type</typeparam>
        /// <param name="source">The upstream maybe source to map.</param>
        /// <param name="mapper">The function receiving the upstream success
        /// item and returns a new success item for the downstream.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IMaybeSource<R> Map<T, R>(this IMaybeSource<T> source, Func<T, R> mapper)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));

            return new MaybeMap<T, R>(source, mapper);
        }

        /// <summary>
        /// Tests the upstream's success value via a predicate
        /// and relays it if the predicate returns false,
        /// completed the downstream otherwise.
        /// </summary>
        /// <typeparam name="T">The upstream value type.</typeparam>
        /// <param name="source">The upstream maybe source to map.</param>
        /// <param name="predicate">The function that receives the upstream
        /// success item and should return true if the success
        /// value should be passed along.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11</remarks>
        public static IMaybeSource<T> Filter<T>(this IMaybeSource<T> source, Func<T, bool> predicate)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(predicate, nameof(predicate));

            return new MaybeFilter<T>(source, predicate);
        }

        public static IMaybeSource<R> FlatMap<T, R>(this IMaybeSource<T> source, Func<T, IMaybeSource<R>> mapper)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));

            throw new NotImplementedException();
        }

        public static IObservable<R> FlatMap<T, R>(this IMaybeSource<T> source, Func<T, IEnumerable<R>> mapper)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));

            throw new NotImplementedException();
        }

        public static IObservable<R> FlatMap<T, R>(this IMaybeSource<T> source, Func<T, IObservable<R>> mapper)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));

            throw new NotImplementedException();
        }

        public static ISingleSource<R> FlatMap<T, R>(this IMaybeSource<T> source, Func<T, ISingleSource<R>> mapper)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));

            throw new NotImplementedException();
        }

        public static ISingleSource<T> DefaultIfEmpty<T>(this IMaybeSource<T> source, T defaultItem)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static IMaybeSource<T> SwitchIfEmpty<T>(this IMaybeSource<T> source, params IMaybeSource<T>[] fallbacks)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static IMaybeSource<T> SwitchIfEmpty<T>(this IMaybeSource<T> source, ISingleSource<T> fallback)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        /// <summary>
        /// Hides the identity and disposable of the upstream from
        /// the downstream.
        /// </summary>
        /// <param name="source">The maybe source to hide.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.9</remarks>
        public static IMaybeSource<T> Hide<T>(this IMaybeSource<T> source)
        {
            RequireNonNull(source, nameof(source));

            return new MaybeHide<T>(source);
        }


        // ------------------------------------------------
        // Leaving the reactive world
        // ------------------------------------------------

        public static void SubscribeSafe<T>(this IMaybeSource<T> source, IMaybeObserver<T> observer)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        /// <summary>
        /// Subscribe to this maybe source and call the
        /// appropriate action depending on the success or terminal signal received.
        /// </summary>
        /// <param name="source">The maybe source to observe.</param>
        /// <param name="onSuccess">Called with the success item when the maybe source succeeds.</param>
        /// <param name="onError">Called with the exception when the maybe source terminates with an error.</param>
        /// <param name="onCompleted">Called when the maybe source completes normally.</param>
        /// <returns>The disposable that allows canceling the source.</returns>
        /// <remarks>Since 0.0.9</remarks>
        public static IDisposable Subscribe<T>(this IMaybeSource<T> source, Action<T> onSuccess = null, Action<Exception> onError = null, Action onCompleted = null)
        {
            RequireNonNull(source, nameof(source));

            var parent = new MaybeLambdaObserver<T>(onSuccess, onError, onCompleted);
            source.Subscribe(parent);
            return parent;
        }

        public static void BlockingSubscribe<T>(this IMaybeSource<T> source, IMaybeObserver<T> observer)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static void BlockingSubscribe<T>(this IMaybeSource<T> source, Action<T> onSuccess, Action<Exception> onError = null, Action onCompleted = null, Action <IDisposable> onSubscribe = null)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static void Wait<T>(this IMaybeSource<T> source, long timeoutMillis = long.MinValue, CancellationTokenSource cts = null)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static bool Wait<T>(this IMaybeSource<T> source, out T result, long timeoutMillis = long.MinValue, CancellationTokenSource cts = null)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static Task<T> ToTask<T>(this IMaybeSource<T> source, CancellationTokenSource cts = null)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        /// <summary>
        /// Subscribes a maybe observer (subclass) to the maybe
        /// source and returns this observer instance as well.
        /// </summary>
        /// <typeparam name="T">The success value type.</typeparam>
        /// <typeparam name="U">The observer type.</typeparam>
        /// <param name="source">The maybe source to subscribe to.</param>
        /// <param name="observer">The maybe observer (subclass) to subscribe with.</param>
        /// <returns>The <paramref name="observer"/> provided as parameter.</returns>
        /// <remarks>Since 0.0.9</remarks>
        public static U SubscribeWith<T, U>(this IMaybeSource<T> source, U observer) where U : IMaybeObserver<T>
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(observer, nameof(observer));

            source.Subscribe(observer);
            return observer;
        }

        //-------------------------------------------------
        // Interoperation with other reactive types
        //-------------------------------------------------

        public static IObservable<R> ConcatMap<T, R>(this IObservable<T> source, Func<T, IMaybeSource<T>> mapper, bool delayErrors = false)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));

            throw new NotImplementedException();
        }

        public static IObservable<R> FlatMap<T, R>(this IObservable<T> source, Func<T, IMaybeSource<R>> mapper, bool delayErrors = false, int maxConcurrency = int.MaxValue)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));

            throw new NotImplementedException();
        }

        public static ISingleSource<R> FlatMap<T, R>(this IMaybeSource<T> source, Func<T, ISingleSource<T>> mapper)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));

            throw new NotImplementedException();
        }

        /// <summary>
        /// Maps the success value of the upstream maybe source
        /// into a completable source and signals its terminal
        /// events to the downstream.
        /// </summary>
        /// <typeparam name="T">The element type of the maybe source.</typeparam>
        /// <param name="source">The maybe source to map into a completable source.</param>
        /// <param name="mapper">The function that takes the success value from the upstream
        /// and returns a completable source to subscribe to and relay terminal events of.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.10</remarks>
        public static ICompletableSource FlatMap<T>(this IMaybeSource<T> source, Func<T, ICompletableSource> mapper)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));

            return new CompletableFlatMapMaybe<T>(source, mapper);
        }

        public static IObservable<R> SwitchMap<T, R>(this IObservable<T> source, Func<T, IMaybeSource<T>> mapper, bool delayErrors = false)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));

            throw new NotImplementedException();
        }

        /// <summary>
        /// Ignores the success signal of the maybe source and
        /// completes the downstream completable observer instead.
        /// </summary>
        /// <typeparam name="T">The success value type of the source.</typeparam>
        /// <param name="source">The source to ignore the success value of.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.9</remarks>
        public static ICompletableSource IgnoreElement<T>(this IMaybeSource<T> source)
        {
            RequireNonNull(source, nameof(source));

            return new CompletableIgnoreElementMaybe<T>(source);
        }

        public static IMaybeSource<T> FirstElement<T>(this IObservable<T> source)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static IMaybeSource<T> SingleElement<T>(this IObservable<T> source)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static IMaybeSource<T> LastElement<T>(this IObservable<T> source)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static IMaybeSource<T> ElementAtIndex<T>(this IObservable<T> source, long index)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        /// <summary>
        /// Converts an ongoing or already terminated task to a maybe source 
        /// and relays its terminal event to observers.
        /// </summary>
        /// <param name="task">The task to observe as a maybe source.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11<br/>
        /// Note that the <see cref="Task"/> API uses an <see cref="AggregateException"/>
        /// to signal there were one or more errors.
        /// </remarks>
        public static IMaybeSource<T> ToMaybe<T>(this Task task)
        {
            RequireNonNull(task, nameof(task));

            return new MaybeFromTaskPlain<T>(task);
        }

        /// <summary>
        /// Converts an ongoing or already terminated task to a maybe source 
        /// and relays its value or error to observers.
        /// </summary>
        /// <param name="task">The task to observe as a maybe source.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.11<br/>
        /// Note that the <see cref="Task{TResult}"/> API uses an <see cref="AggregateException"/>
        /// to signal there were one or more errors.
        /// </remarks>
        public static IMaybeSource<T> ToMaybe<T>(this Task<T> task)
        {
            RequireNonNull(task, nameof(task));

            return new MaybeFromTask<T>(task);
        }
    }
}
