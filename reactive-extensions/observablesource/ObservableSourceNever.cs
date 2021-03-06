﻿using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    internal sealed class ObservableSourceNever<T> : IObservableSource<T>
    {
        internal static readonly IObservableSource<T> Instance = new ObservableSourceNever<T>();

        private ObservableSourceNever()
        {
            // singleton
        }

        public void Subscribe(ISignalObserver<T> observer)
        {
            observer.OnSubscribe(DisposableHelper.Empty<T>());
        }
    }
}
