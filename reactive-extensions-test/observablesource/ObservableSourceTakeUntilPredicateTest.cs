using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceTakeUntilPredicateTest
    {
        [Test]
        public void Basic()
        {
            ObservableSource.Range(1, 5)
                .TakeUntil(v => v == 3)
                .Test()
                .AssertResult(1, 2, 3);
        }

        [Test]
        public void Exclude_All()
        {
            ObservableSource.Range(1, 5)
                .TakeUntil(v => true)
                .Test()
                .AssertResult(1);
        }

        [Test]
        public void Include_All()
        {
            ObservableSource.Range(1, 5)
                .TakeUntil(v => false)
                .Test()
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .TakeUntil(v => v == 3)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Predicate_Crash()
        {
            ObservableSource.Range(1, 5)
                .TakeUntil(v =>
                {
                    if (v == 3)
                    {
                        throw new InvalidOperationException();
                    }
                    return false;
                })
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 1, 2, 3);
        }

        [Test]
        public void Dispose()
        {
            TestHelper.VerifyDisposeObservableSource<int, int>(o => o.TakeUntil(v => v == 4));
        }

        [Test]
        public void False_Disposes()
        {
            var ps = new PublishSubject<int>();

            var to = ps
                .TakeUntil(v => v == 4)
                .Test();

            Assert.True(ps.HasObservers);

            ps.OnNext(1);

            Assert.True(ps.HasObservers);
            to.AssertValuesOnly(1);

            ps.OnNext(2);

            Assert.True(ps.HasObservers);
            to.AssertValuesOnly(1, 2);

            ps.OnNext(3);

            Assert.True(ps.HasObservers);
            to.AssertValuesOnly(1, 2, 3);

            ps.OnNext(4);

            Assert.False(ps.HasObservers);

            to.AssertResult(1, 2, 3, 4);
        }
    }
}
