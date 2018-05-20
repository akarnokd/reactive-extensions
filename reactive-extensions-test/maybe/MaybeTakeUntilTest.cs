using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.maybe
{
    [TestFixture]
    public class MaybeTakeUntilTest
    {
        [Test]
        public void Basic()
        {
            var cs1 = new MaybeSubject<int>();
            var cs2 = new MaybeSubject<int>();

            var to = cs1
                .TakeUntil(cs2)
                .Test();

            Assert.True(cs1.HasObserver());
            Assert.True(cs2.HasObserver());

            cs1.OnCompleted();

            Assert.False(cs1.HasObserver());
            Assert.False(cs2.HasObserver());

            to.AssertResult();
        }

        [Test]
        public void Basic_Other()
        {
            var cs1 = new MaybeSubject<int>();
            var cs2 = new MaybeSubject<int>();

            var to = cs1
                .TakeUntil(cs2)
                .Test();

            Assert.True(cs1.HasObserver());
            Assert.True(cs2.HasObserver());

            cs2.OnCompleted();

            Assert.False(cs1.HasObserver());
            Assert.False(cs2.HasObserver());

            to.AssertResult();
        }

        [Test]
        public void Error()
        {
            var cs1 = new MaybeSubject<int>();
            var cs2 = new MaybeSubject<int>();

            var to = cs1
                .TakeUntil(cs2)
                .Test();

            Assert.True(cs1.HasObserver());
            Assert.True(cs2.HasObserver());

            cs1.OnError(new InvalidOperationException());

            Assert.False(cs1.HasObserver());
            Assert.False(cs2.HasObserver());

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Error_Other()
        {
            var cs1 = new MaybeSubject<int>();
            var cs2 = new MaybeSubject<int>();

            var to = cs1
                .TakeUntil(cs2)
                .Test();

            Assert.True(cs1.HasObserver());
            Assert.True(cs2.HasObserver());

            cs2.OnError(new InvalidOperationException());

            Assert.False(cs1.HasObserver());
            Assert.False(cs2.HasObserver());

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Dispose()
        {
            var cs1 = new MaybeSubject<int>();
            var cs2 = new MaybeSubject<int>();

            var to = cs1
                .TakeUntil(cs2)
                .Test();

            Assert.True(cs1.HasObserver());
            Assert.True(cs2.HasObserver());

            to.Dispose();

            Assert.False(cs1.HasObserver());
            Assert.False(cs2.HasObserver());

            to.AssertEmpty();
        }

        [Test]
        public void Observable_Basic()
        {
            var cs1 = new MaybeSubject<int>();
            var cs2 = new UnicastSubject<int>();

            var to = cs1
                .TakeUntil(cs2)
                .Test();

            Assert.True(cs1.HasObserver());
            Assert.True(cs2.HasObserver());

            cs1.OnCompleted();

            Assert.False(cs1.HasObserver());
            Assert.False(cs2.HasObserver());

            to.AssertResult();
        }

        [Test]
        public void Observable_Basic_Other_OnNext()
        {
            var cs1 = new MaybeSubject<int>();
            var cs2 = new UnicastSubject<int>();

            var to = cs1
                .TakeUntil(cs2)
                .Test();

            Assert.True(cs1.HasObserver());
            Assert.True(cs2.HasObserver());

            cs2.OnNext(1);

            Assert.False(cs1.HasObserver());
            Assert.False(cs2.HasObserver());

            to.AssertResult();
        }

        [Test]
        public void Observable_Basic_Other()
        {
            var cs1 = new MaybeSubject<int>();
            var cs2 = new UnicastSubject<int>();

            var to = cs1
                .TakeUntil(cs2)
                .Test();

            Assert.True(cs1.HasObserver());
            Assert.True(cs2.HasObserver());

            cs2.OnCompleted();

            Assert.False(cs1.HasObserver());
            Assert.False(cs2.HasObserver());

            to.AssertResult();
        }

        [Test]
        public void Observable_Error()
        {
            var cs1 = new MaybeSubject<int>();
            var cs2 = new UnicastSubject<int>();

            var to = cs1
                .TakeUntil(cs2)
                .Test();

            Assert.True(cs1.HasObserver());
            Assert.True(cs2.HasObserver());

            cs1.OnError(new InvalidOperationException());

            Assert.False(cs1.HasObserver());
            Assert.False(cs2.HasObserver());

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Observable_Error_Other()
        {
            var cs1 = new MaybeSubject<int>();
            var cs2 = new UnicastSubject<int>();

            var to = cs1
                .TakeUntil(cs2)
                .Test();

            Assert.True(cs1.HasObserver());
            Assert.True(cs2.HasObserver());

            cs2.OnError(new InvalidOperationException());

            Assert.False(cs1.HasObserver());
            Assert.False(cs2.HasObserver());

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Observable_Dispose()
        {
            var cs1 = new MaybeSubject<int>();
            var cs2 = new UnicastSubject<int>();

            var to = cs1
                .TakeUntil(cs2)
                .Test();

            Assert.True(cs1.HasObserver());
            Assert.True(cs2.HasObserver());

            to.Dispose();

            Assert.False(cs1.HasObserver());
            Assert.False(cs2.HasObserver());

            to.AssertEmpty();
        }
    }
}
