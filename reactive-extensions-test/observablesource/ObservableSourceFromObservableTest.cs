using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceFromObservableTest
    {
        [Test]
        public void Basic()
        {
            IObservableSource<int> source = Observable.Range(1, 5).ToObservableSource();

            source.Test()
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Error()
        {
            IObservableSource<int> source = Observable.Throw<int>(new InvalidOperationException())
                .ToObservableSource();

            source.Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Dispose()
        {
            var subj = new Subject<int>();

            IObservableSource<int> source = subj.ToObservableSource();

            var to = source.Test();

            Assert.True(subj.HasObservers);

            to.AssertEmpty();

            to.Dispose();

            Assert.False(subj.HasObservers);
        }
    }
}
