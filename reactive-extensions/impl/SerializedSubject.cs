using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Wraps an ISubject and serializes access to its OnXXX methods.
    /// </summary>
    /// <typeparam name="T">The upstream value type.</typeparam>
    internal sealed class SerializedSubject<T> : SerializedObserver<T>, ISubject<T>
    {
        readonly ISubject<T> subject;

        public SerializedSubject(ISubject<T> subject) : base(subject)
        {
            this.subject = subject;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return subject.Subscribe(observer);
        }
    }

    /// <summary>
    /// Wraps an ISubject and serializes access to its OnXXX methods.
    /// </summary>
    /// <typeparam name="T">The upstream value type.</typeparam>
    /// <typeparam name="R">The subject's output value type.</typeparam>
    internal sealed class SerializedSubject<T, R> : SerializedObserver<T>, ISubject<T, R>
    {
        readonly ISubject<T, R> subject;

        public SerializedSubject(ISubject<T, R> subject) : base(subject)
        {
            this.subject = subject;
        }

        public IDisposable Subscribe(IObserver<R> observer)
        {
            return subject.Subscribe(observer);
        }
    }
}
