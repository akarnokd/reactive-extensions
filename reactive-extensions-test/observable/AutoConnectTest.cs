using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;
using System.Reactive.Linq;
using System.Collections.Generic;
using System.Reactive.Subjects;

namespace akarnokd.reactive_extensions_test.observable
{
    [TestFixture]
    public class AutoConnectTest
    {
        [Test]
        public void AutoConnect_Basic()
        {
            int called = 0;

            var source = Observable.Defer(() =>
            {
                called++;
                return Observable.Range(1, 5);
            })
            .Replay()
            .AutoConnect();

            Assert.AreEqual(0, called);

            var list = source.ToList().Wait();

            Assert.AreEqual(1, called);
            Assert.AreEqual(new List<int>() { 1, 2, 3, 4, 5 }, list);

            list = source.ToList().Wait();

            Assert.AreEqual(1, called);
            Assert.AreEqual(new List<int>() { 1, 2, 3, 4, 5 }, list);
        }

        [Test]
        public void AutoConnect_Immediately()
        {
            int called = 0;

            var source = Observable.Defer(() =>
            {
                called++;
                return Observable.Range(1, 5);
            })
            .Replay()
            .AutoConnect(0);

            Assert.AreEqual(1, called);

            var list = source.ToList().Wait();

            Assert.AreEqual(1, called);
            Assert.AreEqual(new List<int>() { 1, 2, 3, 4, 5 }, list);

            list = source.ToList().Wait();

            Assert.AreEqual(1, called);
            Assert.AreEqual(new List<int>() { 1, 2, 3, 4, 5 }, list);
        }

        [Test]
        public void AutoConnect_TwoConsumers()
        {
            int called = 0;

            var source = Observable.Defer(() =>
            {
                called++;
                return Observable.Range(1, 5);
            })
            .Replay()
            .AutoConnect(2);

            Assert.AreEqual(0, called);

            var list0 = new List<int>();

            source.Subscribe(v => list0.Add(v));

            Assert.AreEqual(0, called);
            Assert.AreEqual(0, list0.Count);

            var list = source.ToList().Wait();

            Assert.AreEqual(1, called);
            Assert.AreEqual(new List<int>() { 1, 2, 3, 4, 5 }, list);

            Assert.AreEqual(new List<int>() { 1, 2, 3, 4, 5 }, list0);

            list = source.ToList().Wait();

            Assert.AreEqual(1, called);
            Assert.AreEqual(new List<int>() { 1, 2, 3, 4, 5 }, list);
        }

        [Test]
        public void AutoConnect_Dispose()
        {
            var subject = new Subject<int>();

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
