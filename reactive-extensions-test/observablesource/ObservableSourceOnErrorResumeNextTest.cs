using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceOnErrorResumeNextTest
    {
        [Test]
        public void Basic()
        {
            ObservableSource.Range(1, 5)
                .OnErrorResumeNext(v => ObservableSource.Range(6, 5))
                .Test()
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Main_Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .OnErrorResumeNext(v => ObservableSource.Range(6, 5))
                .Test()
                .AssertResult(6, 7, 8, 9, 10);
        }

        [Test]
        public void Fallback_Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .OnErrorResumeNext(v => ObservableSource.Error<int>(new IndexOutOfRangeException()))
                .Test()
                .AssertFailure(typeof(IndexOutOfRangeException));
        }

        [Test]
        public void Handler_Crash()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .OnErrorResumeNext<int>(v => throw new IndexOutOfRangeException())
                .Test()
                .AssertFailure(typeof(AggregateException))
                .AssertCompositeErrorCount(2)
                .AssertCompositeError(0, typeof(InvalidOperationException))
                .AssertCompositeError(1, typeof(IndexOutOfRangeException));
        }

        [Test]
        public void Dispose_Main()
        {
            var subj = new PublishSubject<int>();
            var fallback = new PublishSubject<int>();

            var to = subj.OnErrorResumeNext(v => fallback).Test();

            Assert.True(subj.HasObservers);
            Assert.False(fallback.HasObservers);

            to.Dispose();

            Assert.False(subj.HasObservers);
            Assert.False(fallback.HasObservers);
        }

        [Test]
        public void Dispose_Fallback()
        {
            var subj = new PublishSubject<int>();
            var fallback = new PublishSubject<int>();

            var to = subj.OnErrorResumeNext(v => fallback).Test();

            Assert.True(subj.HasObservers);
            Assert.False(fallback.HasObservers);

            subj.OnError(new InvalidOperationException());

            Assert.False(subj.HasObservers);
            Assert.True(fallback.HasObservers);

            to.Dispose();

            Assert.False(subj.HasObservers);
            Assert.False(fallback.HasObservers);
        }
    }
}
