using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Threading.Tasks;
using System.Threading;

namespace akarnokd.reactive_extensions_test.single
{
    [TestFixture]
    public class SingleFromTask
    {
        [Test]
        public void Task_TResult_Basic()
        {
            var count = 0;

            var task = Task.Factory.StartNew(() => count++);

            var co = task.ToSingle();

            co.Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertResult(0);

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Task_TResult_Basic_From()
        {
            var count = 0;

            var task = Task.Factory.StartNew(() => count++);

            var co = SingleSource.FromTask(task);

            co.Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertResult(0);

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Task_TResult_Error()
        {
            var count = 0;

            var task = Task.Factory.StartNew((Func<int>)(() => {
                count++;
                throw new InvalidOperationException();
            }));

            var co = task.ToSingle();

            co.Test()
                .AwaitDone(TimeSpan.FromSeconds(5))
                .AssertFailure(typeof(AggregateException))
                .AssertCompositeError(typeof(InvalidOperationException));

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Task_TResult_Dispose()
        {
            var cdl = new CountdownEvent(1);

            var task = Task.Factory.StartNew(() =>
            {
                cdl.Wait();
                return 0;
            });

            var co = task.ToSingle();

            var to = co.Test(true);

            cdl.Signal();

            task.Wait();

            to.AssertEmpty();
        }
    }
}
