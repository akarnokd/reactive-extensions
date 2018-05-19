using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.completable
{
    [TestFixture]
    public class CompletableSubscribeSafeTest
    {
        [Test]
        public void Normal_Complete()
        {
            var to = new TestObserver<object>();

            CompletableSource.Empty()
                .SubscribeSafe(to);

            to.AssertSubscribed()
                .AssertResult();
        }

        [Test]
        public void Normal_Error()
        {
            var to = new TestObserver<object>();

            CompletableSource.Error(new InvalidOperationException())
                .SubscribeSafe(to);

            to.AssertSubscribed()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Normal_Dispose()
        {
            var cs = new CompletableSubject();
            var to = new TestObserver<object>();

            cs.SubscribeSafe(to);

            Assert.True(cs.HasObserver());

            to.Dispose();

            Assert.False(cs.HasObserver());
        }

        [Test]
        public void Crash_OnSubscribe()
        {
            var cs = new CompletableSubject();

            cs.SubscribeSafe(new FailingCompletableObserver(true, true, true));

            Assert.False(cs.HasObserver());
        }

        [Test]
        public void Crash_OnCompleted()
        {
            var cs = new CompletableSubject();

            cs.SubscribeSafe(new FailingCompletableObserver(false, true, true));

            Assert.True(cs.HasObserver());

            cs.OnCompleted();
        }

        [Test]
        public void Crash_OnError()
        {
            var cs = new CompletableSubject();

            cs.SubscribeSafe(new FailingCompletableObserver(false, true, true));

            Assert.True(cs.HasObserver());

            cs.OnError(new InvalidOperationException("main"));
        }

        sealed class FailingCompletableObserver : ICompletableObserver
        {
            readonly bool failOnSubscribe;

            readonly bool failOnError;

            readonly bool failOnCompleted;

            public FailingCompletableObserver(bool failOnSubscribe, bool failOnError, bool failOnCompleted)
            {
                this.failOnSubscribe = failOnSubscribe;
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
        }
    }
}
