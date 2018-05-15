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
        /// <param name="dispose">Dispose the TestObserver before the subscription happens</param>
        /// <returns>The new TestObserver instance.</returns>
        public static TestObserver<object> Test(this ICompletableSource source, bool dispose = false)
        {
            RequireNonNull(source, nameof(source));
            var to = new TestObserver<object>();
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
        /// Creates an completable that never terminates.
        /// </summary>
        /// <returns>The shared never-terminating completable instance.</returns>
        public static ICompletableSource Never()
        {
            return CompletableNever.INSTANCE;
        }

        /// <summary>
        /// Creates a failing completable that signals the specified error
        /// immediately.
        /// </summary>
        /// <param name="error">The error to signal.</param>
        /// <returns>The new completable source instance.</returns>
        public static ICompletableSource Error(Exception error)
        {
            RequireNonNull(error, nameof(error));

            return new CompletableError(error);
        }

        /// <summary>
        /// Wraps and calls the given action for each individual
        /// completable observer then completes or fails the observer
        /// depending on the action completes normally or threw an exception.
        /// </summary>
        /// <param name="action">The action to invoke for each individual completable observer.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.6</remarks>
        public static ICompletableSource FromAction(Action action)
        {
            RequireNonNull(action, nameof(action));

            return new CompletableFromAction(action);
        }

        /// <summary>
        /// Creates a completable source that completes or fails
        /// its observers when the given (possibly still ongoing)
        /// task terminates.
        /// </summary>
        /// <param name="task">The task to wrap.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.6</remarks>
        public static ICompletableSource FromTask(Task task)
        {
            return task.ToCompletable();
        }

        /// <summary>
        /// Creates a completable source that completes or fails
        /// its observers when the given (possibly still ongoing)
        /// task terminates.
        /// </summary>
        /// <param name="task">The task to wrap.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.6</remarks>
        public static ICompletableSource FromTask<T>(Task<T> task)
        {
            return task.ToCompletable();
        }

        /// <summary>
        /// Relays the terminal event of the fastest responding
        /// completable source while disposing the others.
        /// </summary>
        /// <param name="sources">The completable sources.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.7</remarks>
        public static ICompletableSource AmbAll(this ICompletableSource[] sources)
        {
            RequireNonNull(sources, nameof(sources));

            return new CompletableAmb(sources);
        }

        /// <summary>
        /// Relays the terminal event of the fastest responding
        /// completable source while disposing the others.
        /// </summary>
        /// <param name="sources">The completable sources.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.7</remarks>
        public static ICompletableSource Amb(params ICompletableSource[] sources)
        {
            return AmbAll(sources);
        }

        /// <summary>
        /// Relays the terminal event of the fastest responding
        /// completable source while disposing the others.
        /// </summary>
        /// <param name="sources">The completable sources.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.7</remarks>
        public static ICompletableSource Amb(IEnumerable<ICompletableSource> sources)
        {
            RequireNonNull(sources, nameof(sources));

            return new CompletableAmbEnumerable(sources);
        }

        public static ICompletableSource ConcatAll(this ICompletableSource[] sources, bool delayErrors = false)
        {
            throw new NotImplementedException();
        }

        public static ICompletableSource Concat(params ICompletableSource[] sources)
        {
            throw new NotImplementedException();
        }

        public static ICompletableSource Concat(IEnumerable<ICompletableSource> sources, bool delayErrors = false)
        {
            throw new NotImplementedException();
        }

        public static ICompletableSource Concat(bool delayErrors, params ICompletableSource[] sources)
        {
            throw new NotImplementedException();
        }

        public static ICompletableSource Concat(this IObservable<ICompletableSource> sources, bool delayErrors = false)
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

        /// <summary>
        /// Defers the creation of the actual completable source
        /// provided by a supplier function until a completable observer completes.
        /// </summary>
        /// <param name="supplier">The function called for each individual completable
        /// observer and should return a completable source to subscribe to.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.6</remarks>
        public static ICompletableSource Defer(Func<ICompletableSource> supplier)
        {
            RequireNonNull(supplier, nameof(supplier));

            return new CompletableDefer(supplier);
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

        /// <summary>
        /// Completes after a specified time elapsed on the given scheduler.
        /// </summary>
        /// <param name="time">The time to wait before signaling OnCompleted.</param>
        /// <param name="scheduler">The scheduler to use for emitting the terminal event.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.6</remarks>
        public static ICompletableSource Timer(TimeSpan time, IScheduler scheduler)
        {
            RequireNonNull(scheduler, nameof(scheduler));

            return new CompletableTimer(time, scheduler);
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

        /// <summary>
        /// Calls the given <paramref name="handler"/> whenever a
        /// completable observer subscribes to the completable <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The completable source to peek into.</param>
        /// <param name="handler">The handler to call.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.7</remarks>
        public static ICompletableSource DoOnSubscribe(this ICompletableSource source, Action<IDisposable> handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            return CompletablePeek.Create(source, onSubscribe: handler);
        }

        /// <summary>
        /// Calls the given <paramref name="handler"/> whenever a
        /// completable observer disposes to the connection to
        /// the completable <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The completable source to peek into.</param>
        /// <param name="handler">The handler to call.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.7</remarks>
        public static ICompletableSource DoOnDispose(this ICompletableSource source, Action handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            return CompletablePeek.Create(source, onDispose: handler);
        }

        /// <summary>
        /// Calls the given <paramref name="handler"/> before a
        /// completable observer gets completed by
        /// the completable <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The completable source to peek into.</param>
        /// <param name="handler">The handler to call.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.7</remarks>
        public static ICompletableSource DoOnCompleted(this ICompletableSource source, Action handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            return CompletablePeek.Create(source, onCompleted: handler);
        }

        /// <summary>
        /// Calls the given <paramref name="handler"/> before a
        /// completable observer receives the error signal from
        /// the completable <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The completable source to peek into.</param>
        /// <param name="handler">The handler to call.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.7</remarks>
        public static ICompletableSource DoOnError(this ICompletableSource source, Action<Exception> handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            return CompletablePeek.Create(source, onError: handler);
        }

        /// <summary>
        /// Calls the given <paramref name="handler"/> before a
        /// completable observer gets terminated normally or with an error by
        /// the completable <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The completable source to peek into.</param>
        /// <param name="handler">The handler to call.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.7</remarks>
        public static ICompletableSource DoOnTerminate(this ICompletableSource source, Action handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            return CompletablePeek.Create(source, onTerminate: handler);
        }

        /// <summary>
        /// Calls the given <paramref name="handler"/> after a
        /// completable observer gets terminated normally or exceptionally by
        /// the completable <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The completable source to peek into.</param>
        /// <param name="handler">The handler to call.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.7</remarks>
        public static ICompletableSource DoAfterTerminate(this ICompletableSource source, Action handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            return CompletablePeek.Create(source, onAfterTerminate: handler);
        }

        /// <summary>
        /// Calls the given <paramref name="handler"/> exactly once per completable
        /// observer and after the completable observer gets terminated normally
        /// or exceptionally or the observer disposes the connection to the
        /// the completable <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The completable source to peek into.</param>
        /// <param name="handler">The handler to call.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.7</remarks>
        public static ICompletableSource DoFinally(this ICompletableSource source, Action handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            return CompletablePeek.Create(source, doFinally: handler);
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

        public static ICompletableSource Cache(this ICompletableSource source, Action<IDisposable> cancel = null)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static ICompletableSource Delay(this ICompletableSource source, TimeSpan time, IScheduler scheduler)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static ICompletableSource DelaySubscription(this ICompletableSource source, TimeSpan time, IScheduler scheduler)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static ICompletableSource DelaySubscription(this ICompletableSource source, ICompletableSource other)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static ICompletableSource TakeUntil(this ICompletableSource source, ICompletableSource other)
        {
            RequireNonNull(source, nameof(source));

            throw new NotImplementedException();
        }

        public static ICompletableSource TakeUntil<U>(this ICompletableSource source, IObservable<U> other)
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

        /// <summary>
        /// Subscribe to this completable source and call the
        /// appropriate action depending on the terminal signal received.
        /// </summary>
        /// <param name="source">The completable source to observer.</param>
        /// <param name="onCompleted">Called when the completable source completes normally.</param>
        /// <param name="onError">Called with the exception when the completable source terminates with an error.</param>
        /// <returns>The disposable that allows cancelling the source.</returns>
        /// <remarks>Since 0.0.6</remarks>
        public static IDisposable Subscribe(this ICompletableSource source, Action onCompleted = null, Action<Exception> onError = null)
        {
            RequireNonNull(source, nameof(source));

            var parent = new CompletableLambdaObserver(onCompleted, onError);
            source.Subscribe(parent);
            return parent;
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

        /// <summary>
        /// Subscribes an completable observer (subclass) to the completable
        /// source and returns this observer instance as well.
        /// </summary>
        /// <typeparam name="T">The completable observer type.</typeparam>
        /// <param name="source">The completable source to subscribe to.</param>
        /// <param name="observer">The completable observer (subclass) to subscribe with.</param>
        /// <returns>The <paramref name="observer"/> provided as parameter.</returns>
        /// <remarks>Since 0.0.6</remarks>
        public static T SubscribeWith<T>(this ICompletableSource source, T observer) where T : ICompletableObserver
        {
            RequireNonNull(observer, nameof(observer));
            source.Subscribe(observer);
            return observer;
        }

        //-------------------------------------------------
        // Interoperation with other reactive types
        //-------------------------------------------------

        /// <summary>
        /// Subscribes to the next observable sequence and relays its
        /// values when the completable source completes normally.
        /// </summary>
        /// <typeparam name="T">The element type of the next observable sequence.</typeparam>
        /// <param name="source">The completable source to start with.</param>
        /// <param name="next">The observable sequence to resume with when the <paramref name="source"/>
        /// completes.</param>
        /// <returns>The new observable instance.</returns>
        /// <remarks>Since 0.0.6</remarks>
        public static IObservable<T> AndThen<T>(this ICompletableSource source, IObservable<T> next)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(next, nameof(next));

            return new CompletableAndThenObservable<T>(source, next);
        }

        /// <summary>
        /// Subscribes to the next completable source and relays its
        /// values when the main completable source completes normally.
        /// </summary>
        /// <param name="source">The completable source to start with.</param>
        /// <param name="next">The completable sequence to resume with when the <paramref name="source"/>
        /// completes.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.6</remarks>
        public static ICompletableSource AndThen(this ICompletableSource source, ICompletableSource next)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(next, nameof(next));

            return new CompletableAndThen(source, next);
        }

        public static ISingleSource<T> AndThen<T>(this ICompletableSource source, ISingleSource<T> next)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(next, nameof(next));

            throw new NotImplementedException();
        }

        public static IMaybeSource<T> AndThen<T>(this ICompletableSource source, IMaybeSource<T> next)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(next, nameof(next));

            throw new NotImplementedException();
        }

        /// <summary>
        /// Ignores the elements of a legacy observable and only relays
        /// the terminal events.
        /// </summary>
        /// <typeparam name="T">The element type of the legacy observable.</typeparam>
        /// <param name="source">The source sequence whoe elements to ignore.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.6</remarks>
        public static ICompletableSource IgnoreAllElements<T>(this IObservable<T> source)
        {
            RequireNonNull(source, nameof(source));

            return new CompletableIgnoreAllElements<T>(source);
        }

        public static ICompletableSource IgnoreElement<T>(this ISingleSource<T> source)
        {
            throw new NotImplementedException();
        }

        public static ICompletableSource IgnoreElement<T>(this IMaybeSource<T> source)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Converts an ongoing or already terminated task to a completable source 
        /// and relays its terminal event to observers.
        /// </summary>
        /// <param name="task">The task to observe as a completable source.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.6<br/>
        /// Note that the <see cref="Task"/> API uses an <see cref="AggregateException"/>
        /// to signal there were one or more errors.
        /// </remarks>
        public static ICompletableSource ToCompletable(this Task task)
        {
            RequireNonNull(task, nameof(task));

            return new CompletableFromTask(task);
        }

        /// <summary>
        /// Converts an ongoing or already terminated task to a completable source 
        /// and relays its terminal event to observers.
        /// </summary>
        /// <param name="task">The task to observe as a completable source.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.6<br/>
        /// Note that the <see cref="Task{TResult}"/> API uses an <see cref="AggregateException"/>
        /// to signal there were one or more errors.
        /// </remarks>
        public static ICompletableSource ToCompletable<T>(this Task<T> task)
        {
            RequireNonNull(task, nameof(task));

            return new CompletableFromTask<T>(task);
        }

        /// <summary>
        /// Exposes a completable source as a legacy observable.
        /// </summary>
        /// <typeparam name="T">The element type of the observable sequence.</typeparam>
        /// <param name="source">The completable source to expose as an <see cref="IObservable{T}"/></param>
        /// <returns>The new observable instance.</returns>
        /// <remarks>Since 0.0.6</remarks>
        public static IObservable<T> ToObservable<T>(this ICompletableSource source)
        {
            RequireNonNull(source, nameof(source));

            return new CompletableToObservable<T>(source);
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
