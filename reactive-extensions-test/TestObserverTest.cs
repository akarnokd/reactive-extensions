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

        [Test]
        public void Async_Fusion_TryPoll_Crash()
        {
            var ms = new MonocastSubject<int>();

            var to = ms.Map<int, int>(v => throw new InvalidOperationException()).Test(fusionMode: FusionSupport.Any);

            to.AssertEmpty()
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Async);

            ms.OnNext(1);

            to.AssertFailure(typeof(InvalidOperationException));

            Assert.False(ms.HasObservers);
        }

        [Test]
        public void Sync_Fused_Bad_OnNext()
        {
            var to = new TestObserver<int>(true, FusionSupport.Sync);
            var parent = new BadSyncFuseableDisposable(to);
            to.OnSubscribe(parent);

            to.AssertFuseable()
                .AssertFusionMode(FusionSupport.Sync);

            to.OnNext(1);

            Assert.AreEqual(1, to.ErrorCount);

            to.AssertError(typeof(InvalidOperationException));
        }

        [Test]
        public void Fused_Failure_Message()
        {
            var ms = new MonocastSubject<int>();

            var to = ms.Test(fusionMode: FusionSupport.Any);

            to.AssertEmpty()
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Async);

            ms.OnNext(1);

            try
            {
                to.AssertEmpty();
                Assert.Fail("Should have thrown");
            }
            catch (AssertionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                Assert.True(msg.Contains("fuseable!"));
                Assert.True(msg.Contains("fusion-requested"));
                Assert.True(msg.Contains("fusion-established"));
            }
        }

        [Test]
        public void AssertValues_Different()
        {
            try
            {
                new TestObserver<int>().AssertValues(1);
                Assert.Fail("Should have thrown");
            }
            catch (AssertionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                Assert.True(msg.Contains("Number of items differ"));
            }
        }

        [Test]
        public void AssertCompleted_Not_Completed()
        {
            try
            {
                new TestObserver<int>().AssertCompleted();
                Assert.Fail("Should have thrown");
            }
            catch (AssertionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                Assert.True(msg.Contains("Not completed"));
            }
        }

        [Test]
        public void AssertCompleted_Multiple_Completions()
        {
            try
            {
                var to = new TestObserver<int>();
                    
                to.OnCompleted();
                to.OnCompleted();

                to.AssertCompleted();
                Assert.Fail("Should have thrown");
            }
            catch (AssertionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                Assert.True(msg.Contains("Multiple completions"));
            }
        }

        [Test]
        public void AssertNotCompleted_Multiple_Completions()
        {
            try
            {
                var to = new TestObserver<int>();

                to.OnCompleted();
                to.OnCompleted();

                to.AssertNotCompleted();
                Assert.Fail("Should have thrown");
            }
            catch (AssertionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                Assert.True(msg.Contains("Multiple completions"));
            }
        }

        [Test]
        public void AssertError_No_Error()
        {
            try
            {
                var to = new TestObserver<int>();

                to.AssertError(typeof(InvalidOperationException));
                Assert.Fail("Should have thrown");
            }
            catch (AssertionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                Assert.True(msg.Contains("No error"));
            }
        }

        [Test]
        public void AssertError_Contains()
        {
            var to = new TestObserver<int>();
            to.OnError(new InvalidOperationException("part_of_message"));

            to.AssertError(typeof(InvalidOperationException), "_of_", true);
        }

        [Test]
        public void AssertError_Doesnt_Contain()
        {
            var to = new TestObserver<int>();
            to.OnError(new InvalidOperationException("part_of_message"));

            try
            {
                to.AssertError(typeof(InvalidOperationException), "_on_", true);
                Assert.Fail("Should have thrown");
            }
            catch (AssertionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                Assert.True(msg.Contains("Exception type present but not with the specified message"));
            }
        }

        [Test]
        public void AssertError_Different_Exception()
        {
            var to = new TestObserver<int>();
            to.OnError(new IndexOutOfRangeException("part_of_message"));

            try
            {
                to.AssertError(typeof(InvalidOperationException), "_on_", true);
                Assert.Fail("Should have thrown");
            }
            catch (AssertionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                Assert.True(msg.Contains("Exception not present"));
            }
        }

        [Test]
        public void AssertValueCount_Wrong()
        {
            var to = new TestObserver<int>();

            try
            {
                to.AssertValueCount(1);
                Assert.Fail("Should have thrown");
            }
            catch (AssertionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                Assert.True(msg.Contains("Number of items differ"));
            }
        }

        [Test]
        public void AssertCompositeException_Out_Of_Index()
        {
            var to = new TestObserver<int>();
            to.OnError(new AggregateException(
                new InvalidOperationException(),
                new IndexOutOfRangeException()
            ));

            try
            {
                to.AssertCompositeError(2, typeof(NullReferenceException));
                Assert.Fail("Should have thrown");
            }
            catch (AssertionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                Assert.True(msg.Contains("The AggregateException index out of bounds"));
            }
        }

        [Test]
        public void AssertCompositeException_Wrong_Error()
        {
            var to = new TestObserver<int>();
            to.OnError(new AggregateException(
                new InvalidOperationException(),
                new IndexOutOfRangeException()
            ));

            try
            {
                to.AssertCompositeError(1, typeof(NullReferenceException));
                Assert.Fail("Should have thrown");
            }
            catch (AssertionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                Assert.True(msg.Contains("Wrong error type"));
            }
        }

        [Test]
        public void AssertCompositeException_Right_Message()
        {
            var to = new TestObserver<int>();
            to.OnError(new AggregateException(
                new InvalidOperationException(),
                new IndexOutOfRangeException("message")
            ));

            to.AssertCompositeError(1, typeof(IndexOutOfRangeException), "message");
        }

        [Test]
        public void AssertCompositeException_Right_Message_Part()
        {
            var to = new TestObserver<int>();
            to.OnError(new AggregateException(
                new InvalidOperationException(),
                new IndexOutOfRangeException("message_of_part")
            ));

            to.AssertCompositeError(1, typeof(IndexOutOfRangeException), "_of_", true);
        }

        [Test]
        public void AssertCompositeException_Wrong_Message()
        {
            var to = new TestObserver<int>();
            to.OnError(new AggregateException(
                new InvalidOperationException(),
                new IndexOutOfRangeException()
            ));

            try
            {
                to.AssertCompositeError(1, typeof(IndexOutOfRangeException), "message");
                Assert.Fail("Should have thrown");
            }
            catch (AssertionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                Assert.True(msg.Contains("Error found with a different message"));
            }
        }

        [Test]
        public void AssertCompositeException_Wrong_Message_Part()
        {
            var to = new TestObserver<int>();
            to.OnError(new AggregateException(
                new InvalidOperationException(),
                new IndexOutOfRangeException("message_of_part")
            ));

            try
            {
                to.AssertCompositeError(1, typeof(IndexOutOfRangeException), "_on_", true);
                Assert.Fail("Should have thrown");
            }
            catch (AssertionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                Assert.True(msg.Contains("Error found with a different message"));
            }
        }

        sealed class BadSyncFuseableDisposable : IFuseableDisposable<int>
        {
            readonly ISignalObserver<int> downstream;

            public BadSyncFuseableDisposable(ISignalObserver<int> downstream)
            {
                this.downstream = downstream;
            }

            public bool TryOffer(int item)
            {
                throw new InvalidOperationException("Should not be called");
            }

            public int TryPoll(out bool success)
            {
                success = false;
                return default(int);
            }

            public bool IsEmpty()
            {
                return true;
            }

            public void Clear()
            {
                // no op
            }

            public int RequestFusion(int mode)
            {
                return mode & FusionSupport.Sync;
            }

            public void Dispose()
            {
                throw new NotImplementedException();
            }
        }
        
    }
}
