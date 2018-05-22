using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.single
{
    [TestFixture]
    public class SingleDoTest
    {
        [Test]
        public void OnSuccess_Basic()
        {
            var count = 0;

            SingleSource.Just(1)
                .DoOnSuccess(v => count++)
                .Test()
                .AssertResult(1);

            Assert.AreEqual(1, count);
        }

        [Test]
        public void OnSuccess_Basic_Twice()
        {
            var count = 0;

            SingleSource.Just(1)
                .DoOnSuccess(v => count++)
                .DoOnSuccess(v => count++)
                .Test()
                .AssertResult(1);

            Assert.AreEqual(2, count);
        }

        [Test]
        public void OnAfterSuccess_Basic()
        {
            var count = -1;

            var to = new TestObserver<int>();

            SingleSource.Just(1)
                .DoAfterSuccess(v => count = to.ItemCount)
                .SubscribeWith(to)
                .AssertResult(1);

            Assert.AreEqual(1, count);
        }

        [Test]
        public void OnAfterSuccess_Basic_Twice()
        {
            var count1 = -1;
            var count2 = -1;

            var to = new TestObserver<int>();

            SingleSource.Just(1)
                .DoAfterSuccess(v => count1 = to.ItemCount)
                .DoAfterSuccess(v => count2 = to.ItemCount)
                .SubscribeWith(to)
                .AssertResult(1);

            Assert.AreEqual(1, count1);
            Assert.AreEqual(1, count2);
        }

        [Test]
        public void OnError_Basic()
        {
            var count = 0;

            SingleSource.Error<int>(new InvalidOperationException())
                .DoOnError(e => count++)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(1, count);
        }

        [Test]
        public void OnError_Basic_Twice()
        {
            var count = 0;

            SingleSource.Error<int>(new InvalidOperationException())
                .DoOnError(e => count++)
                .DoOnError(e => count++)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(2, count);
        }

        [Test]
        public void OnError_Crash()
        {
            var count = 0;

            SingleSource.Error<int>(new InvalidOperationException("outer"))
                .DoOnError(e =>
                {
                    count++;
                    throw new InvalidOperationException("inner");
                })
                .Test()
                .AssertCompositeError(0, typeof(InvalidOperationException), "outer")
                .AssertCompositeError(1, typeof(InvalidOperationException), "inner");
            ;

            Assert.AreEqual(1, count);
        }


        [Test]
        public void OnSubscribe_Basic()
        {
            var count = 0;

            SingleSource.Just(1)
                .DoOnSubscribe(s => count++)
                .Test()
                .AssertResult(1);

            Assert.AreEqual(1, count);
        }

        [Test]
        public void OnSubscribe_Basic_Twice()
        {
            var count = 0;

            SingleSource.Just(1)
                .DoOnSubscribe(s => count++)
                .DoOnSubscribe(s => count++)
                .Test()
                .AssertResult(1);

            Assert.AreEqual(2, count);
        }

        [Test]
        public void OnSubscribe_Crash()
        {
            var count = 0;

            SingleSource.Just(1)
                .DoOnSubscribe(s =>
                {
                    count++;
                    throw new InvalidOperationException();
                })
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
            ;

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Finally_Basic()
        {
            var count = 0;

            SingleSource.Just(1)
                .DoFinally(() => count++)
                .Test()
                .AssertResult(1);

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Finally_Basic_Twice()
        {
            var count = 0;

            SingleSource.Just(1)
                .DoFinally(() => count++)
                .DoFinally(() => count++)
                .Test()
                .AssertResult(1);

            Assert.AreEqual(2, count);
        }

        [Test]
        public void Finally_Crash()
        {
            var count = 0;

            SingleSource.Just(1)
                .DoFinally(() =>
                {
                    count++;
                    throw new InvalidOperationException();
                })
                .Test()
                .AssertResult(1);
            ;

            Assert.AreEqual(1, count);
        }


        [Test]
        public void OnDispose_Basic()
        {
            var count = 0;

            SingleSource.Never<int>()
                .DoOnDispose(() => count++)
                .Test(true)
                .AssertEmpty();

            Assert.AreEqual(1, count);
        }

        [Test]
        public void OnDispose_Basic_Twice()
        {
            var count = 0;

            SingleSource.Never<int>()
                .DoOnDispose(() => count++)
                .DoOnDispose(() => count++)
                .Test(true)
                .AssertEmpty();

            Assert.AreEqual(2, count);
        }

        [Test]
        public void OnDispose_Crash()
        {
            var count = 0;

            SingleSource.Never<int>()
                .DoOnDispose(() =>
                {
                    count++;
                    throw new InvalidOperationException();
                })
                .Test(true)
                .AssertEmpty();
            ;

            Assert.AreEqual(1, count);
        }

        [Test]
        public void OnDispose_Success()
        {
            var count = 0;

            SingleSource.Just(1)
                .DoOnDispose(() =>
                {
                    count++;
                })
                .Test()
                .AssertResult(1);
            ;

            Assert.AreEqual(0, count);
        }

        [Test]
        public void OnDispose_Error()
        {
            var count = 0;

            SingleSource.Error<int>(new InvalidOperationException())
                .DoOnDispose(() =>
                {
                    count++;
                })
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
            ;

            Assert.AreEqual(0, count);
        }

        [Test]
        public void OnTerminate_Basic()
        {
            var count = 0;

            SingleSource.Just(1)
                .DoOnTerminate(() => count++)
                .Test()
                .AssertResult(1);

            Assert.AreEqual(1, count);
        }

        [Test]
        public void OnTerminate_Error()
        {
            var count = 0;

            SingleSource.Error<int>(new InvalidOperationException())
                .DoOnTerminate(() => count++)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(1, count);
        }

        [Test]
        public void OnTerminate_Basic_Twice()
        {
            var count = 0;

            SingleSource.Just(1)
                .DoOnTerminate(() => count++)
                .DoOnTerminate(() => count++)
                .Test()
                .AssertResult(1);

            Assert.AreEqual(2, count);
        }

        [Test]
        public void OnTerminate_Crash()
        {
            var count = 0;

            SingleSource.Just(1)
                .DoOnTerminate(() =>
                {
                    count++;
                    throw new InvalidOperationException();
                })
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
            ;

            Assert.AreEqual(1, count);
        }

        [Test]
        public void OnTerminate_Error_Crash()
        {
            var count = 0;

            SingleSource.Error<int>(new InvalidOperationException("main"))
                .DoOnTerminate(() =>
                {
                    count++;
                    throw new InvalidOperationException("inner");
                })
                .Test()
                .AssertCompositeError(0, typeof(InvalidOperationException), "main")
                .AssertCompositeError(0, typeof(InvalidOperationException), "inner")
                ;
            ;

            Assert.AreEqual(1, count);
        }


        [Test]
        public void AfterTerminate_Basic()
        {
            var count = 0;

            SingleSource.Just(1)
                .DoAfterTerminate(() => count++)
                .Test()
                .AssertResult(1);

            Assert.AreEqual(1, count);
        }

        [Test]
        public void AfterTerminate_Error()
        {
            var count = 0;

            SingleSource.Error<int>(new InvalidOperationException())
                .DoAfterTerminate(() => count++)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(1, count);
        }

        [Test]
        public void AferTerminate_Basic_Twice()
        {
            var count = 0;

            SingleSource.Just(1)
                .DoAfterTerminate(() => count++)
                .DoAfterTerminate(() => count++)
                .Test()
                .AssertResult(1);

            Assert.AreEqual(2, count);
        }

        [Test]
        public void AfterTerminate_Crash()
        {
            var count = 0;

            SingleSource.Just(1)
                .DoAfterTerminate(() =>
                {
                    count++;
                    throw new InvalidOperationException();
                })
                .Test()
                .AssertResult(1);
            ;

            Assert.AreEqual(1, count);
        }

        [Test]
        public void AfterTerminate_Error_Crash()
        {
            var count = 0;

            SingleSource.Error<int>(new InvalidOperationException("main"))
                .DoAfterTerminate(() =>
                {
                    count++;
                    throw new InvalidOperationException("inner");
                })
                .Test()
                .AssertFailure(typeof(InvalidOperationException))
                .AssertError(typeof(InvalidOperationException), "main");
                ;
            ;

            Assert.AreEqual(1, count);
        }

        [Test]
        public void All_Success()
        {
            var success = 0;
            var afterSuccess = 0;
            var error = 0;
            var terminate = 0;
            var afterterminate = 0;
            var subscribe = 0;
            var dispose = 0;
            var final = 0;

            SingleSource.Just(1)
                .DoOnSuccess(v => success++)
                .DoAfterSuccess(v => afterSuccess++)
                .DoOnError(e => error++)
                .DoOnTerminate(() => terminate++)
                .DoAfterTerminate(() => afterterminate++)
                .DoOnSubscribe(d => subscribe++)
                .DoOnDispose(() => dispose++)
                .DoFinally(() => final++)
                .Test()
                .AssertResult(1);

            Assert.AreEqual(1, success);
            Assert.AreEqual(1, afterSuccess);
            Assert.AreEqual(0, error);
            Assert.AreEqual(1, terminate);
            Assert.AreEqual(1, afterterminate);
            Assert.AreEqual(1, subscribe);
            Assert.AreEqual(0, dispose);
            Assert.AreEqual(1, final);
        }

        [Test]
        public void All_Error()
        {
            var success = 0;
            var afterSuccess = 0;
            var error = 0;
            var terminate = 0;
            var afterterminate = 0;
            var subscribe = 0;
            var dispose = 0;
            var final = 0;

            SingleSource.Error<int>(new InvalidOperationException())
                .DoOnSuccess(v => success++)
                .DoOnSuccess(v => afterSuccess++)
                .DoOnError(e => error++)
                .DoOnTerminate(() => terminate++)
                .DoAfterTerminate(() => afterterminate++)
                .DoOnSubscribe(d => subscribe++)
                .DoOnDispose(() => dispose++)
                .DoFinally(() => final++)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(0, success);
            Assert.AreEqual(0, afterSuccess);
            Assert.AreEqual(1, error);
            Assert.AreEqual(1, terminate);
            Assert.AreEqual(1, afterterminate);
            Assert.AreEqual(1, subscribe);
            Assert.AreEqual(0, dispose);
            Assert.AreEqual(1, final);
        }
    }
}
