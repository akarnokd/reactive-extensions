using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceTakeWhileTest
    {
        [Test]
        public void Basic()
        {
            ObservableSource.Range(1, 5)
                .TakeWhile(v => v < 4)
                .Test()
                .AssertResult(1, 2, 3);
        }

        [Test]
        public void Exclude_All()
        {
            ObservableSource.Range(1, 5)
                .TakeWhile(v => false)
                .Test()
                .AssertResult();
        }

        [Test]
        public void Include_All()
        {
            ObservableSource.Range(1, 5)
                .TakeWhile(v => v < 6)
                .Test()
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .TakeWhile(v => v < 4)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Predicate_Crash()
        {
            ObservableSource.Range(1, 5)
                .TakeWhile(v =>
                {
                    if (v == 4)
                    {
                        throw new InvalidOperationException();
                    }
                    return true;
                })
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 1, 2, 3);
        }

        [Test]
        public void Dispose()
        {
            TestHelper.VerifyDisposeObservableSource<int, int>(o => o.TakeWhile(v => v < 4));
        }

        [Test]
        public void False_Disposes()
        {
            var ps = new PublishSubject<int>();

            var to = ps
                .TakeWhile(v => v < 4)
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

            to.AssertResult(1, 2, 3);
        }
    }
}
