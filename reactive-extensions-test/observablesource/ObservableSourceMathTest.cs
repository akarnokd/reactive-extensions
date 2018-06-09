using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Collections.Generic;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceMathTest
    {
        [Test]
        public void Sum_Int()
        {
            ObservableSource.Range(1, 5)
                .Sum()
                .Test()
                .AssertResult(15);
        }

        [Test]
        public void Sum_Long()
        {
            ObservableSource.RangeLong(1, 5)
                .Sum()
                .Test()
                .AssertResult(15L);
        }

        [Test]
        public void Sum_Float()
        {
            ObservableSource.Range(1, 5)
                .Map(v => v + 0.5f)
                .Sum()
                .Test()
                .AssertResult(17.5f);
        }

        [Test]
        public void Sum_Double()
        {
            ObservableSource.Range(1, 5)
                .Map(v => v + 0.5d)
                .Sum()
                .Test()
                .AssertResult(17.5d);
        }

        [Test]
        public void Sum_Decimal()
        {
            ObservableSource.Range(1, 5)
                .Map(v => v + 0.5m)
                .Sum()
                .Test()
                .AssertResult(17.5m);
        }

        [Test]
        public void Sum_Int_Empty()
        {
            ObservableSource.Empty<int>()
                .Sum()
                .Test()
                .AssertResult();
        }

        [Test]
        public void Sum_Long_Empty()
        {
            ObservableSource.Empty<long>()
                .Sum()
                .Test()
                .AssertResult();
        }

        [Test]
        public void Sum_Float_Empty()
        {
            ObservableSource.Empty<float>()
                .Map(v => v + 0.5f)
                .Sum()
                .Test()
                .AssertResult();
        }

        [Test]
        public void Sum_Double_Empty()
        {
            ObservableSource.Empty<double>()
                .Map(v => v + 0.5d)
                .Sum()
                .Test()
                .AssertResult();
        }

        [Test]
        public void Sum_Decimal_Empty()
        {
            ObservableSource.Empty<decimal>()
                .Map(v => v + 0.5m)
                .Sum()
                .Test()
                .AssertResult();
        }

        [Test]
        public void Max_Comparable()
        {
            ObservableSource.Range(1, 5)
                .Max()
                .Test()
                .AssertResult(5);
        }

        [Test]
        public void Max_Comparable_KeySelector()
        {
            ObservableSource.Range(1, 5)
                .Max(v => v / 2)
                .Test()
                .AssertResult(5);
        }

        [Test]
        public void Max_Comparator()
        {
            ObservableSource.Range(1, 5)
                .Max(new ReverseComparer<int>(Comparer<int>.Default))
                .Test()
                .AssertResult(1);
        }

        [Test]
        public void Max_Comparator_KeySelector()
        {
            ObservableSource.Range(1, 5)
                .Max(v => v / 2, new ReverseComparer<int>(Comparer<int>.Default))
                .Test()
                .AssertResult(1);
        }

        [Test]
        public void Max_Comparable_Empty()
        {
            ObservableSource.Empty<int>()
                .Max()
                .Test()
                .AssertResult();
        }

        [Test]
        public void Min_Comparable()
        {
            ObservableSource.Range(1, 5)
                .Min()
                .Test()
                .AssertResult(1);
        }

        [Test]
        public void Min_Comparator()
        {
            ObservableSource.Range(1, 5)
                .Min(new ReverseComparer<int>(Comparer<int>.Default))
                .Test()
                .AssertResult(5);
        }

        [Test]
        public void Min_Comparable_KeySelector()
        {
            ObservableSource.Range(1, 5)
                .Min(v => v / 2)
                .Test()
                .AssertResult(1);
        }

        [Test]
        public void Min_Comparator_KeySelector()
        {
            ObservableSource.Range(1, 5)
                .Min(v => v / 2, new ReverseComparer<int>(Comparer<int>.Default))
                .Test()
                .AssertResult(5);
        }

        [Test]
        public void Min_Comparable_Empty()
        {
            ObservableSource.Empty<int>()
                .Min()
                .Test()
                .AssertResult();
        }

        sealed class ReverseComparer<T> : IComparer<T>
        {
            readonly IComparer<T> comparer;

            public ReverseComparer(IComparer<T> comparer)
            {
                this.comparer = comparer;
            }

            public int Compare(T x, T y)
            {
                return comparer.Compare(y, x);
            }
        }
    }
}
