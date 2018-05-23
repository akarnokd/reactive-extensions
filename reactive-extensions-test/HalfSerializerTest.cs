using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test
{
    [TestFixture]
    public class HalfSerializerTest
    {
        int wip;
        Exception error;

        [Test]
        public void OnNext_OnError_Race()
        {
            var ex = new InvalidOperationException();

            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                wip = 0;
                error = null;

                var to = new TestObserver<int>();

                TestHelper.Race(() =>
                {
                    HalfSerializer.OnNext(to, 1, ref wip, ref error);
                },
                () =>
                {
                    HalfSerializer.OnError(to, ex, ref wip, ref error);
                });

                to.AssertError(typeof(InvalidOperationException))
                    .AssertNotCompleted();

                Assert.True(to.ItemCount <= 1);
            }
        }

        [Test]
        public void OnNext_OnCompleted_Race()
        {
            var ex = new InvalidOperationException();

            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                wip = 0;
                error = null;

                var to = new TestObserver<int>();

                TestHelper.Race(() =>
                {
                    HalfSerializer.OnNext(to, 1, ref wip, ref error);
                },
                () =>
                {
                    HalfSerializer.OnCompleted(to, ref wip, ref error);
                });

                to.AssertNoError()
                    .AssertCompleted();

                Assert.True(to.ItemCount <= 1);
            }
        }
    }
}
