using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test
{
    [TestFixture]
    public class PublishSubjectTest
    {
        [Test]
        public void Basic()
        {
            var ps = new PublishSubject<int>();

            Assert.False(ps.HasObservers);
            Assert.False(ps.HasCompleted());
            Assert.False(ps.HasException());
            Assert.Null(ps.GetException());

            var to1 = ps.Test();

            Assert.True(ps.HasObservers);
            Assert.False(ps.HasCompleted());
            Assert.False(ps.HasException());
            Assert.Null(ps.GetException());

            var to2 = ps.Test(true);

            Assert.True(ps.HasObservers);
            Assert.False(ps.HasCompleted());
            Assert.False(ps.HasException());
            Assert.Null(ps.GetException());

            var to3 = ps.Test();

            to3.Dispose();

            Assert.True(ps.HasObservers);
            Assert.False(ps.HasCompleted());
            Assert.False(ps.HasException());
            Assert.Null(ps.GetException());

            ps.OnNext(1);
            ps.OnNext(2);
            ps.OnNext(3);

            to1.AssertValuesOnly(1, 2, 3);
            to2.AssertEmpty();
            to3.AssertEmpty();

            ps.OnCompleted();

            to1.AssertResult(1, 2, 3);

            Assert.False(ps.HasObservers);
            Assert.True(ps.HasCompleted());
            Assert.False(ps.HasException());
            Assert.Null(ps.GetException());

            ps.Test().AssertResult();
        }

        [Test]
        public void Error()
        {
            var ps = new PublishSubject<int>();

            Assert.False(ps.HasObservers);
            Assert.False(ps.HasCompleted());
            Assert.False(ps.HasException());
            Assert.Null(ps.GetException());

            var to1 = ps.Test();

            var ex = new InvalidOperationException();

            ps.OnNext(1);
            ps.OnNext(2);
            ps.OnNext(3);
            ps.OnError(ex);

            to1.AssertFailure(typeof(InvalidOperationException), 1, 2, 3);

            Assert.False(ps.HasObservers);
            Assert.False(ps.HasCompleted());
            Assert.True(ps.HasException());
            Assert.AreEqual(ex, ps.GetException());

            ps.Test().AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void RefCount()
        {
            var ps = new PublishSubject<int>(true);

            var d = new BooleanDisposable();

            ps.OnSubscribe(d);

            Assert.False(d.IsDisposed());

            var to = ps.Test();

            Assert.True(ps.HasObservers);

            to.Dispose();

            Assert.False(ps.HasObservers);

            Assert.True(d.IsDisposed());
        }
    }
}
