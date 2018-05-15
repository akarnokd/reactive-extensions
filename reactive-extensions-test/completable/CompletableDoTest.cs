using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.completable
{
    [TestFixture]
    public class CompletableDoTest
    {
        public object Completable { get; private set; }

        [Test]
        public void OnCompleted_Basic()
        {
            var count = 0;

            CompletableSource.Empty()
                .DoOnCompleted(() => count++)
                .Test()
                .AssertResult();

            Assert.AreEqual(1, count);
        }

        [Test]
        public void OnCompleted_Basic_Twice()
        {
            var count = 0;

            CompletableSource.Empty()
                .DoOnCompleted(() => count++)
                .DoOnCompleted(() => count++)
                .Test()
                .AssertResult();

            Assert.AreEqual(2, count);
        }

        [Test]
        public void OnCompleted_Crash()
        {
            var count = 0;

            CompletableSource.Empty()
                .DoOnCompleted(() =>
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
        public void OnError_Basic()
        {
            var count = 0;

            CompletableSource.Error(new InvalidOperationException())
                .DoOnError(e => count++)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(1, count);
        }

        [Test]
        public void OnError_Basic_Twice()
        {
            var count = 0;

            CompletableSource.Error(new InvalidOperationException())
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

            CompletableSource.Error(new InvalidOperationException("outer"))
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

            CompletableSource.Empty()
                .DoOnSubscribe(s => count++)
                .Test()
                .AssertResult();

            Assert.AreEqual(1, count);
        }

        [Test]
        public void OnSubscribe_Basic_Twice()
        {
            var count = 0;

            CompletableSource.Empty()
                .DoOnSubscribe(s => count++)
                .DoOnSubscribe(s => count++)
                .Test()
                .AssertResult();

            Assert.AreEqual(2, count);
        }

        [Test]
        public void OnSubscribe_Crash()
        {
            var count = 0;

            CompletableSource.Empty()
                .DoOnCompleted(() =>
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

            CompletableSource.Empty()
                .DoFinally(() => count++)
                .Test()
                .AssertResult();

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Finally_Basic_Twice()
        {
            var count = 0;

            CompletableSource.Empty()
                .DoFinally(() => count++)
                .DoFinally(() => count++)
                .Test()
                .AssertResult();

            Assert.AreEqual(2, count);
        }

        [Test]
        public void Finally_Crash()
        {
            var count = 0;

            CompletableSource.Empty()
                .DoFinally(() =>
                {
                    count++;
                    throw new InvalidOperationException();
                })
                .Test()
                .AssertResult();
            ;

            Assert.AreEqual(1, count);
        }


        [Test]
        public void OnDispose_Basic()
        {
            var count = 0;

            CompletableSource.Never()
                .DoOnDispose(() => count++)
                .Test(true)
                .AssertEmpty();

            Assert.AreEqual(1, count);
        }

        [Test]
        public void OnDispose_Basic_Twice()
        {
            var count = 0;

            CompletableSource.Never()
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

            CompletableSource.Never()
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
        public void OnDispose_Complete()
        {
            var count = 0;

            CompletableSource.Empty()
                .DoOnDispose(() =>
                {
                    count++;
                })
                .Test()
                .AssertResult();
            ;

            Assert.AreEqual(0, count);
        }

        [Test]
        public void OnDispose_Error()
        {
            var count = 0;

            CompletableSource.Error(new InvalidOperationException())
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

            CompletableSource.Empty()
                .DoOnTerminate(() => count++)
                .Test()
                .AssertResult();

            Assert.AreEqual(1, count);
        }

        [Test]
        public void OnTerminate_Error()
        {
            var count = 0;

            CompletableSource.Error(new InvalidOperationException())
                .DoOnTerminate(() => count++)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(1, count);
        }

        [Test]
        public void OnTerminate_Basic_Twice()
        {
            var count = 0;

            CompletableSource.Empty()
                .DoOnTerminate(() => count++)
                .DoOnTerminate(() => count++)
                .Test()
                .AssertResult();

            Assert.AreEqual(2, count);
        }

        [Test]
        public void OnTerminate_Crash()
        {
            var count = 0;

            CompletableSource.Empty()
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

            CompletableSource.Error(new InvalidOperationException("main"))
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

            CompletableSource.Empty()
                .DoAfterTerminate(() => count++)
                .Test()
                .AssertResult();

            Assert.AreEqual(1, count);
        }

        [Test]
        public void AfterTerminate_Error()
        {
            var count = 0;

            CompletableSource.Error(new InvalidOperationException())
                .DoAfterTerminate(() => count++)
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(1, count);
        }

        [Test]
        public void AferTerminate_Basic_Twice()
        {
            var count = 0;

            CompletableSource.Empty()
                .DoAfterTerminate(() => count++)
                .DoAfterTerminate(() => count++)
                .Test()
                .AssertResult();

            Assert.AreEqual(2, count);
        }

        [Test]
        public void AfterTerminate_Crash()
        {
            var count = 0;

            CompletableSource.Empty()
                .DoAfterTerminate(() =>
                {
                    count++;
                    throw new InvalidOperationException();
                })
                .Test()
                .AssertResult();
            ;

            Assert.AreEqual(1, count);
        }

        [Test]
        public void AfterTerminate_Error_Crash()
        {
            var count = 0;

            CompletableSource.Error(new InvalidOperationException("main"))
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
        public void All_Basic()
        {
            var completed = 0;
            var error = 0;
            var terminate = 0;
            var afterterminate = 0;
            var subscribe = 0;
            var dispose = 0;
            var final = 0;
        }
    }
}
