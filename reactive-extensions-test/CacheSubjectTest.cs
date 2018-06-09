using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test
{
    [TestFixture]
    public class CacheSubjectTest
    {
        [Test]
        public void Unbounded_Basic()
        {
            var cs = new CacheSubject<int>();

            var to = cs.Test();

            Assert.True(cs.HasObservers);
            Assert.False(cs.HasCompleted());
            Assert.False(cs.HasException());
            Assert.Null(cs.GetException());   

            to.AssertEmpty();

            cs.EmitAll(1, 2, 3, 4, 5);

            to.AssertResult(1, 2, 3, 4, 5);

            Assert.False(cs.HasObservers);
            Assert.True(cs.HasCompleted());
            Assert.False(cs.HasException());
            Assert.Null(cs.GetException());

            cs.Test().AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Unbounded_Basic_Error()
        {
            var cs = new CacheSubject<int>();

            var to = cs.Test();

            Assert.True(cs.HasObservers);
            Assert.False(cs.HasCompleted());
            Assert.False(cs.HasException());
            Assert.Null(cs.GetException());

            to.AssertEmpty();

            var ex = new InvalidOperationException();

            cs.EmitError(ex, 1, 2, 3, 4, 5);

            to.AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);

            Assert.False(cs.HasObservers);
            Assert.False(cs.HasCompleted());
            Assert.True(cs.HasException());
            Assert.AreEqual(ex, cs.GetException());

            cs.Test().AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);
        }

        [Test]
        public void Unbounded_Take()
        {
            var cs = new CacheSubject<int>();

            var to = cs.Take(3).Test();

            Assert.True(cs.HasObservers);
            Assert.False(cs.HasCompleted());
            Assert.False(cs.HasException());
            Assert.Null(cs.GetException());

            to.AssertEmpty();

            cs.Emit(1, 2, 3, 4, 5);

            to.AssertResult(1, 2, 3);

            Assert.False(cs.HasObservers);
            Assert.False(cs.HasCompleted());
            Assert.False(cs.HasException());
            Assert.Null(cs.GetException());

            cs.OnCompleted();

            Assert.False(cs.HasObservers);
            Assert.True(cs.HasCompleted());
            Assert.False(cs.HasException());
            Assert.Null(cs.GetException());

            cs.Test().AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Unbounded_Long()
        {
            var cs = new CacheSubject<int>();

            var to = cs.Test();

            for (int i = 0; i < 1000; i++)
            {
                cs.OnNext(i);
            }
            cs.OnCompleted();

            to.AssertSubscribed()
                .AssertValueCount(1000)
                .AssertCompleted()
                .AssertNoError();

            for (int j = 0; j < 1000; j++)
            {
                Assert.AreEqual(j, to.Items[j]);
            }
        }

        [Test]
        public void Unbounded_Race()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var cs = new CacheSubject<int>();

                var to = new TestObserver<int>();

                TestHelper.Race(() => {
                    for (int j = 0; j < 1000; j++)
                    {
                        cs.OnNext(j);
                    }
                    cs.OnCompleted();
                }, () => {
                    cs.Subscribe(to);
                });

                to.AwaitDone(TimeSpan.FromSeconds(5));

                to.AssertValueCount(1000)
                    .AssertNoError()
                    .AssertCompleted();
                for (int j = 0; j < 1000; j++)
                {
                    Assert.AreEqual(j, to.Items[j]);
                }
            }
        }
    }
}
