using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Concurrency;

namespace akarnokd.reactive_extensions_test.maybe
{
    [TestFixture]
    public class MaybeWaitTest
    {
        #region + Ignore value +

        [Test]
        public void NoValue_Basic()
        {
            MaybeSource.Empty<int>()
                .Wait();
        }

        [Test]
        public void NoValue_Success()
        {
            MaybeSource.Just(1)
                .Wait();
        }

        [Test]
        public void NoValue_Basic_Timeout()
        {
            MaybeSource.Empty<int>()
                .Wait(5000);
        }

        [Test]
        public void NoValue_Basic_CancellationTokenSource()
        {
            MaybeSource.Empty<int>()
                .Wait(cts: new CancellationTokenSource());
        }

        [Test]
        public void NoValue_Basic_Timeout_CancellationTokenSource()
        {
            MaybeSource.Empty<int>()
                .Wait(5000, cts: new CancellationTokenSource());
        }

        [Test]
        public void NoValue_Delayed_Basic()
        {
            MaybeSource.Empty<int>()
                .Delay(TimeSpan.FromMilliseconds(100), NewThreadScheduler.Default)
                .Wait();
        }

        [Test]
        public void NoValue_Delayed_Basic_Timeout()
        {
            MaybeSource.Empty<int>()
                .Delay(TimeSpan.FromMilliseconds(100), NewThreadScheduler.Default)
                .Wait(5000);
        }

        [Test]
        public void NoValue_Delayed_Basic_CancellationTokenSource()
        {
            MaybeSource.Empty<int>()
                .Delay(TimeSpan.FromMilliseconds(100), NewThreadScheduler.Default)
                .Wait(cts: new CancellationTokenSource());
        }

        [Test]
        public void NoValue_Delayed_Basic_Timeout_CancellationTokenSource()
        {
            MaybeSource.Empty<int>()
                .Delay(TimeSpan.FromMilliseconds(100), NewThreadScheduler.Default)
                .Wait(5000, cts: new CancellationTokenSource());
        }

        [Test]
        public void NoValue_Error()
        {
            try
            {
                MaybeSource.Error<int>(new InvalidOperationException())
                    .Wait();
                Assert.Fail();
            }
            catch (InvalidOperationException)
            {
                // expected
            }
        }

        [Test]
        public void NoValue_Timeout()
        {
            var cs = new MaybeSubject<int>();
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
        public void NoValue_Cancel_Wait()
        {
            var cs = new MaybeSubject<int>();
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

        #endregion + Ignore value +

        #region + Value output +


        [Test]
        public void Value_Basic()
        {
            var b = MaybeSource.Empty<int>()
                .Wait(out var v);

            Assert.False(b);
        }

        [Test]
        public void Value_Success()
        {
            var b = MaybeSource.Just(1)
                .Wait(out var v);

            Assert.True(b);
            Assert.AreEqual(1, v);
        }

        [Test]
        public void Value_Basic_Timeout()
        {
            var b = MaybeSource.Empty<int>()
                .Wait(out var v, 5000);

            Assert.False(b);
        }

        [Test]
        public void Value_Basic_CancellationTokenSource()
        {
            var b = MaybeSource.Empty<int>()
                .Wait(out var v, cts: new CancellationTokenSource());

            Assert.False(b);
        }

        [Test]
        public void Value_Basic_Timeout_CancellationTokenSource()
        {
            var b = MaybeSource.Empty<int>()
                .Wait(out var v, 5000, cts: new CancellationTokenSource());

            Assert.False(b);
        }

        [Test]
        public void Value_Delayed_Basic()
        {
            var b = MaybeSource.Empty<int>()
                .Delay(TimeSpan.FromMilliseconds(100), NewThreadScheduler.Default)
                .Wait(out var v);

            Assert.False(b);
        }

        [Test]
        public void Value_Delayed_Basic_Timeout()
        {
            var b = MaybeSource.Empty<int>()
                .Delay(TimeSpan.FromMilliseconds(100), NewThreadScheduler.Default)
                .Wait(out var v, 5000);

            Assert.False(b);
        }

        [Test]
        public void Value_Delayed_Basic_CancellationTokenSource()
        {
            var b = MaybeSource.Empty<int>()
                .Delay(TimeSpan.FromMilliseconds(100), NewThreadScheduler.Default)
                .Wait(out var v, cts: new CancellationTokenSource());

            Assert.False(b);
        }

        [Test]
        public void Value_Delayed_Basic_Timeout_CancellationTokenSource()
        {
            var b = MaybeSource.Empty<int>()
                .Delay(TimeSpan.FromMilliseconds(100), NewThreadScheduler.Default)
                .Wait(out var v, 5000, cts: new CancellationTokenSource());

            Assert.False(b);
        }

        [Test]
        public void Value_Error()
        {
            try
            {
                MaybeSource.Error<int>(new InvalidOperationException())
                    .Wait(out var v);
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
            var cs = new MaybeSubject<int>();
            try
            {
                cs
                    .Wait(out var v, 100);
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
            var cs = new MaybeSubject<int>();
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
                    .Wait(out var v, cts: cts);
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
