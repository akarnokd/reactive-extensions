using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Threading;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceToTaskTest
    {
        [Test]
        public void IgnoreElementsTask_Basic()
        {
            Assert.True(ObservableSource.Range(1, 5)
                .IgnoreElementsTask()
                .Wait(5000));
        }

        [Test]
        public void IgnoreElementsTask_Empty()
        {
            Assert.True(ObservableSource.Empty<int>()
                .IgnoreElementsTask()
                .Wait(5000));
        }

        [Test]
        public void IgnoreElementsTask_Error()
        {
            try
            {
                Assert.True(ObservableSource.Error<int>(new InvalidOperationException())
                    .IgnoreElementsTask()
                    .Wait(5000));
            }
            catch (AggregateException ex)
            {
                Assert.True(typeof(InvalidOperationException).IsAssignableFrom(ex.InnerExceptions[0]));
            }
        }

        [Test]
        public void IgnoreElementsTask_Dispose()
        {
            var subj = new PublishSubject<int>();

            var cts = new CancellationTokenSource();

            var task = subj.IgnoreElementsTask(cts);

            Assert.True(subj.HasObservers);

            cts.Cancel();

            Assert.False(subj.HasObservers);

            Assert.True(task.IsCanceled);
        }

        [Test]
        public void FirstTask_Basic()
        {
            var task = ObservableSource.Range(1, 5)
                .FirstTask();
            Assert.True(task.Wait(5000));

            Assert.AreEqual(1, task.Result);
        }

        [Test]
        public void FirstTask_Empty()
        {
            try
            {
                Assert.True(ObservableSource.Empty<int>()
                .FirstTask()
                .Wait(5000));
            }
            catch (AggregateException ex)
            {
                Assert.True(typeof(IndexOutOfRangeException).IsAssignableFrom(ex.InnerExceptions[0]));
            }
        }

        [Test]
        public void FirstTask_Error()
        {
            try
            {
                Assert.True(ObservableSource.Error<int>(new InvalidOperationException())
                    .FirstTask()
                    .Wait(5000));
            }
            catch (AggregateException ex)
            {
                Assert.True(typeof(InvalidOperationException).IsAssignableFrom(ex.InnerExceptions[0]));
            }
        }

        [Test]
        public void FirstTask_Dispose()
        {
            var subj = new PublishSubject<int>();

            var cts = new CancellationTokenSource();

            var task = subj.FirstTask(cts);

            Assert.True(subj.HasObservers);

            cts.Cancel();

            Assert.False(subj.HasObservers);

            Assert.True(task.IsCanceled);
        }

        [Test]
        public void ElementAtTask_Basic()
        {
            var task = ObservableSource.Range(1, 5)
                .ElementAtTask(2);
            Assert.True(task.Wait(5000));

            Assert.AreEqual(3, task.Result);
        }

        [Test]
        public void ElementAtTask_Empty()
        {
            try
            {
                Assert.True(ObservableSource.Empty<int>()
                .ElementAtTask(1)
                .Wait(5000));
            }
            catch (AggregateException ex)
            {
                Assert.True(typeof(IndexOutOfRangeException).IsAssignableFrom(ex.InnerExceptions[0]));
            }
        }

        [Test]
        public void ElementAtTask_Shorter()
        {
            try
            {
                Assert.True(ObservableSource.Range(1, 5)
                .ElementAtTask(5)
                .Wait(5000));
            }
            catch (AggregateException ex)
            {
                Assert.True(typeof(IndexOutOfRangeException).IsAssignableFrom(ex.InnerExceptions[0]));
            }
        }

        [Test]
        public void ElementAtTask_Error()
        {
            try
            {
                Assert.True(ObservableSource.Error<int>(new InvalidOperationException())
                    .ElementAtTask(1)
                    .Wait(5000));
            }
            catch (AggregateException ex)
            {
                Assert.True(typeof(InvalidOperationException).IsAssignableFrom(ex.InnerExceptions[0]));
            }
        }

        [Test]
        public void ElementatTask_Dispose()
        {
            var subj = new PublishSubject<int>();

            var cts = new CancellationTokenSource();

            var task = subj.ElementAtTask(1, cts);

            Assert.True(subj.HasObservers);

            cts.Cancel();

            Assert.False(subj.HasObservers);

            Assert.True(task.IsCanceled);
        }

        [Test]
        public void SingleTask_Basic()
        {
            var task = ObservableSource.Just(1)
                .SingleTask();
            Assert.True(task.Wait(5000));

            Assert.AreEqual(1, task.Result);
        }

        [Test]
        public void SingleTask_Empty()
        {
            try
            {
                Assert.True(ObservableSource.Empty<int>()
                .SingleTask()
                .Wait(5000));
            }
            catch (AggregateException ex)
            {
                Assert.True(typeof(IndexOutOfRangeException).IsAssignableFrom(ex.InnerExceptions[0]));
            }
        }

        [Test]
        public void SingleTask_More_Than_One()
        {
            try
            {
                Assert.True(ObservableSource.Range(1, 5)
                .SingleTask()
                .Wait(5000));
            }
            catch (AggregateException ex)
            {
                Assert.True(typeof(IndexOutOfRangeException).IsAssignableFrom(ex.InnerExceptions[0]));
            }
        }

        [Test]
        public void SingleTask_Error()
        {
            try
            {
                Assert.True(ObservableSource.Error<int>(new InvalidOperationException())
                    .SingleTask()
                    .Wait(5000));
            }
            catch (AggregateException ex)
            {
                Assert.True(typeof(InvalidOperationException).IsAssignableFrom(ex.InnerExceptions[0]));
            }
        }

        [Test]
        public void SingleTask_Multiple_Dispose()
        {
            var subj = new PublishSubject<int>();

            var task = subj.SingleTask();

            Assert.True(subj.HasObservers);

            subj.OnNext(1);

            Assert.True(subj.HasObservers);
            Assert.False(task.IsCompleted);
            Assert.False(task.IsCanceled);
            Assert.False(task.IsFaulted);

            subj.OnNext(2);

            Assert.False(subj.HasObservers);

            Assert.True(task.IsCompleted);
            Assert.False(task.IsCanceled);
            Assert.True(task.IsFaulted);

            Assert.True(typeof(IndexOutOfRangeException).IsAssignableFrom((task.Exception as AggregateException).InnerExceptions[0]));
        }

        [Test]
        public void SingleTask_Dispose()
        {
            var subj = new PublishSubject<int>();

            var cts = new CancellationTokenSource();

            var task = subj.SingleTask(cts);

            Assert.True(subj.HasObservers);

            cts.Cancel();

            Assert.False(subj.HasObservers);

            Assert.True(task.IsCanceled);
        }

        [Test]
        public void LastTask_Basic()
        {
            var task = ObservableSource.Range(1, 5)
                .LastTask();
            Assert.True(task.Wait(5000));

            Assert.AreEqual(5, task.Result);
        }

        [Test]
        public void LastTask_Empty()
        {
            try
            {
                Assert.True(ObservableSource.Empty<int>()
                    .LastTask()
                    .Wait(5000));
            }
            catch (AggregateException ex)
            {
                Assert.True(typeof(IndexOutOfRangeException).IsAssignableFrom(ex.InnerExceptions[0]));
            }
        }

        [Test]
        public void LastTask_Error()
        {
            try
            {
                Assert.True(ObservableSource.Error<int>(new InvalidOperationException())
                    .LastTask()
                    .Wait(5000));
            }
            catch (AggregateException ex)
            {
                Assert.True(typeof(InvalidOperationException).IsAssignableFrom(ex.InnerExceptions[0]));
            }
        }

        [Test]
        public void LastTask_Dispose()
        {
            var subj = new PublishSubject<int>();

            var cts = new CancellationTokenSource();

            var task = subj.LastTask(cts);

            Assert.True(subj.HasObservers);

            cts.Cancel();

            Assert.False(subj.HasObservers);

            Assert.True(task.IsCanceled);
        }
    }
}
