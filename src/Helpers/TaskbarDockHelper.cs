using System;
using System.Windows;

namespace Dockus.Helpers;

public static class TaskbarDockHelper
{
    public const double DockMargin = 0;

    public static void SnapToPosition(Window window, string dockPosition)
    {
        try { window.UpdateLayout(); } catch { }

        var workArea = SystemParameters.WorkArea;
        double winWidth = window.ActualWidth > 0 ? window.ActualWidth : (double.IsNaN(window.Width) ? 375 : window.Width);
        double winHeight = window.ActualHeight > 0 ? window.ActualHeight : (double.IsNaN(window.Height) ? 64 : window.Height);

        if (winWidth <= 0) winWidth = 375;
        if (winHeight <= 0) winHeight = 64;

        switch (dockPosition)
        {
            case "BottomRight":
                window.Left = Math.Max(workArea.Left, workArea.Right - winWidth - DockMargin);
                window.Top = Math.Max(workArea.Top, workArea.Bottom - winHeight - DockMargin);
                break;

            case "BottomCenter":
                window.Left = workArea.Left + (workArea.Width - winWidth) / 2.0;
                window.Top = Math.Max(workArea.Top, workArea.Bottom - winHeight - DockMargin);
                break;

            case "BottomLeft":
                window.Left = workArea.Left + DockMargin;
                window.Top = Math.Max(workArea.Top, workArea.Bottom - winHeight - DockMargin);
                break;

            case "Floating":
            default:
                if (window.Left < workArea.Left || window.Left > workArea.Right - 50 ||
                    window.Top < workArea.Top || window.Top > workArea.Bottom - 50)
                {
                    window.Left = Math.Max(workArea.Left, workArea.Right - winWidth - DockMargin);
                    window.Top = Math.Max(workArea.Top, workArea.Bottom - winHeight - DockMargin);
                }
                break;
        }
    }

    public static bool CheckAndAutoSnap(Window window, double snapThreshold = 30)
    {
        var workArea = SystemParameters.WorkArea;
        double winWidth = window.ActualWidth > 0 ? window.ActualWidth : window.Width;
        double winHeight = window.ActualHeight > 0 ? window.ActualHeight : window.Height;

        double distToBottom = Math.Abs(window.Top + winHeight - workArea.Bottom);

        if (distToBottom <= snapThreshold)
        {
            window.Top = workArea.Bottom - winHeight;

            double distToRight = Math.Abs(window.Left + winWidth - workArea.Right);
            double distToCenter = Math.Abs((window.Left + winWidth / 2.0) - (workArea.Left + workArea.Width / 2.0));
            double distToLeft = Math.Abs(window.Left - workArea.Left);

            if (distToRight < 45)
            {
                window.Left = workArea.Right - winWidth;
                return true;
            }
            if (distToCenter < 45)
            {
                window.Left = workArea.Left + (workArea.Width - winWidth) / 2.0;
                return true;
            }
            if (distToLeft < 45)
            {
                window.Left = workArea.Left;
                return true;
            }
            return true;
        }

        return false;
    }
}
