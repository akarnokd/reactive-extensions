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
        public void IObserver_OnNext_OnError_Race()
        {
            var ex = new InvalidOperationException();

            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                wip = 0;
                error = null;

                var to = new TestObserver<int>();

                TestHelper.Race(() =>
                {
                    HalfSerializer.OnNext((IObserver<int>)to, 1, ref wip, ref error);
                },
                () =>
                {
                    HalfSerializer.OnError((IObserver<int>)to, ex, ref wip, ref error);
                });

                to.AssertError(typeof(InvalidOperationException))
                    .AssertNotCompleted();

                Assert.True(to.ItemCount <= 1);
            }
        }

        [Test]
        public void IObserver_OnNext_OnCompleted_Race()
        {
            var ex = new InvalidOperationException();

            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                wip = 0;
                error = null;

                var to = new TestObserver<int>();

                TestHelper.Race(() =>
                {
                    HalfSerializer.OnNext((IObserver<int>)to, 1, ref wip, ref error);
                },
                () =>
                {
                    HalfSerializer.OnCompleted((IObserver<int>)to, ref wip, ref error);
                });

                to.AssertNoError()
                    .AssertCompleted();

                Assert.True(to.ItemCount <= 1);
            }
        }

        [Test]
        public void ISignalObserver_OnNext_OnError_Race()
        {
            var ex = new InvalidOperationException();

            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                wip = 0;
                error = null;

                var to = new TestObserver<int>();

                TestHelper.Race(() =>
                {
                    HalfSerializer.OnNext((ISignalObserver<int>)to, 1, ref wip, ref error);
                },
                () =>
                {
                    HalfSerializer.OnError((ISignalObserver<int>)to, ex, ref wip, ref error);
                });

                to.AssertError(typeof(InvalidOperationException))
                    .AssertNotCompleted();

                Assert.True(to.ItemCount <= 1);
            }
        }

        [Test]
        public void ISignalObserver_OnNext_OnCompleted_Race()
        {
            var ex = new InvalidOperationException();

            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                wip = 0;
                error = null;

                var to = new TestObserver<int>();

                TestHelper.Race(() =>
                {
                    HalfSerializer.OnNext((ISignalObserver<int>)to, 1, ref wip, ref error);
                },
                () =>
                {
                    HalfSerializer.OnCompleted((ISignalObserver<int>)to, ref wip, ref error);
                });

                to.AssertNoError()
                    .AssertCompleted();

                Assert.True(to.ItemCount <= 1);
            }
        }
    }
}
