using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Collections.Generic;
using System.Collections;

namespace akarnokd.reactive_extensions_test.completable
{
    [TestFixture]
    public class CompletableConcatTest
    {
        [Test]
        public void Basic()
        {
            var count = 0;

            CompletableSource.Concat(
                CompletableSource.FromAction(() => count++),
                CompletableSource.FromAction(() => count++)
            )
            .Test()
            .AssertResult();

            Assert.AreEqual(2, count);
        }

        [Test]
        public void Basic_Delay_Errors()
        {
            var count = 0;

            CompletableSource.Concat(true,
                CompletableSource.FromAction(() => count++),
                CompletableSource.FromAction(() => count++)
            )
            .Test()
            .AssertResult();

            Assert.AreEqual(2, count);
        }

        [Test]
        public void Basic_Array()
        {
            var count = 0;

            new []
            {
                CompletableSource.FromAction(() => count++),
                CompletableSource.FromAction(() => count++)
            }.ConcatAll()
            .Test()
            .AssertResult();

            Assert.AreEqual(2, count);
        }

        [Test]
        public void Basic_Array_Delay_Errors()
        {
            var count = 0;

            new[]
            {
                CompletableSource.FromAction(() => count++),
                CompletableSource.FromAction(() => count++)
            }.ConcatAll(true)
            .Test()
            .AssertResult();

            Assert.AreEqual(2, count);
        }

        [Test]
        public void Basic_Enumerable()
        {
            var count = 0;

            CompletableSource.Concat(new List<ICompletableSource>() {
                CompletableSource.FromAction(() => count++),
                CompletableSource.FromAction(() => count++)
            }
            )
            .Test()
            .AssertResult();

            Assert.AreEqual(2, count);
        }

        [Test]
        public void Basic_Enumerable_Delay_Errors()
        {
            var count = 0;

            CompletableSource.Concat(new List<ICompletableSource>() {
                CompletableSource.FromAction(() => count++),
                CompletableSource.FromAction(() => count++)
            }, true
            )
            .Test()
            .AssertResult();

            Assert.AreEqual(2, count);
        }

        [Test]
        public void Basic_Error()
        {
            var count = 0;

            CompletableSource.Concat(
                CompletableSource.Error(new InvalidOperationException()),
                CompletableSource.FromAction(() => count++)
            )
            .Test()
            .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Basic_Enumerable_Error()
        {
            var count = 0;

            CompletableSource.Concat(new List<ICompletableSource>() {
                CompletableSource.Error(new InvalidOperationException()),
                CompletableSource.FromAction(() => count++)
            })
            .Test()
            .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Basic_Error_Delay()
        {
            var count = 0;

            CompletableSource.Concat(true,
                CompletableSource.Error(new InvalidOperationException()),
                CompletableSource.FromAction(() => count++)
            )
            .Test()
            .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Basic_Enumerable_Error_Delay()
        {
            var count = 0;

            CompletableSource.Concat(new List<ICompletableSource>() {
                CompletableSource.Error(new InvalidOperationException()),
                CompletableSource.FromAction(() => count++)
            }, true)
            .Test()
            .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Enumerable_Crashes()
        {
            CompletableSource.Concat(new FailingEnumerable<ICompletableSource>())
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        sealed class FailingEnumerable<T> : IEnumerable<T>, IEnumerator<T>
        {
            public T Current => throw new NotImplementedException();

            object IEnumerator.Current => throw new NotImplementedException();

            public void Dispose()
            {
                // no-op
            }

            public IEnumerator<T> GetEnumerator()
            {
                return this;
            }

            public bool MoveNext()
            {
                throw new InvalidOperationException();
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
