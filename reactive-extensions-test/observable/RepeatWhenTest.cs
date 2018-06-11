using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Linq;

namespace akarnokd.reactive_extensions_test.observable
{
    [TestFixture]
    public class RepeatWhenTest
    {
        [Test]
        public void Basic_Error()
        {
            var us = new UnicastSubject<int>();

            Observable.Range(1, 5).ConcatError(new InvalidOperationException())
                .RepeatWhen(v => us)
                .Test()
                .AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);
        }

        public void Repeat()
        {
            Observable.Return(1)
                .RepeatWhen(v =>
                {
                    var count = 0;
                    return v.TakeWhile(_ => ++count < 5);
                })
                .Test()
                .AssertResult(1, 1, 1, 1, 1);
        }

        [Test]
        public void Handler_Errors()
        {
            Observable.Range(1, 5)
                .RepeatWhen(v => v.Take(1).Skip(1).ConcatError(new NotImplementedException()))
                .Test()
                .AssertFailure(typeof(NotImplementedException), 1, 2, 3, 4, 5);
        }

        [Test]
        public void Handler_Completes()
        {
            Observable.Range(1, 5)
                .RepeatWhen(v => v.Take(1).Skip(1))
                .Test()
                .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Main_Disposed_Handler_Completes()
        {
            var us = new UnicastSubject<int>();

            us.RepeatWhen(v => Observable.Empty<int>())
                .Test()
                .AssertResult();

            Assert.False(us.HasObserver());
        }

        [Test]
        public void Main_Disposed_Handler_Errors()
        {
            var us = new UnicastSubject<int>();

            us.RepeatWhen(v => Observable.Throw<int>(new InvalidOperationException()))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.False(us.HasObserver());
        }

        [Test]
        public void Handler_Crash()
        {
            Observable.Range(1, 5).RepeatWhen<int, int>(v => throw new InvalidOperationException())
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

    }
}
