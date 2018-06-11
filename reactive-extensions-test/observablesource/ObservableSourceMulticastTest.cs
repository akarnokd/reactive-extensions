using NUnit.Framework;
using System;
using akarnokd.reactive_extensions;

namespace akarnokd.reactive_extensions_test.observablesource
{
    [TestFixture]
    public class ObservableSourceMulticastTest
    {
        [Test]
        public void PublishSubject_Basic()
        {
            var count = 0;

            ObservableSource.Range(1, 5)
                .DoOnSubscribe(s => count++)
                .Multicast(() => new PublishSubject<int>(),
                o => o.Take(3).Concat(o))
                .Test()
                .AssertResult(1, 2, 3, 4, 5);

            Assert.AreEqual(1, count);
        }

        [Test]
        public void PublishSubject_Independent_Flows()
        {
            var count = 0;
            var inner = 0;

            var source = ObservableSource.Range(1, 5)
                .DoOnSubscribe(s => count++)
                .Multicast(() => new PublishSubject<int>(),
                o => {
                    inner++;
                    return o.Take(3).Concat(o);
                });


            source.Test()
                .AssertResult(1, 2, 3, 4, 5);

            Assert.AreEqual(1, count);
            Assert.AreEqual(1, inner);

            source.Test()
                .AssertResult(1, 2, 3, 4, 5);

            Assert.AreEqual(2, count);
            Assert.AreEqual(2, inner);
        }

        [Test]
        public void PublishSubject_Factory_Crash()
        {
            var count = 0;

            ObservableSource.Range(1, 5)
                .DoOnSubscribe(s => count++)
                .Multicast<int, int>(() => throw new InvalidOperationException(),
                    o => o.Take(3).Concat(o))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(0, count);
        }

        [Test]
        public void PublishSubject_Handler_Crash()
        {
            var count = 0;

            ObservableSource.Range(1, 5)
                .DoOnSubscribe(s => count++)
                .Multicast<int, int>(() => new PublishSubject<int>(),
                    o => throw new InvalidOperationException())
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(0, count);
        }

        [Test]
        public void PublishSubject_Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .Multicast(() => new PublishSubject<int>(),
                    o => o
                )
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void PublishSubject_Dispose()
        {
            var subj = new PublishSubject<int>();
            var handler = new PublishSubject<int>();

            var to = subj.Publish(o => handler).Test();

            Assert.True(subj.HasObservers);
            Assert.True(handler.HasObservers);

            to.AssertEmpty();

            to.Dispose();

            Assert.False(subj.HasObservers);
            Assert.False(handler.HasObservers);
        }

        [Test]
        public void PublishSubject_Handler_Disconnected_Completes()
        {
            var subj = new PublishSubject<int>();
            var handler = new PublishSubject<int>();

            var to = subj.Publish(o => handler).Test();

            Assert.True(subj.HasObservers);
            Assert.True(handler.HasObservers);

            handler.OnCompleted();

            Assert.False(subj.HasObservers);
            Assert.False(handler.HasObservers);

            to.AssertResult();
        }

        [Test]
        public void PublishSubject_Handler_Disconnected_Error()
        {
            var subj = new PublishSubject<int>();
            var handler = new PublishSubject<int>();

            var to = subj.Publish(o => handler).Test();

            Assert.True(subj.HasObservers);
            Assert.True(handler.HasObservers);

            handler.OnError(new InvalidOperationException());

            Assert.False(subj.HasObservers);
            Assert.False(handler.HasObservers);

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void CacheSubject_Basic()
        {
            var count = 0;

            ObservableSource.Range(1, 5)
                .DoOnSubscribe(s => count++)
                .Multicast(() => new CacheSubject<int>(),
                o => o.Take(3).Concat(o))
                .Test()
                .AssertResult(1, 2, 3, 1, 2, 3, 4, 5);

            Assert.AreEqual(1, count);
        }

        [Test]
        public void CacheSubject_Independent_Flows()
        {
            var count = 0;
            var inner = 0;

            var source = ObservableSource.Range(1, 5)
                .DoOnSubscribe(s => count++)
                .Multicast(() => new CacheSubject<int>(),
                o => {
                    inner++;
                    return o.Take(3).Concat(o);
                });


            source.Test()
                .AssertResult(1, 2, 3, 1, 2, 3, 4, 5);

            Assert.AreEqual(1, count);
            Assert.AreEqual(1, inner);

            source.Test()
                .AssertResult(1, 2, 3, 1, 2, 3, 4, 5);

            Assert.AreEqual(2, count);
            Assert.AreEqual(2, inner);
        }

        [Test]
        public void CacheSubject_Factory_Crash()
        {
            var count = 0;

            ObservableSource.Range(1, 5)
                .DoOnSubscribe(s => count++)
                .Multicast<int, int>(() => throw new InvalidOperationException(),
                    o => o.Take(3).Concat(o))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(0, count);
        }

        [Test]
        public void CacheSubject_Handler_Crash()
        {
            var count = 0;

            ObservableSource.Range(1, 5)
                .DoOnSubscribe(s => count++)
                .Multicast<int, int>(() => new CacheSubject<int>(),
                    o => throw new InvalidOperationException())
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(0, count);
        }

        [Test]
        public void CacheSubject_Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .Multicast(() => new CacheSubject<int>(),
                    o => o
                )
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void CacheSubject_Dispose()
        {
            var subj = new PublishSubject<int>();
            var handler = new PublishSubject<int>();

            var to = subj.Replay(o => handler).Test();

            Assert.True(subj.HasObservers);
            Assert.True(handler.HasObservers);

            to.AssertEmpty();

            to.Dispose();

            Assert.False(subj.HasObservers);
            Assert.False(handler.HasObservers);
        }

        [Test]
        public void CacheSubject_Handler_Disconnected_Completes()
        {
            var subj = new PublishSubject<int>();
            var handler = new PublishSubject<int>();

            var to = subj.Replay(o => handler).Test();

            Assert.True(subj.HasObservers);
            Assert.True(handler.HasObservers);

            handler.OnCompleted();

            Assert.False(subj.HasObservers);
            Assert.False(handler.HasObservers);

            to.AssertResult();
        }

        [Test]
        public void CacheSubject_Handler_Disconnected_Error()
        {
            var subj = new PublishSubject<int>();
            var handler = new PublishSubject<int>();

            var to = subj.Replay(o => handler).Test();

            Assert.True(subj.HasObservers);
            Assert.True(handler.HasObservers);

            handler.OnError(new InvalidOperationException());

            Assert.False(subj.HasObservers);
            Assert.False(handler.HasObservers);

            to.AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void ConnectPublish_Basic()
        {
            var count = 0;

            ObservableSource.Range(1, 5)
                .DoOnSubscribe(s => count++)
                .Multicast(o => o.Publish(),
                o => o.Take(3).Concat(o))
                .Test()
                .AssertResult(1, 2, 3, 4, 5);

            Assert.AreEqual(1, count);
        }

        [Test]
        public void ConnectPublish_Independent_Flows()
        {
            var count = 0;
            var inner = 0;

            var source = ObservableSource.Range(1, 5)
                .DoOnSubscribe(s => count++)
                .Multicast(o => o.Publish(),
                o => {
                    inner++;
                    return o.Take(3).Concat(o);
                });


            source.Test()
                .AssertResult(1, 2, 3, 4, 5);

            Assert.AreEqual(1, count);
            Assert.AreEqual(1, inner);

            source.Test()
                .AssertResult(1, 2, 3, 4, 5);

            Assert.AreEqual(2, count);
            Assert.AreEqual(2, inner);
        }

        [Test]
        public void ConnectPublish_Factory_Crash()
        {
            var count = 0;

            ObservableSource.Range(1, 5)
                .DoOnSubscribe(s => count++)
                .Multicast<int, int, int>(o => throw new InvalidOperationException(),
                    o => o.Take(3).Concat(o))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(0, count);
        }

        [Test]
        public void ConnectPublish_Handler_Crash()
        {
            var count = 0;

            ObservableSource.Range(1, 5)
                .DoOnSubscribe(s => count++)
                .Multicast<int, int, int>(o => o.Publish(),
                    o => throw new InvalidOperationException())
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(0, count);
        }

        [Test]
        public void ConnectPublish_Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .Multicast(o => o.Publish(),
                    o => o
                )
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

        [Test]
        public void ReplayConnect_Basic()
        {
            var count = 0;

            ObservableSource.Range(1, 5)
                .DoOnSubscribe(s => count++)
                .Multicast(o => o.Replay(),
                o => o.Take(3).Concat(o))
                .Test()
                .AssertResult(1, 2, 3, 1, 2, 3, 4, 5);

            Assert.AreEqual(1, count);
        }

        [Test]
        public void ReplayConnect_Independent_Flows()
        {
            var count = 0;
            var inner = 0;

            var source = ObservableSource.Range(1, 5)
                .DoOnSubscribe(s => count++)
                .Multicast(o => o.Replay(),
                o => {
                    inner++;
                    return o.Take(3).Concat(o);
                });


            source.Test()
                .AssertResult(1, 2, 3, 1, 2, 3, 4, 5);

            Assert.AreEqual(1, count);
            Assert.AreEqual(1, inner);

            source.Test()
                .AssertResult(1, 2, 3, 1, 2, 3, 4, 5);

            Assert.AreEqual(2, count);
            Assert.AreEqual(2, inner);
        }

        [Test]
        public void ReplayConnect_Factory_Crash()
        {
            var count = 0;

            ObservableSource.Range(1, 5)
                .DoOnSubscribe(s => count++)
                .Multicast<int, int, int>(o => throw new InvalidOperationException(),
                    o => o.Take(3).Concat(o))
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(0, count);
        }

        [Test]
        public void ReplayConnect_Handler_Crash()
        {
            var count = 0;

            ObservableSource.Range(1, 5)
                .DoOnSubscribe(s => count++)
                .Multicast<int, int, int>(o => o.Replay(),
                    o => throw new InvalidOperationException())
                .Test()
                .AssertFailure(typeof(InvalidOperationException));

            Assert.AreEqual(0, count);
        }

        [Test]
        public void ReplayConnect_Error()
        {
            ObservableSource.Error<int>(new InvalidOperationException())
                .Multicast(o => o.Replay(),
                    o => o
                )
                .Test()
                .AssertFailure(typeof(InvalidOperationException));
        }

    }
}
