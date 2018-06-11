using NUnit.Framework;
using System.Reactive.Linq;
using akarnokd.reactive_extensions;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Collections;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceConcatMapEnumerableTest
    {
        [Test]
        public void Basic()
        {
            ObservableSource.Range(1, 5)
                 .ConcatMap(v => Enumerable.Range(v * 100, 5))
                 .Test()
                 .AssertResult(
                    100, 101, 102, 103, 104,
                    200, 201, 202, 203, 204,
                    300, 301, 302, 303, 304,
                    400, 401, 402, 403, 404,
                    500, 501, 502, 503, 504
                );
        }

        [Test]
        public void Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .ConcatMap(v => Enumerable.Range(v * 100, 5))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Mapper_Crash()
        {
            ObservableSource.Range(1, 5)
                 .ConcatMap<int, int>(v => { throw new InvalidOperationException(); })
                 .Test()
                 .AssertFailure(typeof(InvalidOperationException));
        }

        internal sealed class TestObserverLocal : TestObserver<int>
        {
            public TestObserverLocal() : base(true)
            {
            }

            public override void OnNext(int value)
            {
                base.OnNext(value);
                if (value == 201)
                {
                    Dispose();
                    OnCompleted();
                }
            }
        }

        sealed class OnDispose<T> : IEnumerable<T>, IEnumerator<T>
        {
            readonly IEnumerable<T> source;

            readonly bool[] disposed;

            public T Current => enumerator.Current;

            IEnumerator<T> enumerator;

            object IEnumerator.Current => enumerator.Current;

            internal OnDispose(IEnumerable<T> source, bool[] disposed)
            {
                this.source = source;
                this.disposed = disposed;
            }

            public void Dispose()
            {
                disposed[0] = true;
            }

            public IEnumerator<T> GetEnumerator()
            {
                enumerator = source.GetEnumerator();
                return this;
            }

            public bool MoveNext()
            {
                return enumerator.MoveNext();
            }

            public void Reset()
            {
                enumerator = source.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                enumerator = source.GetEnumerator();
                return this;
            }
        }

        [Test]
        public void Disposed()
        {
            var us = new MonocastSubject<int>();

            var to = new TestObserverLocal();

            bool[] disposed = { false };

            var d = us.ConcatMap(v => new OnDispose<int>(Enumerable.Range(v * 100, 5), disposed))
                .SubscribeWith(to);

            Assert.True(us.HasObserver());

            to.AssertEmpty();

            us.OnNext(1);

            to.AssertValuesOnly(100, 101, 102, 103, 104);

            Assert.True(us.HasObserver());

            us.OnNext(2);

            Assert.False(us.HasObserver());

            Assert.True(disposed[0]);

            to.AssertResult(100, 101, 102, 103, 104, 200, 201);
        }

        [Test]
        public void Dispose_On_MoveNext()
        {
            var to = new TestObserver<int>();

            ObservableSource.Just(1).Hide()
                .ConcatMap(v => moveNextDispose(to))
                .Subscribe(to);

            to.AssertValuesOnly(1)
                .AssertNoError()
                .AssertNotCompleted();
        }

        [Test]
        public void GetEnumerator_Crash()
        {
            ObservableSource.Just(1).Hide()
                .ConcatMap(v => new FailingEnumerable<int>(true, false, false))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void MoveNext_Crash()
        {
            ObservableSource.Just(1).Hide()
                .ConcatMap(v => new FailingEnumerable<int>(false, true, false))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }


        IEnumerable<int> moveNextDispose(TestObserver<int> to)
        {
            yield return 1;

            to.Dispose();

            yield return 2;
        }
    }
}
