using System.Runtime.InteropServices;

namespace KaxamlPlugins;

public static class HelpersUtilities
{
    /// <summary>
    ///     Returns true if the provided <see cref="Exception" /> is considered 'critical'
    /// </summary>
    /// <param name="exception">The <see cref="Exception" /> to evaluate for critical-ness.</param>
    /// <returns>true if the Exception is conisdered critical; otherwise, false.</returns>
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
}