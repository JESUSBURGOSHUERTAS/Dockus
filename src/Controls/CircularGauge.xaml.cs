using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Dockus.Controls;

public partial class CircularGauge : UserControl
{
    public static readonly DependencyProperty PercentageProperty =
        DependencyProperty.Register(nameof(Percentage), typeof(double), typeof(CircularGauge),
            new PropertyMetadata(100.0, OnPropertyChangedStatic));

    public static readonly DependencyProperty ValueTextProperty =
        DependencyProperty.Register(nameof(ValueText), typeof(string), typeof(CircularGauge),
            new PropertyMetadata(string.Empty, OnPropertyChangedStatic));

    public static readonly DependencyProperty LabelTextProperty =
        DependencyProperty.Register(nameof(LabelText), typeof(string), typeof(CircularGauge),
            new PropertyMetadata(string.Empty, OnPropertyChangedStatic));

    public static readonly DependencyProperty LabelBrushProperty =
        DependencyProperty.Register(nameof(LabelBrush), typeof(Brush), typeof(CircularGauge),
            new PropertyMetadata(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")), OnPropertyChangedStatic));

    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(nameof(Header), typeof(string), typeof(CircularGauge),
            new PropertyMetadata(string.Empty, OnPropertyChangedStatic));

    public static readonly DependencyProperty HeaderBrushProperty =
        DependencyProperty.Register(nameof(HeaderBrush), typeof(Brush), typeof(CircularGauge),
            new PropertyMetadata(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")), OnPropertyChangedStatic));

    public static readonly DependencyProperty StrokeBrushProperty =
        DependencyProperty.Register(nameof(StrokeBrush), typeof(Brush), typeof(CircularGauge),
            new PropertyMetadata(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")), OnPropertyChangedStatic));

    public double Percentage
    {
        get => (double)GetValue(PercentageProperty);
        set => SetValue(PercentageProperty, value);
    }

    public string ValueText
    {
        get => (string)GetValue(ValueTextProperty);
        set => SetValue(ValueTextProperty, value);
    }

    public string LabelText
    {
        get => (string)GetValue(LabelTextProperty);
        set => SetValue(LabelTextProperty, value);
    }

    public Brush LabelBrush
    {
        get => (Brush)GetValue(LabelBrushProperty);
        set => SetValue(LabelBrushProperty, value);
    }

    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public Brush HeaderBrush
    {
        get => (Brush)GetValue(HeaderBrushProperty);
        set => SetValue(HeaderBrushProperty, value);
    }

    public Brush StrokeBrush
    {
        get => (Brush)GetValue(StrokeBrushProperty);
        set => SetValue(StrokeBrushProperty, value);
    }

    public CircularGauge()
    {
        InitializeComponent();
        Loaded += (s, e) => UpdateGauge();
        SizeChanged += (s, e) => UpdateGauge();
    }

    private static void OnPropertyChangedStatic(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CircularGauge gauge)
        {
            gauge.UpdateGauge();
        }
    }

    public void UpdateGauge()
    {
        if (ArcPath == null || TxtValue == null || TxtLabel == null) return;

        double pct = Math.Clamp(Percentage, 0, 100);

        TxtValue.Text = !string.IsNullOrEmpty(ValueText) ? ValueText : $"{pct:F0}%";
        TxtLabel.Text = LabelText;
        TxtLabel.Foreground = LabelBrush ?? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));

        if (TxtHeader != null)
        {
            if (!string.IsNullOrWhiteSpace(Header))
            {
                TxtHeader.Text = Header;
                TxtHeader.Foreground = HeaderBrush ?? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
                TxtHeader.Visibility = Visibility.Visible;
            }
            else
            {
                TxtHeader.Visibility = Visibility.Collapsed;
            }
        }

        ArcPath.Stroke = StrokeBrush;

        const double size = 32.0;
        const double radius = 12.0;
        const double cx = size / 2.0; // 16.0
        const double cy = size / 2.0; // 16.0

        if (pct >= 99.5)
        {
            ArcPath.Data = new EllipseGeometry(new Point(cx, cy), radius, radius);
            return;
        }

        if (pct <= 0.5)
        {
            ArcPath.Data = null;
            return;
        }

        double angle = (pct / 100.0) * 360.0;
        double startAngleRad = -90.0 * Math.PI / 180.0;
        double endAngleRad = (angle - 90.0) * Math.PI / 180.0;

        Point startPoint = new Point(cx + radius * Math.Cos(startAngleRad), cy + radius * Math.Sin(startAngleRad));
        Point endPoint = new Point(cx + radius * Math.Cos(endAngleRad), cy + radius * Math.Sin(endAngleRad));
        bool isLargeArc = angle > 180.0;

        var figure = new PathFigure
        {
            StartPoint = startPoint,
            IsClosed = false
        };
        figure.Segments.Add(new ArcSegment
        {
            Point = endPoint,
            Size = new Size(radius, radius),
            SweepDirection = SweepDirection.Clockwise,
            IsLargeArc = isLargeArc
        });

        var geom = new PathGeometry();
        geom.Figures.Add(figure);
        ArcPath.Data = geom;
    }
}
