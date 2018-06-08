﻿using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Concurrency;
using System.Threading;

namespace akarnokd.reactive_extensions_test.completable
{
    [TestFixture]
    public class CompletableUnsubscribeOnTest
    {
        [Test]
        public void Basic()
        {
            var name = "";

            CompletableSource.Empty()
                .DoOnDispose(() => name = Thread.CurrentThread.Name)
                .UnsubscribeOn(NewThreadScheduler.Default)
                .Test()
                .AssertResult();

            Assert.AreEqual("", name);
        }

        [Test]
        public void Error()
        {
            var name = "";

            CompletableSource.Empty()
                .DoOnDispose(() => name = Thread.CurrentThread.Name)
                .UnsubscribeOn(NewThreadScheduler.Default)
                .Test()
                .AssertResult();

            Assert.AreEqual("", name);
        }

        [Test]
        public void Dispose()
        {
            var name = -1;
            var cdl = new CountdownEvent(1);

            CompletableSource.Never()
                .DoOnDispose(() =>
                {
                    name = Thread.CurrentThread.ManagedThreadId;
                    cdl.Signal();
                })
                .UnsubscribeOn(NewThreadScheduler.Default)
                .Test()
                .Dispose();

            Assert.True(cdl.Wait(5000));

            Assert.AreNotEqual(-1, name);
            Assert.AreNotEqual(Thread.CurrentThread.ManagedThreadId, name);
        }


    }
}
