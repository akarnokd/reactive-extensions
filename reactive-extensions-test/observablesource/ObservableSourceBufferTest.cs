using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Collections.Generic;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceBufferTest
    {

        #region + Count-Exact +

        [Test]
        public void Exact_Basic()
        {
            ObservableSource.Range(1, 5)
                .Buffer(1)
                .Test()
                .AssertResult(AsList(1), AsList(2), AsList(3), AsList(4), AsList(5));
        }

        [Test]
        public void Exact_Basic_2()
        {
            ObservableSource.Range(1, 5)
                .Buffer(2)
                .Test()
                .AssertResult(AsList(1, 2), AsList(3, 4), AsList(5));
        }

        [Test]
        public void Exact_Basic_All()
        {
            ObservableSource.Range(1, 5)
                .Buffer(5)
                .Test()
                .AssertResult(AsList(1, 2, 3, 4, 5));
        }

        [Test]
        public void Exact_Empty()
        {
            ObservableSource.Empty<int>()
                .Buffer(1)
                .Test()
                .AssertResult();
        }

        [Test]
        public void Exact_Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .Buffer(1)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Exact_Dispose()
        {
            TestHelper.VerifyDisposeObservableSource<int, IList<int>>(o => o.Buffer(1));
        }


        [Test]
        public void Exact_Custom_Collection()
        {
            ObservableSource.Range(1, 5)
                .Buffer(1, 1, () => new HashSet<int>())
                .Test()
                .AssertResult(AsSet(1), AsSet(2), AsSet(3), AsSet(4), AsSet(5));
        }

        #endregion
        #region Count-Skip

        [Test]
        public void Skip_Basic()
        {
            ObservableSource.Range(1, 5)
                .Buffer(1, 2)
                .Test()
                .AssertResult(AsList(1), AsList(3), AsList(5));
        }

        [Test]
        public void Skip_Basic_2()
        {
            ObservableSource.Range(1, 5)
                .Buffer(2, 3)
                .Test()
                .AssertResult(AsList(1, 2), AsList(4, 5));
        }

        [Test]
        public void Skip_Basic_4()
        {
            ObservableSource.Range(1, 5)
                .Buffer(2, 4)
                .Test()
                .AssertResult(AsList(1, 2), AsList(5));
        }

        [Test]
        public void Skip_Basic_All()
        {
            ObservableSource.Range(1, 5)
                .Buffer(5, 6)
                .Test()
                .AssertResult(AsList(1, 2, 3, 4, 5));
        }

        [Test]
        public void Skip_Empty()
        {
            ObservableSource.Empty<int>()
                .Buffer(1, 2)
                .Test()
                .AssertResult();
        }

        [Test]
        public void Skip_Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .Buffer(1, 2)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Skip_Dispose()
        {
            TestHelper.VerifyDisposeObservableSource<int, IList<int>>(o => o.Buffer(1, 2));
        }

        [Test]
        public void Skip_Custom_Collection()
        {
            ObservableSource.Range(1, 5)
                .Buffer(1, 2, () => new HashSet<int>())
                .Test()
                .AssertResult(AsSet(1), AsSet(3), AsSet(5));
        }

        #endregion

        #region + Count-Overlap +

        [Test]
        public void Overlap_Basic()
        {
            ObservableSource.Range(1, 5)
                .Buffer(2, 1)
                .Test()
                .AssertResult(AsList(1, 2), AsList(2, 3), AsList(3, 4), AsList(4, 5), AsList(5));
        }

        [Test]
        public void Overlap_Basic_2()
        {
            ObservableSource.Range(1, 5)
                .Buffer(4, 2)
                .Test()
                .AssertResult(AsList(1, 2, 3, 4), AsList(3, 4, 5), AsList(5));
        }

        [Test]
        public void Overlap_Basic_All()
        {
            ObservableSource.Range(1, 5)
                .Buffer(10, 6)
                .Test()
                .AssertResult(AsList(1, 2, 3, 4, 5));
        }

        [Test]
        public void Overlap_Empty()
        {
            ObservableSource.Empty<int>()
                .Buffer(2, 1)
                .Test()
                .AssertResult();
        }

        [Test]
        public void Overlap_Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .Buffer(2, 1)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Overlap_Dispose()
        {
            TestHelper.VerifyDisposeObservableSource<int, IList<int>>(o => o.Buffer(2, 1));
        }

        [Test]
        public void Overlap_Custom_Collection()
        {
            ObservableSource.Range(1, 5)
                .Buffer(2, 1, () => new HashSet<int>())
                .Test()
                .AssertResult(AsSet(1, 2), AsSet(2, 3), AsSet(3, 4), AsSet(4, 5), AsSet(5));
        }

        #endregion

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
