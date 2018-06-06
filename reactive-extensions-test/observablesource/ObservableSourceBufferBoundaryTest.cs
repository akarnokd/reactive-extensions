using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Collections.Generic;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceBufferBoundaryTest
    {
        [Test]
        public void Basic()
        {
            var source = new PublishSubject<int>();
            var boundary = new PublishSubject<string>();

            var to = source.Buffer(boundary).Test();

            to.AssertEmpty();

            Assert.True(source.HasObservers);
            Assert.True(boundary.HasObservers);

            source.OnNext(1);
            source.OnNext(2);

            to.AssertEmpty();

            boundary.OnNext("one");

            to.AssertValuesOnly(AsList(1, 2));

            source.OnNext(3);

            boundary.OnNext("two");

            to.AssertValuesOnly(AsList(1, 2), AsList(3));

            boundary.OnNext("three");

            to.AssertValuesOnly(AsList(1, 2), AsList(3));

            source.OnNext(4);
            source.OnNext(5);

            source.OnCompleted();

            to.AssertResult(AsList(1, 2), AsList(3), AsList(4, 5));

            Assert.False(source.HasObservers);
            Assert.False(boundary.HasObservers);
        }

        [Test]
        public void Basic_Boundary_Completes()
        {
            var source = new PublishSubject<int>();
            var boundary = new PublishSubject<string>();

            var to = source.Buffer(boundary).Test();

            to.AssertEmpty();

            Assert.True(source.HasObservers);
            Assert.True(boundary.HasObservers);

            source.OnNext(1);
            source.OnNext(2);

            to.AssertEmpty();

            boundary.OnNext("one");

            to.AssertValuesOnly(AsList(1, 2));

            source.OnNext(3);

            boundary.OnNext("two");

            to.AssertValuesOnly(AsList(1, 2), AsList(3));

            boundary.OnNext("three");

            to.AssertValuesOnly(AsList(1, 2), AsList(3));

            source.OnNext(4);
            source.OnNext(5);

            boundary.OnCompleted();

            to.AssertResult(AsList(1, 2), AsList(3), AsList(4, 5));

            Assert.False(source.HasObservers);
            Assert.False(boundary.HasObservers);
        }

        [Test]
        public void Dispose()
        {
            var source = new PublishSubject<int>();
            var boundary = new PublishSubject<string>();

            var to = source.Buffer(boundary).Test();

            to.AssertEmpty();

            Assert.True(source.HasObservers);
            Assert.True(boundary.HasObservers);

            to.Dispose();

            Assert.False(source.HasObservers);
            Assert.False(boundary.HasObservers);
        }

        [Test]
        public void Error_Main()
        {
            var source = new PublishSubject<int>();
            var boundary = new PublishSubject<string>();

            var to = source.Buffer(boundary).Test();

            to.AssertEmpty();

            Assert.True(source.HasObservers);
            Assert.True(boundary.HasObservers);

            source.OnError(new InvalidOperationException());

            Assert.False(source.HasObservers);
            Assert.False(boundary.HasObservers);

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Error_Boundary()
        {
            var source = new PublishSubject<int>();
            var boundary = new PublishSubject<string>();

            var to = source.Buffer(boundary).Test();

            to.AssertEmpty();

            Assert.True(source.HasObservers);
            Assert.True(boundary.HasObservers);

            boundary.OnError(new InvalidOperationException());

            Assert.False(source.HasObservers);
            Assert.False(boundary.HasObservers);

            to.AssertFailure(typeof(InvalidOperationException));
        }

        static List<T> AsList<T>(params T[] items)
        {
            return new List<T>(items);
        }

        static HashSet<T> AsSet<T>(params T[] items)
        {
            return new HashSet<T>(items);
        }
    }
}
