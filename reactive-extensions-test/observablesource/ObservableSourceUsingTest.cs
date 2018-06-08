using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceUsingTest
    {
        [Test]
        public void Eager_Basic()
        {
            for (int i = 0; i < 10; i++) {
                var res = -1;

                ObservableSource.Using(() => i, j => ObservableSource.Range(j, 5), k => res = k, true)
                    .Test()
                    .AssertResult(i, i + 1, i + 2, i + 3, i + 4);

                Assert.AreEqual(i, res);
            }
        }

        [Test]
        public void Eager_Fused()
        {
            for (int i = 0; i < 10; i++)
            {
                var res = -1;

                ObservableSource.Using(() => i, j => ObservableSource.Range(j, 5), k => res = k, true)
                    .Test(fusionMode: FusionSupport.Any)
                    .AssertFuseable()
                    .AssertFusionMode(FusionSupport.Sync)
                    .AssertResult(i, i + 1, i + 2, i + 3, i + 4);

                Assert.AreEqual(i, res);
            }
        }

        [Test]
        public void Eager_Error()
        {
            ObservableSource.Using(() => 0, _ => ObservableSource.Error<int>(new InvalidOperationException()), eager: true)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }
        
        [Test]
        public void Eager_OnCompleted_Cleanup()
        {
            var complete = -1;
            var to = new TestObserver<int>(requireOnSubscribe: true);

            ObservableSource.Using(() => 0, _ => ObservableSource.Empty<int>(), _ => complete = to.CompletionCount, true)
                .Subscribe(to);

            to.AssertResult();

            Assert.AreEqual(0, complete);
        }

        [Test]
        public void Eager_OnCompleted_Cleanup_SyncFused()
        {
            var complete = -1;
            var to = new TestObserver<int>(requireOnSubscribe: true, fusionRequested: FusionSupport.Any);

            ObservableSource.Using(() => 0, _ => ObservableSource.Range(1, 5), _ => complete = to.CompletionCount, true)
                .Subscribe(to);

            to.AssertResult(1, 2, 3, 4, 5);

            Assert.AreEqual(0, complete);
        }

        [Test]
        public void Eager_OnCompleted_Cleanup_AsyncFused()
        {
            var complete = -1;
            var to = new TestObserver<int>(requireOnSubscribe: true, fusionRequested: FusionSupport.Any);

            ObservableSource.Using(() => 0, _ => ObservableSource.Empty<int>(), _ => complete = to.CompletionCount, true)
                .Subscribe(to);

            to
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Async)
                .AssertResult();

            Assert.AreEqual(0, complete);
        }

        [Test]
        public void Eager_Fused_Crash_Cleanup()
        {
            var complete = -1;
            var to = new TestObserver<int>(requireOnSubscribe: true, fusionRequested: FusionSupport.Any);

            ObservableSource.Using(() => 0, 
                _ => ObservableSource.Range(1, 5)
                .Map<int, int>(w => throw new InvalidOperationException()), 
                _ => complete = to.ErrorCount, true)
                .Subscribe(to);

            to
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Sync)
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(0, complete);
        }

        [Test]
        public void Eager_OnError_Cleanup()
        {
            var complete = -1;
            var to = new TestObserver<int>(requireOnSubscribe: true);

            ObservableSource.Using(() => 0, _ => ObservableSource.Error<int>(new InvalidOperationException()), _ => complete = to.ErrorCount, true)
                .Subscribe(to);

            to.AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(0, complete);
        }

        [Test]
        public void Eager_Dispose_Cleanup()
        {
            var complete = -1;
            var subj = new PublishSubject<int>();
            var to = new TestObserver<int>(requireOnSubscribe: true);

            ObservableSource.Using(() => 0, _ => subj, _ => complete = subj.HasObservers ? 1 : 0, true)
                .Subscribe(to);

            to.Dispose();

            Assert.AreEqual(1, complete);
        }

        [Test]
        public void Eager_Supplier_Crash()
        {
            var complete = 0;

            ObservableSource.Using<int, int>(() => throw new InvalidOperationException(), v => ObservableSource.Never<int>(), _ => complete++, true)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(0, complete);
        }

        [Test]
        public void Eager_Selector_Crash()
        {
            var complete = -1;

            var to = new TestObserver<int>(requireOnSubscribe: true);

            ObservableSource.Using<int, int>(() => 1, _ => throw new InvalidOperationException(), _ => complete = to.ErrorCount, true)
                .Subscribe(to);

            to
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(0, complete);
        }

        [Test]
        public void Eager_Selector_Crash_Dispose_Crash()
        {
            var to = new TestObserver<int>(requireOnSubscribe: true);

            ObservableSource.Using<int, int>(() => 1, _ => throw new InvalidOperationException(), _ => throw new IndexOutOfRangeException(), true)
                .Subscribe(to);

            to
                .AssertSubscribed()
                .AssertValueCount(0)
                .AssertNotCompleted()
                .AssertCompositeErrorCount(2)
                .AssertCompositeError(0, typeof(InvalidOperationException))
                .AssertCompositeError(1, typeof(IndexOutOfRangeException));
        }

        [Test]
        public void Eager_Complete_Dispose_Crash()
        {
            var to = new TestObserver<int>(requireOnSubscribe: true);

            ObservableSource.Using<int, int>(() => 1, _ => ObservableSource.Empty<int>(), _ => throw new InvalidOperationException(), true)
                .Subscribe(to);

            to
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Eager_Error_Dispose_Crash()
        {
            var to = new TestObserver<int>(requireOnSubscribe: true);

            ObservableSource.Using<int, int>(() => 1, 
                _ => ObservableSource.Error<int>(new InvalidOperationException()), 
                _ => throw new IndexOutOfRangeException(), true)
                .Subscribe(to);

            to
                .AssertSubscribed()
                .AssertValueCount(0)
                .AssertNotCompleted()
                .AssertCompositeErrorCount(2)
                .AssertCompositeError(0, typeof(InvalidOperationException))
                .AssertCompositeError(1, typeof(IndexOutOfRangeException));
        }

        [Test]
        public void Eager_Dispose_Crash()
        {
            var to = new TestObserver<int>(requireOnSubscribe: true);

            ObservableSource.Using<int, int>(() => 1,
                _ => ObservableSource.Never<int>(),
                _ => throw new InvalidOperationException(), true)
                .Subscribe(to);

            to.Dispose();

            to.AssertEmpty();
        }

        [Test]
        public void Eager_Dispose()
        {
            TestHelper.VerifyDisposeObservableSource<int, int>(o => ObservableSource.Using(() => 1, _ => o, eager: true));
        }

        [Test]
        public void Lazy_Basic()
        {
            for (int i = 0; i < 10; i++)
            {
                var res = -1;

                ObservableSource.Using(() => i, j => ObservableSource.Range(j, 5), k => res = k, false)
                    .Test()
                    .AssertResult(i, i + 1, i + 2, i + 3, i + 4);

                Assert.AreEqual(i, res);
            }
        }

        [Test]
        public void Lazy_Fused()
        {
            for (int i = 0; i < 10; i++)
            {
                var res = -1;

                ObservableSource.Using(() => i, j => ObservableSource.Range(j, 5), k => res = k, false)
                    .Test(fusionMode: FusionSupport.Any)
                    .AssertFuseable()
                    .AssertFusionMode(FusionSupport.Sync)
                    .AssertResult(i, i + 1, i + 2, i + 3, i + 4);

                Assert.AreEqual(i, res);
            }
        }

        [Test]
        public void Lazy_Error()
        {
            ObservableSource.Using(() => 0, _ => ObservableSource.Error<int>(new InvalidOperationException()), eager: false)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Lazy_OnCompleted_Cleanup()
        {
            var complete = -1;
            var to = new TestObserver<int>(requireOnSubscribe: true);

            ObservableSource.Using(() => 0, _ => ObservableSource.Empty<int>(), _ => complete = to.CompletionCount, false)
                .Subscribe(to);

            to.AssertResult();

            Assert.AreEqual(1, complete);
        }

        [Test]
        public void Lazy_OnCompleted_Cleanup_SyncFused()
        {
            var complete = -1;
            var to = new TestObserver<int>(requireOnSubscribe: true, fusionRequested: FusionSupport.Any);

            ObservableSource.Using(() => 0, _ => ObservableSource.Range(1, 5), _ => complete = to.CompletionCount, false)
                .Subscribe(to);

            to.AssertResult(1, 2, 3, 4, 5);

            Assert.AreEqual(0, complete);
        }

        [Test]
        public void Lazy_OnCompleted_Cleanup_AsyncFused()
        {
            var complete = -1;
            var to = new TestObserver<int>(requireOnSubscribe: true, fusionRequested: FusionSupport.Any);

            ObservableSource.Using(() => 0, _ => ObservableSource.Empty<int>(), _ => complete = to.CompletionCount, false)
                .Subscribe(to);

            to
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Async)
                .AssertResult();

            Assert.AreEqual(1, complete);
        }

        [Test]
        public void Lazy_Fused_Crash_Cleanup()
        {
            var complete = -1;
            var to = new TestObserver<int>(requireOnSubscribe: true, fusionRequested: FusionSupport.Any);

            ObservableSource.Using(() => 0,
                _ => ObservableSource.Range(1, 5)
                .Map<int, int>(w => throw new InvalidOperationException()),
                _ => complete = to.ErrorCount, false)
                .Subscribe(to);

            to
                .AssertFuseable()
                .AssertFusionMode(FusionSupport.Sync)
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(0, complete);
        }

        [Test]
        public void Lazy_OnError_Cleanup()
        {
            var complete = -1;
            var to = new TestObserver<int>(requireOnSubscribe: true);

            ObservableSource.Using(() => 0, _ => ObservableSource.Error<int>(new InvalidOperationException()), _ => complete = to.ErrorCount, false)
                .Subscribe(to);

            to.AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(1, complete);
        }

        [Test]
        public void Lazy_Dispose_Cleanup()
        {
            var complete = -1;
            var subj = new PublishSubject<int>();
            var to = new TestObserver<int>(requireOnSubscribe: true);

            ObservableSource.Using(() => 0, _ => subj, _ => complete = subj.HasObservers ? 1 : 0, false)
                .Subscribe(to);

            to.Dispose();

            Assert.AreEqual(1, complete);
        }

        [Test]
        public void Lazy_Supplier_Crash()
        {
            var complete = 0;

            ObservableSource.Using<int, int>(() => throw new InvalidOperationException(), v => ObservableSource.Never<int>(), _ => complete++, false)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(0, complete);
        }

        [Test]
        public void Lazy_Selector_Crash()
        {
            var complete = -1;

            var to = new TestObserver<int>(requireOnSubscribe: true);

            ObservableSource.Using<int, int>(() => 1, _ => throw new InvalidOperationException(), _ => complete = to.ErrorCount, false)
                .Subscribe(to);

            to
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(1, complete);
        }

        [Test]
        public void Lazy_Selector_Crash_Dispose_Crash()
        {
            var to = new TestObserver<int>(requireOnSubscribe: true);

            ObservableSource.Using<int, int>(() => 1, _ => throw new InvalidOperationException(), _ => throw new IndexOutOfRangeException(), false)
                .Subscribe(to);

            to
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Lazy_Complete_Dispose_Crash()
        {
            var to = new TestObserver<int>(requireOnSubscribe: true);

            ObservableSource.Using<int, int>(() => 1, _ => ObservableSource.Empty<int>(), _ => throw new InvalidOperationException(), false)
                .Subscribe(to);

            to
                .AssertResult();
        }

        [Test]
        public void Lazy_Error_Dispose_Crash()
        {
            var to = new TestObserver<int>(requireOnSubscribe: true);

            ObservableSource.Using<int, int>(() => 1,
                _ => ObservableSource.Error<int>(new InvalidOperationException()),
                _ => throw new IndexOutOfRangeException(), false)
                .Subscribe(to);

            to
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Lazy_Dispose_Crash()
        {
            var to = new TestObserver<int>(requireOnSubscribe: true);

            ObservableSource.Using<int, int>(() => 1,
                _ => ObservableSource.Never<int>(),
                _ => throw new InvalidOperationException(), true)
                .Subscribe(to);

            to.Dispose();

            to.AssertEmpty();
        }

        [Test]
        public void Lazy_Dispose()
        {
            TestHelper.VerifyDisposeObservableSource<int, int>(o => ObservableSource.Using(() => 1, _ => o, eager: false));
        }
    }
}
