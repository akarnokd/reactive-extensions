using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Threading;

namespace akarnokd.reactive_extensions_test.completable
{
    [TestFixture]
    public class CompletableToTaskTest
    {
        [Test]
        public void Basic()
        {
            Assert.True(
                CompletableSource.Empty()
                .ToTask()
                .Wait(5000)
            );
        }

        [Test]
        public void Error()
        {
            try
            {
                Assert.True(
                    CompletableSource.Error(new InvalidOperationException())
                    .ToTask()
                    .Wait(5000)
                );

                Assert.Fail();
            }
            catch (AggregateException ex)
            {
                Assert.True(typeof(InvalidOperationException).IsAssignableFrom(ex.InnerExceptions[0].GetType()));
            }
        }

        [Test]
        public void Basic_Token()
        {
            var cts = new CancellationTokenSource();

            Assert.True(
                CompletableSource.Empty()
                .ToTask(cts)
                .Wait(5000)
            );
        }

        [Test]
        public void Error_Token()
        {
            var cts = new CancellationTokenSource();

            try
            {
                Assert.True(
                    CompletableSource.Error(new InvalidOperationException())
                    .ToTask(cts)
                    .Wait(5000)
                );

                Assert.Fail("Did not throw");
            }
            catch (AggregateException ex)
            {
                Assert.True(typeof(InvalidOperationException).IsAssignableFrom(ex.InnerExceptions[0].GetType()));
            }
        }

        [Test]
        public void Cancel()
        {
            var cts = new CancellationTokenSource();

            var cs = new CompletableSubject();

            var task = cs
                .ToTask(cts);

            Assert.True(cs.HasObserver());

            Assert.False(task.IsCompleted);
            Assert.False(task.IsFaulted);

            cts.Cancel();

            Assert.False(cs.HasObserver());
        }

        [Test]
        public void Cancel_Upfront()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();

            var cs = new CompletableSubject();

            var task = cs
                .ToTask(cts);

            Assert.False(cs.HasObserver());
            Assert.True(task.IsCanceled);
        }
    }
}
