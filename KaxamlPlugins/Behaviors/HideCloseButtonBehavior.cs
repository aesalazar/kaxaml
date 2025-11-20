using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Xaml.Behaviors;

namespace KaxamlPlugins.Behaviors;

/// <summary>
/// Hides the close button of a Window.
/// </summary>
/// <remarks>
/// Cheap way to get out of overriding the Window Chrome.
/// </remarks>
public class HideCloseButtonBehavior : Behavior<Window>
{
    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.Loaded += OnLoaded;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.Loaded -= OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var hwnd = new WindowInteropHelper(AssociatedObject).Handle;
        var style = GetWindowLong(hwnd, GWL_STYLE);
        SetWindowLong(hwnd, GWL_STYLE, style & ~WS_SYSMENU);
    }

    // ReSharper disable InconsistentNaming
    private const int GWL_STYLE = -16;

    private const int WS_SYSMENU = 0x80000;
    // ReSharper restore InconsistentNaming
}