﻿using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceSkipWhile<T> : IObservableSource<T>
    {
        readonly IObservableSource<T> source;

        readonly Func<T, bool> predicate;

        public ObservableSourceSkipWhile(IObservableSource<T> source, Func<T, bool> predicate)
        {
            this.source = source;
            this.predicate = predicate;
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            source.Subscribe(new SkipWhileObserver(observer, predicate));
        }

        sealed class SkipWhileObserver : ISignalObserver<T>, IDisposable
        {
            readonly ISignalObserver<T> downstream;

            readonly Func<T, bool> predicate;

            IDisposable upstream;

            bool done;

            bool gate;

            public SkipWhileObserver(ISignalObserver<T> downstream, Func<T, bool> predicate)
            {
                this.downstream = downstream;
                this.predicate = predicate;
            }

            public void Dispose()
            {
                upstream.Dispose();
            }

            public void OnCompleted()
            {
                if (done)
                {
                    return;
                }
                done = true;
                downstream.OnCompleted();
            }

            public void OnError(Exception ex)
            {
                if (done)
                {
                    return;
                }
                done = true;
                downstream.OnError(ex);
            }

            public void OnNext(T item)
            {
                if (done)
                {
                    return;
                }

                if (gate)
                {
                    downstream.OnNext(item);
                }
                else
                {
                    var b = false;
                    try
                    {
                        b = predicate(item);
                    }
                    catch (Exception ex)
                    {
                        upstream.Dispose();
                        OnError(ex);
                        return;
                    }

                    if (!b)
                    {
                        gate = true;
                        downstream.OnNext(item);
                    }
                }
            }

            public void OnSubscribe(IDisposable d)
            {
                upstream = d;
                downstream.OnSubscribe(this);
            }
        }
    }
}
