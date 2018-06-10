using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using static akarnokd.reactive_extensions.ValidationHelper;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceJoin<TLeft, TRight, TLeftEnd, TRightEnd, TResult> : IObservableSource<TResult>
    {
        readonly IObservableSource<TLeft> left;

        readonly IObservableSource<TRight> right;

        readonly Func<TLeft, IObservableSource<TLeftEnd>> leftSelector;

        readonly Func<TRight, IObservableSource<TRightEnd>> rightSelector;

        readonly Func<TLeft, TRight, TResult> resultSelector;

        public ObservableSourceJoin(IObservableSource<TLeft> left, IObservableSource<TRight> right, Func<TLeft, IObservableSource<TLeftEnd>> leftSelector, Func<TRight, IObservableSource<TRightEnd>> rightSelector, Func<TLeft, TRight, TResult> resultSelector)
        {
            this.left = left;
            this.right = right;
            this.leftSelector = leftSelector;
            this.rightSelector = rightSelector;
            this.resultSelector = resultSelector;
        }

        public void Subscribe(ISignalObserver<TResult> observer)
        {
            var parent = new JoinCoordinator(observer, leftSelector, rightSelector, resultSelector);
            observer.OnSubscribe(parent);

            left.Subscribe(parent.leftObserver);
            right.Subscribe(parent.rightObserver);
        }

        sealed class JoinCoordinator : IDisposable
        {
            readonly ISignalObserver<TResult> downstream;

            readonly Func<TLeft, IObservableSource<TLeftEnd>> leftSelector;

            readonly Func<TRight, IObservableSource<TRightEnd>> rightSelector;

            readonly Func<TLeft, TRight, TResult> resultSelector;

            internal readonly LeftObserver leftObserver;

            internal readonly RightObserver rightObserver;

            readonly Dictionary<long, (TLeft item, LeftEndObserver observer)> leftDictionary;
            readonly Dictionary<long, (TRight item, RightEndObserver observer)> rightDictionary;

            readonly ConcurrentQueue<(TLeft leftValue, TRight rightValue, bool hasLeft, long closeId)> queue;
            
            int wip;

            Exception error;

            bool leftDone;
            bool rightDone;

            long id;

            bool disposed;

            public JoinCoordinator(ISignalObserver<TResult> downstream,
                Func<TLeft, IObservableSource<TLeftEnd>> leftSelector,
                Func<TRight, IObservableSource<TRightEnd>> rightSelector,
                Func<TLeft, TRight, TResult> resultSelector)
            {
                this.downstream = downstream;
                this.leftSelector = leftSelector;
                this.rightSelector = rightSelector;
                this.resultSelector = resultSelector;
                this.leftObserver = new LeftObserver(this);
                this.rightObserver = new RightObserver(this);
                this.leftDictionary = new Dictionary<long, (TLeft item, LeftEndObserver observer)>();
                this.rightDictionary = new Dictionary<long, (TRight item, RightEndObserver observer)>();
                this.queue = new ConcurrentQueue<(TLeft leftValue, TRight rightValue, bool hasLeft, long closeId)>();
            }

            public void Dispose()
            {
                Volatile.Write(ref disposed, true);
                leftObserver.Dispose();
                rightObserver.Dispose();
                Drain();
            }

            void LeftItem(TLeft item)
            {
                queue.Enqueue((item, default, true, 0L));
                Drain();
            }

            void LeftCompleted()
            {
                Volatile.Write(ref leftDone, true);
                Drain();
            }

            void LeftError(Exception ex)
            {
                rightObserver.Dispose();
                Interlocked.CompareExchange(ref error, ex, null);
                Drain();
            }

            void RightItem(TRight item)
            {
                queue.Enqueue((default, item, false, 0L));
                Drain();
            }

            void RightCompleted()
            {
                Volatile.Write(ref rightDone, true);
                Drain();
            }

            void RightError(Exception ex)
            {
                leftObserver.Dispose();
                Interlocked.CompareExchange(ref error, ex, null);
                Drain();
            }

            void LeftClose(long id)
            {
                queue.Enqueue((default, default, default, id));
                Drain();
            }

            void LeftCloseError(Exception ex)
            {
                leftObserver.Dispose();
                rightObserver.Dispose();
                Interlocked.CompareExchange(ref error, ex, null);
                Drain();
            }

            void RightClose(long id)
            {
                queue.Enqueue((default, default, default, id));
                Drain();
            }

            void RightCloseError(Exception ex)
            {
                leftObserver.Dispose();
                rightObserver.Dispose();
                Interlocked.CompareExchange(ref error, ex, null);
                Drain();
            }

            void Drain()
            {
                if (Interlocked.Increment(ref wip) != 1)
                {
                    return;
                }

                var missed = 1;
                var downstream = this.downstream;
                var leftDictionary = this.leftDictionary;
                var rightDictionary = this.rightDictionary;

                for (; ; )
                {
                    if (Volatile.Read(ref disposed))
                    {
                        foreach (var e in leftDictionary)
                        {
                            e.Value.observer.Dispose();
                        }
                        leftDictionary.Clear();

                        foreach (var e in rightDictionary)
                        {
                            e.Value.observer.Dispose();
                        }
                        rightDictionary.Clear();
                    }
                    else
                    {
                        var ex = Volatile.Read(ref error);
                        if (ex != null)
                        {
                            downstream.OnError(ex);
                            Volatile.Write(ref disposed, true);
                            continue;
                        }


                        var doneLeft = Volatile.Read(ref leftDone);
                        var doneRight = Volatile.Read(ref rightDone);

                        var success = queue.TryDequeue(out var entry);

                        if (doneLeft && doneRight && !success)
                        {
                            downstream.OnCompleted();
                            Volatile.Write(ref disposed, true);
                            continue;
                        }
                        else
                        if (success)
                        {
                            var closeId = entry.closeId;
                            if (closeId == 0)
                            {
                                if (entry.hasLeft)
                                {
                                    var v = entry.leftValue;

                                    var src = default(IObservableSource<TLeftEnd>);

                                    try
                                    {
                                        src = RequireNonNullRef(leftSelector(v), "The leftSelector returned a null IObservableSource");
                                    }
                                    catch (Exception exc)
                                    {
                                        leftObserver.Dispose();
                                        rightObserver.Dispose();
                                        Interlocked.CompareExchange(ref error, exc, null);
                                        continue;
                                    }

                                    long openId = -(++id);
                                    var endObserver = new LeftEndObserver(this, openId);
                                    leftDictionary.Add(openId, (v, endObserver));
                                    src.Subscribe(endObserver);

                                    foreach (var right in rightDictionary)
                                    {
                                        if (Volatile.Read(ref disposed) || Volatile.Read(ref error) != null)
                                        {
                                            break;
                                        }

                                        var w = default(TResult);

                                        try
                                        {
                                            w = resultSelector(v, right.Value.item);
                                        }
                                        catch (Exception exc)
                                        {
                                            leftObserver.Dispose();
                                            rightObserver.Dispose();
                                            Interlocked.CompareExchange(ref error, exc, null);
                                            break;
                                        }

                                        downstream.OnNext(w);
                                    }
                                }
                                else
                                {
                                    var v = entry.rightValue;

                                    var src = default(IObservableSource<TRightEnd>);

                                    try
                                    {
                                        src = RequireNonNullRef(rightSelector(v), "The rightSelector returned a null IObservableSource");
                                    }
                                    catch (Exception exc)
                                    {
                                        leftObserver.Dispose();
                                        rightObserver.Dispose();
                                        Interlocked.CompareExchange(ref error, exc, null);
                                        continue;
                                    }

                                    long openId = (++id);
                                    var endObserver = new RightEndObserver(this, openId);
                                    rightDictionary.Add(openId, (v, endObserver));
                                    src.Subscribe(endObserver);

                                    foreach (var left in leftDictionary)
                                    {
                                        if (Volatile.Read(ref disposed) || Volatile.Read(ref error) != null)
                                        {
                                            break;
                                        }

                                        var w = default(TResult);

                                        try
                                        {
                                            w = resultSelector(left.Value.item, v);
                                        }
                                        catch (Exception exc)
                                        {
                                            leftObserver.Dispose();
                                            rightObserver.Dispose();
                                            Interlocked.CompareExchange(ref error, exc, null);
                                            break;
                                        }

                                        downstream.OnNext(w);
                                    }
                                }
                            }
                            else
                            {
                                if (closeId < 0)
                                {
                                    leftDictionary.Remove(closeId);
                                }
                                else
                                {
                                    rightDictionary.Remove(closeId);
                                }
                            }

                            continue;
                        }
                    }

                    missed = Interlocked.Add(ref wip, -missed);
                    if (missed == 0)
                    {
                        break;
                    }
                }
            }

            internal sealed class LeftObserver : ISignalObserver<TLeft>, IDisposable
            {
                readonly JoinCoordinator parent;

                IDisposable upstream;

                public LeftObserver(JoinCoordinator parent)
                {
                    this.parent = parent;
                }

                public void Dispose()
                {
                    DisposableHelper.Dispose(ref upstream);
                }

                public void OnCompleted()
                {
                    parent.LeftCompleted();
                }

                public void OnError(Exception ex)
                {
                    parent.LeftError(ex);
                }

                public void OnNext(TLeft item)
                {
                    parent.LeftItem(item);
                }

                public void OnSubscribe(IDisposable d)
                {
                    DisposableHelper.SetOnce(ref upstream, d);
                }
            }

            internal sealed class RightObserver : ISignalObserver<TRight>, IDisposable
            {
                readonly JoinCoordinator parent;

                IDisposable upstream;

                public RightObserver(JoinCoordinator parent)
                {
                    this.parent = parent;
                }

                public void Dispose()
                {
                    DisposableHelper.Dispose(ref upstream);
                }

                public void OnCompleted()
                {
                    parent.RightCompleted();
                }

                public void OnError(Exception ex)
                {
                    parent.RightError(ex);
                }

                public void OnNext(TRight item)
                {
                    parent.RightItem(item);
                }

                public void OnSubscribe(IDisposable d)
                {
                    DisposableHelper.SetOnce(ref upstream, d);
                }
            }

            sealed class LeftEndObserver : ISignalObserver<TLeftEnd>, IDisposable
            {
                readonly JoinCoordinator parent;

                readonly long id;

                IDisposable upstream;

                public LeftEndObserver(JoinCoordinator parent, long id)
                {
                    this.parent = parent;
                    this.id = id;
                }

                public void Dispose()
                {
                    DisposableHelper.Dispose(ref upstream);
                }

                public void OnCompleted()
                {
                    if (!DisposableHelper.IsDisposed(ref upstream))
                    {
                        parent.LeftClose(id);
                    }
                }

                public void OnError(Exception ex)
                {
                    if (!DisposableHelper.IsDisposed(ref upstream))
                    {
                        parent.LeftCloseError(ex);
                    }
                }

                public void OnNext(TLeftEnd item)
                {
                    if (DisposableHelper.Dispose(ref upstream))
                    {
                        parent.LeftClose(id);
                    }
                }

                public void OnSubscribe(IDisposable d)
                {
                    DisposableHelper.SetOnce(ref upstream, d);
                }
            }

            sealed class RightEndObserver : ISignalObserver<TRightEnd>, IDisposable
            {
                readonly JoinCoordinator parent;

                readonly long id;

                IDisposable upstream;

                public RightEndObserver(JoinCoordinator parent, long id)
                {
                    this.parent = parent;
                    this.id = id;
                }

                public void Dispose()
                {
                    DisposableHelper.Dispose(ref upstream);
                }

                public void OnCompleted()
                {
                    if (!DisposableHelper.IsDisposed(ref upstream))
                    {
                        parent.RightClose(id);
                    }
                }

                public void OnError(Exception ex)
                {
                    if (!DisposableHelper.IsDisposed(ref upstream))
                    {
                        parent.RightCloseError(ex);
                    }
                }

                public void OnNext(TRightEnd item)
                {
                    if (DisposableHelper.Dispose(ref upstream))
                    {
                        parent.RightClose(id);
                    }
                }

                public void OnSubscribe(IDisposable d)
                {
                    DisposableHelper.SetOnce(ref upstream, d);
                }
            }

        }
    }
}
