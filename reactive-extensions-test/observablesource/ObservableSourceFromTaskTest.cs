using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Threading.Tasks;
using System.Threading;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceFromTaskTest
    {
        [Test]
        public void Plain_Basic()
        {
            ObservableSource.FromTask<int>(Task.Factory.StartNew(() => { }))
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertResult();
        }

        [Test]
        public void Plain_Basic_To()
        {
            Task.Factory.StartNew(() => { }).ToObservableSource<int>()
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertResult();
        }

        [Test]
        public void Plain_Error()
        {
            ObservableSource.FromTask<int>(Task.Factory.StartNew(() => { throw new InvalidOperationException(); }))
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertFailure(typeof(AggregateException))
                .AssertCompositeError(typeof(InvalidOperationException));
        }

        [Test]
        public void Plain_Dispose()
        {
            var ttc = new TaskCompletionSource<int>();

            var to = ObservableSource.FromTask<int>((Task)ttc.Task)
                .Test();

            to.AssertEmpty();

            to.Dispose();

            Assert.True(ttc.TrySetResult(0));

            Thread.Sleep(100);

            to.AssertEmpty();
        }

        [Test]
        public void Plain_Cancel()
        {
            var ttc = new TaskCompletionSource<int>();

            var to = ObservableSource.FromTask<int>((Task)ttc.Task)
                .Test();

            to.AssertEmpty();

            Assert.True(ttc.TrySetCanceled());

            to
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertFailure(typeof(OperationCanceledException));
        }

        [Test]
        public void Plain_Fused()
        {
            var ttc = new TaskCompletionSource<int>();

            var to = ObservableSource.FromTask<int>((Task)ttc.Task)
                .Test(fusionMode: FusionSupport.Any)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Async);

            to.AssertEmpty();

            Assert.True(ttc.TrySetResult(default(int)));

            to.AwaitDone(TimeSpan.FromSeconds(5))
                .AssertResult();
        }

        [Test]
        public void Plain_Fused_Api()
        {
            var ttc = new TaskCompletionSource<int>();

            var src = ObservableSource.FromTask<int>((Task) ttc.Task);

            TestHelper.AssertFuseableApi(src);
        }

        [Test]
        public void Value_Basic()
        {
            var count = 0;

            var source = ObservableSource.FromTask<int>(Task.Factory.StartNew(() => ++count));

            source
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertResult(1);

            source
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertResult(1);

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Value_Basic_To()
        {
            Task.Factory.StartNew(() => 1).ToObservableSource()
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertResult(1);
        }

        [Test]
        public void Value_Error()
        {
            ObservableSource.FromTask(Task.Factory.StartNew<int>(() => { throw new InvalidOperationException(); }))
                .Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertFailure(typeof(AggregateException))
                .AssertCompositeError(typeof(InvalidOperationException));
        }

        [Test]
        public void Value_Dispose()
        {
            var ttc = new TaskCompletionSource<int>();

            var to = ObservableSource.FromTask(ttc.Task)
                .Test();

            to.AssertEmpty();

            to.Dispose();

            Assert.True(ttc.TrySetResult(0));

            Thread.Sleep(100);

            to.AssertEmpty();
        }

        [Test]
        public void Value_Cancel()
        {
            var ttc = new TaskCompletionSource<int>();

            var to = ObservableSource.FromTask(ttc.Task)
                .Test();

            to.AssertEmpty();

            Assert.True(ttc.TrySetCanceled());

            to
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertFailure(typeof(OperationCanceledException));
        }

        [Test]
        public void Value_Fused()
        {
            var ttc = new TaskCompletionSource<int>();

            var to = ObservableSource.FromTask<int>(ttc.Task)
                .Test(fusionMode: FusionSupport.Any)
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Async);

            to.AssertEmpty();

            Assert.True(ttc.TrySetResult(1));

            to.AwaitDone(TimeSpan.FromSeconds(5))
                .AssertResult(1);
        }

    }
}
