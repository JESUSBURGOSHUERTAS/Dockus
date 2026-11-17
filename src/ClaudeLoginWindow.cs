using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Dockus.Services;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace Dockus;

/// <summary>
/// Ventana donde el usuario inicia sesión en claude.ai por su cuenta. Dockus no ve
/// ni guarda la contraseña: solo detecta cuándo la sesión queda activa y se cierra.
/// </summary>
public class ClaudeLoginWindow : Window
{
    private readonly ClaudeWebUsageService _service;
    private readonly WebView2 _web = new();
    private readonly TextBlock _hint = new()
    {
        Text = "Inicia sesión en claude.ai como siempre. La ventana se cierra sola cuando la sesión quede activa.",
        Foreground = new SolidColorBrush(Color.FromRgb(0xCB, 0xD5, 0xE1)),
        FontSize = 12,
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(12, 8, 12, 8)
    };
    private bool _checking;

    public ClaudeLoginWindow(ClaudeWebUsageService service)
    {
        _service = service;
        Title = "Iniciar sesión en claude.ai · Dockus";
        Width = 480;
        Height = 720;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new SolidColorBrush(Color.FromRgb(0x0F, 0x17, 0x2A));

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(_hint, 0);
        Grid.SetRow(_web, 1);
        grid.Children.Add(_hint);
        grid.Children.Add(_web);
        Content = grid;

        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var env = await _service.GetEnvironmentAsync();
            await _web.EnsureCoreWebView2Async(env);
            _web.CoreWebView2.Settings.AreDevToolsEnabled = false;
            _web.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
            _web.CoreWebView2.NewWindowRequested += (s, args) =>
            {
                // Ventanas emergentes de inicio de sesión (p. ej. Google) se abren dentro de esta misma vista.
                args.Handled = true;
                _web.CoreWebView2.Navigate(args.Uri);
            };
            _web.CoreWebView2.Navigate("https://claude.ai/login");
        }
        catch (Exception ex)
        {
            _hint.Text = "No se pudo abrir el navegador integrado (WebView2). " + ex.Message;
        }
    }

    private async void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (_checking) return;
        if (!Uri.TryCreate(_web.CoreWebView2.Source, UriKind.Absolute, out var uri)) return;
        if (!uri.Host.Equals("claude.ai", StringComparison.OrdinalIgnoreCase)) return;

        var path = uri.AbsolutePath.ToLowerInvariant();
        if (path.StartsWith("/login") || path.StartsWith("/magic-link") || path.StartsWith("/sso") || path.StartsWith("/oauth")) return;

        _checking = true;
        try
        {
            _hint.Text = "Comprobando la sesión…";
            if (await _service.CheckSignedInAsync())
            {
                DialogResult = true;
                Close();
                return;
            }
            _hint.Text = "Todavía no hay una sesión activa. Termina de iniciar sesión en claude.ai.";
        }
        finally
        {
            _checking = false;
        }
    }
}
