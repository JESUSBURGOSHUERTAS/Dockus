using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Dockus.Helpers;
using Dockus.Models;
using Dockus.Services;

namespace Dockus;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly ProviderManager _providerManager = new();
    private DispatcherTimer? _refreshTimer;
    private FileSystemWatcher? _antigravityWatcher;

    private TokenUsageSnapshot _usage = new();
    public TokenUsageSnapshot Usage
    {
        get => _usage;
        set
        {
            _usage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentProviderShortName));
            OnPropertyChanged(nameof(ShortConversationId));
            OnPropertyChanged(nameof(FormattedLastTurnCache));
            OnPropertyChanged(nameof(TodayBreakdownText));
        }
    }

    private AppSettings _settings = new();
    public AppSettings Settings
    {
        get => _settings;
        set
        {
            _settings = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(FormattedOpacity));
        }
    }

    private ObservableCollection<ConversationItem> _recentConversations = new();
    public ObservableCollection<ConversationItem> RecentConversations
    {
        get => _recentConversations;
        set
        {
            _recentConversations = value;
            OnPropertyChanged();
        }
    }

    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded != value)
            {
                _isExpanded = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ExpandIconText));
                Settings.IsExpanded = value;
                Settings.Save();
                AdjustPositionAfterResize();
            }
        }
    }

    public string ExpandIconText => IsExpanded ? "▲" : "▼";

    public string CurrentProviderShortName
    {
        get
        {
            return Settings.ActiveProviderId switch
            {
                "antigravity_gemini" => "GEMINI IDE",
                "gemini_api" => "GEMINI API",
                "openai" => "OPENAI",
                "claude" => "CLAUDE",
                "deepseek" => "DEEPSEEK",
                _ => "GEMINI"
            };
        }
    }

    public string ShortConversationId
    {
        get
        {
            if (string.IsNullOrEmpty(Usage.ConversationId)) return "n/a";
            return Usage.ConversationId.Length > 8 ? Usage.ConversationId[..8] : Usage.ConversationId;
        }
    }

    public string FormattedLastTurnCache => TokenUsageSnapshot.FormatNumber(Usage.LastTurnCacheTokens);

    public string TodayBreakdownText
    {
        get
        {
            var inStr = TokenUsageSnapshot.FormatNumber(Usage.TodayInputTokens);
            var outStr = TokenUsageSnapshot.FormatNumber(Usage.TodayOutputTokens);
            var cacheStr = TokenUsageSnapshot.FormatNumber(Usage.TodayCacheTokens);
            return $"In: {inStr} · Out: {outStr} · Cache: {cacheStr}";
        }
    }

    public string FormattedOpacity => $"{(int)(Settings.GlassOpacity * 100)}%";

    // Provider Radios Booleans
    public bool IsProviderAntigravity => Settings.ActiveProviderId == "antigravity_gemini";
    public bool IsProviderGeminiApi => Settings.ActiveProviderId == "gemini_api";
    public bool IsProviderOpenAi => Settings.ActiveProviderId == "openai";
    public bool IsProviderClaude => Settings.ActiveProviderId == "claude";
    public bool IsProviderDeepSeek => Settings.ActiveProviderId == "deepseek";

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        Settings = AppSettings.Load();
        _isExpanded = Settings.IsExpanded;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // 1. Enable Windows Acrylic / Mica Blur behind the window
            WindowBlurHelper.EnableBlur(this);

            // 2. Set Opacity & Topmost
            MainGlassBorder.Opacity = Settings.GlassOpacity;
            Topmost = Settings.AlwaysOnTop;

            // 3. Initial snap position to Windows Taskbar
            TaskbarDockHelper.SnapToPosition(this, Settings.DockPosition);

            // 4. Setup Watcher for Antigravity Transcripts for instant sync
            SetupAntigravityWatcher();

            // 5. Setup Dispatcher Timer for smooth periodic polling
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1.5)
            };
            _refreshTimer.Tick += async (s, ev) => await RefreshDataAsync();
            _refreshTimer.Start();

            // 6. Initial data load
            _ = RefreshDataAsync();
        }
        catch { }
    }

    private void SetupAntigravityWatcher()
    {
        try
        {
            var brainDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".gemini",
                "antigravity",
                "brain"
            );

            if (Directory.Exists(brainDir))
            {
                _antigravityWatcher = new FileSystemWatcher(brainDir)
                {
                    Filter = "*.jsonl",
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                    EnableRaisingEvents = true
                };

                _antigravityWatcher.Changed += (s, e) =>
                {
                    Dispatcher.InvokeAsync(async () => await RefreshDataAsync());
                };
            }
        }
        catch { }
    }

    private async Task RefreshDataAsync()
    {
        try
        {
            var usage = await _providerManager.GetActiveUsageAsync(Settings);
            Usage = usage;

            if (IsExpanded)
            {
                var recents = await _providerManager.GetRecentConversationsAsync(Settings.ActiveProviderId, 4);
                RecentConversations = new ObservableCollection<ConversationItem>(recents);
            }
        }
        catch { }
    }

    private void AdjustPositionAfterResize()
    {
        // When expanding or collapsing, recalculate Top so bottom anchor remains steady
        Dispatcher.InvokeAsync(() =>
        {
            UpdateLayout();
            if (Settings.DockPosition != "Floating")
            {
                TaskbarDockHelper.SnapToPosition(this, Settings.DockPosition);
            }
        }, DispatcherPriority.Render);
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (Settings.AutoSnapToTaskbar)
        {
            bool snapped = TaskbarDockHelper.CheckAndAutoSnap(this);
            if (snapped)
            {
                // Determine which dock area it was snapped to
                var workArea = SystemParameters.WorkArea;
                if (Math.Abs(Left + Width - (workArea.Right - TaskbarDockHelper.DockMargin - 16)) < 40)
                    Settings.DockPosition = "BottomRight";
                else if (Math.Abs(Left + Width / 2.0 - (workArea.Left + workArea.Width / 2.0)) < 40)
                    Settings.DockPosition = "BottomCenter";
                else if (Math.Abs(Left - (workArea.Left + TaskbarDockHelper.DockMargin + 16)) < 40)
                    Settings.DockPosition = "BottomLeft";
            }
            else
            {
                Settings.DockPosition = "Floating";
                Settings.WindowLeft = Left;
                Settings.WindowTop = Top;
            }
            Settings.Save();
        }
    }

    private void BtnToggleExpand_Click(object sender, RoutedEventArgs e)
    {
        IsExpanded = !IsExpanded;
    }

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        await RefreshDataAsync();
    }

    private async void ProviderRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string providerId)
        {
            Settings.ActiveProviderId = providerId;
            Settings.Save();
            NotifyProviderProperties();
            await RefreshDataAsync();
        }
    }

    private void NotifyProviderProperties()
    {
        OnPropertyChanged(nameof(IsProviderAntigravity));
        OnPropertyChanged(nameof(IsProviderGeminiApi));
        OnPropertyChanged(nameof(IsProviderOpenAi));
        OnPropertyChanged(nameof(IsProviderClaude));
        OnPropertyChanged(nameof(IsProviderDeepSeek));
        OnPropertyChanged(nameof(CurrentProviderShortName));
    }

    private void BtnDock_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string pos)
        {
            DockTo(pos);
        }
    }

    private void DockTo(string dockPos)
    {
        Settings.DockPosition = dockPos;
        Settings.Save();
        TaskbarDockHelper.SnapToPosition(this, dockPos);
    }

    private void AlwaysOnTop_Changed(object sender, RoutedEventArgs e)
    {
        Topmost = Settings.AlwaysOnTop;
        Settings.Save();
    }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MainGlassBorder != null)
        {
            MainGlassBorder.Opacity = Settings.GlassOpacity;
            OnPropertyChanged(nameof(FormattedOpacity));
            Settings.Save();
        }
    }

    private async void BtnLimit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag && long.TryParse(tag, out var limit))
        {
            Settings.MaxContextTokens = limit;
            Settings.Save();
            await RefreshDataAsync();
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _refreshTimer?.Stop();
        _antigravityWatcher?.Dispose();
        base.OnClosed(e);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
