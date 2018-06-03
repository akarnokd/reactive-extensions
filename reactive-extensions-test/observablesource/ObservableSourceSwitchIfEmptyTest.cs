using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceSwitchIfEmptyTest
    {
        [Test]
        public void Basic()
        {
            ObservableSource.Range(1, 5)
                .SwitchIfEmpty(ObservableSource.Range(6, 5))
                .Test()
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Empty()
        {
            ObservableSource.Empty<int>()
                .SwitchIfEmpty(ObservableSource.Range(6, 5))
                .Test()
                .AssertResult(6, 7, 8, 9, 10);
        }

        [Test]
        public void Empty_Many_Fallbacks()
        {
            var count = 0;

            ObservableSource.Empty<int>()
                .SwitchIfEmpty(ObservableSource.FromAction<int>(() => ++count), 
                ObservableSource.Range(6, 5))
                .Test()
                .AssertResult(6, 7, 8, 9, 10);

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Error()
        {
            var count = 0;

            ObservableSource.Error<int>(new InvalidOperationException())
                .SwitchIfEmpty(ObservableSource.FromAction<int>(() => ++count))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Error_Fallback()
        {
            ObservableSource.Empty<int>()
                .SwitchIfEmpty(ObservableSource.Error<int>(new InvalidOperationException()))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Main_Dispose()
        {
            var subj = new PublishSubject<int>();
            var fallback = new PublishSubject<int>();

            var to = subj.SwitchIfEmpty(fallback).Test();

            Assert.True(subj.HasObservers);
            Assert.False(fallback.HasObservers);

            to.Dispose();

            Assert.False(subj.HasObservers);
            Assert.False(fallback.HasObservers);
        }

        [Test]
        public void Fallback_Dispose()
        {
            var subj = new PublishSubject<int>();
            var fallback = new PublishSubject<int>();

            var to = subj.SwitchIfEmpty(fallback).Test();

            Assert.True(subj.HasObservers);
            Assert.False(fallback.HasObservers);

            subj.OnCompleted();

            Assert.False(subj.HasObservers);
            Assert.True(fallback.HasObservers);

            to.Dispose();

            Assert.False(subj.HasObservers);
            Assert.False(fallback.HasObservers);
        }
    }
}
