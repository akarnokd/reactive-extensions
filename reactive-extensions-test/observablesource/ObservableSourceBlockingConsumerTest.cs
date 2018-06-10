using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Concurrency;
using System.Threading;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceBlockingConsumerTest
    {
        [Test]
        public void First_Basic()
        {
            var v = ObservableSource.Range(1, 5)
                .BlockingFirst();

            Assert.AreEqual(1, v);
        }

        [Test]
        public void First_Empty()
        {
            try
            {
                ObservableSource.Empty<int>()
                    .BlockingFirst();
                Assert.Fail("Should have thrown");
            }
            catch (IndexOutOfRangeException)
            {
                // expected
            }
        }

        [Test]
        public void First_Delayed()
        {
            var v = ObservableSource.Range(1, 5).Delay(TimeSpan.FromMilliseconds(20), ThreadPoolScheduler.Instance)
                .BlockingFirst();

            Assert.AreEqual(1, v);
        }

        [Test]
        public void First_Error()
        {
            try
            {
                ObservableSource.Error<int>(new InvalidOperationException())
                    .BlockingFirst();
                Assert.Fail("Should have thrown");
            }
            catch (InvalidOperationException)
            {
                // expected
            }
        }

        [Test]
        public void First_Cancel()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();

            try
            {
                ObservableSource.Never<int>()
                    .BlockingFirst(cts);

                Assert.Fail("Should have thrown");
            }
            catch (OperationCanceledException)
            {
                // expected
            }
        }

        [Test]
        public void TryFirst_NonEmpty()
        {
            var v = ObservableSource.Range(1, 5)
                .BlockingTryFirst(out var success);

            Assert.True(success);
            Assert.AreEqual(1, v);
        }

        [Test]
        public void TryFirst_Empty()
        {
            var v = ObservableSource.Empty<int>()
                .BlockingTryFirst(out var success);

            Assert.False(success);
            Assert.AreEqual(default(int), v);
        }

        [Test]
        public void TryFirst_Cancel()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();

            try
            {
                ObservableSource.Never<int>()
                    .BlockingTryFirst(out var _, cts);

                Assert.Fail("Should have thrown");
            }
            catch (OperationCanceledException)
            {
                // expected
            }
        }

        [Test]
        public void TryFirst_Timeout()
        {
            try
            {
                ObservableSource.Never<int>()
                    .BlockingTryFirst(out var _, TimeSpan.FromMilliseconds(20));

                Assert.Fail("Should have thrown");
            }
            catch (TimeoutException)
            {
                // expected
            }
        }

        [Test]
        public void TryFirst_Cancel_Timeout()
        {
            var cts = new CancellationTokenSource();

            try
            {
                ObservableSource.Never<int>()
                    .BlockingTryFirst(out var _, TimeSpan.FromMilliseconds(20));

                Assert.Fail("Should have thrown");
            }
            catch (TimeoutException)
            {
                // expected
            }
        }

        [Test]
        public void Last_Basic()
        {
            var v = ObservableSource.Range(1, 5)
                .BlockingLast();

            Assert.AreEqual(5, v);
        }

        [Test]
        public void Last_Empty()
        {
            try
            {
                ObservableSource.Empty<int>()
                    .BlockingLast();
                Assert.Fail("Should have thrown");
            }
            catch (IndexOutOfRangeException)
            {
                // expected
            }
        }

        [Test]
        public void Last_Delayed()
        {
            var v = ObservableSource.Range(1, 5).Delay(TimeSpan.FromMilliseconds(20), ThreadPoolScheduler.Instance)
                .BlockingLast();

            Assert.AreEqual(5, v);
        }

        [Test]
        public void Last_Error()
        {
            try
            {
                ObservableSource.Error<int>(new InvalidOperationException())
                    .BlockingLast();
                Assert.Fail("Should have thrown");
            }
            catch (InvalidOperationException)
            {
                // expected
            }
        }

        [Test]
        public void Last_Cancel()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();

            try
            {
                ObservableSource.Never<int>()
                    .BlockingLast(cts);

                Assert.Fail("Should have thrown");
            }
            catch (OperationCanceledException)
            {
                // expected
            }
        }

        [Test]
        public void TryLast_NonEmpty()
        {
            var v = ObservableSource.Range(1, 5)
                .BlockingTryLast(out var success);

            Assert.True(success);
            Assert.AreEqual(5, v);
        }

        [Test]
        public void TryLast_Empty()
        {
            var v = ObservableSource.Empty<int>()
                .BlockingTryLast(out var success);

            Assert.False(success);
            Assert.AreEqual(default(int), v);
        }

        [Test]
        public void TryLast_Cancel()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();

            try
            {
                ObservableSource.Never<int>()
                    .BlockingTryLast(out var _, cts);

                Assert.Fail("Should have thrown");
            }
            catch (OperationCanceledException)
            {
                // expected
            }
        }

        [Test]
        public void TryLast_Timeout()
        {
            try
            {
                ObservableSource.Never<int>()
                    .BlockingTryLast(out var _, TimeSpan.FromMilliseconds(20));

                Assert.Fail("Should have thrown");
            }
            catch (TimeoutException)
            {
                // expected
            }
        }

        [Test]
        public void TryLast_Cancel_Timeout()
        {
            var cts = new CancellationTokenSource();

            try
            {
                ObservableSource.Never<int>()
                    .BlockingTryLast(out var _, TimeSpan.FromMilliseconds(20));

                Assert.Fail("Should have thrown");
            }
            catch (TimeoutException)
            {
                // expected
            }
        }

        [Test]
        public void Single_Basic()
        {
            var v = ObservableSource.Just(1)
                .BlockingFirst();

            Assert.AreEqual(1, v);
        }

        [Test]
        public void Single_Empty()
        {
            try
            {
                ObservableSource.Empty<int>()
                    .BlockingSingle();
                Assert.Fail("Should have thrown");
            }
            catch (IndexOutOfRangeException)
            {
                // expected
            }
        }

        [Test]
        public void Single_Multiple()
        {
            try
            {
                ObservableSource.Range(1, 5)
                    .BlockingSingle();
                Assert.Fail("Should have thrown");
            }
            catch (IndexOutOfRangeException)
            {
                // expected
            }
        }

        [Test]
        public void Single_Delayed()
        {
            var v = ObservableSource.Just(1).Delay(TimeSpan.FromMilliseconds(20), ThreadPoolScheduler.Instance)
                .BlockingSingle();

            Assert.AreEqual(1, v);
        }

        [Test]
        public void Single_Error()
        {
            try
            {
                ObservableSource.Error<int>(new InvalidOperationException())
                    .BlockingSingle();
                Assert.Fail("Should have thrown");
            }
            catch (InvalidOperationException)
            {
                // expected
            }
        }

        [Test]
        public void Single_Cancel()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();

            try
            {
                ObservableSource.Never<int>()
                    .BlockingSingle(cts);

                Assert.Fail("Should have thrown");
            }
            catch (OperationCanceledException)
            {
                // expected
            }
        }

        [Test]
        public void TrySingle_NonEmpty()
        {
            var v = ObservableSource.Just(1)
                .BlockingTrySingle(out var success);

            Assert.True(success);
            Assert.AreEqual(1, v);
        }

        [Test]
        public void TrySingle_Multiple()
        {
            try
            {
                var v = ObservableSource.Range(1, 5)
                    .BlockingTrySingle(out var success);
                Assert.Fail("Should have thrown");
            }
            catch (IndexOutOfRangeException)
            {

            }
        }

        [Test]
        public void TrySingle_Empty()
        {
            var v = ObservableSource.Empty<int>()
                .BlockingTrySingle(out var success);

            Assert.False(success);
            Assert.AreEqual(default(int), v);
        }

        [Test]
        public void TrySingle_Cancel()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();

            try
            {
                ObservableSource.Never<int>()
                    .BlockingTrySingle(out var _, cts);

                Assert.Fail("Should have thrown");
            }
            catch (OperationCanceledException)
            {
                // expected
            }
        }

        [Test]
        public void TrySingle_Timeout()
        {
            try
            {
                ObservableSource.Never<int>()
                    .BlockingTrySingle(out var _, TimeSpan.FromMilliseconds(20));

                Assert.Fail("Should have thrown");
            }
            catch (TimeoutException)
            {
                // expected
            }
        }

        [Test]
        public void TrySingle_Cancel_Timeout()
        {
            var cts = new CancellationTokenSource();

            try
            {
                ObservableSource.Never<int>()
                    .BlockingTrySingle(out var _, TimeSpan.FromMilliseconds(20));

                Assert.Fail("Should have thrown");
            }
            catch (TimeoutException)
            {
                // expected
            }
        }
    }
}
