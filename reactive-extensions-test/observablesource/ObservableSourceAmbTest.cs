using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Collections.Generic;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceAmbTest
    {
        [Test]
        public void Array_Basic()
        {
            ObservableSource.Amb(
                ObservableSource.Range(1, 5),
                ObservableSource.Range(6, 5)
            )
            .Test()
            .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Array_None()
        {
            ObservableSource.Amb<int>()
            .Test()
            .AssertResult();
        }

        [Test]
        public void Array_Single()
        {
            ObservableSource.Amb(
                ObservableSource.Range(1, 5)
            )
            .Test()
            .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Array_Basic_2()
        {
            ObservableSource.Amb(
                ObservableSource.Never<int>(),
                ObservableSource.Range(6, 5)
            )
            .Test()
            .AssertResult(6, 7, 8, 9, 10);
        }

        [Test]
        public void Array_Error()
        {
            ObservableSource.Amb(
                ObservableSource.Error<int>(new InvalidOperationException()),
                ObservableSource.Range(6, 5)
            )
            .Test()
            .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Array_Error_2()
        {
            ObservableSource.Amb(
                ObservableSource.Never<int>(),
                ObservableSource.Error<int>(new InvalidOperationException())
            )
            .Test()
            .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Array_Error_3()
        {
            ObservableSource.Amb(
                ObservableSource.Never<int>(),
                ObservableSource.Range(1, 5).ConcatError(new InvalidOperationException())
            )
            .Test()
            .AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);
        }

        [Test]
        public void Array_Dispose()
        {
            TestHelper.VerifyDisposeObservableSource<int, int>(o => ObservableSource.Amb(o, o));
        }

        [Test]
        public void Enumerable_Basic()
        {
            ObservableSource.Amb(
                new List<IObservableSource<int>>()
                {
                    ObservableSource.Range(1, 5),
                    ObservableSource.Range(6, 5)
                }
            )
            .Test()
            .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Enumerable_None()
        {
            ObservableSource.Amb<int>(new List<IObservableSource<int>>())
            .Test()
            .AssertResult();
        }

        [Test]
        public void Enumerable_Single()
        {
            ObservableSource.Amb(
                new List<IObservableSource<int>>()
                {
                    ObservableSource.Range(1, 5)
                }
            )
            .Test()
            .AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void Enumerable_Basic_2()
        {
            ObservableSource.Amb(
                new List<IObservableSource<int>>()
                {
                    ObservableSource.Never<int>(),
                    ObservableSource.Range(6, 5)
                }
            )
            .Test()
            .AssertResult(6, 7, 8, 9, 10);
        }

        [Test]
        public void Enumerable_Error()
        {
            ObservableSource.Amb(
                new List<IObservableSource<int>>()
                {
                    ObservableSource.Error<int>(new InvalidOperationException()),
                    ObservableSource.Range(6, 5)
                }
            )
            .Test()
            .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Enumerable_Error_2()
        {
            ObservableSource.Amb(
                new List<IObservableSource<int>>()
                {
                    ObservableSource.Never<int>(),
                    ObservableSource.Error<int>(new InvalidOperationException())
                }
            )
            .Test()
            .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Enumerable_Error_3()
        {
            ObservableSource.Amb(
                new List<IObservableSource<int>>()
                {
                    ObservableSource.Never<int>(),
                    ObservableSource.Range(1, 5).ConcatError(new InvalidOperationException())
                }
            )
            .Test()
            .AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4, 5);
        }

        [Test]
        public void Enumerable_Dispose()
        {
            TestHelper.VerifyDisposeObservableSource<int, int>(o => ObservableSource.Amb(
                new List<IObservableSource<int>>()
                {
                    o, o
                }
            ));
        }
    }
}
