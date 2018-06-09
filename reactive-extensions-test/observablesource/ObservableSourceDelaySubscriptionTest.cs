using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceDelaySubscriptionTest
    {
        [Test]
        public void Other_Basic()
        {
            ObservableSource.Range(1, 5)
                .DelaySubscription(ObservableSource.Range(6, 5))
                .Test()
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Other_Basic_2()
        {
            ObservableSource.Range(1, 5)
                .DelaySubscription(ObservableSource.Empty<int>())
                .Test()
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Other_Next_Disposes()
        {
            var subj = new PublishSubject<int>();

            var to = ObservableSource.Range(1, 5)
                .DelaySubscription(subj)
                .Test();

            Assert.True(subj.HasObservers);

            subj.OnNext(1);

            Assert.False(subj.HasObservers);

            to
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Other_Error_Main()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .DelaySubscription(ObservableSource.Range(6, 5))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Other_Error_Other()
        {
            var count = 0;

            ObservableSource.FromFunc(() => ++count)
                .DelaySubscription(ObservableSource.Error<int>(new InvalidOperationException()))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Other_Dispose_Other()
        {
            TestHelper.VerifyDisposeObservableSource<int, int>(o => ObservableSource.Range(1, 5).DelaySubscription(o));
        }

        [Test]
        public void Other_Dispose_Source()
        {
            TestHelper.VerifyDisposeObservableSource<int, int>(o => o.DelaySubscription(ObservableSource.Empty<int>()));
        }
    }
}
