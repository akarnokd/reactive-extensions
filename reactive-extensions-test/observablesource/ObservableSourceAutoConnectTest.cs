using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Collections.Generic;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceAutoConnectTest
    {
        [Test]
        public void AutoConnect_Basic()
        {
            int called = 0;

            var source = ObservableSource.Defer(() =>
            {
                called++;
                return ObservableSource.Range(1, 5);
            })
            .Replay()
            .AutoConnect();

            Assert.AreEqual(0, called);

            var list = source.Test();

            Assert.AreEqual(1, called);
            list.AssertResult(1, 2, 3, 4, 5);

            list = source.Test();

            Assert.AreEqual(1, called);
            list.AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void AutoConnect_Immediately()
        {
            int called = 0;

            var source = ObservableSource.Defer(() =>
            {
                called++;
                return ObservableSource.Range(1, 5);
            })
            .Replay()
            .AutoConnect(0);

            Assert.AreEqual(1, called);

            var list = source.Test();

            Assert.AreEqual(1, called);
            list.AssertResult(1, 2, 3, 4, 5);

            list = source.Test();

            Assert.AreEqual(1, called);
            list.AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void AutoConnect_TwoConsumers()
        {
            int called = 0;

            var source = ObservableSource.Defer(() =>
            {
                called++;
                return ObservableSource.Range(1, 5);
            })
            .Replay()
            .AutoConnect(2);

            Assert.AreEqual(0, called);

            var list0 = new List<int>();

            source.Subscribe(v => list0.Add(v));

            Assert.AreEqual(0, called);
            Assert.AreEqual(0, list0.Count);

            var list = source.Test();

            Assert.AreEqual(1, called);
            list.AssertResult(1, 2, 3, 4, 5);

            Assert.AreEqual(new List<int>() { 1, 2, 3, 4, 5 }, list0);

            list = source.Test();

            Assert.AreEqual(1, called);
            list.AssertResult(1, 2, 3, 4, 5);
        }

        [Test]
        public void AutoConnect_Dispose()
        {
            var subject = new PublishSubject<int>();

            var disposable = new IDisposable[1];

            var source = subject
            .Replay()
            .AutoConnect(1, d => disposable[0] = d);

            Assert.Null(disposable[0]);

            var list = new List<int>();

            source.Subscribe(v => list.Add(v));

            Assert.NotNull(disposable[0]);

            subject.OnNext(1);
            subject.OnNext(2);
            subject.OnNext(3);

            disposable[0].Dispose();

            subject.OnNext(4);
            subject.OnNext(5);

            Assert.AreEqual(new List<int>() { 1, 2, 3 }, list);

        }
    }
}
