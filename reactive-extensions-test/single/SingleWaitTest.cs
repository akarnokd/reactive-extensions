using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Concurrency;

namespace akarnokd.reactive_extensions_test.single
{
    [TestFixture]
    public class SingleWaitTest
    {

        #region + Value output +

        [Test]
        public void Value_Success()
        {
            var v = SingleSource.Just(1)
                .Wait();

            Assert.AreEqual(1, v);
        }

        [Test]
        public void Value_Success_Timeout()
        {
            var v = SingleSource.Just(1)
                .Wait(5000);

            Assert.AreEqual(1, v);
        }

        [Test]
        public void Value_Success_CancellationTokenSource()
        {
            var v = SingleSource.Just(1)
                .Wait(cts: new CancellationTokenSource());

            Assert.AreEqual(1, v);
        }

        [Test]
        public void Value_Success_Timeout_CancellationTokenSource()
        {
            var v = SingleSource.Just(1)
                .Wait(5000, cts: new CancellationTokenSource());

            Assert.AreEqual(1, v);
        }

        [Test]
        public void Value_Delayed_Success()
        {
            var v = SingleSource.Just(1)
                .Delay(TimeSpan.FromMilliseconds(100), NewThreadScheduler.Default)
                .Wait();

            Assert.AreEqual(1, v);
        }

        [Test]
        public void Value_Delayed_Success_Timeout()
        {
            var v = SingleSource.Just(1)
                .Delay(TimeSpan.FromMilliseconds(100), NewThreadScheduler.Default)
                .Wait(5000);

            Assert.AreEqual(1, v);
        }

        [Test]
        public void Value_Delayed_Success_CancellationTokenSource()
        {
            var v = SingleSource.Just(1)
                .Delay(TimeSpan.FromMilliseconds(100), NewThreadScheduler.Default)
                .Wait(cts: new CancellationTokenSource());

            Assert.AreEqual(1, v);
        }

        [Test]
        public void Value_Delayed_Success_Timeout_CancellationTokenSource()
        {
            var v = SingleSource.Just(1)
                .Delay(TimeSpan.FromMilliseconds(100), NewThreadScheduler.Default)
                .Wait(5000, cts: new CancellationTokenSource());

            Assert.AreEqual(1, v);
        }

        [Test]
        public void Value_Error()
        {
            try
            {
                SingleSource.Error<int>(new InvalidOperationException())
                    .Wait();
                Assert.Fail();
            }
            catch (InvalidOperationException)
            {
                // expected
            }
        }

        [Test]
        public void Value_Timeout()
        {
            var cs = new SingleSubject<int>();
            try
            {
                cs
                    .Wait(100);
                Assert.Fail();
            }
            catch (TimeoutException)
            {
                // expected
            }

            Assert.False(cs.HasObserver());
        }

        [Test]
        public void Value_Cancel_Wait()
        {
            var cs = new SingleSubject<int>();
            var cts = new CancellationTokenSource();
            try
            {
                Task.Factory.StartNew(() =>
                {
                    while (!cs.HasObserver()) ;

                    Thread.Sleep(100);

                    cts.Cancel();
                });

                cs
                    .Wait(cts: cts);
                Assert.Fail();
            }
            catch (OperationCanceledException)
            {
                // expected
            }

            Assert.False(cs.HasObserver());
        }

        #endregion + Value output +
    }
}
