using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceSkipUntilTest
    {
        [Test]
        public void Basic()
        {
            ObservableSource.Range(1, 5)
                .SkipUntil(ObservableSource.Never<int>())
                .Test()
                .AssertResult();
        }

        [Test]
        public void Until_Just()
        {
            ObservableSource.Range(1, 5)
                .SkipUntil(ObservableSource.Just(1))
                .Test()
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Until_Empty()
        {
            ObservableSource.Range(1, 5)
                .SkipUntil(ObservableSource.Empty<int>())
                .Test()
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Main_Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .SkipUntil(ObservableSource.Never<int>())
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Other_Error()
        {
            ObservableSource.Never<int>()
                .SkipUntil(ObservableSource.Error<int>(new InvalidOperationException()))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Dispose()
        {
            var subj1 = new PublishSubject<int>();
            var subj2 = new PublishSubject<int>();

            var to = subj1.SkipUntil(subj2).Test();

            Assert.True(subj1.HasObservers);
            Assert.True(subj2.HasObservers);

            to.AssertEmpty();

            to.Dispose();

            Assert.False(subj1.HasObservers);
            Assert.False(subj2.HasObservers);
        }

        [Test]
        public void Main_Completed_Disposes_Other()
        {
            var subj1 = new PublishSubject<int>();
            var subj2 = new PublishSubject<int>();

            var to = subj1.SkipUntil(subj2).Test();

            Assert.True(subj1.HasObservers);
            Assert.True(subj2.HasObservers);

            to.AssertEmpty();

            subj1.OnCompleted();

            Assert.False(subj1.HasObservers);
            Assert.False(subj2.HasObservers);

            to.AssertResult();
        }

        [Test]
        public void Main_Error_Disposes_Other()
        {
            var subj1 = new PublishSubject<int>();
            var subj2 = new PublishSubject<int>();

            var to = subj1.SkipUntil(subj2).Test();

            Assert.True(subj1.HasObservers);
            Assert.True(subj2.HasObservers);

            to.AssertEmpty();

            subj1.OnError(new InvalidOperationException());

            Assert.False(subj1.HasObservers);
            Assert.False(subj2.HasObservers);

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Ohter_Completed_Doesnt_Dispose_Main()
        {
            var subj1 = new PublishSubject<int>();
            var subj2 = new PublishSubject<int>();

            var to = subj1.SkipUntil(subj2).Test();

            Assert.True(subj1.HasObservers);
            Assert.True(subj2.HasObservers);

            to.AssertEmpty();

            subj2.OnCompleted();

            Assert.True(subj1.HasObservers);
            Assert.False(subj2.HasObservers);

            subj1.OnNext(1);
            subj1.OnCompleted();

            to.AssertResult(1);
        }

        [Test]
        public void Other_Next_Doesnt_Dispose_Main()
        {
            var subj1 = new PublishSubject<int>();
            var subj2 = new PublishSubject<int>();

            var to = subj1.SkipUntil(subj2).Test();

            Assert.True(subj1.HasObservers);
            Assert.True(subj2.HasObservers);

            to.AssertEmpty();

            subj2.OnNext(2);

            Assert.True(subj1.HasObservers);
            Assert.False(subj2.HasObservers);

            subj1.OnNext(1);
            subj1.OnCompleted();

            to.AssertResult(1);
        }

        [Test]
        public void Other_Error_Disposes_Main()
        {
            var subj1 = new PublishSubject<int>();
            var subj2 = new PublishSubject<int>();

            var to = subj1.SkipUntil(subj2).Test();

            Assert.True(subj1.HasObservers);
            Assert.True(subj2.HasObservers);

            to.AssertEmpty();

            subj2.OnError(new InvalidOperationException());

            Assert.False(subj1.HasObservers);
            Assert.False(subj2.HasObservers);

            to.AssertFailure(typeof(InvalidOperationException));
        }
    }
}
