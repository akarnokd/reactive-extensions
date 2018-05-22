using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Collections.Generic;

namespace akarnokd.reactive_extensions_test.single
{
    [TestFixture]
    public class SingleZipTest
    {
        int Sum(int[] a)
        {
            int s = 0;
            foreach (var i in a)
            {
                s += i;
            }
            return s;
        }

        #region + Array +

        [Test]
        public void Array_Success()
        {
            new[]
            {
                SingleSource.Just(1),
                SingleSource.Just(10)
            }
            .Zip(Sum)
            .Test()
            .AssertResult(11);
        }

        [Test]
        public void Array_Success_Many()
        {
            for (int i = 1; i < 32; i++)
            {
                var o = new ISingleSource<int>[i];

                var src = SingleSource.Just(1);

                for (int j = 0; j < o.Length; j++)
                {
                    o[j] = src;
                }

                o.Zip(Sum)
                .Test()
                .AssertResult(i);
            }
        }

        [Test]
        public void Array_Success_DelayError()
        {
            new[]
            {
                SingleSource.Just(1),
                SingleSource.Just(10)
            }
            .Zip(Sum, true)
            .Test()
            .AssertResult(11);
        }

        [Test]
        public void Array_Error_First()
        {
            var count = 0;

            var src = SingleSource.FromFunc(() =>
            {
                return ++count;
            });

            new[]
            {
                SingleSource.Error<int>(new InvalidOperationException()),
                src
            }
            .Zip(Sum)
            .Test()
            .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Array_Error_Second()
        {
            var count = 0;

            var src = SingleSource.FromFunc(() =>
            {
                return ++count;
            });

            new[]
            {
                src,
                SingleSource.Error<int>(new InvalidOperationException())
            }
            .Zip(Sum)
            .Test()
            .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Array_Error_First_Delayed()
        {
            var count = 0;

            var src = SingleSource.FromFunc(() =>
            {
                return ++count;
            });

            new[]
            {
                SingleSource.Error<int>(new InvalidOperationException()),
                src
            }
            .Zip(Sum, true)
            .Test()
            .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Array_Error_Second_Delayed()
        {
            var count = 0;

            var src = SingleSource.FromFunc(() =>
            {
                return ++count;
            });

            new[]
            {
                src,
                SingleSource.Error<int>(new InvalidOperationException())
            }
            .Zip(Sum, true)
            .Test()
            .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Array_Mapper_Crash()
        {
            new[]
            {
                SingleSource.Just(1),
                SingleSource.Just(10)
            }
            .Zip<int, int>(v => { throw new InvalidOperationException(); })
            .Test()
            .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Array_Empty()
        {
            SingleSource.Zip(new ISingleSource<int>[0], Sum)
                .Test()
                .AssertFailure(typeof(IndexOutOfRangeException));
        }

        [Test]
        public void Array_Single()
        {
            SingleSource.Zip(new ISingleSource<int>[] { SingleSource.Just(1) }, Sum)
                .Test()
                .AssertResult(1);
        }

        [Test]
        public void Array_Error_Success_First()
        {
            var count = 0;

            var src = SingleSource.FromFunc<int>(() => ++count);

            new[]
            {
                SingleSource.Error<int>(new InvalidOperationException()),
                src
            }
            .Zip(Sum)
            .Test()
            .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Array_Error_Success_Second()
        {
            var count = 0;

            var src = SingleSource.FromFunc<int>(() => ++count);

            var err = SingleSource.FromFunc<int>(() =>
            {
                ++count;
                throw new InvalidOperationException();
            });

            new[]
            {
                src,
                err
            }
            .Zip(Sum)
            .Test()
            .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(2, count);
        }

        [Test]
        public void Array_Error_Success_First_Delayed()
        {
            var count = 0;

            var src = SingleSource.FromFunc<int>(() => ++count);

            new[]
            {
                SingleSource.Error<int>(new InvalidOperationException()),
                src
            }
            .Zip(Sum, true)
            .Test()
            .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Array_Error_Success_Second_Delayed()
        {
            var count = 0;

            var src = SingleSource.FromFunc<int>(() => ++count);

            new[]
            {
                src,
                SingleSource.Error<int>(new InvalidOperationException())
            }
            .Zip(Sum, true)
            .Test()
            .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Array_Null_Entry()
        {
            var count = 0;

            var src = SingleSource.FromFunc(() => ++count);

            new[]
            {
                src,
                src,
                null,
                src,
                src
            }
            .Zip(Sum)
            .Test()
            .AssertFailure(typeof(NullReferenceException));
        }

        [Test]
        public void Array_Null_Entry_DelayError()
        {
            var count = 0;

            var src = SingleSource.FromFunc(() => ++count);

            new[]
            {
                src,
                src,
                null,
                src,
                src
            }
            .Zip(Sum, true)
            .Test()
            .AssertFailure(typeof(NullReferenceException));
        }

        #endregion + Array +

        #region + IEnumerable +

        [Test]
        public void Enumerable_Success()
        {
            new List<ISingleSource<int>>()
            {
                SingleSource.Just(1),
                SingleSource.Just(10)
            }
            .Zip(Sum)
            .Test()
            .AssertResult(11);
        }

        [Test]
        public void Enumerable_Success_Many()
        {
            for (int i = 1; i < 32; i++)
            {
                var o = new List<ISingleSource<int>>();

                var src = SingleSource.Just(1);

                for (int j = 0; j < i; j++)
                {
                    o.Add(src);
                }

                o.Zip(Sum)
                .Test()
                .AssertResult(i);
            }
        }

        [Test]
        public void Enumerable_Success_DelayError()
        {
            new List<ISingleSource<int>>()
            {
                SingleSource.Just(1),
                SingleSource.Just(10)
            }
            .Zip(Sum, true)
            .Test()
            .AssertResult(11);
        }

        [Test]
        public void Enumerable_Error_First()
        {
            var count = 0;

            var src = SingleSource.FromFunc(() =>
            {
                return ++count;
            });

            new List<ISingleSource<int>>()
            {
                SingleSource.Error<int>(new InvalidOperationException()),
                src
            }
            .Zip(Sum)
            .Test()
            .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Enumerable_Error_Second()
        {
            var count = 0;

            var src = SingleSource.FromFunc(() =>
            {
                return ++count;
            });

            new List<ISingleSource<int>>()
            {
                src,
                SingleSource.Error<int>(new InvalidOperationException())
            }
            .Zip(Sum)
            .Test()
            .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Enumerable_Error_First_Delayed()
        {
            var count = 0;

            var src = SingleSource.FromFunc(() =>
            {
                return ++count;
            });

            new List<ISingleSource<int>>()
            {
                SingleSource.Error<int>(new InvalidOperationException()),
                src
            }
            .Zip(Sum, true)
            .Test()
            .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Enumerable_Error_Second_Delayed()
        {
            var count = 0;

            var src = SingleSource.FromFunc(() =>
            {
                return ++count;
            });

            new List<ISingleSource<int>>()
            {
                src,
                SingleSource.Error<int>(new InvalidOperationException())
            }
            .Zip(Sum, true)
            .Test()
            .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Enumerable_Mapper_Crash()
        {
            new List<ISingleSource<int>>()
            {
                SingleSource.Just(1),
                SingleSource.Just(10)
            }
            .Zip<int, int>(v => { throw new InvalidOperationException(); })
            .Test()
            .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Enumerable_Empty()
        {
            SingleSource.Zip(new List<ISingleSource<int>>(), Sum)
                .Test()
                .AssertFailure(typeof(IndexOutOfRangeException));
        }

        [Test]
        public void Enumerable_Single()
        {
            SingleSource.Zip(new List<ISingleSource<int>>() { SingleSource.Just(1) }, Sum)
                .Test()
                .AssertResult(1);
        }

        [Test]
        public void Enumerable_Error_Success_First()
        {
            var count = 0;

            var src = SingleSource.FromFunc<int>(() => ++count);

            new List<ISingleSource<int>>()
            {
                SingleSource.Error<int>(new InvalidOperationException()),
                src
            }
            .Zip(Sum)
            .Test()
            .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Enumerable_Error_Success_Second()
        {
            var count = 0;

            var src = SingleSource.FromFunc<int>(() => ++count);

            var err = SingleSource.FromFunc<int>(() =>
            {
                ++count;
                throw new InvalidOperationException();
            });

            new List<ISingleSource<int>>()
            {
                src,
                err
            }
            .Zip(Sum)
            .Test()
            .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(2, count);
        }

        [Test]
        public void Enumerable_Error_Success_First_Delayed()
        {
            var count = 0;

            var src = SingleSource.FromFunc<int>(() => ++count);

            new List<ISingleSource<int>>()
            {
                SingleSource.Error<int>(new InvalidOperationException()),
                src
            }
            .Zip(Sum, true)
            .Test()
            .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Enumerable_Error_Success_Second_Delayed()
        {
            var count = 0;

            var src = SingleSource.FromFunc<int>(() => ++count);

            new List<ISingleSource<int>>()
            {
                src,
                SingleSource.Error<int>(new InvalidOperationException())
            }
            .Zip(Sum, true)
            .Test()
            .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Enumerable_Null_Entry()
        {
            var count = 0;

            var src = SingleSource.FromFunc(() => ++count);

            new List<ISingleSource<int>>()
            {
                src,
                src,
                null,
                src,
                src
            }
            .Zip(Sum)
            .Test()
            .AssertFailure(typeof(NullReferenceException));
        }

        [Test]
        public void Enumerable_Null_Entry_DelayError()
        {
            var count = 0;

            var src = SingleSource.FromFunc(() => ++count);

            new List<ISingleSource<int>>()
            {
                src,
                src,
                null,
                src,
                src
            }
            .Zip(Sum, true)
            .Test()
            .AssertFailure(typeof(NullReferenceException));
        }
        [Test]
        public void Enumerable_GetEnumerator_Crash()
        {
            SingleSource.Zip(new FailingEnumerable<ISingleSource<int>>(true, false, false), Sum)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Enumerable_MoveNext_Crash()
        {
            SingleSource.Zip(new FailingEnumerable<ISingleSource<int>>(false, true, false), Sum)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        #endregion + IEnumerable +
    }
}
