using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Linq;

namespace akarnokd.reactive_extensions_test.completable
{
    [TestFixture]
    public class CompletableRepeatWhenTest
    {
        [Test]
        public void Basic_Error()
        {
            var us = new UnicastSubject<int>();

            CompletableSource.Error(new InvalidOperationException())
                .RepeatWhen(v => us)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        public void Repeat()
        {
            var subs = 0;

            CompletableSource.FromAction(() => subs++)
                .RepeatWhen(v =>
                {
                    var count = 0;
                    return v.TakeWhile(_ => ++count < 5);
                })
                .Test()
                .AssertResult();

            Assert.AreEqual(5, subs);
        }

        [Test]
        public void Handler_Errors()
        {
            var subs = 0;

            CompletableSource.FromAction(() => subs++)
                .RepeatWhen(v => v.Take(1).Skip(1).ConcatError(new NotImplementedException()))
                .Test()
                .AssertFailure(typeof(NotImplementedException));

            Assert.AreEqual(1, subs);
        }

        [Test]
        public void Handler_Completes()
        {
            var subs = 0;

            CompletableSource.FromAction(() => subs++)
                .RepeatWhen(v => v.Take(1).Skip(1))
                .Test()
                .AssertResult();

            Assert.AreEqual(1, subs);
        }

        [Test]
        public void Handler_Completes_Immediately()
        {
            var us = new CompletableSubject();

            us
                .RepeatWhen(v => Observable.Empty<int>())
                .Test()
                .AssertResult();

            Assert.False(us.HasObserver());
        }

        [Test]
        public void Handler_Errors_Immediately()
        {
            var us = new CompletableSubject();

            us
                .RepeatWhen(v => Observable.Throw<int>(new InvalidOperationException()))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.False(us.HasObserver());
        }

        [Test]
        public void Main_Disposed_Handler_Completes()
        {
            var count = 0;

            CompletableSource.FromAction(() => count++)
            .RepeatWhen(v => Observable.Empty<int>())
                .Test()
                .AssertResult();

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Main_Disposed_Handler_Errors()
        {
            var count = 0;

            CompletableSource.FromAction(() => count++)
                .RepeatWhen(v => Observable.Throw<int>(new InvalidOperationException()))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(0, count);
        }
    }
}
