using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceToObservableTest
    {
        [Test]
        public void Basic()
        {
            IObservable<int> source = ObservableSource.Range(1, 5).ToObservable();

            source.Test()
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Error()
        {
            IObservable<int> source = ObservableSource.Error<int>(new InvalidOperationException())
                .ToObservable();

            source.Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Dispose()
        {
            var subj = new PublishSubject<int>();

            IObservable<int> source = subj.ToObservable();

            var to = source.Test();

            Assert.True(subj.HasObservers);

            to.AssertEmpty();

            to.Dispose();

            Assert.False(subj.HasObservers);
        }
    }
}
