using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace akarnokd.reactive_extensions_test.single
{
    [TestFixture]
    public class SingleFlatMapObservableTest
    {
        [Test]
        public void Success_To_Empty()
        {
            SingleSource.Just(1)
                .FlatMap(v => Observable.Empty<int>())
                .Test()
                .AssertResult();
        }

        [Test]
        public void Success_To_Just()
        {
            SingleSource.Just(1)
                .FlatMap(v => Observable.Range(v, 1))
                .Test()
                .AssertResult(1);
        }

        [Test]
        public void Success_To_Range()
        {
            SingleSource.Just(1)
                .FlatMap(v => Observable.Range(v, 5))
                .Test()
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Take()
        {
            SingleSource.Just(1)
                .FlatMap(v => Observable.Range(v, 5))
                .SubscribeOn(NewThreadScheduler.Default)
                .Take(3)
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertResult(1, 2, 3);
        }

        [Test]
        public void Error()
        {
            SingleSource.Error<int>(new InvalidOperationException())
                .FlatMap(v => Observable.Range(v, 5))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Mapper_Crash()
        {
            Func<int, IObservable<int>> f = v => throw new InvalidOperationException();

            SingleSource.Just(1)
                .FlatMap(f)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Error_Other()
        {
            SingleSource.Just(1)
                .FlatMap(v => Observable.Throw<int>(new InvalidOperationException()))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Dispose()
        {
            TestHelper.VerifyDisposeSingle<int, int>(m => m.FlatMap(v => Enumerable.Range(1, 5)));
        }
    }
}
