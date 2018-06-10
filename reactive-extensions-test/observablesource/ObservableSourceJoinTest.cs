using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceJoinTest
    {
        [Test]
        public void Basic()
        {
            var left = new PublishSubject<int>();
            var right = new PublishSubject<int>();

            var to = left.Join(right, 
                a => ObservableSource.Never<int>(), 
                b => ObservableSource.Never<int>(), 
                (a, b) => a + b)
                .Test();

            to.AssertEmpty();

            left.OnNext(1);
            right.OnNext(100);

            to.AssertValuesOnly(101);

            left.OnNext(2);

            to.AssertValuesOnly(101, 102);

            right.OnNext(200);

            to.AssertValuesOnly(101, 102, 201, 202);

            left.OnCompleted();

            to.AssertValuesOnly(101, 102, 201, 202);

            right.OnCompleted();

            to.AssertResult(101, 102, 201, 202);
        }

        [Test]
        public void Left_Error()
        {
            var left = new PublishSubject<int>();
            var right = new PublishSubject<int>();

            var to = left.Join(right,
                a => ObservableSource.Never<int>(),
                b => ObservableSource.Never<int>(),
                (a, b) => a + b)
                .Test();

            to.AssertEmpty();

            left.OnNext(1);
            right.OnNext(100);

            to.AssertValuesOnly(101);

            left.OnError(new InvalidOperationException());

            to.AssertFailure(typeof(InvalidOperationException), 101);

            Assert.False(left.HasObservers);
            Assert.False(right.HasObservers);
        }

        [Test]
        public void Right_Error()
        {
            var left = new PublishSubject<int>();
            var right = new PublishSubject<int>();

            var to = left.Join(right,
                a => ObservableSource.Never<int>(),
                b => ObservableSource.Never<int>(),
                (a, b) => a + b)
                .Test();

            to.AssertEmpty();

            left.OnNext(1);
            right.OnNext(100);

            to.AssertValuesOnly(101);

            right.OnError(new InvalidOperationException());

            to.AssertFailure(typeof(InvalidOperationException), 101);

            Assert.False(left.HasObservers);
            Assert.False(right.HasObservers);
        }

        [Test]
        public void Left_End_Error()
        {
            var left = new PublishSubject<int>();
            var right = new PublishSubject<int>();
            var leftEnd = new PublishSubject<int>();
            var rightEnd = new PublishSubject<int>();

            var to = left.Join(right,
                a => leftEnd,
                b => rightEnd,
                (a, b) => a + b)
                .Test();

            to.AssertEmpty();

            left.OnNext(1);
            right.OnNext(100);

            to.AssertValuesOnly(101);

            Assert.True(left.HasObservers);
            Assert.True(right.HasObservers);
            Assert.True(leftEnd.HasObservers);
            Assert.True(rightEnd.HasObservers);

            leftEnd.OnError(new InvalidOperationException());

            to.AssertFailure(typeof(InvalidOperationException), 101);

            Assert.False(left.HasObservers);
            Assert.False(right.HasObservers);
            Assert.False(leftEnd.HasObservers);
            Assert.False(rightEnd.HasObservers);
        }

        [Test]
        public void Right_End_Error()
        {
            var left = new PublishSubject<int>();
            var right = new PublishSubject<int>();
            var leftEnd = new PublishSubject<int>();
            var rightEnd = new PublishSubject<int>();

            var to = left.Join(right,
                a => leftEnd,
                b => rightEnd,
                (a, b) => a + b)
                .Test();

            to.AssertEmpty();

            left.OnNext(1);
            right.OnNext(100);

            to.AssertValuesOnly(101);

            Assert.True(left.HasObservers);
            Assert.True(right.HasObservers);
            Assert.True(leftEnd.HasObservers);
            Assert.True(rightEnd.HasObservers);

            rightEnd.OnError(new InvalidOperationException());

            to.AssertFailure(typeof(InvalidOperationException), 101);

            Assert.False(left.HasObservers);
            Assert.False(right.HasObservers);
            Assert.False(leftEnd.HasObservers);
            Assert.False(rightEnd.HasObservers);
        }

        [Test]
        public void Left_Close()
        {
            var left = new PublishSubject<int>();
            var right = new PublishSubject<int>();
            var leftEnd = new PublishSubject<int>();
            var rightEnd = new PublishSubject<int>();

            var to = left.Join(right,
                a => leftEnd,
                b => rightEnd,
                (a, b) => a + b)
                .Test();

            to.AssertEmpty();

            left.OnNext(1);
            right.OnNext(100);

            Assert.True(left.HasObservers);
            Assert.True(right.HasObservers);
            Assert.True(leftEnd.HasObservers);
            Assert.True(rightEnd.HasObservers);

            to.AssertValuesOnly(101);

            leftEnd.OnNext(1);

            right.OnNext(200);

            to.AssertValuesOnly(101);

            left.OnNext(2);

            to.AssertValuesOnly(101, 102, 202);

            to.Dispose();

            Assert.False(left.HasObservers);
            Assert.False(right.HasObservers);
            Assert.False(leftEnd.HasObservers);
            Assert.False(rightEnd.HasObservers);
        }

        [Test]
        public void Left_Close_Completed()
        {
            var left = new PublishSubject<int>();
            var right = new PublishSubject<int>();
            var leftEnd = new PublishSubject<int>();
            var rightEnd = new PublishSubject<int>();

            var to = left.Join(right,
                a => leftEnd,
                b => rightEnd,
                (a, b) => a + b)
                .Test();

            to.AssertEmpty();

            left.OnNext(1);
            right.OnNext(100);

            Assert.True(left.HasObservers);
            Assert.True(right.HasObservers);
            Assert.True(leftEnd.HasObservers);
            Assert.True(rightEnd.HasObservers);

            to.AssertValuesOnly(101);

            leftEnd.OnNext(1);

            right.OnNext(200);

            to.AssertValuesOnly(101);

            left.OnNext(2);

            to.AssertValuesOnly(101, 102, 202);

            leftEnd.OnCompleted();

            to.Dispose();

            Assert.False(left.HasObservers);
            Assert.False(right.HasObservers);
            Assert.False(leftEnd.HasObservers);
            Assert.False(rightEnd.HasObservers);
        }

        [Test]
        public void Right_Close()
        {
            var left = new PublishSubject<int>();
            var right = new PublishSubject<int>();
            var leftEnd = new PublishSubject<int>();
            var rightEnd = new PublishSubject<int>();

            var to = left.Join(right,
                a => leftEnd,
                b => rightEnd,
                (a, b) => a + b)
                .Test();

            to.AssertEmpty();

            left.OnNext(1);
            right.OnNext(100);

            Assert.True(left.HasObservers);
            Assert.True(right.HasObservers);
            Assert.True(leftEnd.HasObservers);
            Assert.True(rightEnd.HasObservers);

            to.AssertValuesOnly(101);

            rightEnd.OnNext(1);

            left.OnNext(2);

            to.AssertValuesOnly(101);

            right.OnNext(200);

            to.AssertValuesOnly(101, 201, 202);

            to.Dispose();

            Assert.False(left.HasObservers);
            Assert.False(right.HasObservers);
            Assert.False(leftEnd.HasObservers);
            Assert.False(rightEnd.HasObservers);
        }

        [Test]
        public void Right_Close_Completed()
        {
            var left = new PublishSubject<int>();
            var right = new PublishSubject<int>();
            var leftEnd = new PublishSubject<int>();
            var rightEnd = new PublishSubject<int>();

            var to = left.Join(right,
                a => leftEnd,
                b => rightEnd,
                (a, b) => a + b)
                .Test();

            to.AssertEmpty();

            left.OnNext(1);
            right.OnNext(100);

            Assert.True(left.HasObservers);
            Assert.True(right.HasObservers);
            Assert.True(leftEnd.HasObservers);
            Assert.True(rightEnd.HasObservers);

            to.AssertValuesOnly(101);

            rightEnd.OnNext(1);

            left.OnNext(2);

            to.AssertValuesOnly(101);

            right.OnNext(200);

            to.AssertValuesOnly(101, 201, 202);

            rightEnd.OnCompleted();

            to.Dispose();

            Assert.False(left.HasObservers);
            Assert.False(right.HasObservers);
            Assert.False(leftEnd.HasObservers);
            Assert.False(rightEnd.HasObservers);
        }

        [Test]
        public void Left_Selector_Crash()
        {
            var left = new PublishSubject<int>();
            var right = new PublishSubject<int>();
            var leftEnd = new PublishSubject<int>();
            var rightEnd = new PublishSubject<int>();

            var to = left.Join<int, int, int, int, int>(right,
                a => throw new InvalidOperationException(),
                b => rightEnd,
                (a, b) => a + b)
                .Test();

            to.AssertEmpty();

            right.OnNext(100);
            left.OnNext(1);

            to.AssertFailure(typeof(InvalidOperationException));

            Assert.False(left.HasObservers);
            Assert.False(right.HasObservers);
            Assert.False(leftEnd.HasObservers);
            Assert.False(rightEnd.HasObservers);
        }

        [Test]
        public void Right_Selector_Crash()
        {
            var left = new PublishSubject<int>();
            var right = new PublishSubject<int>();
            var leftEnd = new PublishSubject<int>();
            var rightEnd = new PublishSubject<int>();

            var to = left.Join<int, int, int, int, int>(right,
                a => leftEnd,
                b => throw new InvalidOperationException(),
                (a, b) => a + b)
                .Test();

            to.AssertEmpty();

            left.OnNext(1);
            right.OnNext(100);

            to.AssertFailure(typeof(InvalidOperationException));

            Assert.False(left.HasObservers);
            Assert.False(right.HasObservers);
            Assert.False(leftEnd.HasObservers);
            Assert.False(rightEnd.HasObservers);
        }

        [Test]
        public void Result_Selector_Crash()
        {
            var left = new PublishSubject<int>();
            var right = new PublishSubject<int>();
            var leftEnd = new PublishSubject<int>();
            var rightEnd = new PublishSubject<int>();

            var to = left.Join<int, int, int, int, int>(right,
                a => leftEnd,
                b => rightEnd,
                (a, b) => throw new InvalidOperationException())
                .Test();

            to.AssertEmpty();

            left.OnNext(1);
            right.OnNext(100);

            to.AssertFailure(typeof(InvalidOperationException));

            Assert.False(left.HasObservers);
            Assert.False(right.HasObservers);
            Assert.False(leftEnd.HasObservers);
            Assert.False(rightEnd.HasObservers);
        }
    }
}
