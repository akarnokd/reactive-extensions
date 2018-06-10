using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Collections.Generic;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceWindowTest
    {
        [Test]
        public void Exact_Basic_1()
        {
            ObservableSource.Range(1, 5)
                .Window(1)
                .FlatMap(v => v.ToList())
                .Test()
                .AssertResult(
                    new List<int>() { 1 },
                    new List<int>() { 2 },
                    new List<int>() { 3 },
                    new List<int>() { 4 },
                    new List<int>() { 5 }
                );
        }

        [Test]
        public void Exact_Empty()
        {
            ObservableSource.Empty<int>()
                .Window(1)
                .FlatMap(v => v.ToList())
                .Test()
                .AssertResult();
        }
        [Test]
        public void Exact_Basic_2()
        {
            ObservableSource.Range(1, 10)
                .Window(2)
                .FlatMap(v => v.ToList())
                .Test()
                .AssertResult(
                    new List<int>() { 1, 2 },
                    new List<int>() { 3, 4 },
                    new List<int>() { 5, 6 },
                    new List<int>() { 7, 8 },
                    new List<int>() { 9, 10 }
                );
        }

        [Test]
        public void Exact_Basic_3()
        {
            ObservableSource.Range(1, 10)
                .Window(3)
                .FlatMap(v => v.ToList())
                .Test()
                .AssertResult(
                    new List<int>() { 1, 2, 3 },
                    new List<int>() { 4, 5, 6 },
                    new List<int>() { 7, 8, 9 },
                    new List<int>() { 10 }
                );
        }

        [Test]
        public void Exact_Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .Window(3)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Exact_Error_Window()
        {
            ObservableSource.Just(1).ConcatError<int>(new InvalidOperationException())
                .Window(3)
                .FlatMap(w => w, true)
                .Test()
                .AssertValueCount(1)
                .AssertNotCompleted()
                .AssertError(typeof(AggregateException))
                .AssertCompositeErrorCount(2)
                .AssertCompositeError(0, typeof(InvalidOperationException))
                .AssertCompositeError(1, typeof(InvalidOperationException));
        }

        [Test]
        public void Exact_Take_1()
        {
            var disposed = 0;

            ObservableSource.Range(1, 10)
                .DoOnDispose(() => disposed++)
                .Window(5)
                .Take(1)
                .FlatMap(v => v.ToList())
                .Test()
                .AssertResult(
                    new List<int>() { 1, 2, 3, 4, 5 }
                );

            Assert.AreEqual(1, disposed);
        }

        [Test]
        public void Skip_Basic_1()
        {
            ObservableSource.Range(1, 5)
                .Window(1, 2)
                .FlatMap(v => v.ToList())
                .Test()
                .AssertResult(
                    new List<int>() { 1 },
                    new List<int>() { 3 },
                    new List<int>() { 5 }
                );
        }

        [Test]
        public void Skip_Empty()
        {
            ObservableSource.Empty<int>()
                .Window(1, 2)
                .FlatMap(v => v.ToList())
                .Test()
                .AssertResult();
        }
        [Test]
        public void Skip_Basic_2()
        {
            ObservableSource.Range(1, 10)
                .Window(2, 3)
                .FlatMap(v => v.ToList())
                .Test()
                .AssertResult(
                    new List<int>() { 1, 2 },
                    new List<int>() { 4, 5 },
                    new List<int>() { 7, 8 },
                    new List<int>() { 10 }
                );
        }

        [Test]
        public void Skip_Basic_3()
        {
            ObservableSource.Range(1, 10)
                .Window(3, 4)
                .FlatMap(v => v.ToList())
                .Test()
                .AssertResult(
                    new List<int>() { 1, 2, 3 },
                    new List<int>() { 5, 6, 7 },
                    new List<int>() { 9, 10 }
                );
        }

        [Test]
        public void Skip_Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .Window(3, 4)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Skip_Error_Window()
        {
            ObservableSource.Just(1).ConcatError<int>(new InvalidOperationException())
                .Window(3, 4)
                .FlatMap(w => w, true)
                .Test()
                .AssertValueCount(1)
                .AssertNotCompleted()
                .AssertError(typeof(AggregateException))
                .AssertCompositeErrorCount(2)
                .AssertCompositeError(0, typeof(InvalidOperationException))
                .AssertCompositeError(1, typeof(InvalidOperationException));
        }

        [Test]
        public void Skip_Take_1()
        {
            var disposed = 0;

            ObservableSource.Range(1, 10)
                .DoOnDispose(() => disposed++)
                .Window(5, 6)
                .Take(1)
                .FlatMap(v => v.ToList())
                .Test()
                .AssertResult(
                    new List<int>() { 1, 2, 3, 4, 5 }
                );

            Assert.AreEqual(1, disposed);
        }

        [Test]
        public void Overlap_Basic_2()
        {
            ObservableSource.Range(1, 5)
                .Window(2, 1)
                .FlatMap(v => v.ToList())
                .Test()
                .AssertResult(
                    new List<int>() { 1, 2 },
                    new List<int>() { 2, 3 },
                    new List<int>() { 3, 4 },
                    new List<int>() { 4, 5 },
                    new List<int>() { 5 }
                );
        }

        [Test]
        public void Overlap_Empty()
        {
            ObservableSource.Empty<int>()
                .Window(2, 1)
                .FlatMap(v => v.ToList())
                .Test()
                .AssertResult();
        }

        [Test]
        public void Overlap_Basic_3_1()
        {
            ObservableSource.Range(1, 10)
                .Window(3, 1)
                .FlatMap(v => v.ToList())
                .Test()
                .AssertResult(
                    new List<int>() { 1, 2, 3 },
                    new List<int>() { 2, 3, 4 },
                    new List<int>() { 3, 4, 5 },
                    new List<int>() { 4, 5, 6 },
                    new List<int>() { 5, 6, 7 },
                    new List<int>() { 6, 7, 8 },
                    new List<int>() { 7, 8, 9 },
                    new List<int>() { 8, 9, 10 },
                    new List<int>() { 9, 10 },
                    new List<int>() { 10 }
                );
        }

        [Test]
        public void Overlap_Basic_3_2()
        {
            ObservableSource.Range(1, 10)
                .Window(3, 2)
                .FlatMap(v => v.ToList())
                .Test()
                .AssertResult(
                    new List<int>() { 1, 2, 3 },
                    new List<int>() { 3, 4, 5 },
                    new List<int>() { 5, 6, 7 },
                    new List<int>() { 7, 8, 9 },
                    new List<int>() { 9, 10 }
                );
        }

        [Test]
        public void Overlap_Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .Window(3, 1)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Overlap_Error_Window()
        {
            ObservableSource.Just(1).ConcatError<int>(new InvalidOperationException())
                .Window(3, 1)
                .FlatMap(w => w, true)
                .Test()
                .AssertValueCount(1)
                .AssertNotCompleted()
                .AssertError(typeof(AggregateException))
                .AssertCompositeErrorCount(2)
                .AssertCompositeError(0, typeof(InvalidOperationException))
                .AssertCompositeError(1, typeof(InvalidOperationException));
        }

        [Test]
        public void Overlap_Take_1()
        {
            var disposed = 0;

            ObservableSource.Range(1, 10)
                .DoOnDispose(() => disposed++)
                .Window(5, 1)
                .Take(1)
                .FlatMap(v => v.ToList())
                .Test()
                .AssertResult(
                    new List<int>() { 1, 2, 3, 4, 5 }
                );

            Assert.AreEqual(1, disposed);
        }
    }
}
