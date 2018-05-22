using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Collections.Generic;

namespace akarnokd.reactive_extensions_test.single
{
    [TestFixture]
    public class SingleFlatMapEnumerableTest
    {
        [Test]
        public void Success_To_Empty()
        {
            SingleSource.Just(1)
                .FlatMap(v => Enumerable.Empty<int>())
                .Test()
                .AssertResult();
        }

        [Test]
        public void Success_To_Just()
        {
            SingleSource.Just(1)
                .FlatMap(v => Enumerable.Range(v, 1))
                .Test()
                .AssertResult(1);
        }

        [Test]
        public void Success_To_Range()
        {
            SingleSource.Just(1)
                .FlatMap(v => Enumerable.Range(v, 5))
                .Test()
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Take()
        {
            SingleSource.Just(1)
                .FlatMap(v => Enumerable.Range(v, 5))
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
                .FlatMap(v => Enumerable.Range(v, 5))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Mapper_Crash()
        {
            Func<int, IEnumerable<int>> f = v => throw new InvalidOperationException();

            SingleSource.Just(1)
                .FlatMap(f)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void GetEnumerable_Crash()
        {
            SingleSource.Error<int>(new InvalidOperationException())
                .FlatMap(v => new FailingEnumerable<int>(true, false, false))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void MoveNext_Crash()
        {
            SingleSource.Error<int>(new InvalidOperationException())
                .FlatMap(v => new FailingEnumerable<int>(false, true, false))
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
