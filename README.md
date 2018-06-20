# reactive-extensions

Travis-CI: <a href='https://travis-ci.org/akarnokd/reactive-extensions/builds'><img src='https://travis-ci.org/akarnokd/reactive-extensions.svg?branch=master'></a>
VSTS: [![Build Status](https://akarnokd.visualstudio.com/_apis/public/build/definitions/94ed59dd-ad54-4947-8246-cf05f35562a6/7/badge)](https://akarnokd.visualstudio.com/MyFirstProject/_build/index?definitionId=7)
NuGet: <a href='https://www.nuget.org/packages/akarnokd.reactive_extensions'><img src='https://img.shields.io/nuget/v/akarnokd.reactive_extensions.svg' alt="reactive-extensions NuGet version"/></a>

Extensions to the [dotnet/reactive](https://github.com/dotnet/reactive) library.

### Setup

```
Install-Package akarnokd.reactive_extensions -Version 0.0.24-alpha
```

### Dependencies

- `NETStandard2.0`
- [`System.Reactive` 4.0.0 or newer](https://www.nuget.org/packages/System.Reactive/).


### Table of contents

- `IObservable` support
  - [Operators](#operators)
  - [Other classes](#other-classes) 
    - [TestObserver](#testobserver)
    - [TestScheduler](#testscheduler)
- New reactive types
  - [ICompletableSource](#icompletablesource)
    - [CompletableSubject](#completablesubject)
  - [ISingleSource](#isinglesource)
    - [SingleSubject](#singlesubject)
  - [IMaybeSource](#imaybesource)
    - [MaybeSubject](#maybesubject)
  - [IObservableSource](#iobservablesource)
    - [PublishSubject](#publishsubject)
	- [CacheSubject](#cachesubject)
	- [MonocastSubject](#monocastsubject)
  - [IAsyncEnumerable](#iasyncenumerable)

## Operators

These operators are available as extension methods on `IObservable` via the
`akarnokd.reactive_extensions.ReactiveExtensions` static class.

```cs
using akarnokd.reactive_extensions;
```

- Datasources
  - [Create](#create)
  - [IntervalRange](#intervalrange)
- Side-effecting sequences
  - [DoAfterNext](#doafternext)
  - [DoAfterTerminate](#doafterterminate)
  - [DoFinally](#dofinally)
  - [DoOnDispose](#doondispose)
  - [DoOnSubscribe](#doonsubscribe)
- Custom asynchronous boundaries
  - [ObserveOn](#observeon)
  - [ToSerialized](#toserialized)
- Combinators
  - [CombineLatest](#combinelatest)
  - [ConcatEager](#concateager)
  - [ConcatMap](#concatmap)
  - [ConcatMapEager](#concatmapeager)
  - [ConcatMany](#concatmany)
  - [MergeMany](#mergemany)
  - [SwitchMany](#switchmany)
  - [SwitchMap](#switchmap)
  - [WithLatestFrom](#withlatestfrom)
  - [Zip](#zip)
- Aggregators
  - [Cache](#cache)
  - [Collect](#collect)
- Composition
  - [Compose](#compose)
- Termination handling
  - [Repeat](#repeat)
  - [RepeatWhen](#repeatwhen)
  - [Retry](#retry)
  - [RetryWhen](#retrywhen)
  - [SwitchIfEmpty](#switchifempty)
  - [TakeUntil](#takeuntil)
  - [UnsubscribeOn](#unsubscribeon)
- Hot <-> Cold conversion
  - [AutoConnect](#autoconnect)
- Blocking
  - [BlockingEnumerable](#blockingenumerable)
  - [BlockingSubscribe](#blockingsubscribe)
  - [BlockingSubscribeWhile](#blockingsubscribewhile)
- Test support
  - [Test](#test)

### AutoConnect

Automatically connect the upstream `IConnectableObservable` at most once when the
specified number of `IObserver`s have subscribed to this `IObservable`.

*Since 0.0.4*

### BlockingEnumerable

Consumes the source observable in a blocking fashion
through an IEnumerable.

```cs
var source = Observable.Range(1, 5).SubscribeOn(NewThreadScheduler.Default);

foreach (var v in source.BlockingEnumerable()) {
	Console.WriteLine(v);
}
Console.WriteLine("Done");
```

*Since 0.0.4*

### BlockingSubscribe

Consumes the source observable in a blocking fashion
via callbacks or an observer on the caller thread.

Consuming via an `IObservable`:

```cs
var source = Observable.Range(1, 5).SubscribeOn(NewThreadScheduler.Default);

var to = new TestObserver<int>();
source.BlockingSubscribe(to);

to.AssertResult(1, 2, 3, 4, 5);
```

Consuming via `Action` delegates:

```cs
source.BlockingSubscribe(
    Console.WriteLine, 
	Console.WriteLine, 
	() => Console.WriteLine("Done")
);
```

*Since 0.0.4*<br/>
See also: [BlockingSubscribeWhile](#blockingsubscribewhile)

### BlockingSubscribeWhile

Consumes the source observable in a blocking fashion on
the current thread and relays events to the given callback actions
while the `onNext` predicate returns true.

```cs
source.BlockingSubscribe(v => {
    Console.WriteLine(v);
    return v == 3;
}, Console.WriteLine, () => Console.WriteLine("Done"));
```

*Since 0.0.4*<br/>
See also: [BlockingSubscribe](#blockingsubscribe)

### Cache

Subscribes once to the upstream when the first observer subscribes to the operator, buffers
all upstream events and relays/replays them to current or late observers.

```cs
var count = 0;

var cached = Observable.Range(1, 5)
.DoOnSubscribe(() => count++)
.Cache();

// not yet subscribed to Range()
Assert.AreEqual(0, count);

// relays events live
cached.Test().AssertResult(1, 2, 3, 4, 5);

Assert.AreEqual(1, count);

// replays the buffered events
cached.Test().AssertResult(1, 2, 3, 4, 5);

// no further subscriptions
Assert.AreEqual(1, count);
```

*Since 0.0.4*

### Collect

Collects upstream items into a per-observer collection object created
via a function and added via a collector action and emits this collection
when the upstream completes.

```cs
Observable.Range(1, 5)
.Collect(() => new List<int>(), (a, b) => a.Add(b))
.Test()
.AssertResult(new List<int>() { 1, 2, 3, 4, 5 });
```

*Since: 0.0.3*

### CombineLatest

Combines the latest items of each source observable through
a mapper function, optionally delaying errors until all
of them terminates.

```cs
ReactiveExtensions.CombineLatest(a => a, source1, source);

new [] { source1, source2 }.CombineLatest(a => a, true);
```

*Since: 0.0.5*

### Compose

Applies a function to the source at assembly-time and returns the
observable returned by this function.
This allows creating reusable set of operators to be applied on observables.

```cs
Func<IObservable<int>, IObservable<int>> applySchedulers =
    o => o.SubscribeOn(Scheduler.NewThread).ObserveOn(Scheduler.UI);

var o1 = Observable.Range(1, 5).Compose(applySchedulers);
var o2 = Observable.Range(10, 5).Compose(applySchedulers);
var o3 = Observable.Range(20, 5).Compose(applySchedulers);
```
*Since: 0.0.3*

### ConcatEager

Concatenates a sequence of observables eagerly by running some
or all of them at once and emitting their items in order.
It is equivalent to `sources.ConcatMapEager(v => v)`.

```cs
new[] 
{
    Observable.Range(1, 5),
    Observable.Range(6, 5),
    Observable.Range(11, 5)
}
.ToObservable()
.ConcatEager()
.Test()
.AssertResult(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15);
```

*Since: 0.0.3*<br/>
See also: [ConcatMapEager](#concatmapeager)

### ConcatMap

Maps the upstream values into enumerables and emits their items in-order.

### ConcatMany

Concatenates (flattens) a sequence of observables.

### ConcatMapEager

Maps the upstream values into observables, runs some or all of them at once and emits items of
one such observable until it completes, then switches to the next observable, and so on until no
more observables are running.

*Since: 0.0.2*<br/>
See also: [ConcatEager](#concateager)

### Create

Creates an observable sequence by providing an emitter
API to bridge the callback world with the reactive world.

```cs
ReactiveExtensions.Create(emitter => {

	Task.Factory.StartNew(() => {
		for (int i = 0; i < 1000; i++) {
		    if (emitter.IsDisposed()) {
				return;
			}
			emitter.OnNext(1);
		}
	    if (emitter.IsDisposed()) {
			return;
		}
		emitter.OnCompleted();
	});

}, true)
.Test()
.AwaitDone(TimeSpan.FromSeconds(5))
.AssertValueCount(1000)
.AssertNoError()
.AssertCompleted();
```

*Since: 0.0.5*

### DoAfterNext

Call a handler after the current item has been emitted to the downstream.

### DoAfterTerminate

Call a handler after the upstream terminated normally or with an error.

### DoOnSubscribe

Call a handler just before the downstream subscribes to the upstream.

### DoOnDispose

Call a handler when the downstream disposes the sequence.

### DoFinally

Call a handler after the upstream terminates normally, with an error or the
downstream disposes the sequence.

### IntervalRange

Emits a range of values over time.


```cs
ReactiveExtensions.IntervalRange(1, 5, 
    TimeSpan.FromSeconds(1), TimeSpan.FromSecoconds(2), NewThreadScheduler.Default)
	.Test()
	.AwaitDone(TimeSpan.FromSeconds(12))
	.AssertResult(1, 2, 3, 4, 5);
```

### MergeMany

Merges some or all inner observables provided via an observable sequence and emits their
items in a sequential manner.

### ObserveOn

Observe the signals of the upstream on a specified scheduler and optionally
delay any error after all upstream items have been delivered.

(The standard `ObserveOn` by default allows errors to cut ahead.)

### Repeat

Repeatedly re-subscribes to the source observable if the predicate
function returns true upon the completion of the previous
subscription.

*Since: 0.0.3*

### RepeatWhen

Repeats (resubscribes to) the source observable after a normal completion and when the observable
returned by a handler produces an arbitrary item.

*Since: 0.0.4*

### Retry

Repeatedly re-subscribes to the source observable if the predicate
function returns true upon the failure of the previous
subscription.

*Since: 0.0.3*

### RetryWhen

Retries (resubscribes to) the source observable after a failure and when the observable
returned by a handler produces an arbitrary item.

*Since: 0.0.4*

### SwitchIfEmpty

Switches to the (next) fallback observable if the main source
or a previous fallback is empty.

```cs
var source = new Subject<int>();

var to = source.SwitchIfEmpty(Observable.Range(1, 5)).Test();

source.OnCompleted();

to.AssertResult(1, 2, 3, 4, 5);
```

*Since: 0.0.3*

### SwitchMany

Switches to a new inner observable when the outer observable produces one,
disposing the previous active observable.
The operator can be configured to delay all errors until all sources have
terminated.

*Since: 0.0.4*<br/>
See also: [SwitchMap](#switchmap)

### SwitchMap

Switches to a new observable mapped via a function in response to
an new upstream item, disposing the previous active observable.
The operator can be configured to delay all errors until all sources have
terminated.


*Since: 0.0.4*<br/>
See also: [SwitchMany](#switchmany)

### TakeUntil

Checks a predicate after an item has been emitted and completes
the sequence if it returns false.

```cs
Observable.Range(1, 5)
.TakeUntil(v => v == 3)
.Test()
.AssertResult(1, 2, 3);
```

*Since: 0.0.3*

### Test

Subscribe a `TestObserver` to the observable sequence to perform various
convenient assertions.

### ToSerialized

Wraps an `IObserver` or an `ISubject` and serializes the calls to its `OnNext`, `OnError` and `OnCompleted`
methods by making sure they are called non-overlappingly and non-concurrently.

### UnsubscribeOn

Makes sure the `Dispose()` call towards the upstream happens on the specified
scheduler.

*Since: 0.0.4*

### WithLatestFrom

Combines the latest values of multiple alternate observables with the value of the
main observable sequence through a function. Unlike `CombineLatest`, new items
from the alternate observables do not trigger an emission. The operator can
optionally delay errors from the alternate sources until the main source terminates.

*Since: 0.0.4*

### Zip

Combines the next item of each source observable (a row of values) through
a mapper function, optionally delaying errors until no more
rows can be created.

```cs
ReactiveExtensions.Zip(a => a, source1, source);

new [] { Observable.Range(1, 5), Observable.Range(1, 10) }
.Zip(a => a[0] + a[1], true)
.Test()
.AssertResult(2, 4, 6, 8, 10);
```

*Since: 0.0.5*


## Other classes

### TestObserver

An `IObserver` that offers a bunch of `AssertX` methods to check what signals
has been received from the upstream as well as have some trivial output checks
in a more convenient fashion.

```cs
var to = new TestObserver<int>();

Observable.Range(1, 5).Subscribe(to);

to.AssertResult(1, 2, 3, 4, 5);
```

There is the `Test()` extension method to make this more convenient in some cases:

```cs
Observable.Range(1, 5)
.Test()
.AwaitDone(TimeSpan.FromSeconds(5))
.AssertResult(1, 2, 3, 4 ,5)
```

### TestScheduler

An `IScheduler` implementation that allows manually advancing a virtual
time and thus executing tasks up to a certain due time.

```cs
var scheduler = new TestScheduler();

var to = Observable.IntervalRange(1, 5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), scheduler)
.Test();

to.AssertEmpty();

scheduler.AdvanceTimeBy(TimeSpan.FromSeconds(1));

to.AssertValuesOnly(1);

scheduler.AdvanceTimeBy(TimeSpan.FromSeconds(4));

to.AssertValuesOnly(1, 2, 3);

scheduler.AdvanceTimeBy(TimeSpan.FromSeconds(4));

to.AssertResult(1, 2, 3, 4, 5);

Assert.False(scheduler.HasTasks());
```

*Since 0.0.6*

### UnicastSubject

A special `ISubject` that supports exactly one `IObserver` during the subject's lifetime
and buffers signals until such observer subscribes to it.

## ICompletableSource

A reactive type which signals an `OnCompleted()` or an `OnError()`, no items.

Consumer type: `ICompletableObserver`

Extension methods host: `CompletableSource`

*Since: 0.0.5*

### Factory methods

- `Amb`
- `AmbAll`
- `Concat`
- `ConcatAll`
- `Create`
- `Defer`
- `Empty`
- `Error`
- `FromAction`
- `FromTask`
- `Merge`
- `MergeAll`
- `Never`
- `Timer`
- `Using`

### Instance methods

- `AndThen`
- `Compose`
- `Cache`
- `Delay`
- `DelaySubscription`
- `DoAfterTerminate`
- `DoFinally`
- `DoOnCompleted`
- `DoOnDispose`
- `DoOnError`
- `DoOnSubscribe`
- `DoOnTerminate`
- `ObserveOn`
- `OnErrorComplete`
- `OnErrorResumeNext`
- `OnTerminateDetach`
- `Repeat`
- `RepeatWhen`
- `Retry`
- `RetryWhen`
- `SubscribeOn`
- `TakeUntil`
- `Timeout`
- `UnsubscribeOn`

### Consumer methods

- `BlockingSubscribe`
- `Subscribe`
- `SubscribeSafe`
- `SubscribeWith`
- `Test`
- `Wait`

### Interoperation

- `AndThen` (with `IObservable`, `ISingleSource` & `IMaybeSource`)
- `ConcatMap` (on `Observable`)
- `FlatMap` (on `Observable`)
- `IgnoreAllElements` (on `IObservable`)
- `SwitchMap` (on `Observable`)
- `ToCompletable` (on `Task`)
- `ToMaybe`
- `ToObservable`
- `ToSingle`
- `ToTask`

### CompletableSubject

A completable-based, hot subject that multicasts the termination event
it receives through its completable observer API surface.

This subject supports an optional reference-counting behavior: when
all completable observers dispose before the subject terminates, it will
terminate its upstream connection (if any).

*Since 0.0.6*

## ISingleSource

A reactive type wich signals an `OnSuccess()` or an `OnError()`.

Consumer type: `ISingleObserver<T>`

Extension methods host: `SingleSource`

*Since: 0.0.5*

### Factory methods

- `Amb`
- `AmbAll`
- `Concat`
- `ConcatAll`
- `ConcatEager`
- `ConcatEagerAll`
- `Create`
- `Defer`
- `Error`
- `FromFunc`
- `FromTask`
- `Just`
- `Merge`
- `MergeAll`
- `Never`
- `Timer`
- `Using`
- `Zip`

### Instance methods

- `Compose`
- `Filter`
- `FlatMap`
- `Cache`
- `Delay`
- `DelaySubscription`
- `DoAfterSuccess`
- `DoAfterTerminate`
- `DoOnDispose`
- `DoOnError`
- `DoOnSubscribe`
- `DoOnSuccess`
- `DoOnTerminate`
- `FlatMap` (onto `IEnumerable`, `ISingleSource`)
- `Map`
- `ObserveOn`
- `OnErrorResumeNext`
- `OnTerminateDetach`
- `Repeat`
- `RepeatWhen`
- `Retry`
- `RetryWhen`
- `SubscribeOn`
- `SubscribeSafe`
- `TakeUntil`
- `Timeout`
- `UnsubscribeOn`

### Consumer methods

- `BlockingSubscribe`
- `Subscribe`
- `SubscribeSafe`
- `SubscribeWith`
- `Test`
- `Wait`

### Interoperation

- `ConcatMap` (on `IObservable`)
- `ConcatMapEager` (on `IObservable`)
- `ElementAtOrDefault`
- `ElementAtOrError`
- `FirstOrDefault`
- `FirstOrError`
- `FlatMap` (onto `ICompletableSource`, `IMaybeSource`, `IObservable`)
- `IgnoreElement`
- `LastOrDefault`
- `LastOrError`
- `SingleOrDefault`
- `SingleOrError`
- `SwitchMap` (on `IObservable`)
- `ToMaybe`
- `ToObservable`
- `ToSingle`
- `ToTask`

### SingleSubject

A single-based, hot subject that multicasts the value or failure event
it receives through its single observer API surface.

This subject supports an optional reference-counting behavior: when
all single observers dispose before the subject succeeds or fails, it will
terminate its upstream connection (if any).

*Since 0.0.9*

## IMaybeSource

A reactive type wich signals an `OnSuccess()`, an `OnError()` or an `OnCompleted()`
in a mutually exclusive fashion.

Consumer type: `IMaybeObserver<T>`

Extension methods host: `MaybeSource`

*Since: 0.0.5*

### Factory methods

- `Amb`
- `Create`
- `Concat`
- `ConcatAll`
- `ConcatEager`
- `ConcatEagerAll`
- `Defer`
- `Empty`
- `Error`
- `FromAction`
- `FromFunc`
- `FromTask`
- `Just`
- `Merge`
- `MergeAll`
- `Never`
- `Timer`
- `Using`
- `Zip`

### Instance methods

- `Cache`
- `Compose`
- `DefaultIfEmpty`
- `Delay`
- `DelaySubscription`
- `DoAfterSuccess`
- `DoAfterTerminate`
- `DoFinally`
- `DoOnCompleted`
- `DoOnDispose`
- `DoOnError`
- `DoOnSubscribe`
- `DoOnSuccess`
- `DoOnTerminate`
- `Filter`
- `Map`
- `ObserveOn`
- `OnErrorComplete`
- `OnErrorResumeNext`
- `OnTerminateDetach`
- `Repeat`
- `RepeatWhen`
- `Retry`
- `RetryWhen`
- `SubscribeOn`
- `SwitchIfEmpty`
- `TakeUntil`
- `Timeout`
- `UnsubscribeOn`

### Consumer methods

- `BlockingSubscribe`
- `Subscribe`
- `SubscribeSafe`
- `SubscribeWith`
- `Test`
- `Wait`

### Interoperation

- `ConcatMap` (on `IObservable`)
- `ConcatMapEager` (on `IObservable`)
- `ElementAtIndex` (on `IObservable`)
- `FirstElement` (on `IObservable`)
- `FlatMap` (onto `ICompletableSource`, `ISingleSource`, `IObservable`, `IEnumerable`)
- `FlatMap` (on `IObservable`)
- `IgnoreElement`
- `LastElement` (on `IObservable`)
- `SingleElement` (on `IObservable`)
- `SwitchMap` (on `IObservable`)
- `ToObservable`
- `ToSingle`
- `ToTask`

### MaybeSubject

A maybe-based, hot subject that multicasts the value or termination event
it receives through its maybe observer API surface.

This subject supports an optional reference-counting behavior: when
all maybe observers dispose before the subject succeeds or terminates, it will
terminate its upstream connection (if any).

*Since 0.0.9*

## IObservableSource

*Since 0.0.17*

A fully push-based observable source implementation that fixes the issues with
the `IObservable` interface.

Consumer type: `ISignalObserver` (may change)

Main differences:

- `IObservable.Subscribe` is now void and thus fire-and-forget. Works both in synchronous and asynchronous cases because cancellation is now pushed down,
- `ISignalObserver.OnSubscribe` receives a Disposable upfront so it can be cancelled synchronously and asynchronously.

### Factory methods

- `Amb`
- `CombineLatest`
- `Concat`
- `ConcatEager`
- `Create`
- `Defer`
- `Empty`
- `Error`
- `FromAction`
- `FromArray`
- `FromFunc`
- `Interval`
- `IntervalRange`
- `Just`
- `Merge`
- `Never`
- `Range`
- `RangeLong`
- `Switch`
- `Timer`
- `Using`
- `Zip`

### Instance methods

- `All`
- `Any`
- `Buffer`
- `Cache`
- `Collect`
- `Compose`
- `ConcatMap`
- `ConcatMapEager`
- `Delay`
- `DelaySubscription`
- `DoAfterNext`
- `DoAfterTerminate`
- `DoFinally`
- `DoOnCompleted`
- `DoOnDispose`
- `DoOnError`
- `DoOnNext`
- `DoOnSubscribe`
- `DoOnTerminate`
- `ElementAt`
- `Filter`
- `First`
- `FlatMap`
- `GroupBy`
- `Hide`
- `IgnoreElements`
- `IsEmpty`
- `Join`
- `Last`
- `Map`
- `Max`
- `Min`
- `Multicast`
- `ObserveOn`
- `OnErrorResumeNext`
- `OnTerminateDetach`
- `Publish`
- `Reduce`
- `Repeat`
- `RepeatWhen`
- `Replay`
- `Retry`
- `RetryWhen`
- `Single`
- `Skip`
- `SkipLast`
- `SkipUntil`
- `SkipWhile`
- `SubscribeOn`
- `Sum`
- `SwitchIfEmpty`
- `SwitchMap`
- `Take`
- `TakeLast`
- `TakeUntil`
- `TakeWhile`
- `Timeout`
- `ToDictionary`
- `ToList`
- `ToLookup`
- `UnsubscribeOn`
- `Window`
- `WithLatestFrom`

### Consumer methods

- `BlockingEnumerable`
- `BlockingSubscribe`
- `BlockingSubscribeWhile`
- `BlockingFirst`
- `BlockingLast`
- `BlockingSingle`
- `BlockingTryFirst`
- `BlockingTryLast`
- `BlockingTrySingle`
- `Subscribe`
- `SubscribeWith`
- `Test`

### Interoperation with other async types

- `ElementAtTask`
- `FirstTask`
- `FromTask`
- `IgnoreElementsTask`
- `LastTask`
- `SingleTask`
- `ToObservableSource` (on `T[]`, `IObservable`, `IEnumerable`, `Task`)
- `ToObservable` (into `IObservable`)

### IConnectableObservableSource

Represents an observable source that can be connected and disconnected from, 
allowing signal observers to gather up before the flow starts.

- `AutoConnect`
- `RefCount`

### PublishSubject

Multicasts the same items to one or more currently subscribed observers (same as an Rx.NET `Subject`).

### MonocastSubject

Buffers items until a single observer subscribes and replays/relays all items to it.


### CacheSubject

Caches some or all items and relays/replays it to current or future observers.

## IAsyncEnumerable

*Since: 0.0.25*

Implementation of sources and operators for the async-streams from the [C# 8 proposal](https://github.com/dotnet/csharplang/blob/master/proposals/async-streams.md).
Since there are no external package/assembly available with the interface definitions, the proposed
structure is implemented in the `akarnokd.reactive_extensions` namespace:

- `IAsyncEnumerable<T>`: the parent type for lazy-deferred enumerator creation
- `IAsyncEnumerator<T>`: the interface that can be consumed asynchronously
- `IAsyncDisposable`: cancellation management with the option to wait for the cancellation to finish

In the proposal, the concern of the cost of 2 interface calls on `IAsyncEnumerator` resulted in an
alternative structure with a direct-poll like feature. This resembles the operator fusion
that the modern reactive libraries (including `IObservableSource`) uses so instead of changing
the `IAsyncEnumerator`, this library proposes a marker interface with the single `TryPoll` method
so that not all `IAsyncEnumerable`s have to support it

- `IAsyncFusedEnumerator<T>`: try polling for the next upstream item in a synchronous fashion.
- `AsyncFusedState`: the outcome of the poll, including the ability to indicate no further items will arrive.

The factory and extension methods are located in `akarnokd.reactive_extensions.AsyncEnumerable`.

### Factory methods

- `Concat`
- `Defer`
- `Empty`
- `Error`
- `FromEnumerable`
- `Just`
- `Never`
- `Range`

### Instance methods

- `Filter`
- `Map`
- `Take`
- `Skip`

### Consumers

- `BlockingFirst`
- `TestAsync`
- `TestAsyncFused`
- `ToAsyncEnumerable` (on `IEnumerable`)


### Interoperation

