using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Standard state-peeking, thread-safe methods for all subject types.
    /// </summary>
    /// <remarks>Since 0.0.6</remarks>
    public interface ISubjectExtensions
    {
        /// <summary>
        /// Returns true if there is at least one observer currently subscribed to
        /// this ISubject.
        /// </summary>
        /// <returns>True if there is an observer currently subscribed.</returns>
        bool HasObserver();

        /// <summary>
        /// Returns true if this subject terminated normally.
        /// </summary>
        /// <returns>True if this subject terminated normally.</returns>
        bool HasCompleted();

        /// <summary>
        /// Returns true if this subject terminated with an error.
        /// </summary>
        /// <returns>True if this subject terminated with an error.</returns>
        bool HasException();

        /// <summary>
        /// Returns the terminal exception, if any.
        /// </summary>
        /// <returns>The terminal exception or null if the subject has not yet terminated or not with an error.</returns>
        Exception GetException();
    }
}
