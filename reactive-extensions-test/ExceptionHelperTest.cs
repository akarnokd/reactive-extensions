using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test
{
    [TestFixture]
    public class ExceptionHelperTest
    {
        Exception error;

        [Test]
        public void Terminated_Add()
        {
            ExceptionHelper.Terminate(ref error);
            Assert.False(ExceptionHelper.AddException(ref error, new InvalidOperationException()));
        }

        [Test]
        public void Add_Aggregate()
        {
            Assert.True(ExceptionHelper.AddException(ref error, new InvalidOperationException("first")));
            Assert.True(ExceptionHelper.AddException(ref error, new InvalidOperationException("second")));

            if (error is AggregateException a)
            {
                Assert.AreEqual("first", a.InnerExceptions[0].Message);
                Assert.AreEqual("second", a.InnerExceptions[1].Message);
            }
            else
            {
                Assert.Fail("Not aggregated");
            }

            Assert.True(ExceptionHelper.AddException(ref error, new InvalidOperationException("third")));

            if (error is AggregateException b)
            {
                Assert.AreEqual("first", b.InnerExceptions[0].Message);
                Assert.AreEqual("second", b.InnerExceptions[1].Message);
                Assert.AreEqual("third", b.InnerExceptions[2].Message);
            }
            else
            {
                Assert.Fail("Not aggregated");
            }
        }

        [Test]
        public void Race()
        {
            for (int i = 0; i < TestHelper.RACE_LOOPS; i++)
            {
                TestHelper.Race(() => {
                    Assert.True(ExceptionHelper.AddException(ref error, new InvalidOperationException("first")));
                }, () => {
                    Assert.True(ExceptionHelper.AddException(ref error, new InvalidOperationException("second")));
                });

                if (error is AggregateException a)
                {

                    Assert.True(
                        (a.InnerExceptions[0].Message == "first" && a.InnerExceptions[1].Message == "second")
                        || (a.InnerExceptions[1].Message == "first" && a.InnerExceptions[0].Message == "second")
                    );
                }
                else
                {
                    Assert.Fail("Not aggregated");
                }
            }
        }
    }
}
