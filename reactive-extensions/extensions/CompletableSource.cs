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
        /// that can be used for signaling a completion or error event.</param>
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

        /// <summary>
        /// Runs the completable sources one after the other and
        /// completes if all of them completed normally or
        /// terminates eagerly or after all terminated.
        /// </summary>
        /// <param name="sources">The completable sources.</param>
        /// <param name="delayErrors">If true, errors from completable sources are delayed until all of them terminates.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.7</remarks>
        public static ICompletableSource ConcatAll(this ICompletableSource[] sources, bool delayErrors = false)
        {
            RequireNonNull(sources, nameof(sources));

            return new CompletableConcat(sources, delayErrors);
        }

        /// <summary>
        /// Runs the completable sources one after the other and
        /// completes if all of them completed normally or
        /// fails if the current completable source failed.
        /// </summary>
        /// <param name="sources">The completable sources.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.7</remarks>
        public static ICompletableSource Concat(params ICompletableSource[] sources)
        {
            return ConcatAll(sources);
        }

        /// <summary>
        /// Runs the completable sources one after the other and
        /// completes if all of them completed normally or
        /// terminates eagerly or after all terminated.
        /// </summary>
        /// <param name="sources">The completable sources.</param>
        /// <param name="delayErrors">If true, errors from completable sources are delayed until all of them terminates.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.7</remarks>
        public static ICompletableSource Concat(IEnumerable<ICompletableSource> sources, bool delayErrors = false)
        {
            return new CompletableConcatEnumerable(sources, delayErrors);
        }

        /// <summary>
        /// Runs the completable sources one after the other and
        /// completes if all of them completed normally or
        /// terminates eagerly or after all terminated.
        /// </summary>
        /// <param name="sources">The completable sources.</param>
        /// <param name="delayErrors">If true, errors from completable sources are delayed until all of them terminates.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.7</remarks>
        public static ICompletableSource Concat(bool delayErrors, params ICompletableSource[] sources)
        {
            return ConcatAll(sources, delayErrors);
        }

        /// <summary>
        /// Runs the completable sources one after the other 
        /// provided by the observable sequence and
        /// completes if all of them completed normally or
        /// terminates eagerly or after all terminated.
        /// </summary>
        /// <param name="sources">The completable sources.</param>
        /// <param name="delayErrors">If true, errors from completable sources are delayed until all of them terminates.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.7</remarks>
        public static ICompletableSource Concat(this IObservable<ICompletableSource> sources, bool delayErrors = false)
        {
            return sources.ConcatMap(v => v, delayErrors);
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

        /// <summary>
        /// Merges an array of completable sources and completes if
        /// all of the sources complete or terminate.
        /// </summary>
        /// <param name="sources">The array of inner completable sources.</param>
        /// <param name="delayErrors">If true, all errors are delayed until all sources terminate.</param>
        /// <param name="maxConcurrency">The maximum number of inner completable sources to run at once.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.10</remarks>
        public static ICompletableSource MergeAll(this ICompletableSource[] sources, bool delayErrors = false, int maxConcurrency = int.MaxValue)
        {
            RequireNonNull(sources, nameof(sources));
            RequirePositive(maxConcurrency, nameof(maxConcurrency));

            return new CompletableMerge(sources, delayErrors, maxConcurrency);
        }

        /// <summary>
        /// Merges an array of completable sources and completes if
        /// all of the sources complete.
        /// </summary>
        /// <param name="sources">The array of inner completable sources.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.10</remarks>
        public static ICompletableSource Merge(params ICompletableSource[] sources)
        {
            return MergeAll(sources);
        }

        /// <summary>
        /// Merges an enumerable of completable sources and completes if
        /// all of the sources complete or terminate.
        /// </summary>
        /// <param name="sources">The enumerable sequence of inner completable sources.</param>
        /// <param name="delayErrors">If true, all errors are delayed until all sources terminate.</param>
        /// <param name="maxConcurrency">The maximum number of inner completable sources to run at once.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.10</remarks>
        public static ICompletableSource Merge(this IEnumerable<ICompletableSource> sources, bool delayErrors = false, int maxConcurrency = int.MaxValue)
        {
            RequireNonNull(sources, nameof(sources));

            return new CompletableMergeEnumerable(sources, delayErrors, maxConcurrency);
        }

        /// <summary>
        /// Merges an array of completable sources, running some at a time, and completes if
        /// all of the sources complete.
        /// </summary>
        /// <param name="sources">The array of inner completable sources.</param>
        /// <param name="maxConcurrency">The maximum number of inner completable sources to run at once.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.10</remarks>
        public static ICompletableSource Merge(int maxConcurrency, params ICompletableSource[] sources)
        {
            return MergeAll(sources, maxConcurrency: maxConcurrency);
        }

        /// <summary>
        /// Merges an array of completable sources and completes if
        /// all of the sources complete or terminate.
        /// </summary>
        /// <param name="sources">The array of inner completable sources.</param>
        /// <param name="delayErrors">If true, all errors are delayed until all sources terminate.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.10</remarks>
        public static ICompletableSource Merge(bool delayErrors, params ICompletableSource[] sources)
        {
            return MergeAll(sources, delayErrors);
        }

        /// <summary>
        /// Merges an array of completable sources and completes if
        /// all of the sources complete or terminate.
        /// </summary>
        /// <param name="sources">The array of inner completable sources.</param>
        /// <param name="delayErrors">If true, all errors are delayed until all sources terminate.</param>
        /// <param name="maxConcurrency">The maximum number of inner completable sources to run at once.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.10</remarks>
        public static ICompletableSource Merge(bool delayErrors, int maxConcurrency, params ICompletableSource[] sources)
        {
            return MergeAll(sources, delayErrors, maxConcurrency);
        }

        /// <summary>
        /// Merges an observable sequence of completable sources and completes if
        /// all of the sources complete or terminate.
        /// </summary>
        /// <param name="sources">The observable sequence of inner completable sources.</param>
        /// <param name="delayErrors">If true, all errors are delayed until all sources terminate.</param>
        /// <param name="maxConcurrency">The maximum number of inner completable sources to run at once.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.10</remarks>
        public static ICompletableSource Merge(this IObservable<ICompletableSource> sources, bool delayErrors = false, int maxConcurrency = int.MaxValue)
        {
            RequireNonNull(sources, nameof(sources));

            return sources.FlatMap(v => v, delayErrors, maxConcurrency);
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

        /// <summary>
        /// Generates a resource and a dependent completable source
        /// for each completable observer and cleans up the resource
        /// just before or just after the completable source terminated
        /// or the observer has disposed the setup.
        /// </summary>
        /// <typeparam name="S">The resource type.</typeparam>
        /// <param name="resourceSupplier">The supplier for a per-observer resource.</param>
        /// <param name="sourceSelector">Function that receives the per-observer resource returned
        /// by <paramref name="resourceSupplier"/> and returns a completable source.</param>
        /// <param name="resourceCleanup">The optional callback for cleaning up the resource supplied by
        /// the <paramref name="resourceSupplier"/>.</param>
        /// <param name="eagerCleanup">If true, the per-observer resource is cleaned up before the
        /// terminal event is signaled to the downstream. If false, the cleanup happens after.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.8</remarks>
        public static ICompletableSource Using<S>(Func<S> resourceSupplier, Func<S, ICompletableSource> sourceSelector, Action<S> resourceCleanup = null, bool eagerCleanup = true)
        {
            RequireNonNull(resourceSupplier, nameof(resourceSupplier));
            RequireNonNull(sourceSelector, nameof(sourceSelector));

            return new CompletableUsing<S>(resourceSupplier, sourceSelector, resourceCleanup, eagerCleanup);
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

        /// <summary>
        /// If the upstream doesn't terminate within the specified
        /// timeout, the completable observer is terminated with
        /// a <see cref="TimeoutException"/> or is switched to the optional
        /// <paramref name="fallback"/> completable source.
        /// </summary>
        /// <param name="source">The completable source to timeout.</param>
        /// <param name="timeout">The time to wait before canceling the source.</param>
        /// <param name="scheduler">The scheduler to use wait for the termination of the upstream.</param>
        /// <param name="fallback">The optional completable source to switch to if the upstream times out.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.8</remarks>
        public static ICompletableSource Timeout(this ICompletableSource source, TimeSpan timeout, IScheduler scheduler, ICompletableSource fallback = null)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(scheduler, nameof(scheduler));

            return new CompletableTimeout(source, timeout, scheduler, fallback);
        }

        /// <summary>
        /// Suppresses an upstream error and completes the completable observer
        /// instead.
        /// </summary>
        /// <param name="source">The completable source to suppress the errors of.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.8</remarks>
        public static ICompletableSource OnErrorComplete(this ICompletableSource source)
        {
            RequireNonNull(source, nameof(source));

            return new CompletableOnErrorComplete(source);
        }

        /// <summary>
        /// Switches to a <paramref name="fallback"/> completable source if
        /// the upstream fails.
        /// </summary>
        /// <param name="source">The completable source that can fail.</param>
        /// <param name="fallback">The fallback completable source to resume with if <paramref name="source"/> fails.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.8</remarks>
        public static ICompletableSource OnErrorResumeNext(this ICompletableSource source, ICompletableSource fallback)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(fallback, nameof(fallback));

            return new CompletableOnErrorResumeNext(source, fallback);
        }

        /// <summary>
        /// Switches to a fallback completable source provided
        /// by a handler function if the main completable source fails.
        /// </summary>
        /// <param name="source">The completable source that can fail.</param>
        /// <param name="handler">The function that receives the exception from the main
        /// source and should return a fallback completable source to resume with.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.8</remarks>
        public static ICompletableSource OnErrorResumeNext(this ICompletableSource source, Func<Exception, ICompletableSource> handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            return new CompletableOnErrorResumeNextSelector(source, handler);
        }

        /// <summary>
        /// Repeatedly subscribes to the completable source after the
        /// previous subscription completes.
        /// </summary>
        /// <param name="source">The completable source to repeat.</param>
        /// <param name="times">The maximum number of repeats. <code>times=1</code>
        /// will repeat the source once, thus the source is subscribed to twice.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.8</remarks>
        public static ICompletableSource Repeat(this ICompletableSource source, long times = long.MaxValue)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNegative(times, nameof(times));

            return new CompletableRepeat(source, times);
        }

        /// <summary>
        /// Repeatedly subscribes to the completable source after the
        /// previous subscription completes and if the <paramref name="predicate"/>
        /// returns true.
        /// </summary>
        /// <param name="source">The completable source to repeat.</param>
        /// <param name="predicate">The function receiving the current repeat count (1-based)
        /// and should return true if the re-subscription should happen.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.8</remarks>
        public static ICompletableSource Repeat(this ICompletableSource source, Func<long, bool> predicate)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(predicate, nameof(predicate));

            return new CompletableRepeatPredicate(source, predicate);
        }

        /// <summary>
        /// Repeats (resubscribes to) the completable source after a completion and when the observable
        /// returned by a handler produces an arbitrary item.
        /// </summary>
        /// <typeparam name="U">The arbitrary element type signaled by the handler observable.</typeparam>
        /// <param name="source">The completable source to repeat while it successfully terminates.</param>
        /// <param name="handler">The function that is called for each observer and takes an observable sequence of
        /// errors. It should return an observable of arbitrary items that should signal that arbitrary item in
        /// response to receiving the completion signal from the source observable. If this observable signals
        /// a terminal event, the sequence is terminated with that signal instead.</param>
        /// <returns>An observable sequence producing the elements of the given sequence repeatedly while it terminates successfully.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="handler"/> is null.</exception>
        /// <remarks>Since 0.0.10</remarks>
        public static ICompletableSource RepeatWhen<U>(this ICompletableSource source, Func<IObservable<object>, IObservable<U>> handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            return new CompletableRepeatWhen<U>(source, handler);
        }

        /// <summary>
        /// Repeatedly subscribes to the completable source after the
        /// previous subscription fails.
        /// </summary>
        /// <param name="source">The completable source to repeat.</param>
        /// <param name="times">The maximum number of repeats. <code>times=1</code>
        /// will repeat the source once, thus the source is subscribed to twice.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.8</remarks>
        public static ICompletableSource Retry(this ICompletableSource source, long times = long.MaxValue)
        {
            RequireNonNull(source, nameof(source));

            return new CompletableRetry(source, times);
        }

        /// <summary>
        /// Repeatedly subscribes to the completable source after the
        /// previous subscription fails and if the <paramref name="predicate"/>
        /// returns true.
        /// </summary>
        /// <param name="source">The completable source to repeat.</param>
        /// <param name="predicate">The function receiving the current repeat count (1-based)
        /// and should return true if the re-subscription should happen.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.8</remarks>
        public static ICompletableSource Retry(this ICompletableSource source, Func<Exception, long, bool> predicate)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(predicate, nameof(predicate));

            return new CompletableRetryPredicate(source, predicate);
        }

        /// <summary>
        /// Retries (resubscribes to) the completable source after a failure and when the observable
        /// returned by a handler produces an arbitrary item.
        /// </summary>
        /// <typeparam name="U">The arbitrary element type signaled by the handler observable.</typeparam>
        /// <param name="source">The completable source to repeat until it successfully terminates.</param>
        /// <param name="handler">The function that is called for each observer and takes an observable sequence of
        /// errors. It should return an observable of arbitrary items that should signal that arbitrary item in
        /// response to receiving the failure Exception from the source observable. If this observable signals
        /// a terminal event, the sequence is terminated with that signal instead.</param>
        /// <returns>An observable sequence producing the elements of the given sequence repeatedly until it terminates successfully.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="handler"/> is null.</exception>
        /// <remarks>Since 0.0.10</remarks>
        public static ICompletableSource RetryWhen<U>(this ICompletableSource source, Func<IObservable<Exception>, IObservable<U>> handler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(handler, nameof(handler));

            return new CompletableRetryWhen<U>(source, handler);
        }

        /// <summary>
        /// Subscribes to the source on the given scheduler.
        /// </summary>
        /// <param name="source">The target completable source to subscribe to</param>
        /// <param name="scheduler">The scheduler to use when subscribing to <paramref name="source"/>.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.8</remarks>
        public static ICompletableSource SubscribeOn(this ICompletableSource source, IScheduler scheduler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(scheduler, nameof(scheduler));

            return new CompletableSubscribeOn(source, scheduler);
        }

        /// <summary>
        /// Signals the terminal events of the completable source
        /// through the specified <paramref name="scheduler"/>.
        /// </summary>
        /// <param name="source">The completable source to observe on the specified scheduler.</param>
        /// <param name="scheduler">The scheduler to use.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.8</remarks>
        public static ICompletableSource ObserveOn(this ICompletableSource source, IScheduler scheduler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(scheduler, nameof(scheduler));

            return new CompletableObserveOn(source, scheduler);
        }

        /// <summary>
        /// When the downstream disposes, the upstream's disposable
        /// is called from the given scheduler.
        /// Note that termination in general doesn't call
        /// <code>Dispose()</code> on the upstream.
        /// </summary>
        /// <param name="source">The completable source to dispose.</param>
        /// <param name="scheduler">The scheduler to use.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.8</remarks>
        public static ICompletableSource UnsubscribeOn(this ICompletableSource source, IScheduler scheduler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(scheduler, nameof(scheduler));

            return new CompletableUnsubscribeOn(source, scheduler);
        }

        /// <summary>
        /// When the upstream terminates or the downstream disposes,
        /// it detaches the references between the two, avoiding
        /// leaks of one or the other.
        /// </summary>
        /// <param name="source">The completable source to detach from upon termination or cancellation.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.8</remarks>
        public static ICompletableSource OnTerminateDetach(this ICompletableSource source)
        {
            RequireNonNull(source, nameof(source));

            return new CompletableOnTerminateDetach(source);
        }

        /// <summary>
        /// Subscribes to the upstream completable source when the first
        /// completable observer subscribes to this completable source
        /// and relays/replays the upstream terminal event to current
        /// or late observers.
        /// </summary>
        /// <param name="source">The upstream completable source to subscribe to.</param>
        /// <param name="cancel">Called before subscribing to the upstream to allow canceling the operation.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.10</remarks>
        public static ICompletableSource Cache(this ICompletableSource source, Action<IDisposable> cancel = null)
        {
            RequireNonNull(source, nameof(source));

            return new CompletableCache(source, cancel);
        }

        /// <summary>
        /// Delay the delivery of the terminal events from the
        /// upstream completable source by the given time amount.
        /// </summary>
        /// <param name="source">The completable source to delay signals of.</param>
        /// <param name="time">The time delay.</param>
        /// <param name="scheduler">The scheduler to use for the timed wait and signal emission.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.9</remarks>
        public static ICompletableSource Delay(this ICompletableSource source, TimeSpan time, IScheduler scheduler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(scheduler, nameof(scheduler));

            return new CompletableDelay(source, time, scheduler);
        }

        /// <summary>
        /// Delay the subscription to the main completable source
        /// until the specified time elapsed.
        /// </summary>
        /// <param name="source">The completable source to delay subscribing to.</param>
        /// <param name="time">The delay time.</param>
        /// <param name="scheduler">The scheduler to use for the timed wait and subscription.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.9</remarks>
        public static ICompletableSource DelaySubscription(this ICompletableSource source, TimeSpan time, IScheduler scheduler)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(scheduler, nameof(scheduler));

            return new CompletableDelaySubscriptionTime(source, time, scheduler);
        }

        /// <summary>
        /// Delay the subscription to the main completable source
        /// until the other source completes.
        /// </summary>
        /// <param name="source">The completable source to delay subscribing to.</param>
        /// <param name="other">The source that should complete to trigger the main subscription.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.9</remarks>
        public static ICompletableSource DelaySubscription(this ICompletableSource source, ICompletableSource other)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(other, nameof(other));

            return new CompletableDelaySubscription(source, other);
        }

        /// <summary>
        /// Terminates when either the main or the other source terminates,
        /// disposing the other sequence.
        /// </summary>
        /// <param name="source">The main completable source to consume.</param>
        /// <param name="other">The other completable source that could stop the <paramref name="source"/>.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.10</remarks>
        public static ICompletableSource TakeUntil(this ICompletableSource source, ICompletableSource other)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(other, nameof(other));

            return new CompletableTakeUntil(source, other);
        }

        /// <summary>
        /// Terminates when either the main or the other source terminates,
        /// disposing the other sequence.
        /// </summary>
        /// <param name="source">The main completable source to consume.</param>
        /// <param name="other">The other observable that could stop the <paramref name="source"/>
        /// by emitting an item or completing.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.10</remarks>
        public static ICompletableSource TakeUntil<U>(this ICompletableSource source, IObservable<U> other)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(other, nameof(other));

            return new CompletableTakeUntilObservable<U>(source, other);
        }

        /// <summary>
        /// Hides the identity and disposable of the upstream from
        /// the downstream.
        /// </summary>
        /// <param name="source">The completable source to hide.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.9</remarks>
        public static ICompletableSource Hide(this ICompletableSource source)
        {
            RequireNonNull(source, nameof(source));

            return new CompletableHide(source);
        }

        // ------------------------------------------------
        // Leaving the reactive world
        // ------------------------------------------------

        /// <summary>
        /// Subscribes to this completable source and suppresses exceptions
        /// throw by the OnXXX methods of the <paramref name="observer"/>.
        /// </summary>
        /// <param name="source">The completable source to subscribe to safely.</param>
        /// <param name="observer">The unreliable observer.</param>
        /// <remarks>Since 0.0.10</remarks>
        public static void SubscribeSafe(this ICompletableSource source, ICompletableObserver observer)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(observer, nameof(observer));

            source.Subscribe(new CompletableSafeObserver(observer));
        }

        /// <summary>
        /// Subscribe to this completable source and call the
        /// appropriate action depending on the terminal signal received.
        /// </summary>
        /// <param name="source">The completable source to observe.</param>
        /// <param name="onCompleted">Called when the completable source completes normally.</param>
        /// <param name="onError">Called with the exception when the completable source terminates with an error.</param>
        /// <returns>The disposable that allows canceling the source.</returns>
        /// <remarks>Since 0.0.6</remarks>
        public static IDisposable Subscribe(this ICompletableSource source, Action onCompleted = null, Action<Exception> onError = null)
        {
            RequireNonNull(source, nameof(source));

            var parent = new CompletableLambdaObserver(onCompleted, onError);
            source.Subscribe(parent);
            return parent;
        }

        /// <summary>
        /// Subscribes to the source and blocks until it terminated, then
        /// calls the appropriate completable observer method on the current
        /// thread.
        /// </summary>
        /// <param name="source">The upstream completable source to block for.</param>
        /// <param name="observer">The completable observer to call the methods on the current thread.</param>
        /// <remarks>Since 0.0.10</remarks>
        public static void BlockingSubscribe(this ICompletableSource source, ICompletableObserver observer)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(observer, nameof(observer));

            var parent = new CompletableBlockingObserver(observer);
            observer.OnSubscribe(parent);

            source.Subscribe(parent);

            parent.Run();
        }

        /// <summary>
        /// Subscribes to the source and blocks until it terminated, then
        /// calls the appropriate completable observer method on the current
        /// thread.
        /// </summary>
        /// <param name="source">The upstream completable source to block for.</param>
        /// <param name="onCompleted">Action called when the upstream completes.</param>
        /// <param name="onError">Action called with the exception when the upstream fails.</param>
        /// <param name="onSubscribe">Action called with a disposable just before subscribing to the upstream
        /// and allows disposing the sequence and unblocking this method call.</param>
        /// <remarks>Since 0.0.10</remarks>
        public static void BlockingSubscribe(this ICompletableSource source, Action onCompleted = null, Action<Exception> onError = null, Action<IDisposable> onSubscribe = null)
        {
            RequireNonNull(source, nameof(source));

            var parent = new CompletableBlockingConsumer(onCompleted, onError);
            onSubscribe?.Invoke(parent);

            source.Subscribe(parent);

            parent.Run();
        }

        /// <summary>
        /// Wait until the upstream terminates and rethrow any exception it
        /// signaled.
        /// </summary>
        /// <param name="source">The completable source to wait for.</param>
        /// <param name="timeoutMillis">The maximum time to wait for termination.</param>
        /// <param name="cts">The means to cancel the wait from outside.</param>
        /// <exception cref="TimeoutException">If a timeout happens, which also cancels the upstream.</exception>
        /// <remarks>Since 0.0.10</remarks>
        public static void Wait(this ICompletableSource source, int timeoutMillis = int.MaxValue, CancellationTokenSource cts = null)
        {
            RequireNonNull(source, nameof(source));

            var parent = new CompletableWait();
            source.Subscribe(parent);

            parent.Wait(timeoutMillis, cts);
        }

        /// <summary>
        /// Subscribes a completable observer (subclass) to the completable
        /// source and returns this observer instance as well.
        /// </summary>
        /// <typeparam name="T">The completable observer type.</typeparam>
        /// <param name="source">The completable source to subscribe to.</param>
        /// <param name="observer">The completable observer (subclass) to subscribe with.</param>
        /// <returns>The <paramref name="observer"/> provided as parameter.</returns>
        /// <remarks>Since 0.0.6</remarks>
        public static T SubscribeWith<T>(this ICompletableSource source, T observer) where T : ICompletableObserver
        {
            RequireNonNull(source, nameof(source));
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

        /// <summary>
        /// Subscribes to the next single source and relays its
        /// values when the main completable source completes normally.
        /// </summary>
        /// <param name="source">The completable source to start with.</param>
        /// <param name="next">The single source to resume with when the <paramref name="source"/>
        /// completes.</param>
        /// <returns>The new single source instance.</returns>
        /// <remarks>Since 0.0.9</remarks>
        public static ISingleSource<T> AndThen<T>(this ICompletableSource source, ISingleSource<T> next)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(next, nameof(next));

            return new CompletableAndThenSingle<T>(source, next);
        }

        /// <summary>
        /// Subscribes to the next maybe source and relays its
        /// values when the main completable source completes normally.
        /// </summary>
        /// <param name="source">The completable source to start with.</param>
        /// <param name="next">The maybe source to resume with when the <paramref name="source"/>
        /// completes.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.9</remarks>
        public static IMaybeSource<T> AndThen<T>(this ICompletableSource source, IMaybeSource<T> next)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(next, nameof(next));

            return new CompletableAndThenMaybe<T>(source, next);
        }

        /// <summary>
        /// Ignores the elements of a legacy observable and only relays
        /// the terminal events.
        /// </summary>
        /// <typeparam name="T">The element type of the legacy observable.</typeparam>
        /// <param name="source">The source sequence whose elements to ignore.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.6</remarks>
        public static ICompletableSource IgnoreAllElements<T>(this IObservable<T> source)
        {
            RequireNonNull(source, nameof(source));

            return new CompletableIgnoreAllElements<T>(source);
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

        /// <summary>
        /// When the upstream completable source completes, the
        /// downstream single observer receives a success item.
        /// </summary>
        /// <typeparam name="T">The type of the success item.</typeparam>
        /// <param name="source">The source completable to convert.</param>
        /// <param name="successItem">The item to emit when the source completes.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.9</remarks>
        public static ISingleSource<T> ToSingle<T>(this ICompletableSource source, T successItem)
        {
            RequireNonNull(source, nameof(source));

            return new CompletableToSingle<T>(source, successItem);
        }

        /// <summary>
        /// When the upstream completable source completes, the
        /// downstream maybe observer completes as well.
        /// </summary>
        /// <typeparam name="T">The type of the success item.</typeparam>
        /// <param name="source">The source completable to convert.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.9</remarks>
        public static IMaybeSource<T> ToMaybe<T>(this ICompletableSource source)
        {
            RequireNonNull(source, nameof(source));

            return new CompletableToMaybeComplete<T>(source);
        }

        /// <summary>
        /// When the upstream completable source completes, the
        /// downstream maybe observer receives a success item.
        /// </summary>
        /// <typeparam name="T">The type of the success item.</typeparam>
        /// <param name="source">The source completable to convert.</param>
        /// <param name="successItem">The item to emit when the source completes.</param>
        /// <returns>The new maybe source instance.</returns>
        /// <remarks>Since 0.0.9</remarks>
        public static IMaybeSource<T> ToMaybe<T>(this ICompletableSource source, T successItem)
        {
            RequireNonNull(source, nameof(source));

            return new CompletableToMaybeSuccess<T>(source, successItem);
        }

        /// <summary>
        /// Subscribe to a completable source and expose the terminal
        /// signal as a <see cref="Task"/>.
        /// </summary>
        /// <param name="source">The source completable to convert.</param>
        /// <param name="cts">The cancellation token source to watch for external cancellation.</param>
        /// <returns>The new task instance.</returns>
        /// <remarks>Since 0.0.9</remarks>
        public static Task ToTask(this ICompletableSource source, CancellationTokenSource cts = null)
        {
            RequireNonNull(source, nameof(source));

            var parent = new CompletableToTask();
            parent.Init(cts);
            source.Subscribe(parent);
            return parent.Task;
        }

        /// <summary>
        /// Maps the elements of an observable source into completable sources
        /// and runs them one after the other completes, optionally delaying
        /// errors until all sources terminate.
        /// </summary>
        /// <typeparam name="T">The element type of the upstream observable source.</typeparam>
        /// <param name="source">The observable source to map into inner completable sources.</param>
        /// <param name="mapper">The function receiving the upstream item and returns a completable source.</param>
        /// <param name="delayErrors">If true, all errors are delayed until the main source and all
        /// inner sources terminate.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.7</remarks>
        public static ICompletableSource ConcatMap<T>(this IObservable<T> source, Func<T, ICompletableSource> mapper, bool delayErrors = false)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));

            return new CompletableConcatMap<T>(source, mapper, delayErrors);
        }

        /// <summary>
        /// Maps the upstream items into completable sources, runs
        /// some or all of them and terminates when all sources terminate.
        /// </summary>
        /// <typeparam name="T">The value type of the observable sequence.</typeparam>
        /// <param name="source">The source observable sequence to map.</param>
        /// <param name="mapper">The function that takes an upstream item and
        /// should return a completable source.</param>
        /// <param name="delayErrors">If true, errors from any source are delayed until all sources terminate.</param>
        /// <param name="maxConcurrency">The maximum number of inner completable sources to run at once.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.10</remarks>
        public static ICompletableSource FlatMap<T>(this IObservable<T> source, Func<T, ICompletableSource> mapper, bool delayErrors = false, int maxConcurrency = int.MaxValue)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));

            return new CompletableFlatMapObservable<T>(source, mapper, delayErrors, maxConcurrency);
        }

        /// <summary>
        /// Maps the upstream observable sequence into a
        /// completable source and switches to it, disposing the
        /// previous completable source, optionally delaying
        /// errors until all sources terminated.
        /// </summary>
        /// <typeparam name="T">The element type of the observable sequence.</typeparam>
        /// <param name="source">The observable sequence to map.</param>
        /// <param name="mapper">The function receiving the upstream item and
        /// should return a completable source.</param>
        /// <param name="delayErrors">If true, all errors are delayed until all sources terminate.</param>
        /// <returns>The new completable source instance.</returns>
        /// <remarks>Since 0.0.10</remarks>
        public static ICompletableSource SwitchMap<T>(this IObservable<T> source, Func<T, ICompletableSource> mapper, bool delayErrors = false)
        {
            RequireNonNull(source, nameof(source));
            RequireNonNull(mapper, nameof(mapper));

            return new CompletableSwitchMap<T>(source, mapper, delayErrors);
        }
    }
}
