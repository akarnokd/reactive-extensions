using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test
{
    [TestFixture]
    public class SingleSubjectTest
    {
        [Test]
        public void Success()
        {
            var ms = new SingleSubject<int>();

            Assert.False(ms.HasObservers);
            Assert.False(ms.HasCompleted());
            Assert.False(ms.HasException());
            Assert.IsNull(ms.GetException());
            Assert.False(ms.HasValue());
            Assert.False(ms.TryGetValue(out var _));

            var to = ms.Test();

            Assert.True(ms.HasObservers);
            Assert.False(ms.HasCompleted());
            Assert.False(ms.HasException());
            Assert.IsNull(ms.GetException());
            Assert.False(ms.HasValue());
            Assert.False(ms.TryGetValue(out var _));

            to.AssertSubscribed().AssertEmpty();

            ms.OnSuccess(1);

            Assert.False(ms.HasObservers);
            Assert.True(ms.HasCompleted());
            Assert.False(ms.HasException());
            Assert.IsNull(ms.GetException());
            Assert.True(ms.HasValue());
            var v = default(int);
            Assert.True(ms.TryGetValue(out v));
            Assert.AreEqual(1, v);

            to.AssertResult(1);

            ms.Test().AssertSubscribed().AssertResult(1);

            ms.Test(true).AssertSubscribed().AssertEmpty();
        }

        [Test]
        public void Error()
        {
            var ms = new SingleSubject<int>();

            Assert.False(ms.HasObserver());
            Assert.False(ms.HasCompleted());
            Assert.False(ms.HasException());
            Assert.IsNull(ms.GetException());
            Assert.False(ms.HasValue());
            Assert.False(ms.TryGetValue(out var _));

            var to = ms.Test();

            Assert.True(ms.HasObserver());
            Assert.False(ms.HasCompleted());
            Assert.False(ms.HasException());
            Assert.IsNull(ms.GetException());
            Assert.False(ms.HasValue());
            Assert.False(ms.TryGetValue(out var _));

            to.AssertSubscribed().AssertEmpty();

            ms.OnError(new InvalidOperationException());

            Assert.False(ms.HasObserver());
            Assert.False(ms.HasCompleted());
            Assert.True(ms.HasException());
            Assert.IsNotNull(ms.GetException());
            Assert.True(typeof(InvalidOperationException).IsAssignableFrom(ms.GetException().GetType()));

            Assert.False(ms.HasValue());
            Assert.False(ms.TryGetValue(out var _));

            to.AssertFailure(typeof(InvalidOperationException));

            ms.Test().AssertSubscribed().AssertFailure(typeof(InvalidOperationException));

            ms.Test(true).AssertSubscribed().AssertEmpty();
        }

        [Test]
        public void Disposed_Upfront()
        {
            var ms = new SingleSubject<int>();

            var to = ms.Test();

            var to2 = ms.Test(true).AssertSubscribed().AssertEmpty();

            ms.OnSuccess(1);

            to.AssertResult(1);

            to2.AssertEmpty();
        }

        [Test]
        public void RefCount()
        {
            var ms = new SingleSubject<int>(true);

            var sad = new SingleAssignmentDisposable();

            ms.OnSubscribe(sad);

            Assert.False(sad.IsDisposed());

            var to = ms.Test();

            Assert.False(sad.IsDisposed());

            var to2 = ms.Test(true).AssertSubscribed().AssertEmpty();

            Assert.False(sad.IsDisposed());

            to.Dispose();

            Assert.True(sad.IsDisposed());

            ms.Test().AssertFailure(typeof(OperationCanceledException));

        }

        [Test]
        public void Success_Dispose_Other()
        {
            var ms = new SingleSubject<int>();

            var to1 = new TestObserver<int>();

            var count = 0;

            ms.Subscribe(v => { to1.Dispose(); }, e => { count = 1; });

            ms.Subscribe(to1);

            ms.OnSuccess(1);

            to1.AssertEmpty();

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Error_Dispose_Other()
        {
            var ms = new SingleSubject<int>();

            var to1 = new TestObserver<int>();

            var count = 0;

            ms.Subscribe(v => { count = 1; }, e => { to1.Dispose(); });

            ms.Subscribe(to1);

            ms.OnError(new InvalidOperationException());

            to1.AssertEmpty();

            Assert.AreEqual(0, count);
        }

    }
}
