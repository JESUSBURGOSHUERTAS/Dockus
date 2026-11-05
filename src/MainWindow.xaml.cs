using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    private readonly AntigravityQuotaService _quotaService = new();
    private DispatcherTimer? _refreshTimer;
    private int _tickCount = 0;

    private AntigravityQuotaSummary _quotas = new();
    public AntigravityQuotaSummary Quotas
    {
        get => _quotas;
        set
        {
            _quotas = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(GeminiWeeklyPercentage));
            OnPropertyChanged(nameof(GeminiFiveHourPercentage));
            OnPropertyChanged(nameof(ClaudeWeeklyPercentage));
            OnPropertyChanged(nameof(ArgonWeeklyPercentage));
            OnPropertyChanged(nameof(GeminiWeeklyText));
            OnPropertyChanged(nameof(GeminiFiveHourText));
            OnPropertyChanged(nameof(ClaudeWeeklyText));
            OnPropertyChanged(nameof(ArgonWeeklyText));
            OnPropertyChanged(nameof(GeminiWeeklyResetText));
            OnPropertyChanged(nameof(GeminiFiveHourResetText));
            OnPropertyChanged(nameof(GeminiWeeklyDesc));
        }
    }

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

    private bool _showQuotasView = true;
    public bool ShowQuotasView
    {
        get => _showQuotasView;
        set
        {
            if (_showQuotasView != value)
            {
                _showQuotasView = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowTokensView));
            }
        }
    }

    public bool ShowTokensView => !_showQuotasView;

    public string ExpandIconText => IsExpanded ? "▲" : "▼";

    public string CurrentProviderShortName => Settings.ActiveProviderId switch
    {
        "antigravity_gemini" => "GEMINI IDE",
        "gemini_api" => "GEMINI API",
        "openai" => "OPENAI",
        "claude" => "CLAUDE",
        "deepseek" => "DEEPSEEK",
        _ => "GEMINI"
    };

    public string ShortConversationId => string.IsNullOrEmpty(Usage.ConversationId)
        ? "n/a"
        : (Usage.ConversationId.Length > 8 ? Usage.ConversationId[..8] : Usage.ConversationId);

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

    // Quotas formatted helpers
    public double GeminiWeeklyPercentage => Quotas.GeminiGroup?.WeeklyRemainingPercentage ?? 100.0;
    public double GeminiFiveHourPercentage => Quotas.GeminiGroup?.FiveHourRemainingPercentage ?? 100.0;
    public double ClaudeWeeklyPercentage => Quotas.ClaudeGptGroup?.WeeklyRemainingPercentage ?? 100.0;
    public double ArgonWeeklyPercentage => Quotas.ArgonGroup?.WeeklyRemainingPercentage ?? 100.0;

    public string GeminiWeeklyText => Quotas.GeminiGroup?.WeeklyBucket?.FormattedRemaining ?? "100%";
    public string GeminiFiveHourText => Quotas.GeminiGroup?.FiveHourBucket?.FormattedRemaining ?? "100%";
    public string ClaudeWeeklyText => Quotas.ClaudeGptGroup?.WeeklyBucket?.FormattedRemaining ?? "100%";
    public string ArgonWeeklyText => Quotas.ArgonGroup?.WeeklyBucket?.FormattedRemaining ?? "100%";

    public string GeminiWeeklyResetText => Quotas.GeminiGroup?.WeeklyBucket?.FormattedReset ?? "";
    public string GeminiFiveHourResetText => Quotas.GeminiGroup?.FiveHourBucket?.FormattedReset ?? "";
    public string GeminiWeeklyDesc => !string.IsNullOrEmpty(Quotas.GeminiGroup?.WeeklyBucket?.Description)
        ? Quotas.GeminiGroup.WeeklyBucket.Description
        : "Límite semanal compartido de modelos Gemini";

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
            WindowBlurHelper.EnableBlur(this);
            MainGlassBorder.Opacity = Settings.GlassOpacity;
            Topmost = Settings.AlwaysOnTop;

            TaskbarDockHelper.SnapToPosition(this, Settings.DockPosition);

            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2.0)
            };
            _refreshTimer.Tick += async (s, ev) =>
            {
                _tickCount++;
                // Refresh tokens every 2s, quota every 10s (every 5 ticks)
                bool refreshQuota = (_tickCount % 5 == 0) || (_tickCount <= 2);
                await RefreshDataAsync(forceRefresh: false, updateQuota: refreshQuota);
            };
            _refreshTimer.Start();

            _ = RefreshDataAsync(forceRefresh: false, updateQuota: true);
        }
        catch { }
    }

    private async Task RefreshDataAsync(bool forceRefresh = false, bool updateQuota = true)
    {
        try
        {
            if (updateQuota)
            {
                var q = await _quotaService.FetchQuotaSummaryAsync(forceRefresh);
                if (q.Groups.Count > 0)
                {
                    Quotas = q;
                }
            }

            var usage = await _providerManager.GetActiveUsageAsync(Settings);
            Usage = usage;

            if (IsExpanded && ShowTokensView)
            {
                var recents = await _providerManager.GetRecentConversationsAsync(Settings.ActiveProviderId, 4);
                RecentConversations = new ObservableCollection<ConversationItem>(recents);
            }
        }
        catch { }
    }

    private void AdjustPositionAfterResize()
    {
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
        await RefreshDataAsync(forceRefresh: true, updateQuota: true);
    }

    private void BtnTabQuotas_Click(object sender, RoutedEventArgs e)
    {
        ShowQuotasView = true;
        AdjustPositionAfterResize();
    }

    private void BtnTabTokens_Click(object sender, RoutedEventArgs e)
    {
        ShowQuotasView = false;
        AdjustPositionAfterResize();
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
            await RefreshDataAsync(forceRefresh: false, updateQuota: false);
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _refreshTimer?.Stop();
        base.OnClosed(e);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
