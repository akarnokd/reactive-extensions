﻿using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.maybe
{
    [TestFixture]
    public class MaybeSubscribeSafeTest
    {
        [Test]
        public void Normal_Success()
        {
            var to = new TestObserver<int>();

            MaybeSource.Just(1)
                .SubscribeSafe(to);

            to.AssertSubscribed()
                .AssertResult(1);
        }

        [Test]
        public void Normal_Complete()
        {
            var to = new TestObserver<int>();

            MaybeSource.Empty<int>()
                .SubscribeSafe(to);

            to.AssertSubscribed()
                .AssertResult();
        }

        [Test]
        public void Normal_Error()
        {
            var to = new TestObserver<int>();

            MaybeSource.Error<int>(new InvalidOperationException())
                .SubscribeSafe(to);

            to.AssertSubscribed()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Normal_Dispose()
        {
            var cs = new MaybeSubject<int>();
            var to = new TestObserver<int>();

            cs.SubscribeSafe(to);

            Assert.True(cs.HasObserver());

            to.Dispose();

            Assert.False(cs.HasObserver());
        }

        [Test]
        public void Crash_OnSubscribe()
        {
            var cs = new MaybeSubject<int>();

            cs.SubscribeSafe(new FailingCompletableObserver(true, true, true, true));

            Assert.False(cs.HasObserver());
        }

        [Test]
        public void Crash_OnSuccess()
        {
            var cs = new MaybeSubject<int>();

            cs.SubscribeSafe(new FailingCompletableObserver(false, true, true, true));

            Assert.True(cs.HasObserver());

            cs.OnSuccess(1);
        }

        [Test]
        public void Crash_OnCompleted()
        {
            var cs = new MaybeSubject<int>();

            cs.SubscribeSafe(new FailingCompletableObserver(false, true, true, true));

            Assert.True(cs.HasObserver());

            cs.OnCompleted();
        }

        [Test]
        public void Crash_OnError()
        {
            var cs = new MaybeSubject<int>();

            cs.SubscribeSafe(new FailingCompletableObserver(false, true, true, true));

            Assert.True(cs.HasObserver());

            cs.OnError(new InvalidOperationException("main"));
        }

        sealed class FailingCompletableObserver : IMaybeObserver<int>
        {
            readonly bool failOnSubscribe;

            readonly bool failOnSuccess;

            readonly bool failOnError;

            readonly bool failOnCompleted;

            public FailingCompletableObserver(bool failOnSubscribe, bool failOnSuccess, bool failOnError, bool failOnCompleted)
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