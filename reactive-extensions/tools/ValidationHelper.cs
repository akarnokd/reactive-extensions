using System;
using System.Collections.Generic;
using System.Text;

namespace akarnokd.reactive_extensions
{
    /// <summary>
    /// Utility methods for operator parameter validations.
    /// </summary>
    /// <remarks>Since 0.0.5</remarks>
    internal static class ValidationHelper
    {

        /// <summary>
        /// Perform a null-check on an argument and throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        /// <typeparam name="X">Nullable classes only.</typeparam>
        /// <param name="reference">The target reference to check.</param>
        /// <param name="paramName">The name of the parameter in the original method.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="reference"/> is null.</exception>
        /// <remarks>Since 0.0.2</remarks>
        internal static void RequireNonNull<X>(X reference, string paramName) where X : class
        {
            if (reference == null)
            {
                throw new ArgumentNullException(paramName);
            }
        }

        /// <summary>
        /// Perform a null-check on a reference and throws an <see cref="NullReferenceException"/>.
        /// </summary>
        /// <typeparam name="X">Nullable classes only.</typeparam>
        /// <param name="reference">The target reference to check.</param>
        /// <param name="message">The error message to add.</param>
        /// <exception cref="NullReferenceException">If <paramref name="reference"/> is null.</exception>
        /// <remarks>Since 0.0.6</remarks>
        internal static X RequireNonNullRef<X>(X reference, string message) where X : class
        {
            if (reference == null)
            {
                throw new NullReferenceException(message);
            }
            return reference;
        }

        /// <summary>
        /// Verify the <paramref name="value"/> is positive.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="paramName">The name of the parameter in the original method.</param>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="value"/> is non-positive.</exception>
        /// <remarks>Since 0.0.2</remarks>
        internal static void RequirePositive(int value, string paramName)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(paramName, value, "Positive value required: " + value);
            }
        }

        /// <summary>
        /// Verify the <paramref name="value"/> is positive.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="paramName">The name of the parameter in the original method.</param>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="value"/> is non-positive.</exception>
        /// <remarks>Since 0.0.2</remarks>
        internal static void RequirePositive(long value, string paramName)
        {
            if (value <= 0L)
            {
                throw new ArgumentOutOfRangeException(paramName, value, "Positive value required: " + value);
            }
        }

        /// <summary>
        /// Verify the <paramref name="value"/> is non-negative.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="paramName">The name of the parameter in the original method.</param>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="value"/> is negative.</exception>
        /// <remarks>Since 0.0.6</remarks>
        internal static void RequireNonNegative(int value, string paramName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(paramName, value, "Non-negative value required: " + value);
            }
        }

        /// <summary>
        /// Verify the <paramref name="value"/> is non-negative.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="paramName">The name of the parameter in the original method.</param>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="value"/> is negative.</exception>
        /// <remarks>Since 0.0.6</remarks>
        internal static void RequireNonNegative(long value, string paramName)
        {
            if (value < 0L)
            {
                throw new ArgumentOutOfRangeException(paramName, value, "Non-negative value required: " + value);
            }
        }
    }
}
