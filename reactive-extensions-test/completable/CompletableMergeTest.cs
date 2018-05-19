using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Collections.Generic;
using System.Collections;

namespace akarnokd.reactive_extensions_test.completable
{
    [TestFixture]
    public class CompletableMergeTest
    {
        #region Array-based

        [Test]
        public void Array_Basic()
        {
            var count = 0;
            var source = CompletableSource.FromAction(() => count++);

            new[] { source, source, source }
            .MergeAll()
            .Test()
            .AssertResult();

            Assert.AreEqual(3, count);
        }

        [Test]
        public void Array_Basic_Params()
        {
            var count = 0;
            var source = CompletableSource.FromAction(() => count++);

            CompletableSource.Merge(source, source, source)
            .Test()
            .AssertResult();

            Assert.AreEqual(3, count);
        }

        [Test]
        public void Array_Basic_Delay_Errors()
        {
            var count = 0;
            var source = CompletableSource.FromAction(() => count++);

            CompletableSource.Merge(true, new[] { source, source, source })
            .Test()
            .AssertResult();

            Assert.AreEqual(3, count);
        }

        [Test]
        public void Array_Basic_Max_Concurrency()
        {

            for (int i = 1; i < 5; i++)
            {
                var count = 0;
                var source = CompletableSource.FromAction(() => count++);

                CompletableSource.Merge(i, new[] { source, source, source })
                .Test()
                .WithTag("maxConcurrency=" + i)
                .AssertResult();

                Assert.AreEqual(3, count);
            }
        }

        [Test]
        public void Array_Basic_Max_Concurrency_Delay_Errors()
        {
            for (int i = 1; i < 5; i++)
            {
                var count = 0;
                var source = CompletableSource.FromAction(() => count++);

                CompletableSource.Merge(true, i, new[] { source, source, source })
                .Test()
                .WithTag("maxConcurrency=" + i)
                .AssertResult();

                Assert.AreEqual(3, count);
            }
        }

        [Test]
        public void Array_Error()
        {
            var count = 0;
            var source = CompletableSource.FromAction(() => count++);
            var err = CompletableSource.Error(new InvalidOperationException());

            CompletableSource.Merge(source, err, source)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Array_Error_Max_Concurrency()
        {

            for (int i = 1; i < 5; i++)
            {
                var count = 0;
                var source = CompletableSource.FromAction(() => count++);
                var err = CompletableSource.Error(new InvalidOperationException());

                CompletableSource.Merge(i, source, err, source)
                    .Test()
                    .AssertFailure(typeof(InvalidOperationException));

                Assert.AreEqual(1, count);
            }
        }

        [Test]
        public void Array_Error_Delayed()
        {
            var count = 0;
            var source = CompletableSource.FromAction(() => count++);
            var err = CompletableSource.Error(new InvalidOperationException());

            CompletableSource.Merge(true, source, err, source)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(2, count);
        }

        [Test]
        public void Array_Error_Delayed_Max_Concurrency()
        {

            for (int i = 1; i < 5; i++)
            {
                var count = 0;
                var source = CompletableSource.FromAction(() => count++);
                var err = CompletableSource.Error(new InvalidOperationException());

                CompletableSource.Merge(true, i, source, err, source)
                    .Test()
                    .AssertFailure(typeof(InvalidOperationException));

                Assert.AreEqual(2, count);
            }
        }

        [Test]
        public void Array_Dispose()
        {
            var cs1 = new CompletableSubject();
            var cs2 = new CompletableSubject();

            var to = CompletableSource.Merge(cs1, cs2)
                .Test();

            Assert.True(cs1.HasObserver());
            Assert.True(cs2.HasObserver());

            to.Dispose();

            Assert.False(cs1.HasObserver());
            Assert.False(cs2.HasObserver());
        }

        [Test]
        public void Array_Dispose_Max_Concurrency()
        {
            var cs1 = new CompletableSubject();
            var cs2 = new CompletableSubject();

            var to = CompletableSource.Merge(2, cs1, cs2)
                .Test();

            Assert.True(cs1.HasObserver());
            Assert.True(cs2.HasObserver());

            to.Dispose();

            Assert.False(cs1.HasObserver());
            Assert.False(cs2.HasObserver());
        }

        [Test]
        public void Array_Max_Concurrency()
        {
            var cs1 = new CompletableSubject();
            var cs2 = new CompletableSubject();

            var to = CompletableSource.Merge(1, cs1, cs2)
                .Test();

            Assert.True(cs1.HasObserver());
            Assert.False(cs2.HasObserver());

            cs1.OnCompleted();

            Assert.False(cs1.HasObserver());
            Assert.True(cs2.HasObserver());

            cs2.OnCompleted();

            to.AssertResult();
        }

        [Test]
        public void Array_Error_Disposes_Other()
        {
            var cs1 = new CompletableSubject();
            var cs2 = new CompletableSubject();

            var to = CompletableSource.Merge(cs1, cs2)
                .Test();

            Assert.True(cs1.HasObserver());
            Assert.True(cs2.HasObserver());

            cs1.OnError(new InvalidOperationException());

            Assert.False(cs1.HasObserver());
            Assert.False(cs2.HasObserver());

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Array_Error_Disposes_Other_Max_Concurrency()
        {
            var cs1 = new CompletableSubject();
            var cs2 = new CompletableSubject();

            var to = CompletableSource.Merge(2, cs1, cs2)
                .Test();

            Assert.True(cs1.HasObserver());
            Assert.True(cs2.HasObserver());

            cs1.OnError(new InvalidOperationException());

            Assert.False(cs1.HasObserver());
            Assert.False(cs2.HasObserver());

            to.AssertFailure(typeof(InvalidOperationException));
        }


        [Test]
        public void Array_Race()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var cs1 = new CompletableSubject();
                var cs2 = new CompletableSubject();

                var to = CompletableSource.Merge(new [] { cs1, cs2 })
                    .Test();

                TestHelper.Race(() => {
                    cs1.OnCompleted();
                }, () => {
                    cs2.OnCompleted();
                });

                to.AssertResult();
            }
        }

        [Test]
        public void Array_Race_MaxConcurrent()
        {
            for (int k = 1; k < 4; k++)
            {
                for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
                {
                    var cs1 = new CompletableSubject();
                    var cs2 = new CompletableSubject();

                    var to = CompletableSource.Merge(new [] { cs1, cs2 }, maxConcurrency: k)
                        .Test();

                    TestHelper.Race(() =>
                    {
                        cs1.OnCompleted();
                    }, () =>
                    {
                        cs2.OnCompleted();
                    });

                    to.AssertResult();
                }
            }
        }


        #endregion Array-based

        #region Enumerable-based

        [Test]
        public void Enumerable_Basic()
        {
            var count = 0;
            var source = CompletableSource.FromAction(() => count++);

            new List<ICompletableSource>() { source, source, source }
            .Merge()
            .Test()
            .AssertResult();

            Assert.AreEqual(3, count);
        }

        [Test]
        public void Enumerable_Basic_Delay_Errors()
        {
            var count = 0;
            var source = CompletableSource.FromAction(() => count++);

            CompletableSource.Merge(
                new List<ICompletableSource>() { source, source, source }
                , true
            )
            .Test()
            .AssertResult();

            Assert.AreEqual(3, count);
        }

        [Test]
        public void Enumerable_Basic_Max_Concurrency()
        {

            for (int i = 1; i < 5; i++)
            {
                var count = 0;
                var source = CompletableSource.FromAction(() => count++);

                CompletableSource.Merge(
                    new List<ICompletableSource>() { source, source, source },
                    maxConcurrency: i
                )
                .Test()
                .WithTag("maxConcurrency=" + i)
                .AssertResult();

                Assert.AreEqual(3, count);
            }
        }

        [Test]
        public void Enumerable_Basic_Max_Concurrency_Delay_Errors()
        {
            for (int i = 1; i < 5; i++)
            {
                var count = 0;
                var source = CompletableSource.FromAction(() => count++);

                CompletableSource.Merge(
                    new List<ICompletableSource>() { source, source, source }
                    , true, i
                )
                .Test()
                .WithTag("maxConcurrency=" + i)
                .AssertResult();

                Assert.AreEqual(3, count);
            }
        }

        [Test]
        public void Enumerable_Error()
        {
            var count = 0;
            var source = CompletableSource.FromAction(() => count++);
            var err = CompletableSource.Error(new InvalidOperationException());

            CompletableSource.Merge(new List<ICompletableSource>() { source, err, source })
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Enumerable_Error_Max_Concurrency()
        {

            for (int i = 1; i < 5; i++)
            {
                var count = 0;
                var source = CompletableSource.FromAction(() => count++);
                var err = CompletableSource.Error(new InvalidOperationException());

                CompletableSource.Merge(new List<ICompletableSource>() { source, err, source }, maxConcurrency: i)
                    .Test()
                    .AssertFailure(typeof(InvalidOperationException));

                Assert.AreEqual(1, count);
            }
        }

        [Test]
        public void Enumerable_Error_Delayed()
        {
            var count = 0;
            var source = CompletableSource.FromAction(() => count++);
            var err = CompletableSource.Error(new InvalidOperationException());

            CompletableSource.Merge(new List<ICompletableSource>() { source, err, source }, true)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(2, count);
        }

        [Test]
        public void Enumerable_Error_Delayed_Max_Concurrency()
        {

            for (int i = 1; i < 5; i++)
            {
                var count = 0;
                var source = CompletableSource.FromAction(() => count++);
                var err = CompletableSource.Error(new InvalidOperationException());

                CompletableSource.Merge(new List<ICompletableSource>() { source, err, source }, true, i)
                    .Test()
                    .AssertFailure(typeof(InvalidOperationException));

                Assert.AreEqual(2, count);
            }
        }

        [Test]
        public void Enumerable_Dispose()
        {
            var cs1 = new CompletableSubject();
            var cs2 = new CompletableSubject();

            var to = CompletableSource.Merge(new List<ICompletableSource>() { cs1, cs2 })
                .Test();

            Assert.True(cs1.HasObserver());
            Assert.True(cs2.HasObserver());

            to.Dispose();

            Assert.False(cs1.HasObserver());
            Assert.False(cs2.HasObserver());
        }

        [Test]
        public void Enumerable_Dispose_Max_Concurrency()
        {
            var cs1 = new CompletableSubject();
            var cs2 = new CompletableSubject();

            var to = CompletableSource.Merge(new List<ICompletableSource>() { cs1, cs2 }, maxConcurrency: 2)
                .Test();

            Assert.True(cs1.HasObserver());
            Assert.True(cs2.HasObserver());

            to.Dispose();

            Assert.False(cs1.HasObserver());
            Assert.False(cs2.HasObserver());
        }

        [Test]
        public void Enumerable_Max_Concurrency()
        {
            var cs1 = new CompletableSubject();
            var cs2 = new CompletableSubject();

            var to = CompletableSource.Merge(new List<ICompletableSource>() { cs1, cs2 }, maxConcurrency: 1)
                .Test();

            Assert.True(cs1.HasObserver());
            Assert.False(cs2.HasObserver());

            cs1.OnCompleted();

            Assert.False(cs1.HasObserver());
            Assert.True(cs2.HasObserver());

            cs2.OnCompleted();

            to.AssertResult();
        }

        [Test]
        public void Enumerable_Error_Disposes_Other()
        {
            var cs1 = new CompletableSubject();
            var cs2 = new CompletableSubject();

            var to = CompletableSource.Merge(new List<ICompletableSource>() { cs1, cs2 })
                .Test();

            Assert.True(cs1.HasObserver());
            Assert.True(cs2.HasObserver());

            cs1.OnError(new InvalidOperationException());

            Assert.False(cs1.HasObserver());
            Assert.False(cs2.HasObserver());

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Enumerable_Error_Disposes_Other_Max_Concurrency()
        {
            var cs1 = new CompletableSubject();
            var cs2 = new CompletableSubject();

            var to = CompletableSource.Merge(new List<ICompletableSource>() { cs1, cs2 }, maxConcurrency: 2)
                .Test();

            Assert.True(cs1.HasObserver());
            Assert.True(cs2.HasObserver());

            cs1.OnError(new InvalidOperationException());

            Assert.False(cs1.HasObserver());
            Assert.False(cs2.HasObserver());

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Enumerable_Race()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                var cs1 = new CompletableSubject();
                var cs2 = new CompletableSubject();

                var to = CompletableSource.Merge(new List<ICompletableSource>() { cs1, cs2 })
                    .Test();

                TestHelper.Race(() => {
                    cs1.OnCompleted();
                }, () => {
                    cs2.OnCompleted();
                });

                to.AssertResult();
            }
        }

        [Test]
        public void Enumerable_Race_MaxConcurrent()
        {
            for (int k = 1; k < 4; k++)
            {
                for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
                {
                    var cs1 = new CompletableSubject();
                    var cs2 = new CompletableSubject();

                    var to = CompletableSource.Merge(new List<ICompletableSource>() { cs1, cs2 }, maxConcurrency: k)
                        .Test();

                    TestHelper.Race(() =>
                    {
                        cs1.OnCompleted();
                    }, () =>
                    {
                        cs2.OnCompleted();
                    });

                    to.AssertResult();
                }
            }
        }

        [Test]
        public void Enumerable_GetEnumerable_Crash()
        {
            CompletableSource.Merge(
                new FailingEnumerable<ICompletableSource>(true, false, false)
            )
            .Test()
            .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Enumerable_GetEnumerable_Crash_Max_Concurrency()
        {
            CompletableSource.Merge(
                new FailingEnumerable<ICompletableSource>(true, false, false),
                maxConcurrency: 2
            )
            .Test()
            .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Enumerable_MoveNext_Crash()
        {
            CompletableSource.Merge(
                new FailingEnumerable<ICompletableSource>(false, true, false)
            )
            .Test()
            .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Enumerable_MoveNext_Crash_Max_Concurrency()
        {
            CompletableSource.Merge(
                new FailingEnumerable<ICompletableSource>(false, true, false),
                maxConcurrency: 2
            )
            .Test()
            .AssertFailure(typeof(InvalidOperationException));
        }

        sealed class FailingEnumerable<T> : IEnumerable<T>, IEnumerator<T>
        {
            readonly bool failGetEnumerable;

            readonly bool failMoveNext;

            readonly bool failOnDispose;

            public FailingEnumerable(bool failGetEnumerable, bool failMoveNext, bool failOnDispose)
            {
                this.failGetEnumerable = failGetEnumerable;
                this.failMoveNext = failMoveNext;
                this.failOnDispose = failOnDispose;
            }

            public T Current => default(T);

            object IEnumerator.Current => null;

            public void Dispose()
            {
                if (failOnDispose)
                {
                    throw new InvalidOperationException();
                }
            }

            public IEnumerator<T> GetEnumerator()
            {
                if (failGetEnumerable)
                {
                    throw new InvalidOperationException();
                }
                return this;
            }

            public bool MoveNext()
            {
                if (failMoveNext)
                {
                    throw new InvalidOperationException();
                }
                return false;
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        #endregion Enumerable-based
    }
}
