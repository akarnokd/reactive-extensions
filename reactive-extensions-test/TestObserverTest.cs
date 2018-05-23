using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Collections.Generic;

namespace akarnokd.reactive_extensions_test
{
    [TestFixture]
    public class TestObserverTest
    {
        [Test]
        public void OnNext_After_Termination()
        {
            var to = new TestObserver<int>();

            to.OnCompleted();

            to.OnNext(1);

            to.AssertError(typeof(InvalidOperationException));
        }

        [Test]
        public void OnSuccess_After_Termination()
        {
            var to = new TestObserver<int>();

            to.OnCompleted();

            to.OnSuccess(1);

            to.AssertError(typeof(InvalidOperationException));
        }

        [Test]
        public void OnError_After_Termination()
        {
            var to = new TestObserver<int>();

            to.OnCompleted();

            to.OnError(new ArgumentNullException());

            Assert.AreEqual(1, to.ErrorCount);
            Assert.AreEqual(1, to.CompletionCount);

            typeof(InvalidOperationException).IsAssignableFrom(to.Errors[0].GetType());
        }

        [Test]
        public void OnCompleted_After_Termination()
        {
            var to = new TestObserver<int>();

            to.OnCompleted();

            to.OnCompleted();

            Assert.AreEqual(2, to.CompletionCount);
        }

        [Test]
        public void AwaitDone_Timeout()
        {
            var to = new TestObserver<int>();

            to.AwaitDone(TimeSpan.FromMilliseconds(100));

            Assert.True(to.IsDisposed());
        }

        [Test]
        public void Fail_Full_Info()
        {
            var to = new TestObserver<int>();

            to.OnSubscribe(DisposableHelper.EMPTY);
            to.WithTag("Tag");

            try
            {
                to.AwaitDone(TimeSpan.FromMilliseconds(100));

                to.OnCompleted();

                to.AssertEmpty();

                Assert.Fail();
            }
            catch (Exception ex)
            {
                if (!ex.Message.StartsWith("Unexpected"))
                {
                    throw ex;
                }
            }
        }

        [Test]
        public void Fail_Multi_Error_Info()
        {
            var to = new TestObserver<int>();

            to.OnSubscribe(DisposableHelper.EMPTY);
            to.WithTag("Tag");

            try
            {
                to.AwaitDone(TimeSpan.FromMilliseconds(100));

                to.OnError(new NullReferenceException());

                to.OnError(new InvalidOperationException());

                to.AssertEmpty();

                Assert.Fail();
            }
            catch (Exception ex)
            {
                if (!ex.Message.StartsWith("Multiple errors"))
                {
                    throw ex;
                }
            }
        }

        [Test]
        public void Fail_Single_Error_Info()
        {
            var to = new TestObserver<int>();

            to.OnSubscribe(DisposableHelper.EMPTY);
            to.WithTag("Tag");

            try
            {
                to.AwaitDone(TimeSpan.FromMilliseconds(100));

                to.OnError(new NullReferenceException());

                to.AssertEmpty();

                Assert.Fail();
            }
            catch (Exception ex)
            {
                if (!ex.Message.StartsWith("Unexpected"))
                {
                    throw ex;
                }
            }
        }

        [Test]
        public void Wrong_Value()
        {
            var to = new TestObserver<int>();

            to.OnNext(1);

            try
            {
                to.AssertValuesOnly(2);
                Assert.Fail();
            }
            catch (Exception ex)
            {
                if (!ex.Message.StartsWith("Item @ 0 differ."))
                {
                    throw ex;
                }
            }
        }

        [Test]
        public void Wrong_Enumerable()
        {
            var to = new TestObserver<List<int>>();

            to.OnNext(new List<int>() { 1 });

            try
            {
                to.AssertValuesOnly(new List<int>() { 2 });
                Assert.Fail();
            }
            catch (Exception ex)
            {
                if (!ex.Message.StartsWith("Item @ 0/0 differ."))
                {
                    throw ex;
                }
            }
        }

        [Test]
        public void Wrong_Types()
        {
            var to = new TestObserver<object>();

            to.OnNext(1);

            try
            {
                to.AssertValuesOnly(new List<int>() { 2 });
                Assert.Fail();
            }
            catch (Exception ex)
            {
                if (!ex.Message.StartsWith("Item @ 0 differ."))
                {
                    throw ex;
                }
            }
        }

        [Test]
        public void Wrong_Types_2()
        {
            var to = new TestObserver<object>();

            to.OnNext(new List<int>() { 1 });

            try
            {
                to.AssertValuesOnly(2);
                Assert.Fail();
            }
            catch (Exception ex)
            {
                if (!ex.Message.StartsWith("Item @ 0 differ."))
                {
                    throw ex;
                }
            }
        }

        [Test]
        public void Enumerable_Item_Shorter()
        {
            var to = new TestObserver<List<int>>();

            to.OnNext(new List<int>() { 1, 2 });

            try
            {
                to.AssertValuesOnly(new List<int>() { 1 });
                Assert.Fail();
            }
            catch (Exception ex)
            {
                if (!ex.Message.StartsWith("Item @ 0, less items expected"))
                {
                    throw ex;
                }
            }
        }

        [Test]
        public void Enumerable_Item_Longer()
        {
            var to = new TestObserver<List<int>>();

            to.OnNext(new List<int>() { 1 });

            try
            {
                to.AssertValuesOnly(new List<int>() { 1, 2 });
                Assert.Fail();
            }
            catch (Exception ex)
            {
                if (!ex.Message.StartsWith("Item @ 0, more items expected"))
                {
                    throw ex;
                }
            }
        }
    }
}
