using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Tests the upstream's success value via a predicate
    /// and relays it if the predicate returns false,
    /// completes the downstream otherwise.
    /// </summary>
    /// <typeparam name="T">The upstream value type.</typeparam>
    /// <remarks>Since 0.0.11</remarks>
    internal sealed class SingleFilter<T> : IMaybeSource<T>
    {
        readonly ISingleSource<T> source;

        readonly Func<T, bool> predicate;

        public SingleFilter(ISingleSource<T> source, Func<T, bool> predicate)
        {
            this.source = source;
            this.predicate = predicate;
        }

        public void Subscribe(IMaybeObserver<T> observer)
        {
            source.Subscribe(new FilterObserver<T>(observer, predicate));
        }
    }

}
