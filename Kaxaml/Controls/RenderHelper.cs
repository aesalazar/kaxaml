using System;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Kaxaml.Controls;

public static class RenderHelper
{
    #region Static Methods

    public static BitmapSource? ElementToBitmap(FrameworkElement e)
    {
        if (e.ActualWidth == 0 || e.ActualHeight == 0) return null;
        return VisualToBitmap(e, (int)e.ActualWidth, (int)e.ActualHeight, null);
    }

    public static BitmapSource? ElementToBitmap(FrameworkElement e, GrayscaleParameters parameters)
    {
        if (e.ActualWidth == 0 || e.ActualHeight == 0) return null;
        return VisualToBitmap(e, (int)e.ActualWidth, (int)e.ActualHeight, parameters);
    }

    public static BitmapSource? ElementToGrayscaleBitmap(FrameworkElement e)
    {
        if (e.ActualWidth == 0 || e.ActualHeight == 0) return null;
        return VisualToBitmap(e, (int)e.ActualWidth, (int)e.ActualHeight, new GrayscaleParameters());
    }

    public static BitmapSource HwndToBitmap(IntPtr hwnd)
    {
        return HwndToBitmap(hwnd, null);
    }

    public static BitmapSource HwndToBitmap(IntPtr hwnd, GrayscaleParameters? parameters)
    {
        var dc = NativeMethods.GetWindowDC(hwnd);
        var memDc = NativeMethods.CreateCompatibleDC(dc);
        var l = 0;
        var t = 0;

        NativeMethods.Rect rct;
        NativeMethods.GetWindowRect(hwnd, out rct);

        var w = rct.Right - rct.Left;
        var h = rct.Bottom - rct.Top;

        var hbm = NativeMethods.CreateCompatibleBitmap(dc, w, h);
        var oldbm = NativeMethods.SelectObject(memDc, hbm);

        var b = NativeMethods.BitBlt(memDc, l, t, w, h, dc, l, t, 0x00CC0020);
        var src = Imaging.CreateBitmapSourceFromHBitmap(hbm, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        NativeMethods.SelectObject(memDc, oldbm);
        NativeMethods.DeleteObject(hbm);
        NativeMethods.DeleteDC(memDc);
        NativeMethods.ReleaseDC(hwnd, dc);

        if (parameters != null)
        {
            var width = (int)src.Width;
            var height = (int)src.Height;

            var pixels = new byte[width * height * 4];
            src.CopyPixels(pixels, width * 4, 0);

            for (var p = 0; p < pixels.Length; p += 4)
            {
                // compute grayscale
                var pixelvalue =
                    pixels[p + 0] * parameters.RedDistribution +
                    pixels[p + 1] * parameters.GreenDistribution +
                    pixels[p + 2] * parameters.BlueDistribution;

                // compute compression
                pixelvalue = pixelvalue * parameters.Compression + 256 * ((1 - parameters.Compression) / 2);

                // compute washout
                pixelvalue = Math.Min(256, pixelvalue + 256 * parameters.Washout);

                var v = (byte)pixelvalue;
                pixels[p + 0] = v;
                pixels[p + 1] = v;
                pixels[p + 2] = v;
            }

            return BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgr32, null, pixels, width * 4);
        }

        return src;
    }

    public static BitmapSource HwndToGrayscaleBitmap(IntPtr hwnd)
    {
        return HwndToBitmap(hwnd, new GrayscaleParameters());
    }

    public static BitmapSource VisualToBitmap(Visual e, int width, int height)
    {
        return VisualToBitmap(e, width, height, null);
    }

    public static BitmapSource VisualToBitmap(Visual e, int width, int height, GrayscaleParameters? parameters)
    {
        ArgumentNullException.ThrowIfNull(e);
        e.VerifyAccess();

        var src = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        src.Render(e);

        if (parameters != null)
        {
            var pixels = new byte[width * height * 4];
            src.CopyPixels(pixels, width * 4, 0);

            for (var p = 0; p < pixels.Length; p += 4)
            {
                // compute grayscale
                var pixelvalue =
                    pixels[p + 0] * parameters.RedDistribution +
                    pixels[p + 1] * parameters.GreenDistribution +
                    pixels[p + 2] * parameters.BlueDistribution;

                // compute compression
                pixelvalue = pixelvalue * parameters.Compression + 256 * ((1 - parameters.Compression) / 2);

                // compute washout
                pixelvalue = Math.Min(256, pixelvalue + 256 * parameters.Washout);

                var v = (byte)pixelvalue;
                pixels[p + 0] = v;
                pixels[p + 1] = v;
                pixels[p + 2] = v;
            }

            return BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgr32, null, pixels, width * 4);
        }

        return src;
    }

    public static string VisualToFile(Visual e, int width, int height, GrayscaleParameters parameters, string filename)
    {
        var src = VisualToBitmap(e, width, height, parameters);

        using (var fs = new FileStream(filename, FileMode.Create))
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(src));
            encoder.Save(fs);
        }

        return filename;
    }

    #endregion Static Methods
}