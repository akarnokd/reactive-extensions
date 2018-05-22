using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.single
{
    [TestFixture]
    public class SingleSubscribeSafeTest
    {
        [Test]
        public void Normal_Success()
        {
            var to = new TestObserver<int>();

            SingleSource.Just(1)
                .SubscribeSafe(to);

            to.AssertSubscribed()
                .AssertResult(1);
        }

        [Test]
        public void Normal_Error()
        {
            var to = new TestObserver<int>();

            SingleSource.Error<int>(new InvalidOperationException())
                .SubscribeSafe(to);

            to.AssertSubscribed()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Normal_Dispose()
        {
            var cs = new SingleSubject<int>();
            var to = new TestObserver<int>();

            cs.SubscribeSafe(to);

            Assert.True(cs.HasObserver());

            to.Dispose();

            Assert.False(cs.HasObserver());
        }

        [Test]
        public void Crash_OnSubscribe()
        {
            var cs = new SingleSubject<int>();

            cs.SubscribeSafe(new FailingSingleObserver(true, true, true, true));

            Assert.False(cs.HasObserver());
        }

        [Test]
        public void Crash_OnSuccess()
        {
            var cs = new SingleSubject<int>();

            cs.SubscribeSafe(new FailingSingleObserver(false, true, true, true));

            Assert.True(cs.HasObserver());

            cs.OnSuccess(1);
        }

        [Test]
        public void Crash_OnError()
        {
            var cs = new SingleSubject<int>();

            cs.SubscribeSafe(new FailingSingleObserver(false, true, true, true));

            Assert.True(cs.HasObserver());

            cs.OnError(new InvalidOperationException("main"));
        }

        sealed class FailingSingleObserver : ISingleObserver<int>
        {
            readonly bool failOnSubscribe;

            readonly bool failOnSuccess;

            readonly bool failOnError;

            readonly bool failOnCompleted;

            public FailingSingleObserver(bool failOnSubscribe, bool failOnSuccess, bool failOnError, bool failOnCompleted)
            {
                this.failOnSubscribe = failOnSubscribe;
                this.failOnSuccess = failOnSuccess;
                this.failOnError = failOnError;
                this.failOnCompleted = failOnCompleted;
            }

            public void OnCompleted()
            {
                if (failOnCompleted)
                {
                    throw new InvalidOperationException();
                }
            }

            public void OnError(Exception error)
            {
                if (failOnError)
                {
                    throw new InvalidOperationException();
                }
            }

            public void OnSubscribe(IDisposable d)
            {
                if (failOnSubscribe)
                {
                    throw new InvalidOperationException();
                }
            }

            public void OnSuccess(int item)
            {
                if (failOnSuccess)
                {
                    throw new InvalidOperationException();
                }
            }
        }
    }
}
