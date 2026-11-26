using System;
using System.Runtime.InteropServices;
using Dockus.Models;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace Dockus.Services;

/// <summary>Forma del ícono: distingue cada IA sin depender del color.</summary>
public enum TrayShape
{
    Circle,   // Claude
    Hexagon   // Gemini
}

/// <summary>
/// Indicador de uso en la bandeja de la barra de tareas (junto al reloj), uno por IA.
/// El contorno (círculo o hexágono) se llena con el % USADO de la ventana elegida en Dockus
/// (⏱ 5h o 📅 7d). Colores:
/// azul &lt; 50 %, naranja 50-79 %, bermellón ≥ 80 %. Sin número, desde 80 % se añade un punto
/// en el centro para que el aviso no dependa solo del color. Gris sin datos.
/// </summary>
public sealed class UsageTrayIcon : IDisposable
{
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly string _title;
    private readonly TrayShape _shape;
    private readonly string _defaultEmptyHint;
    private IntPtr _currentHandle = IntPtr.Zero;
    private string _lastKey = string.Empty;

    /// <summary>Dibuja el % usado como número dentro del contorno (como el indicador de batería).</summary>
    public bool ShowNumber { get; set; } = true;

    public event Action? ShowRequested;
    public event Action? RefreshRequested;
    public event Action? ExitRequested;

    /// <param name="title">Nombre de la IA ("Claude", "Gemini").</param>
    /// <param name="shape">Forma del contorno.</param>
    /// <param name="defaultEmptyHint">Texto del tooltip cuando no hay datos.</param>
    public UsageTrayIcon(string title, TrayShape shape, string defaultEmptyHint)
    {
        _title = title;
        _shape = shape;
        _defaultEmptyHint = defaultEmptyHint;

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Mostrar Dockus", null, (s, e) => ShowRequested?.Invoke());
        menu.Items.Add($"Actualizar uso de {title}", null, (s, e) => RefreshRequested?.Invoke());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Salir", null, (s, e) => ExitRequested?.Invoke());

        _notifyIcon = new Forms.NotifyIcon
        {
            ContextMenuStrip = menu,
            Text = $"Dockus · {title}"
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
        var key = $"{used:F0}|{stale}|{ShowNumber}";
        if (key != _lastKey)
        {
            _lastKey = key;
            SetIcon(DrawRing(used, stale, ShowNumber, _shape));
        }

        _notifyIcon.Text = Truncate(BuildTooltip(plan, emptyHint, bucket), 127);
    }

    private string BuildTooltip(ClaudePlanUsage? plan, string? emptyHint, QuotaBucket? ringBucket)
    {
        if (plan == null || !plan.HasData)
            return $"Dockus · {_title}\n" + (emptyHint ?? _defaultEmptyHint);

        // ● marca la ventana que dibuja el anillo
        string Line(string name, QuotaBucket? b) => b == null
            ? $"  {name}: sin datos"
            : $"{(ReferenceEquals(b, ringBucket) ? "●" : "  ")} {name}: {b.FormattedUsed} usado · reinicia {b.FormattedCountdown}";

        var text = $"{_title} · {plan.Source}\n{Line("5 horas", plan.FiveHour)}\n{Line("Semana", plan.Weekly)}";
        if (plan.IsStale) text += $"\nDatos de {plan.FormattedAge}";
        return text;
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";

    // Colores de estado: azul, naranja y bermellón
    private static readonly Drawing.Color OkBlue = Drawing.Color.FromArgb(86, 180, 233);       // #56B4E9 azul cielo
    private static readonly Drawing.Color WarnOrange = Drawing.Color.FromArgb(230, 159, 0);    // #E69F00 naranja
    private static readonly Drawing.Color HighVermillion = Drawing.Color.FromArgb(213, 94, 0); // #D55E00 bermellón

    /// <summary>true si la barra de tareas usa tema claro (texto oscuro); por defecto, oscura.</summary>
    private static bool TaskbarIsLight()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("SystemUsesLightTheme") is int v && v == 1;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Contorno hexagonal (vértice arriba), empezando arriba y en sentido horario.</summary>
    private static Drawing.Drawing2D.GraphicsPath HexagonPath(Drawing.RectangleF r)
    {
        float cx = r.X + r.Width / 2f, cy = r.Y + r.Height / 2f;
        var pts = new Drawing.PointF[7];
        for (int i = 0; i <= 6; i++)
        {
            double a = (-90 + 60 * i) * Math.PI / 180.0;
            pts[i] = new Drawing.PointF(cx + r.Width / 2f * (float)Math.Cos(a), cy + r.Height / 2f * (float)Math.Sin(a));
        }
        var path = new Drawing.Drawing2D.GraphicsPath();
        path.AddLines(pts);
        return path;
    }

    /// <summary>Dibuja solo la fracción <paramref name="fraction"/> del contorno (barra de progreso).</summary>
    private static void DrawPartialPath(Drawing.Graphics g, Drawing.Drawing2D.GraphicsPath path, float fraction, Drawing.Pen pen)
    {
        using var flat = (Drawing.Drawing2D.GraphicsPath)path.Clone();
        flat.Flatten(null, 0.1f);
        var pts = flat.PathPoints;
        if (pts.Length < 2) return;

        static double Dist(Drawing.PointF a, Drawing.PointF b) =>
            Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

        double total = 0;
        for (int i = 1; i < pts.Length; i++) total += Dist(pts[i - 1], pts[i]);
        double target = total * Math.Clamp(fraction, 0f, 1f), acc = 0;

        var output = new System.Collections.Generic.List<Drawing.PointF> { pts[0] };
        for (int i = 1; i < pts.Length && acc < target; i++)
        {
            double d = Dist(pts[i - 1], pts[i]);
            if (acc + d <= target)
            {
                output.Add(pts[i]);
                acc += d;
            }
            else
            {
                double t = (target - acc) / d;
                output.Add(new Drawing.PointF(
                    pts[i - 1].X + (float)t * (pts[i].X - pts[i - 1].X),
                    pts[i - 1].Y + (float)t * (pts[i].Y - pts[i - 1].Y)));
                acc = target;
            }
        }
        if (output.Count > 1) g.DrawLines(pen, output.ToArray());
    }

    private static IntPtr DrawRing(double? usedPercent, bool stale, bool showNumber, TrayShape shape = TrayShape.Circle, int? sizeOverride = null)
    {
        // Tamaño del ícono pequeño según el DPI actual (16 px al 100 %, 20 px al 125 %, 24 px al 150 %).
        int size = sizeOverride ?? Math.Max(16, Forms.SystemInformation.SmallIconSize.Width);
        using var bmp = new Drawing.Bitmap(size, size, Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = Drawing.Graphics.FromImage(bmp);
        g.SmoothingMode = Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Drawing.Color.Transparent);

        // Con número, el anillo es más delgado para dejarle espacio a las cifras.
        float stroke = showNumber ? Math.Max(1.6f, size * 0.10f) : Math.Max(2.5f, size * 0.17f);
        float inset = stroke / 2f + 0.5f;
        var rect = new Drawing.RectangleF(inset, inset, size - inset * 2, size - inset * 2);

        using var hexagon = shape == TrayShape.Hexagon ? HexagonPath(rect) : null;

        // Distinción por silueta, no por color: Gemini es una ficha hexagonal RELLENA y Claude un
        // anillo HUECO. A 16-20 px, relleno contra hueco se reconoce mucho mejor que el contorno solo.
        if (hexagon != null)
        {
            using var fill = new Drawing.SolidBrush(Drawing.Color.FromArgb(255, 51, 65, 85)); // #334155 pizarra
            g.FillPath(fill, hexagon);
        }

        // Contorno de fondo bien visible en barra oscura o clara (antes casi no se veía).
        using (var track = new Drawing.Pen(Drawing.Color.FromArgb(200, 100, 116, 139), stroke) { LineJoin = Drawing.Drawing2D.LineJoin.Round })
        {
            if (hexagon != null) g.DrawPath(track, hexagon);
            else g.DrawEllipse(track, rect);
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
                    EndCap = Drawing.Drawing2D.LineCap.Round,
                    LineJoin = Drawing.Drawing2D.LineJoin.Round
                };
                if (hexagon != null)
                {
                    if (sweep >= 359.5f) g.DrawPath(pen, hexagon);
                    else DrawPartialPath(g, hexagon, (float)(used / 100.0), pen);
                }
                else if (sweep >= 359.5f) g.DrawEllipse(pen, rect);
                else g.DrawArc(pen, rect, -90f, sweep);
            }

            if (showNumber)
            {
                // El número ya avisa por sí solo (no depende del color).
                // Sobre la ficha rellena el número va siempre claro; en el anillo hueco depende de la barra.
                DrawCenteredNumber(g, size, stroke, used >= 99.5 ? "100" : used.ToString("0"), forceLight: hexagon != null);
            }
            else if (used >= 80)
            {
                // Señal de forma además del color: punto central cuando ya se usó 80 % o más.
                float dot = size * 0.30f;
                using var brush = new Drawing.SolidBrush(color);
                g.FillEllipse(brush, (size - dot) / 2f, (size - dot) / 2f, dot, dot);
            }
        }

        return bmp.GetHicon();
    }

    /// <summary>Dibuja las cifras lo más grandes posible dentro del anillo, con contraste según la barra.</summary>
    private static void DrawCenteredNumber(Drawing.Graphics g, int size, float stroke, string text, bool forceLight = false)
    {
        float inner = size - stroke * 2f - 1f;
        var textColor = !forceLight && TaskbarIsLight() ? Drawing.Color.FromArgb(15, 23, 42) : Drawing.Color.FromArgb(248, 250, 252);
        using var family = new Drawing.FontFamily("Segoe UI");

        // Se usa una ruta (no DrawString) para medir la tinta real y centrarla al píxel.
        float em = size * 0.75f;
        Drawing.Drawing2D.GraphicsPath? path = null;
        Drawing.RectangleF bounds = default;
        for (int i = 0; i < 20; i++)
        {
            path?.Dispose();
            path = new Drawing.Drawing2D.GraphicsPath();
            path.AddString(text, family, (int)Drawing.FontStyle.Bold, em, Drawing.PointF.Empty, Drawing.StringFormat.GenericTypographic);
            bounds = path.GetBounds();
            if (bounds.Width <= inner * 0.92f && bounds.Height <= inner * 0.72f) break;
            em *= 0.92f;
        }

        using (path)
        using (var m = new Drawing.Drawing2D.Matrix())
        {
            m.Translate(size / 2f - (bounds.X + bounds.Width / 2f), size / 2f - (bounds.Y + bounds.Height / 2f));
            path!.Transform(m);
            using var brush = new Drawing.SolidBrush(textColor);
            g.FillPath(brush, path);
        }
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
