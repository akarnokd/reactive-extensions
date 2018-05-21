using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Collections.Generic;

namespace akarnokd.reactive_extensions_test.maybe
{
    [TestFixture]
    public class MaybeZipTest
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
                MaybeSource.Just(1),
                MaybeSource.Just(10)
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
                var o = new IMaybeSource<int>[i];

                var src = MaybeSource.Just(1);

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
                MaybeSource.Just(1),
                MaybeSource.Just(10)
            }
            .Zip(Sum, true)
            .Test()
            .AssertResult(11);
        }

        [Test]
        public void Array_Error_First()
        {
            var count = 0;

            var src = MaybeSource.FromFunc(() =>
            {
                return ++count;
            });

            new[]
            {
                MaybeSource.Error<int>(new InvalidOperationException()),
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

            var src = MaybeSource.FromFunc(() =>
            {
                return ++count;
            });

            new[]
            {
                src,
                MaybeSource.Error<int>(new InvalidOperationException())
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

            var src = MaybeSource.FromFunc(() =>
            {
                return ++count;
            });

            new[]
            {
                MaybeSource.Error<int>(new InvalidOperationException()),
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

            var src = MaybeSource.FromFunc(() =>
            {
                return ++count;
            });

            new[]
            {
                src,
                MaybeSource.Error<int>(new InvalidOperationException())
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
                MaybeSource.Just(1),
                MaybeSource.Just(10)
            }
            .Zip<int, int>(v => { throw new InvalidOperationException(); })
            .Test()
            .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Array_Empty()
        {
            MaybeSource.Zip(new IMaybeSource<int>[0], Sum)
                .Test()
                .AssertResult();
        }

        [Test]
        public void Array_Single()
        {
            MaybeSource.Zip(new IMaybeSource<int>[] { MaybeSource.Just(1) }, Sum)
                .Test()
                .AssertResult(1);
        }

        [Test]
        public void Array_Empty_First()
        {
            var count = 0;

            var src = MaybeSource.FromFunc(() =>
            {
                return ++count;
            });

            new[]
            {
                MaybeSource.Empty<int>(),
                src
            }
            .Zip(Sum)
            .Test()
            .AssertResult();

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Array_Empty_Second()
        {
            var count = 0;

            var src = MaybeSource.FromFunc(() =>
            {
                return ++count;
            });

            new[]
            {
                src,
                MaybeSource.Empty<int>()
            }
            .Zip(Sum)
            .Test()
            .AssertResult();

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Array_Empty_First_Delayed()
        {
            var count = 0;

            var src = MaybeSource.FromFunc(() =>
            {
                return ++count;
            });

            new[]
            {
                MaybeSource.Empty<int>(),
                src
            }
            .Zip(Sum, true)
            .Test()
            .AssertResult();

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Array_Empty_Second_Delayed()
        {
            var count = 0;

            var src = MaybeSource.FromFunc(() =>
            {
                return ++count;
            });

            new[]
            {
                src,
                MaybeSource.Empty<int>()
            }
            .Zip(Sum, true)
            .Test()
            .AssertResult();

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Array_Error_Empty_First()
        {
            var count = 0;

            var src = MaybeSource.FromAction<int>(() =>
            {
                ++count;
            });

            new[]
            {
                MaybeSource.Error<int>(new InvalidOperationException()),
                src
            }
            .Zip(Sum)
            .Test()
            .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Array_Error_Empty_Second()
        {
            var count = 0;

            var src = MaybeSource.FromAction<int>(() =>
            {
                ++count;
            });

            var err = MaybeSource.FromAction<int>(() =>
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
            .AssertResult();

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Array_Error_Empty_First_Delayed()
        {
            var count = 0;

            var src = MaybeSource.FromAction<int>(() =>
            {
                ++count;
            });

            new[]
            {
                MaybeSource.Error<int>(new InvalidOperationException()),
                src
            }
            .Zip(Sum, true)
            .Test()
            .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Array_Error_Empty_Second_Delayed()
        {
            var count = 0;

            var src = MaybeSource.FromAction<int>(() =>
            {
                ++count;
            });

            new[]
            {
                src,
                MaybeSource.Error<int>(new InvalidOperationException())
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

            var src = MaybeSource.FromFunc(() => ++count);

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

            var src = MaybeSource.FromFunc(() => ++count);

            new[]
            {
                src,
                src,
                MaybeSource.Empty<int>(),
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
            new List<IMaybeSource<int>>()
            {
                MaybeSource.Just(1),
                MaybeSource.Just(10)
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
                var o = new List<IMaybeSource<int>>();

                var src = MaybeSource.Just(1);

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
            new List<IMaybeSource<int>>()
            {
                MaybeSource.Just(1),
                MaybeSource.Just(10)
            }
            .Zip(Sum, true)
            .Test()
            .AssertResult(11);
        }

        [Test]
        public void Enumerable_Error_First()
        {
            var count = 0;

            var src = MaybeSource.FromFunc(() =>
            {
                return ++count;
            });

            new List<IMaybeSource<int>>()
            {
                MaybeSource.Error<int>(new InvalidOperationException()),
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

            var src = MaybeSource.FromFunc(() =>
            {
                return ++count;
            });

            new List<IMaybeSource<int>>()
            {
                src,
                MaybeSource.Error<int>(new InvalidOperationException())
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

            var src = MaybeSource.FromFunc(() =>
            {
                return ++count;
            });

            new List<IMaybeSource<int>>()
            {
                MaybeSource.Error<int>(new InvalidOperationException()),
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

            var src = MaybeSource.FromFunc(() =>
            {
                return ++count;
            });

            new List<IMaybeSource<int>>()
            {
                src,
                MaybeSource.Error<int>(new InvalidOperationException())
            }
            .Zip(Sum, true)
            .Test()
            .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Enumerable_Mapper_Crash()
        {
            new List<IMaybeSource<int>>()
            {
                MaybeSource.Just(1),
                MaybeSource.Just(10)
            }
            .Zip<int, int>(v => { throw new InvalidOperationException(); })
            .Test()
            .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Enumerable_Empty()
        {
            MaybeSource.Zip(new List<IMaybeSource<int>>(), Sum)
                .Test()
                .AssertResult();
        }

        [Test]
        public void Enumerable_Single()
        {
            MaybeSource.Zip(new List<IMaybeSource<int>>() { MaybeSource.Just(1) }, Sum)
                .Test()
                .AssertResult(1);
        }

        [Test]
        public void Enumerable_Empty_First()
        {
            var count = 0;

            var src = MaybeSource.FromFunc(() =>
            {
                return ++count;
            });

            new List<IMaybeSource<int>>()
            {
                MaybeSource.Empty<int>(),
                src
            }
            .Zip(Sum)
            .Test()
            .AssertResult();

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Enumerable_Empty_Second()
        {
            var count = 0;

            var src = MaybeSource.FromFunc(() =>
            {
                return ++count;
            });

            new List<IMaybeSource<int>>()
            {
                src,
                MaybeSource.Empty<int>()
            }
            .Zip(Sum)
            .Test()
            .AssertResult();

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Enumerable_Empty_First_Delayed()
        {
            var count = 0;

            var src = MaybeSource.FromFunc(() =>
            {
                return ++count;
            });

            new List<IMaybeSource<int>>()
            {
                MaybeSource.Empty<int>(),
                src
            }
            .Zip(Sum, true)
            .Test()
            .AssertResult();

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Enumerable_Empty_Second_Delayed()
        {
            var count = 0;

            var src = MaybeSource.FromFunc(() =>
            {
                return ++count;
            });

            new List<IMaybeSource<int>>()
            {
                src,
                MaybeSource.Empty<int>()
            }
            .Zip(Sum, true)
            .Test()
            .AssertResult();

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Enumerable_Error_Empty_First()
        {
            var count = 0;

            var src = MaybeSource.FromAction<int>(() =>
            {
                ++count;
            });

            new List<IMaybeSource<int>>()
            {
                MaybeSource.Error<int>(new InvalidOperationException()),
                src
            }
            .Zip(Sum)
            .Test()
            .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Enumerable_Error_Empty_Second()
        {
            var count = 0;

            var src = MaybeSource.FromAction<int>(() =>
            {
                ++count;
            });

            var err = MaybeSource.FromAction<int>(() =>
            {
                ++count;
                throw new InvalidOperationException();
            });

            new List<IMaybeSource<int>>()
            {
                src,
                err
            }
            .Zip(Sum)
            .Test()
            .AssertResult();

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Enumerable_Error_Empty_First_Delayed()
        {
            var count = 0;

            var src = MaybeSource.FromAction<int>(() =>
            {
                ++count;
            });

            new List<IMaybeSource<int>>()
            {
                MaybeSource.Error<int>(new InvalidOperationException()),
                src
            }
            .Zip(Sum, true)
            .Test()
            .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Enumerable_Error_Empty_Second_Delayed()
        {
            var count = 0;

            var src = MaybeSource.FromAction<int>(() =>
            {
                ++count;
            });

            new List<IMaybeSource<int>>()
            {
                src,
                MaybeSource.Error<int>(new InvalidOperationException())
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

            var src = MaybeSource.FromFunc(() => ++count);

            new List<IMaybeSource<int>>()
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

            var src = MaybeSource.FromFunc(() => ++count);

            new List<IMaybeSource<int>>()
            {
                src,
                MaybeSource.Empty<int>(),
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
            MaybeSource.Zip(new FailingEnumerable<IMaybeSource<int>>(true, false, false), Sum)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Enumerable_MoveNext_Crash()
        {
            MaybeSource.Zip(new FailingEnumerable<IMaybeSource<int>>(false, true, false), Sum)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        #endregion + IEnumerable +
    }
}
