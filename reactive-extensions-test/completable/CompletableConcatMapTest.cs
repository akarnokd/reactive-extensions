using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Linq;

namespace akarnokd.reactive_extensions_test.completable
{
    [TestFixture]
    public class CompletableConcatMapTest
    {
        [Test]
        public void Basic()
        {
            var count = 0;

            Observable.Range(1, 10)
                .ConcatMap(v => CompletableSource.FromAction(() => count++))
                .Test()
                .AssertResult();

            Assert.AreEqual(10, count);
        }

        [Test]
        public void Basic_DelayError()
        {
            var count = 0;

            Observable.Range(1, 10)
                .ConcatMap(v => CompletableSource.FromAction(() => count++), true)
                .Test()
                .AssertResult();

            Assert.AreEqual(10, count);
        }

        [Test]
        public void Error()
        {
            var count = 0;

            var us = new UnicastSubject<int>();

            var to = us
                .ConcatMap(v => {
                    if (v == 2)
                    {
                        return CompletableSource.Error(new InvalidOperationException());
                    }
                    return CompletableSource.FromAction(() => count++);
                })
                .Test();

            to.AssertEmpty()
                .AssertSubscribed();

            us.OnNext(0);
            us.OnNext(1);
            us.OnNext(2);

            to.AssertFailure(typeof(InvalidOperationException));

            Assert.False(us.HasObserver());

            Assert.AreEqual(2, count);

        }

        [Test]
        public void Error_Delayed()
        {
            var count = 0;

            var us = new UnicastSubject<int>();

            var to = us
                .ConcatMap(v => {
                    if (v == 2)
                    {
                        return CompletableSource.Error(new InvalidOperationException());
                    }
                    return CompletableSource.FromAction(() => count++);
                }, true)
                .Test();

            to.AssertEmpty()
                .AssertSubscribed();

            us.OnNext(0);
            us.OnNext(1);
            us.OnNext(2);

            Assert.AreEqual(2, count);

            to.AssertEmpty();

            us.OnNext(3);

            Assert.AreEqual(3, count);
            us.OnCompleted();

            to.AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(3, count);
        }

        [Test]
        public void Error_Main_Delayed()
        {
            var count = 0;

            var us = new UnicastSubject<int>();
            var cs = new CompletableSubject();

            var to = us
                .ConcatMap(v => {
                    if (v == 2)
                    {
                        return cs;
                    }
                    return CompletableSource.FromAction(() => count++);
                }, true)
                .Test();

            to.AssertEmpty()
                .AssertSubscribed();

            us.OnNext(0);
            us.OnNext(1);
            us.OnNext(2);
            us.OnError(new InvalidOperationException());

            Assert.AreEqual(2, count);

            to.AssertEmpty();

            Assert.True(cs.HasObserver());

            Assert.AreEqual(2, count);

            cs.OnCompleted();

            to.AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(2, count);
        }

        [Test]
        public void Mapper_Crash()
        {
            var count = 0;

            var us = new UnicastSubject<int>();

            var to = us
                .ConcatMap(v => {
                    if (v == 2)
                    {
                        throw new InvalidOperationException();
                    }
                    return CompletableSource.FromAction(() => count++);
                }, true)
                .Test();

            to.AssertEmpty()
                .AssertSubscribed();

            us.OnNext(0);
            us.OnNext(1);
            us.OnNext(2);

            Assert.False(us.HasObserver());

            Assert.AreEqual(2, count);

            to.AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(2, count);
        }
    }
}
