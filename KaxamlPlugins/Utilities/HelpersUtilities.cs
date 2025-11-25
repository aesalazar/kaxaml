using System.Runtime.InteropServices;

namespace KaxamlPlugins.Utilities;

public static class HelpersUtilities
{
    /// <summary>
    ///     Returns true if the provided <see cref="Exception" /> is considered 'critical'
    /// </summary>
    /// <param name="exception">The <see cref="Exception" /> to evaluate for critical-ness.</param>
    /// <returns>true if the Exception is considered critical; otherwise, false.</returns>
    /// <remarks>
    ///     These exceptions are consider critical:
    ///     <list type="bullets">
    ///         <item>
    ///             <see cref="OutOfMemoryException" />
    ///         </item>
    ///         <item>
    ///             <see cref="StackOverflowException" />
    ///         </item>
    ///         <item>
    ///             <see cref="ThreadAbortException" />
    ///         </item>
    ///         <item>
    ///             <see cref="SEHException" />
    ///         </item>
    ///     </list>
    ///     Taken from:
    ///     https://github.com/thinkpixellab/bot/blob/0fe417e5f708a8887e07d1ab02f6711b307e6ac5/net40-client/Core/Util.cs#L138
    /// </remarks>
    public static bool IsCriticalException(this Exception? exception)
    {
        // Copied with respect from WPF WindowsBase->MS.Internal.CriticalExceptions.IsCriticalException
        // NullReferenceException, SecurityException --> not going to consider these critical
        while (exception != null)
        {
            if (exception is OutOfMemoryException or StackOverflowException or ThreadAbortException or SEHException)
                return true;

            exception = exception.InnerException;
        }

        return false;
    } //*** static IsCriticalException

    /// <summary>
    /// Casts nullable object to non-null when expected not to be null.
    /// </summary>
    /// <typeparam name="T">Type of source object when not null.</typeparam>
    /// <param name="value">Source object.</param>
    /// <param name="exceptionMessage">Message to include if it turns out to be null.</param>
    /// <returns>"Unboxed" object.</returns>
    /// <exception cref="NullReferenceException">Thrown if the object is, in fact, null.</exception>
    /// <remarks>
    /// This is a cleaner way of use the "bangs" (exclamation marks) when referencing
    /// an object that should not be null at the time.  If, for some reason it turns
    /// out to be null, it allows for a message to help with code readability.
    /// </remarks>
    public static T ShouldNotBeNull<T>(this T? value, string exceptionMessage) =>
        value is null
            ? throw new NullReferenceException(exceptionMessage)
            : (T)value;
}