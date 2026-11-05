using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Dockus.Controls;

public class AeroParticlesCanvas : FrameworkElement
{
    private struct Particle
    {
        public double X;
        public double Y;
        public double Vx;
        public double Vy;
        public double Radius;
        public double Alpha;
        public double AlphaSpeed;
        public Color BaseColor;
    }

    private readonly List<Particle> _particles = new();
    private readonly Random _rand = new();
    private DateTime _lastRender = DateTime.Now;
    private bool _isRenderingHooked;

    public AeroParticlesCanvas()
    {
        IsHitTestVisible = false;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SizeChanged += OnSizeChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        InitParticles();
        if (!_isRenderingHooked)
        {
            CompositionTarget.Rendering += OnRendering;
            _isRenderingHooked = true;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_isRenderingHooked)
        {
            CompositionTarget.Rendering -= OnRendering;
            _isRenderingHooked = false;
        }
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_particles.Count == 0 && ActualWidth > 0 && ActualHeight > 0)
        {
            InitParticles();
        }
    }

    private void InitParticles()
    {
        _particles.Clear();
        int count = 16;
        double w = ActualWidth > 50 ? ActualWidth : 400;
        double h = ActualHeight > 20 ? ActualHeight : 64;

        Color[] colors = new[]
        {
            Color.FromArgb(140, 56, 189, 248),  // Neon Cyan
            Color.FromArgb(140, 192, 132, 252), // Electric Purple
            Color.FromArgb(140, 52, 211, 153),  // Emerald Mint
            Color.FromArgb(150, 248, 250, 252)  // Soft Crystal White
        };

        for (int i = 0; i < count; i++)
        {
            _particles.Add(new Particle
            {
                X = _rand.NextDouble() * w,
                Y = _rand.NextDouble() * h,
                Vx = (_rand.NextDouble() - 0.5) * 0.25,
                Vy = -(_rand.NextDouble() * 0.18 + 0.05), // Very gentle upward drift
                Radius = _rand.NextDouble() * 1.2 + 0.8,
                Alpha = _rand.NextDouble() * 0.22 + 0.08,
                AlphaSpeed = (_rand.NextDouble() * 0.01 + 0.003) * (_rand.Next(2) == 0 ? 1 : -1),
                BaseColor = colors[_rand.Next(colors.Length)]
            });
        }
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        var now = DateTime.Now;
        double dt = (now - _lastRender).TotalSeconds;
        if (dt < 0.030) return; // ~33 FPS cap for zero CPU usage
        _lastRender = now;

        double w = ActualWidth > 0 ? ActualWidth : 400;
        double h = ActualHeight > 0 ? ActualHeight : 64;

        for (int i = 0; i < _particles.Count; i++)
        {
            var p = _particles[i];
            p.X += p.Vx;
            p.Y += p.Vy;

            p.Alpha += p.AlphaSpeed;
            if (p.Alpha > 0.32)
            {
                p.Alpha = 0.32;
                p.AlphaSpeed = -Math.Abs(p.AlphaSpeed);
            }
            else if (p.Alpha < 0.05)
            {
                p.Alpha = 0.05;
                p.AlphaSpeed = Math.Abs(p.AlphaSpeed);
            }

            // Wrap around edges smoothly
            if (p.X < 0) p.X = w;
            if (p.X > w) p.X = 0;
            if (p.Y < 0)
            {
                p.Y = h;
                p.X = _rand.NextDouble() * w;
            }
            if (p.Y > h) p.Y = 0;

            _particles[i] = p;
        }

        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        foreach (var p in _particles)
        {
            byte a = (byte)Math.Clamp(p.Alpha * 255, 0, 255);
            var col = Color.FromArgb(a, p.BaseColor.R, p.BaseColor.G, p.BaseColor.B);
            var brush = new SolidColorBrush(col);
            brush.Freeze();

            dc.DrawEllipse(brush, null, new Point(p.X, p.Y), p.Radius, p.Radius);
        }
    }
}
