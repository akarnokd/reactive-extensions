using System;
using NUnit.Framework;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class SerializedSignalObserverTest
    {
        [Test]
        public void Basic()
        {
            var up = new MonocastSubject<int>();

            var to = new TestObserver<int>();

            up.Subscribe(ObservableSource.ToSerialized(to));

            up.EmitAll(1, 2, 3, 4, 5);

            to.AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Basic_With_Error()
        {
            var up = new MonocastSubject<int>();

            var to = new TestObserver<int>();

            up.Subscribe(ObservableSource.ToSerialized(to));

            up.EmitError(new InvalidOperationException(), 1, 2, 3, 4, 5);

            to.AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);
        }

        [Test]
        public void OnNext_Race()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var to = new TestObserver<int>();

                var s = ObservableSource.ToSerialized(to);

                Action emit = () => {
                    for (int j = 0; j < 500; j++)
                    {
                        s.OnNext(j);
                    }
                };

                TestHelper.Race(emit, emit);

                to.AssertValueCount(1000);
            }
        }

        [Test]
        public void OnNext_OnCompleted_Race()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var to = new TestObserver<int>();

                var s = ObservableSource.ToSerialized(to);

                Action emit = () => {
                    for (int j = 0; j < 500; j++)
                    {
                        s.OnNext(j);
                    }
                };

                Action complete = () =>
                {
                    for (int j = 0; j < 250; j++)
                    {
                        s.OnNext(j);
                    }

                    s.OnCompleted();
                };

                TestHelper.Race(emit, complete);

                Assert.True(to.ItemCount >= 250);

                to.AssertNoError()
                    .AssertCompleted();
            }
        }

        [Test]
        public void OnNext_OnError_Race()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var to = new TestObserver<int>();

                var s = ObservableSource.ToSerialized(to);

                var ex = new InvalidOperationException();

                Action emit = () => {
                    for (int j = 0; j < 500; j++)
                    {
                        s.OnNext(j);
                    }
                };

                Action complete = () =>
                {
                    for (int j = 0; j < 250; j++)
                    {
                        s.OnNext(j);
                    }

                    s.OnError(ex);
                };

                TestHelper.Race(emit, complete);

                Assert.True(to.ItemCount >= 250);

                to.AssertError(typeof(InvalidOperationException))
                    .AssertNotCompleted();
            }
        }
    }
}
