using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Concurrency;

namespace akarnokd.reactive_extensions_test.completable
{
    [TestFixture]
    public class CompletableWaitTest
    {
        [Test]
        public void Basic()
        {
            CompletableSource.Empty()
                .Wait();
        }

        [Test]
        public void Basic_Timeout()
        {
            CompletableSource.Empty()
                .Wait(5000);
        }

        [Test]
        public void Basic_CancellationTokenSource()
        {
            CompletableSource.Empty()
                .Wait(cts: new CancellationTokenSource());
        }

        [Test]
        public void Basic_Timeout_CancellationTokenSource()
        {
            CompletableSource.Empty()
                .Wait(5000, cts: new CancellationTokenSource());
        }

        [Test]
        public void Delayed_Basic()
        {
            CompletableSource.Empty()
                .Delay(TimeSpan.FromMilliseconds(100), NewThreadScheduler.Default)
                .Wait();
        }

        [Test]
        public void Delayed_Basic_Timeout()
        {
            CompletableSource.Empty()
                .Delay(TimeSpan.FromMilliseconds(100), NewThreadScheduler.Default)
                .Wait(5000);
        }

        [Test]
        public void Delayed_Basic_CancellationTokenSource()
        {
            CompletableSource.Empty()
                .Delay(TimeSpan.FromMilliseconds(100), NewThreadScheduler.Default)
                .Wait(cts: new CancellationTokenSource());
        }

        [Test]
        public void Delayed_Basic_Timeout_CancellationTokenSource()
        {
            CompletableSource.Empty()
                .Delay(TimeSpan.FromMilliseconds(100), NewThreadScheduler.Default)
                .Wait(5000, cts: new CancellationTokenSource());
        }

        [Test]
        public void Error()
        {
            try
            {
                CompletableSource.Error(new InvalidOperationException())
                    .Wait();
                Assert.Fail();
            }
            catch (InvalidOperationException)
            {
                // expected
            }
        }

        [Test]
        public void Timeout()
        {
            var cs = new CompletableSubject();
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
        public void Cancel_Wait()
        {
            var cs = new CompletableSubject();
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
    }
}
