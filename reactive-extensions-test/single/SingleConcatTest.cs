using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Collections.Generic;

namespace akarnokd.reactive_extensions_test.single
{
    [TestFixture]
    public class SingleConcatTest
    {
        #region + Array +

        [Test]
        public void Array_Empty()
        {
            new ISingleSource<int>[0]
            .ConcatAll()
            .Test()
            .AssertResult();
        }

        [Test]
        public void Array_Empty_DelayError()
        {
            new ISingleSource<int>[0]
            .ConcatAll(true)
            .Test()
            .AssertResult();
        }

        [Test]
        public void Array_Basic()
        {
            new[]
            {
                SingleSource.Just(1),
                SingleSource.Just(2)
            }
            .ConcatAll()
            .Test()
            .AssertResult(1, 2);
        }

        [Test]
        public void Array_Basic_DelayError()
        {
            new[]
            {
                SingleSource.Just(1),
                SingleSource.Just(2)
            }
            .ConcatAll(true)
            .Test()
            .AssertResult(1, 2);
        }

        [Test]
        public void Array_Basic_Params()
        {
            SingleSource.Concat(
                SingleSource.Just(1),
                SingleSource.Just(2)
            )
            .Test()
            .AssertResult(1, 2);
        }

        [Test]
        public void Array_Basic_Params_DelayError()
        {
            SingleSource.Concat(true,
                SingleSource.Just(1),
                SingleSource.Just(2)
            )
            .Test()
            .AssertResult(1, 2);
        }

        [Test]
        public void Array_Mixed()
        {
            new[]
            {
                SingleSource.Just(1),
                SingleSource.Just(2),
                SingleSource.Just(3)
            }
            .ConcatAll()
            .Test()
            .AssertResult(1, 2, 3);
        }

        [Test]
        public void Array_Error_Last()
        {
            new[]
            {
                SingleSource.Just(1),
                SingleSource.Just(2),
                SingleSource.Just(3),
                SingleSource.Error<int>(new InvalidOperationException())
            }
            .ConcatAll()
            .Test()
            .AssertFailure(typeof(InvalidOperationException), 1, 2, 3);
        }

        [Test]
        public void Array_Error_Middle()
        {
            var count = 0;

            var src = SingleSource.FromFunc(() => ++count);

            new[]
            {
                src,
                src,
                SingleSource.Error<int>(new InvalidOperationException()),
                src,
                src
            }
            .ConcatAll()
            .Test()
            .AssertFailure(typeof(InvalidOperationException), 1, 2);
        }

        [Test]
        public void Array_Error_Last_Delay()
        {
            new[]
            {
                SingleSource.Just(1),
                SingleSource.Just(2),
                SingleSource.Just(3),
                SingleSource.Error<int>(new InvalidOperationException())
            }
            .ConcatAll(true)
            .Test()
            .AssertFailure(typeof(InvalidOperationException), 1, 2, 3);
        }

        [Test]
        public void Array_Error_Middle_Delay()
        {
            var count = 0;

            var src = SingleSource.FromFunc(() => ++count);

            new[]
            {
                src,
                src,
                SingleSource.Error<int>(new InvalidOperationException()),
                src,
                src
            }
            .ConcatAll(true)
            .Test()
            .AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4);
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
            .ConcatAll()
            .Test()
            .AssertFailure(typeof(NullReferenceException), 1, 2);
        }

        #endregion + Array +

        #region + Enumerable +

        [Test]
        public void Enumerable_Empty()
        {
            new List<ISingleSource<int>>()
            .Concat()
            .Test()
            .AssertResult();
        }

        [Test]
        public void Enumerable_Empty_DelayError()
        {
            new List<ISingleSource<int>>()
            .Concat(true)
            .Test()
            .AssertResult();
        }

        [Test]
        public void Enumerable_Basic()
        {
            new List<ISingleSource<int>>()
            {
                SingleSource.Just(1),
                SingleSource.Just(2)
            }
            .Concat()
            .Test()
            .AssertResult(1, 2);
        }

        [Test]
        public void Enumerable_Basic_DelayError()
        {
            new List<ISingleSource<int>>()
            {
                SingleSource.Just(1),
                SingleSource.Just(2)
            }
            .Concat(true)
            .Test()
            .AssertResult(1, 2);
        }

        [Test]
        public void Enumerable_Basic_Params()
        {
            SingleSource.Concat(
                new List<ISingleSource<int>>() {
                    SingleSource.Just(1),
                    SingleSource.Just(2)
                }
            )
            .Test()
            .AssertResult(1, 2);
        }

        [Test]
        public void Enumerable_Basic_Params_DelayError()
        {
            SingleSource.Concat(
                new List<ISingleSource<int>>() {
                    SingleSource.Just(1),
                    SingleSource.Just(2)
                }, true
            )
            .Test()
            .AssertResult(1, 2);
        }

        [Test]
        public void Enumerable_Mixed()
        {
            new List<ISingleSource<int>>()
            {
                SingleSource.Just(1),
                SingleSource.Just(2),
                SingleSource.Just(3)
            }
            .Concat()
            .Test()
            .AssertResult(1, 2, 3);
        }

        [Test]
        public void Enumerable_Error_Last()
        {
            new List<ISingleSource<int>>()
            {
                SingleSource.Just(1),
                SingleSource.Just(2),
                SingleSource.Just(3),
                SingleSource.Error<int>(new InvalidOperationException())
            }
            .Concat()
            .Test()
            .AssertFailure(typeof(InvalidOperationException), 1, 2, 3);
        }

        [Test]
        public void Enumerable_Error_Middle()
        {
            var count = 0;

            var src = SingleSource.FromFunc(() => ++count);

            new List<ISingleSource<int>>()
            {
                src,
                src,
                SingleSource.Error<int>(new InvalidOperationException()),
                src,
                src
            }
            .Concat()
            .Test()
            .AssertFailure(typeof(InvalidOperationException), 1, 2);
        }

        [Test]
        public void Enumerable_Error_Last_Delay()
        {
            new List<ISingleSource<int>>()
            {
                SingleSource.Just(1),
                SingleSource.Just(2),
                SingleSource.Just(3),
                SingleSource.Error<int>(new InvalidOperationException())
            }
            .Concat(true)
            .Test()
            .AssertFailure(typeof(InvalidOperationException), 1, 2, 3);
        }

        [Test]
        public void Enumerable_Error_Middle_Delay()
        {
            var count = 0;

            var src = SingleSource.FromFunc(() => ++count);

            new List<ISingleSource<int>>()
            {
                src,
                src,
                SingleSource.Error<int>(new InvalidOperationException()),
                src,
                src
            }
            .Concat(true)
            .Test()
            .AssertFailure(typeof(InvalidOperationException), 1, 2, 3, 4);
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
            .Concat()
            .Test()
            .AssertFailure(typeof(NullReferenceException), 1, 2);
        }

        [Test]
        public void Enumerable_GetEnumerator_Crash()
        {
            SingleSource.Concat(new FailingEnumerable<ISingleSource<int>>(true, false, false))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void Enumerable_MoveNext_Crash()
        {
            SingleSource.Concat(new FailingEnumerable<ISingleSource<int>>(false, true, false))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        #endregion + Enumerable +
    }
}
