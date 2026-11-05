using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
            NotifyActiveGaugeProperties();
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
            NotifyActiveGaugeProperties();
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
            OnPropertyChanged(nameof(IsHourlyMode));
            OnPropertyChanged(nameof(IsWeeklyMode));
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

    public bool IsHourlyMode
    {
        get => Settings.IsHourlyMode;
        set
        {
            if (Settings.IsHourlyMode != value)
            {
                Settings.IsHourlyMode = value;
                Settings.Save();
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsWeeklyMode));
                NotifyActiveGaugeProperties();
            }
        }
    }

    public bool IsWeeklyMode => !IsHourlyMode;

    private int _activeTabIndex = 0; // 0 = Quotas, 1 = Tokens, 2 = Connections
    public bool ShowQuotasView => _activeTabIndex == 0;
    public bool ShowTokensView => _activeTabIndex == 1;
    public bool ShowConnectionsView => _activeTabIndex == 2;

    public void SetActiveTab(int index)
    {
        _activeTabIndex = index;
        OnPropertyChanged(nameof(ShowQuotasView));
        OnPropertyChanged(nameof(ShowTokensView));
        OnPropertyChanged(nameof(ShowConnectionsView));
        AdjustPositionAfterResize();
    }

    public string ExpandIconText => IsExpanded ? "▲" : "▼";

    public string CurrentProviderShortName => Settings.ActiveProviderId switch
    {
        "antigravity_gemini" => "GEMINI IDE",
        "gemini_api" => "GEMINI API",
        "openai" => "OPENAI",
        "claude" => "CLAUDE",
        "deepseek" => "DEEPSEEK",
        "ollama" => "OLLAMA",
        _ => "GEMINI"
    };

    public string ActiveProviderIconGlyph => Settings.ActiveProviderId switch
    {
        "antigravity_gemini" => "✦",
        "deepseek" => "🐋",
        "claude" => "✻",
        "gemini_api" => "✦",
        "openai" => "⚙",
        "ollama" => "🦙",
        _ => "✦"
    };

    public string ActiveProviderShortCode => Settings.ActiveProviderId switch
    {
        "antigravity_gemini" => "AGY",
        "deepseek" => "DSK",
        "claude" => "CLD",
        "gemini_api" => "GEM",
        "openai" => "GPT",
        "ollama" => "OLM",
        _ => "AGY"
    };

    public Brush ActiveProviderAccentBrush => Settings.ActiveProviderId switch
    {
        "antigravity_gemini" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38BDF8")),
        "deepseek" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38BDF8")),
        "claude" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C084FC")),
        "gemini_api" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#60A5FA")),
        "openai" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
        "ollama" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")),
        _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38BDF8"))
    };

    public string ActiveProviderName => Settings.ActiveProviderId switch
    {
        "antigravity_gemini" => "Google Antigravity (IDE Local)",
        "deepseek" => "DeepSeek Platform (V3 / R1)",
        "claude" => "Anthropic Claude API",
        "gemini_api" => "Google AI Studio API",
        "openai" => "OpenAI API (GPT-4o)",
        "ollama" => "Ollama (Servidor Local)",
        _ => "Google Antigravity"
    };

    public string DeepSeekStatusBadge => string.IsNullOrWhiteSpace(Settings.DeepSeekApiKey) ? "Sin clave" : "Configurado";
    public string ClaudeStatusBadge => string.IsNullOrWhiteSpace(Settings.ClaudeApiKey) ? "Sin clave" : "Configurado";
    public string GeminiStatusBadge => string.IsNullOrWhiteSpace(Settings.GeminiApiKey) ? "Sin clave" : "Configurado";
    public string OpenAiStatusBadge => string.IsNullOrWhiteSpace(Settings.OpenAiApiKey) ? "Sin clave" : "Configurado";
    public string OllamaStatusBadge => "Local";

    // Test status texts
    private string _deepSeekTestStatus = string.Empty;
    public string DeepSeekTestStatus
    {
        get => _deepSeekTestStatus;
        set { _deepSeekTestStatus = value; OnPropertyChanged(); }
    }

    private string _claudeTestStatus = string.Empty;
    public string ClaudeTestStatus
    {
        get => _claudeTestStatus;
        set { _claudeTestStatus = value; OnPropertyChanged(); }
    }

    private string _geminiTestStatus = string.Empty;
    public string GeminiTestStatus
    {
        get => _geminiTestStatus;
        set { _geminiTestStatus = value; OnPropertyChanged(); }
    }

    private string _openAiTestStatus = string.Empty;
    public string OpenAiTestStatus
    {
        get => _openAiTestStatus;
        set { _openAiTestStatus = value; OnPropertyChanged(); }
    }

    private string _ollamaTestStatus = string.Empty;
    public string OllamaTestStatus
    {
        get => _ollamaTestStatus;
        set { _ollamaTestStatus = value; OnPropertyChanged(); }
    }

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

    // Raw Quota Percentages
    public double GeminiWeeklyPercentage => Quotas.GeminiGroup?.WeeklyRemainingPercentage ?? 100.0;
    public double GeminiFiveHourPercentage => Quotas.GeminiGroup?.FiveHourRemainingPercentage ?? 100.0;
    public double ClaudeWeeklyPercentage => Quotas.ClaudeGptGroup?.WeeklyRemainingPercentage ?? 100.0;
    public double ClaudeFiveHourPercentage => Quotas.ClaudeGptGroup?.FiveHourRemainingPercentage ?? 100.0;
    public double ArgonWeeklyPercentage => Quotas.ArgonGroup?.WeeklyRemainingPercentage ?? 100.0;

    public string GeminiWeeklyText => Quotas.GeminiGroup?.WeeklyBucket?.FormattedRemaining ?? "100%";
    public string GeminiFiveHourText => Quotas.GeminiGroup?.FiveHourBucket?.FormattedRemaining ?? "100%";
    public string ClaudeWeeklyText => Quotas.ClaudeGptGroup?.WeeklyBucket?.FormattedRemaining ?? "100%";
    public string ClaudeFiveHourText => Quotas.ClaudeGptGroup?.FiveHourBucket?.FormattedRemaining ?? "100%";
    public string ArgonWeeklyText => Quotas.ArgonGroup?.WeeklyBucket?.FormattedRemaining ?? "100%";

    public string GeminiWeeklyCountdown => Quotas.GeminiGroup?.WeeklyCountdown ?? "Listo";
    public string GeminiFiveHourCountdown => Quotas.GeminiGroup?.FiveHourCountdown ?? "Listo";
    public string ClaudeFiveHourCountdown => Quotas.ClaudeGptGroup?.FiveHourCountdown ?? "Listo";
    public string ClaudeWeeklyCountdown => Quotas.ClaudeGptGroup?.WeeklyCountdown ?? "Listo";
    public string ArgonWeeklyCountdown => Quotas.ArgonGroup?.WeeklyCountdown ?? "Listo";

    public string GeminiWeeklyDesc => !string.IsNullOrEmpty(Quotas.GeminiGroup?.WeeklyBucket?.Description)
        ? Quotas.GeminiGroup.WeeklyBucket.Description
        : "Límite semanal compartido de modelos Gemini";

    // Dynamic Gauges on Compact Bar depending on IsHourlyMode
    public double MainGeminiPercentage => IsHourlyMode ? GeminiFiveHourPercentage : GeminiWeeklyPercentage;
    public string MainGeminiValueText => IsHourlyMode ? GeminiFiveHourText : GeminiWeeklyText;
    public string MainGeminiLabel => IsHourlyMode
        ? (GeminiFiveHourCountdown == "Listo" ? "100% Libre" : GeminiFiveHourCountdown)
        : (GeminiWeeklyCountdown == "Listo" ? "100% Libre" : GeminiWeeklyCountdown);
    public Brush MainGeminiStroke => new SolidColorBrush((Color)ColorConverter.ConvertFromString(IsHourlyMode ? "#38BDF8" : "#10B981"));
    public string MainGeminiTooltip => $"GEMINI MODELS\n• Ventana 5 Horas: {GeminiFiveHourText} (Reinicia: {GeminiFiveHourCountdown})\n• Límite Semanal: {GeminiWeeklyText} (Reinicia: {GeminiWeeklyCountdown})";

    public double SecondaryGeminiPercentage => IsHourlyMode ? GeminiWeeklyPercentage : GeminiFiveHourPercentage;
    public string SecondaryGeminiValueText => IsHourlyMode ? GeminiWeeklyText : GeminiFiveHourText;
    public string SecondaryGeminiLabel => IsHourlyMode
        ? $"Semana ({GeminiWeeklyCountdown})"
        : $"5h ({GeminiFiveHourCountdown})";

    public double MainClaudePercentage => IsHourlyMode ? ClaudeFiveHourPercentage : ClaudeWeeklyPercentage;
    public string MainClaudeValueText => IsHourlyMode ? ClaudeFiveHourText : ClaudeWeeklyText;
    public string MainClaudeLabel => IsHourlyMode
        ? (ClaudeFiveHourCountdown == "Listo" ? "100% Libre" : ClaudeFiveHourCountdown)
        : (ClaudeWeeklyCountdown == "Listo" ? "100% Libre" : ClaudeWeeklyCountdown);
    public Brush MainClaudeStroke => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C084FC"));
    public string MainClaudeTooltip => $"CLAUDE & GPT MODELS\n• Ventana 5 Horas: {ClaudeFiveHourText} (Reinicia: {ClaudeFiveHourCountdown})\n• Límite Semanal: {ClaudeWeeklyText} (Reinicia: {ClaudeWeeklyCountdown})";

    public double MainArgonPercentage => ArgonWeeklyPercentage;
    public string MainArgonValueText => ArgonWeeklyText;
    public string MainArgonLabel => ArgonWeeklyCountdown == "Listo" ? "100% Libre" : ArgonWeeklyCountdown;
    public Brush MainArgonStroke => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
    public string MainArgonTooltip => $"GEMINI 4 ARGON\n• Límite Semanal: {ArgonWeeklyText} (Reinicia: {ArgonWeeklyCountdown})\n• Cuota dedicada de razonamiento rápido";

    public string ActiveWindowBadgeText => IsHourlyMode ? $"⏱ {GeminiFiveHourCountdown}" : $"📅 {GeminiWeeklyCountdown}";

    // ROUTER PROPERTIES FOR THE 3 CIRCULAR GAUGES ON COMPACT BAR
    public string Gauge1Header => Settings.ActiveProviderId switch
    {
        "deepseek" => "DEEPSEEK",
        "claude" => "CLAUDE",
        "gemini_api" => "GEMINI",
        "openai" => "OPENAI",
        "ollama" => "OLLAMA",
        _ => "GEMINI"
    };

    public double Gauge1Percentage => Settings.ActiveProviderId switch
    {
        "deepseek" => 100.0,
        "claude" => 100.0,
        "gemini_api" => 100.0,
        "openai" => 100.0,
        "ollama" => 100.0,
        _ => MainGeminiPercentage
    };

    public string Gauge1ValueText => Settings.ActiveProviderId switch
    {
        "deepseek" => (Usage.IsActiveNow ? "100%" : "Off"),
        "claude" => (Usage.IsActiveNow ? "100%" : "Off"),
        "gemini_api" => (Usage.IsActiveNow ? "100%" : "Off"),
        "openai" => (Usage.IsActiveNow ? "100%" : "Off"),
        "ollama" => (Usage.IsActiveNow ? "100%" : "Off"),
        _ => MainGeminiValueText
    };

    public string Gauge1Label => Settings.ActiveProviderId switch
    {
        "deepseek" => "V3 / R1",
        "claude" => "Sonnet",
        "gemini_api" => "AI Studio",
        "openai" => "GPT-4o",
        "ollama" => "Local",
        _ => MainGeminiLabel
    };

    public Brush Gauge1Stroke => Settings.ActiveProviderId switch
    {
        "deepseek" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38BDF8")),
        "claude" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C084FC")),
        "gemini_api" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#60A5FA")),
        "openai" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
        "ollama" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")),
        _ => MainGeminiStroke
    };

    public string Gauge1Tooltip => Settings.ActiveProviderId switch
    {
        "deepseek" => $"DEEPSEEK V3 / R1\n• Estado: {Usage.ConversationTitle}",
        "claude" => $"ANTHROPIC CLAUDE\n• Estado: {Usage.ConversationTitle}",
        "gemini_api" => $"GOOGLE AI STUDIO\n• Estado: {Usage.ConversationTitle}",
        "openai" => $"OPENAI API\n• Estado: {Usage.ConversationTitle}",
        "ollama" => $"OLLAMA LOCAL\n• Estado: {Usage.ConversationTitle}",
        _ => MainGeminiTooltip
    };

    // Gauge 2
    public string Gauge2Header => Settings.ActiveProviderId == "antigravity_gemini" ? "CLAUDE" : "CONTEXT";
    public double Gauge2Percentage => Settings.ActiveProviderId == "antigravity_gemini" ? MainClaudePercentage : 100.0;
    public string Gauge2ValueText => Settings.ActiveProviderId switch
    {
        "deepseek" => "64k",
        "claude" => "200k",
        "gemini_api" => "1M",
        "openai" => "128k",
        "ollama" => "32k",
        _ => MainClaudeValueText
    };
    public string Gauge2Label => Settings.ActiveProviderId == "antigravity_gemini" ? MainClaudeLabel : "Tokens";
    public Brush Gauge2Stroke => Settings.ActiveProviderId == "antigravity_gemini" ? MainClaudeStroke : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38BDF8"));
    public string Gauge2Tooltip => Settings.ActiveProviderId == "antigravity_gemini" ? MainClaudeTooltip : $"Ventana de contexto máxima: {Gauge2ValueText} tokens";

    // Gauge 3
    public string Gauge3Header => Settings.ActiveProviderId == "antigravity_gemini" ? "ARGON" : "ESTADO";
    public double Gauge3Percentage => Settings.ActiveProviderId == "antigravity_gemini" ? MainArgonPercentage : (Usage.IsActiveNow ? 100.0 : 0.0);
    public string Gauge3ValueText => Settings.ActiveProviderId == "antigravity_gemini" ? MainArgonValueText : (Usage.IsActiveNow ? "OK" : "Setup");
    public string Gauge3Label => Settings.ActiveProviderId == "antigravity_gemini" ? MainArgonLabel : (Usage.IsActiveNow ? "Conectado" : "Sin Clave");
    public Brush Gauge3Stroke => Settings.ActiveProviderId == "antigravity_gemini" ? MainArgonStroke : new SolidColorBrush((Color)ColorConverter.ConvertFromString(Usage.IsActiveNow ? "#10B981" : "#F43F5E"));
    public string Gauge3Tooltip => Settings.ActiveProviderId == "antigravity_gemini" ? MainArgonTooltip : $"Estado de la conexión: {Usage.ConversationTitle}";

    public void NotifyActiveGaugeProperties()
    {
        OnPropertyChanged(nameof(GeminiWeeklyPercentage));
        OnPropertyChanged(nameof(GeminiFiveHourPercentage));
        OnPropertyChanged(nameof(ClaudeWeeklyPercentage));
        OnPropertyChanged(nameof(ClaudeFiveHourPercentage));
        OnPropertyChanged(nameof(ArgonWeeklyPercentage));
        OnPropertyChanged(nameof(GeminiWeeklyText));
        OnPropertyChanged(nameof(GeminiFiveHourText));
        OnPropertyChanged(nameof(ClaudeWeeklyText));
        OnPropertyChanged(nameof(ClaudeFiveHourText));
        OnPropertyChanged(nameof(ArgonWeeklyText));
        OnPropertyChanged(nameof(GeminiWeeklyCountdown));
        OnPropertyChanged(nameof(GeminiFiveHourCountdown));
        OnPropertyChanged(nameof(GeminiWeeklyDesc));

        OnPropertyChanged(nameof(MainGeminiPercentage));
        OnPropertyChanged(nameof(MainGeminiValueText));
        OnPropertyChanged(nameof(MainGeminiLabel));
        OnPropertyChanged(nameof(MainGeminiStroke));
        OnPropertyChanged(nameof(MainGeminiTooltip));

        OnPropertyChanged(nameof(SecondaryGeminiPercentage));
        OnPropertyChanged(nameof(SecondaryGeminiValueText));
        OnPropertyChanged(nameof(SecondaryGeminiLabel));

        OnPropertyChanged(nameof(MainClaudePercentage));
        OnPropertyChanged(nameof(MainClaudeValueText));
        OnPropertyChanged(nameof(MainClaudeLabel));
        OnPropertyChanged(nameof(MainClaudeStroke));
        OnPropertyChanged(nameof(MainClaudeTooltip));

        OnPropertyChanged(nameof(MainArgonPercentage));
        OnPropertyChanged(nameof(MainArgonValueText));
        OnPropertyChanged(nameof(MainArgonLabel));
        OnPropertyChanged(nameof(MainArgonStroke));
        OnPropertyChanged(nameof(MainArgonTooltip));

        OnPropertyChanged(nameof(ActiveWindowBadgeText));

        // Router properties for gauges
        OnPropertyChanged(nameof(Gauge1Header));
        OnPropertyChanged(nameof(Gauge1Percentage));
        OnPropertyChanged(nameof(Gauge1ValueText));
        OnPropertyChanged(nameof(Gauge1Label));
        OnPropertyChanged(nameof(Gauge1Stroke));
        OnPropertyChanged(nameof(Gauge1Tooltip));

        OnPropertyChanged(nameof(Gauge2Header));
        OnPropertyChanged(nameof(Gauge2Percentage));
        OnPropertyChanged(nameof(Gauge2ValueText));
        OnPropertyChanged(nameof(Gauge2Label));
        OnPropertyChanged(nameof(Gauge2Stroke));
        OnPropertyChanged(nameof(Gauge2Tooltip));

        OnPropertyChanged(nameof(Gauge3Header));
        OnPropertyChanged(nameof(Gauge3Percentage));
        OnPropertyChanged(nameof(Gauge3ValueText));
        OnPropertyChanged(nameof(Gauge3Label));
        OnPropertyChanged(nameof(Gauge3Stroke));
        OnPropertyChanged(nameof(Gauge3Tooltip));
    }

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
            if (updateQuota && Settings.ActiveProviderId == "antigravity_gemini")
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
                if (Math.Abs(Left + Width - workArea.Right) < 45)
                    Settings.DockPosition = "BottomRight";
                else if (Math.Abs(Left + Width / 2.0 - (workArea.Left + workArea.Width / 2.0)) < 45)
                    Settings.DockPosition = "BottomCenter";
                else if (Math.Abs(Left - workArea.Left) < 45)
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

    private void BtnModeHours_Click(object sender, RoutedEventArgs e)
    {
        IsHourlyMode = true;
        NotifyActiveGaugeProperties();
    }

    private void BtnModeWeek_Click(object sender, RoutedEventArgs e)
    {
        IsHourlyMode = false;
        NotifyActiveGaugeProperties();
    }

    private void BtnTabQuotas_Click(object sender, RoutedEventArgs e) => SetActiveTab(0);
    private void BtnTabTokens_Click(object sender, RoutedEventArgs e) => SetActiveTab(1);
    private void BtnTabConnections_Click(object sender, RoutedEventArgs e) => SetActiveTab(2);

    private void BtnProviderSelector_Click(object sender, RoutedEventArgs e)
    {
        ProviderQuickPopup.IsOpen = !ProviderQuickPopup.IsOpen;
    }

    private void BtnSelectProvider_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string providerId)
        {
            SwitchActiveProvider(providerId);
        }
    }

    public void SwitchActiveProvider(string providerId)
    {
        Settings.ActiveProviderId = providerId;
        Settings.Save();
        ProviderQuickPopup.IsOpen = false;

        NotifyProviderChanged();
        _ = RefreshDataAsync(forceRefresh: true, updateQuota: true);
    }

    public void NotifyProviderChanged()
    {
        OnPropertyChanged(nameof(ActiveProviderIconGlyph));
        OnPropertyChanged(nameof(ActiveProviderShortCode));
        OnPropertyChanged(nameof(ActiveProviderAccentBrush));
        OnPropertyChanged(nameof(ActiveProviderName));
        OnPropertyChanged(nameof(CurrentProviderShortName));
        OnPropertyChanged(nameof(DeepSeekStatusBadge));
        OnPropertyChanged(nameof(ClaudeStatusBadge));
        OnPropertyChanged(nameof(GeminiStatusBadge));
        OnPropertyChanged(nameof(OpenAiStatusBadge));
        NotifyActiveGaugeProperties();
    }

    private void BtnOpenConnectionsTab_Click(object sender, RoutedEventArgs e)
    {
        ProviderQuickPopup.IsOpen = false;
        IsExpanded = true;
        SetActiveTab(2);
    }

    private async void BtnSaveAndTestDeepSeek_Click(object sender, RoutedEventArgs e)
    {
        Settings.Save();
        DeepSeekTestStatus = "Probando conexión con DeepSeek Platform...";
        var provider = new DeepSeekProvider();
        var res = await provider.TestConnectionAsync(Settings.DeepSeekApiKey);
        DeepSeekTestStatus = res.Message;
        NotifyProviderChanged();
    }

    private async void BtnSaveAndTestClaude_Click(object sender, RoutedEventArgs e)
    {
        Settings.Save();
        ClaudeTestStatus = "Probando conexión con Anthropic Claude...";
        var provider = new ClaudeProvider();
        var res = await provider.TestConnectionAsync(Settings.ClaudeApiKey);
        ClaudeTestStatus = res.Message;
        NotifyProviderChanged();
    }

    private async void BtnSaveAndTestGemini_Click(object sender, RoutedEventArgs e)
    {
        Settings.Save();
        GeminiTestStatus = "Probando conexión con Google AI Studio...";
        var provider = new GoogleGeminiApiProvider();
        var res = await provider.TestConnectionAsync(Settings.GeminiApiKey);
        GeminiTestStatus = res.Message;
        NotifyProviderChanged();
    }

    private async void BtnSaveAndTestOpenAi_Click(object sender, RoutedEventArgs e)
    {
        Settings.Save();
        OpenAiTestStatus = "Probando conexión con OpenAI API...";
        var provider = new OpenAiProvider();
        var res = await provider.TestConnectionAsync(Settings.OpenAiApiKey);
        OpenAiTestStatus = res.Message;
        NotifyProviderChanged();
    }

    private async void BtnSaveAndTestOllama_Click(object sender, RoutedEventArgs e)
    {
        Settings.Save();
        OllamaTestStatus = $"Probando conexión con Ollama en {Settings.OllamaBaseUrl}...";
        var provider = new OllamaProvider();
        var res = await provider.TestConnectionAsync(Settings.OllamaBaseUrl);
        OllamaTestStatus = res.Message;
        NotifyProviderChanged();
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
