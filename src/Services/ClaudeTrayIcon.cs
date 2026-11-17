using System;
using System.Runtime.InteropServices;
using Dockus.Models;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace Dockus.Services;

/// <summary>
/// Anillo de uso del plan de Claude en la bandeja de la barra de tareas (junto al reloj).
/// El anillo se llena con el % USADO de la ventana elegida en Dockus (⏱ 5h o 📅 7d).
/// Colores:
/// azul &lt; 50 %, naranja 50-79 %, bermellón ≥ 80 %. Desde 80 % se añade además un punto
/// en el centro, para que el aviso no dependa solo del color. Gris sin datos.
/// </summary>
public sealed class ClaudeTrayIcon : IDisposable
{
    private readonly Forms.NotifyIcon _notifyIcon;
    private IntPtr _currentHandle = IntPtr.Zero;
    private string _lastKey = string.Empty;

    public event Action? ShowRequested;
    public event Action? RefreshRequested;
    public event Action? ExitRequested;

    public ClaudeTrayIcon()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Mostrar Dockus", null, (s, e) => ShowRequested?.Invoke());
        menu.Items.Add("Actualizar uso de Claude", null, (s, e) => RefreshRequested?.Invoke());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Salir", null, (s, e) => ExitRequested?.Invoke());

        _notifyIcon = new Forms.NotifyIcon
        {
            ContextMenuStrip = menu,
            Text = "Dockus · Claude"
        };
        _notifyIcon.MouseClick += (s, e) =>
        {
            if (e.Button == Forms.MouseButtons.Left) ShowRequested?.Invoke();
        };

        Update(null);
        _notifyIcon.Visible = true;
    }

    /// <param name="showWeekly">true: el anillo muestra el límite semanal; false: la ventana de 5 horas
    /// (sigue el selector ⏱ 5h / 📅 7d de Dockus). Si esa ventana no tiene datos, usa la otra.</param>
    public void Update(ClaudePlanUsage? plan, string? emptyHint = null, bool showWeekly = false)
    {
        var bucket = showWeekly ? (plan?.Weekly ?? plan?.FiveHour) : (plan?.FiveHour ?? plan?.Weekly);
        double? used = bucket?.UsedPercentage;
        bool stale = plan?.IsStale == true;

        // Solo redibuja si cambió algo visible (evita crear íconos cada 2 s).
        var key = $"{used:F0}|{stale}";
        if (key != _lastKey)
        {
            _lastKey = key;
            SetIcon(DrawRing(used, stale));
        }

        _notifyIcon.Text = Truncate(BuildTooltip(plan, emptyHint, bucket), 127);
    }

    private static string BuildTooltip(ClaudePlanUsage? plan, string? emptyHint, QuotaBucket? ringBucket)
    {
        if (plan == null || !plan.HasData)
            return "Dockus · Claude\n" + (emptyHint ?? "Sin datos. Inicia sesión en claude.ai desde Dockus.");

        // ● marca la ventana que dibuja el anillo
        string Line(string name, QuotaBucket? b) => b == null
            ? $"  {name}: sin datos"
            : $"{(ReferenceEquals(b, ringBucket) ? "●" : "  ")} {name}: {b.FormattedUsed} usado · reinicia {b.FormattedCountdown}";

        var text = $"Claude · {plan.Source}\n{Line("5 horas", plan.FiveHour)}\n{Line("Semana", plan.Weekly)}";
        if (plan.IsStale) text += $"\nDatos de {plan.FormattedAge}";
        return text;
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";

    // Colores de estado: azul, naranja y bermellón
    private static readonly Drawing.Color OkBlue = Drawing.Color.FromArgb(86, 180, 233);       // #56B4E9 azul cielo
    private static readonly Drawing.Color WarnOrange = Drawing.Color.FromArgb(230, 159, 0);    // #E69F00 naranja
    private static readonly Drawing.Color HighVermillion = Drawing.Color.FromArgb(213, 94, 0); // #D55E00 bermellón

    private static IntPtr DrawRing(double? usedPercent, bool stale)
    {
        // Tamaño del ícono pequeño según el DPI actual (16 px al 100 %, 24 px al 150 %...).
        int size = Math.Max(16, Forms.SystemInformation.SmallIconSize.Width);
        using var bmp = new Drawing.Bitmap(size, size, Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = Drawing.Graphics.FromImage(bmp);
        g.SmoothingMode = Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Drawing.Color.Transparent);

        float stroke = Math.Max(2.5f, size * 0.17f);
        float inset = stroke / 2f + 0.5f;
        var rect = new Drawing.RectangleF(inset, inset, size - inset * 2, size - inset * 2);

        using (var track = new Drawing.Pen(Drawing.Color.FromArgb(110, 148, 163, 184), stroke))
        {
            g.DrawEllipse(track, rect);
        }

        if (usedPercent != null)
        {
            var used = Math.Clamp(usedPercent.Value, 0, 100);
            var color = used >= 80 ? HighVermillion : used >= 50 ? WarnOrange : OkBlue;
            if (stale) color = Drawing.Color.FromArgb(160, color);

            float sweep = (float)(used / 100.0 * 360.0);
            if (sweep > 0.5f)
            {
                using var pen = new Drawing.Pen(color, stroke)
                {
                    StartCap = Drawing.Drawing2D.LineCap.Round,
                    EndCap = Drawing.Drawing2D.LineCap.Round
                };
                if (sweep >= 359.5f) g.DrawEllipse(pen, rect);
                else g.DrawArc(pen, rect, -90f, sweep);
            }

            // Señal de forma además del color: punto central cuando ya se usó 80 % o más.
            if (used >= 80)
            {
                float dot = size * 0.30f;
                using var brush = new Drawing.SolidBrush(color);
                g.FillEllipse(brush, (size - dot) / 2f, (size - dot) / 2f, dot, dot);
            }
        }

        return bmp.GetHicon();
    }

    private void SetIcon(IntPtr handle)
    {
        var oldHandle = _currentHandle;
        var oldIcon = _notifyIcon.Icon;
        using (var icon = Drawing.Icon.FromHandle(handle))
        {
            // Clone: el NotifyIcon conserva su propia copia; los HICON anteriores se liberan abajo.
            _notifyIcon.Icon = (Drawing.Icon)icon.Clone();
        }
        _currentHandle = handle;
        oldIcon?.Dispose();
        if (oldHandle != IntPtr.Zero) DestroyIcon(oldHandle);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Icon?.Dispose();
        _notifyIcon.Dispose();
        if (_currentHandle != IntPtr.Zero) DestroyIcon(_currentHandle);
        _currentHandle = IntPtr.Zero;
    }
}
