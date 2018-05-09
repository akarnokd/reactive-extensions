using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    internal sealed class DoOnSubscribe<T> : IObservable<T>
    {
        readonly IObservable<T> source;

        readonly Action handler;

        internal DoOnSubscribe(IObservable<T> source, Action handler)
        {
            this.source = source;
            this.handler = handler;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            try
            {
                handler();
            }
            catch (Exception ex)
            {
                observer.OnError(ex);
                return DisposableHelper.EMPTY;
            }

            return source.Subscribe(observer);
        }
    }
}
