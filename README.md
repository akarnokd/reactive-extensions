# reactive-extensions

<!-- <a href='https://www.nuget.org/packages/reactive-extensions'><img src='https://img.shields.io/nuget/v/reactive-extensions.svg' alt="reactive-extensions NuGet version"/></a> -->
<a href='https://travis-ci.org/akarnokd/reactive-extensions/builds'><img src='https://travis-ci.org/akarnokd/reactive-extensions.svg?branch=master'></a>

Extensions to the dotnet/reactive library.

## Operators

These operators are available as extension methods on `IObservable` via the
`akarnokd.reactive_extensions.ReactiveExtensions` static class.

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

### ObserveOn

Observe the signals of the upstream on a specified scheduler and optionally
delay any error after all upstream items have been delivered.

(The standard `ObserveOn` by default allows errors to cut ahead.)

### Test

Subscribe a `TestObserver` to the observable sequence to perform various
convenient assertions.

### ToSerialized

Wraps an `IObserver` and serializes the calls to its `OnNext`, `OnError` and `OnCompleted`
methods by making sure they are called non-overlappingly and non-concurrently.

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

### UnicastSubject

A special `ISubject` that supports exactly one `IObserver` during the subject's lifetime
and buffers signals until such observer subscribes to it.