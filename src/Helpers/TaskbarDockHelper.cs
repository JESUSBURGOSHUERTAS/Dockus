using System;
using System.Windows;

namespace Dockus.Helpers;

public static class TaskbarDockHelper
{
    public const double DockMargin = 8;

    public static void SnapToPosition(Window window, string dockPosition)
    {
        try { window.UpdateLayout(); } catch { }

        var workArea = SystemParameters.WorkArea;
        double winWidth = window.ActualWidth > 0 ? window.ActualWidth : (double.IsNaN(window.Width) ? 500 : window.Width);
        double winHeight = window.ActualHeight > 0 ? window.ActualHeight : (double.IsNaN(window.Height) ? 74 : window.Height);

        if (winWidth <= 0) winWidth = 500;
        if (winHeight <= 0) winHeight = 74;

        switch (dockPosition)
        {
            case "BottomRight":
                window.Left = workArea.Right - winWidth - DockMargin - 16;
                window.Top = workArea.Bottom - winHeight - DockMargin;
                break;

            case "BottomCenter":
                window.Left = workArea.Left + (workArea.Width - winWidth) / 2.0;
                window.Top = workArea.Bottom - winHeight - DockMargin;
                break;

            case "BottomLeft":
                window.Left = workArea.Left + DockMargin + 16;
                window.Top = workArea.Bottom - winHeight - DockMargin;
                break;

            case "Floating":
            default:
                // Keep current or center if outside screen
                if (window.Left < workArea.Left || window.Left > workArea.Right - 50 ||
                    window.Top < workArea.Top || window.Top > workArea.Bottom - 50)
                {
                    window.Left = workArea.Right - winWidth - DockMargin - 16;
                    window.Top = workArea.Bottom - winHeight - DockMargin;
                }
                break;
        }
    }

    public static bool CheckAndAutoSnap(Window window, double snapThreshold = 35)
    {
        var workArea = SystemParameters.WorkArea;
        double winWidth = window.ActualWidth > 0 ? window.ActualWidth : window.Width;
        double winHeight = window.ActualHeight > 0 ? window.ActualHeight : window.Height;

        double distToBottom = Math.Abs(window.Top + winHeight - (workArea.Bottom - DockMargin));

        if (distToBottom <= snapThreshold)
        {
            window.Top = workArea.Bottom - winHeight - DockMargin;

            // Check if near right or center or left
            double distToRight = Math.Abs(window.Left + winWidth - (workArea.Right - DockMargin - 16));
            double distToCenter = Math.Abs((window.Left + winWidth / 2.0) - (workArea.Left + workArea.Width / 2.0));
            double distToLeft = Math.Abs(window.Left - (workArea.Left + DockMargin + 16));

            if (distToRight < 60)
            {
                window.Left = workArea.Right - winWidth - DockMargin - 16;
                return true;
            }
            if (distToCenter < 60)
            {
                window.Left = workArea.Left + (workArea.Width - winWidth) / 2.0;
                return true;
            }
            if (distToLeft < 60)
            {
                window.Left = workArea.Left + DockMargin + 16;
                return true;
            }
            return true;
        }

        return false;
    }
}

