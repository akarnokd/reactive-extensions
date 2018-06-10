using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceWindowBoundaryTest
    {
        [Test]
        public void Basic()
        {
            var subj = new PublishSubject<int>();

            var boundary = new PublishSubject<int>();

            var to = subj.Window(boundary).Test();

            to.AssertEmpty();

            Assert.True(subj.HasObservers);
            Assert.True(boundary.HasObservers);

            boundary.OnNext(1);

            to.AssertEmpty();

            subj.OnNext(1);

            to.AssertValueCount(1);

            subj.OnNext(2);

            to.AssertValueCount(1);

            boundary.OnNext(1);

            Assert.True(subj.HasObservers);
            Assert.True(boundary.HasObservers);

            to.AssertValueCount(1);

            to.Items[0].Test().AssertResult(1, 2);

            subj.OnNext(3);

            to.AssertValueCount(2);

            subj.OnCompleted();

            Assert.False(subj.HasObservers);
            Assert.False(boundary.HasObservers);

            to.Items[1].Test().AssertResult(3);

            to.AssertCompleted()
                .AssertNoError();
        }

        [Test]
        public void Error_Main()
        {
            var subj = new PublishSubject<int>();

            var boundary = new PublishSubject<int>();

            var to = subj.Window(boundary).Test();

            to.AssertEmpty();

            Assert.True(subj.HasObservers);
            Assert.True(boundary.HasObservers);

            subj.OnError(new InvalidOperationException());

            Assert.False(subj.HasObservers);
            Assert.False(boundary.HasObservers);

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Error_Boundary()
        {
            var subj = new PublishSubject<int>();

            var boundary = new PublishSubject<int>();

            var to = subj.Window(boundary).Test();

            to.AssertEmpty();

            Assert.True(subj.HasObservers);
            Assert.True(boundary.HasObservers);

            boundary.OnError(new InvalidOperationException());

            Assert.False(subj.HasObservers);
            Assert.False(boundary.HasObservers);

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Complete_Boundary()
        {
            var subj = new PublishSubject<int>();

            var boundary = new PublishSubject<int>();

            var to = subj.Window(boundary).Test();

            to.AssertEmpty();

            Assert.True(subj.HasObservers);
            Assert.True(boundary.HasObservers);

            boundary.OnCompleted();

            Assert.False(subj.HasObservers);
            Assert.False(boundary.HasObservers);

            to.AssertResult();
        }
    }
}
