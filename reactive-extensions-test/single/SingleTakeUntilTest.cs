using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.single
{
    [TestFixture]
    public class SingleTakeUntilTest
    {
        [Test]
        public void Success()
        {
            var cs1 = new SingleSubject<int>();
            var cs2 = new SingleSubject<int>();

            var to = cs1
                .TakeUntil(cs2)
                .Test();

            Assert.True(cs1.HasObserver());
            Assert.True(cs2.HasObserver());

            cs1.OnSuccess(1);

            Assert.False(cs1.HasObserver());
            Assert.False(cs2.HasObserver());

            to.AssertResult(1);
        }

        [Test]
        public void Success_Other()
        {
            var cs1 = new SingleSubject<int>();
            var cs2 = new SingleSubject<int>();

            var to = cs1
                .TakeUntil(cs2)
                .Test();

            Assert.True(cs1.HasObserver());
            Assert.True(cs2.HasObserver());

            cs2.OnSuccess(1);

            Assert.False(cs1.HasObserver());
            Assert.False(cs2.HasObserver());

            to.AssertFailure(typeof(IndexOutOfRangeException));
        }

        [Test]
        public void Error()
        {
            var cs1 = new SingleSubject<int>();
            var cs2 = new SingleSubject<int>();

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
            var cs1 = new SingleSubject<int>();
            var cs2 = new SingleSubject<int>();

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
            var cs1 = new SingleSubject<int>();
            var cs2 = new SingleSubject<int>();

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
        public void Observable_Success()
        {
            var cs1 = new SingleSubject<int>();
            var cs2 = new UnicastSubject<int>();

            var to = cs1
                .TakeUntil(cs2)
                .Test();

            Assert.True(cs1.HasObserver());
            Assert.True(cs2.HasObserver());

            cs1.OnSuccess(1);

            Assert.False(cs1.HasObserver());
            Assert.False(cs2.HasObserver());

            to.AssertResult(1);
        }

        [Test]
        public void Observable_Basic_Other_OnNext()
        {
            var cs1 = new SingleSubject<int>();
            var cs2 = new UnicastSubject<int>();

            var to = cs1
                .TakeUntil(cs2)
                .Test();

            Assert.True(cs1.HasObserver());
            Assert.True(cs2.HasObserver());

            cs2.OnNext(1);

            Assert.False(cs1.HasObserver());
            Assert.False(cs2.HasObserver());

            to.AssertFailure(typeof(IndexOutOfRangeException));
        }

        [Test]
        public void Observable_Basic_Other()
        {
            var cs1 = new SingleSubject<int>();
            var cs2 = new UnicastSubject<int>();

            var to = cs1
                .TakeUntil(cs2)
                .Test();

            Assert.True(cs1.HasObserver());
            Assert.True(cs2.HasObserver());

            cs2.OnCompleted();

            Assert.False(cs1.HasObserver());
            Assert.False(cs2.HasObserver());

            to.AssertFailure(typeof(IndexOutOfRangeException));
        }

        [Test]
        public void Observable_Error()
        {
            var cs1 = new SingleSubject<int>();
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
            var cs1 = new SingleSubject<int>();
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
            var cs1 = new SingleSubject<int>();
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
