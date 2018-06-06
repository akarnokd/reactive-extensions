using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceLambdaSubscribeTest
    {
        [Test]
        public void Basic()
        {
            var to = new TestObserver<int>();

            var source = ObservableSource.Range(1, 5);

            source.Subscribe(to.OnNext, to.OnError, to.OnCompleted, to.OnSubscribe);

            to.AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Basic_No_Lambda()
        {
            var to = new TestObserver<int>();

            var source = ObservableSource.Range(1, 5);

            source.Subscribe();
        }

        [Test]
        public void Empty()
        {
            var to = new TestObserver<int>();

            var source = ObservableSource.Empty<int>();

            source.Subscribe(to.OnNext, to.OnError, to.OnCompleted, to.OnSubscribe);

            to.AssertResult();
        }

        [Test]
        public void Error()
        {
            var to = new TestObserver<int>();

            var source = ObservableSource.Error<int>(new InvalidOperationException());

            source.Subscribe(to.OnNext, to.OnError, to.OnCompleted, to.OnSubscribe);

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Dispose()
        {
            var ps = new PublishSubject<int>();

            var d = ps.Subscribe();

            Assert.True(ps.HasObservers);

            d.Dispose();

            Assert.False(ps.HasObservers);
        }

        [Test]
        public void OnSubscribe_Crash()
        {
            var ps = new PublishSubject<int>();
            var error = default(Exception);

            var d = ps.Subscribe(onSubscribe: v => throw new InvalidOperationException(), onError: e => error = e);

            Assert.False(ps.HasObservers);
            Assert.True(typeof(InvalidOperationException).IsAssignableFrom(error.GetType()));
        }

        [Test]
        public void OnNext_Crash()
        {
            var ps = new PublishSubject<int>();
            var error = default(Exception);

            var d = ps.Subscribe(onNext: v => throw new InvalidOperationException(), onError: e => error = e);

            Assert.True(ps.HasObservers);

            ps.OnNext(1);

            Assert.False(ps.HasObservers);

            Assert.True(typeof(InvalidOperationException).IsAssignableFrom(error.GetType()));
        }

        [Test]
        public void OnError_Crash()
        {
            var ps = new PublishSubject<int>();

            var d = ps.Subscribe(onError: v => throw new InvalidOperationException());

            Assert.True(ps.HasObservers);

            ps.OnError(new IndexOutOfRangeException());

            Assert.False(ps.HasObservers);
        }

        [Test]
        public void OnCompleted_Crash()
        {
            var ps = new PublishSubject<int>();

            var d = ps.Subscribe(onCompleted: () => throw new InvalidOperationException());

            Assert.True(ps.HasObservers);

            ps.OnCompleted();

            Assert.False(ps.HasObservers);
        }
    }
}
