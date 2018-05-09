using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// An abstract observable that hosts an upstream source observable,
    /// specifies a default Subscribe() behavior and allows creating a
    /// BaseObserver via a method to be implemented by subclasses.
    /// </summary>
    /// <typeparam name="T">The upstream value type.</typeparam>
    /// <typeparam name="R">The downstream value type</typeparam>
    internal abstract class BaseObservable<T, R> : IObservable<R>
    {
        protected readonly IObservable<T> source;

        internal BaseObservable(IObservable<T> source)
        {
            this.source = source;
        }

        public virtual IDisposable Subscribe(IObserver<R> observer)
        {
            var parent = CreateObserver(observer);
            var d = source.Subscribe(parent);
            parent.OnSubscribe(d);
            return parent;
        }

        protected abstract BaseObserver<T, R> CreateObserver(IObserver<R> observer);
    }
}
