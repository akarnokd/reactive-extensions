﻿using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Makes sure downstream exceptions are suppressed.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <remarks>Since 0.0.11</remarks>
    internal sealed class SingleSafeObserver<T> : ISingleObserver<T>, IDisposable
    {
        readonly ISingleObserver<T> downstream;

        IDisposable upstream;

        public SingleSafeObserver(ISingleObserver<T> downstream)
        {
            this.downstream = downstream;
        }

        public void Dispose()
        {
            upstream.Dispose();
        }

        public void OnError(Exception error)
        {
            try
            {
                downstream.OnError(error);
            }
            catch (Exception)
            {
                // TODO what should happen with these?
            }
        }

        public void OnSubscribe(IDisposable d)
        {
            upstream = d;
            try
            {
                downstream.OnSubscribe(this);
            }
            catch (Exception)
            {
                d.Dispose();
                // TODO what should happen with these?
            }
        }

        public void OnSuccess(T item)
        {
            try
            {
                downstream.OnSuccess(item);
            }
            catch (Exception)
            {
                // TODO what should happen with these?
            }
}
    }
}
