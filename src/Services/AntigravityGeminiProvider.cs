using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Dockus.Models;
using Microsoft.Data.Sqlite;

namespace Dockus.Services;

public class AntigravityGeminiProvider : ITokenProvider
{
    public string Id => "antigravity_gemini";
    public string DisplayName => "Gemini (Antigravity)";
    public string Description => "Monitor nativo en tiempo real de sesiones y tokens de Gemini en Antigravity";
    public string IconGlyph => "✦";
    public string AccentColorHex => "#38BDF8"; // Sky blue

    private readonly string _antigravityDir;
    private readonly string _brainDir;
    private readonly string _dbPath;

    public AntigravityGeminiProvider()
    {
        _antigravityDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".gemini",
            "antigravity"
        );
        _brainDir = Path.Combine(_antigravityDir, "brain");
        _dbPath = Path.Combine(_antigravityDir, "conversation_summaries.db");
    }

    public bool IsConfigured(AppSettings settings)
    {
        return Directory.Exists(_brainDir);
    }

    public async Task<TokenUsageSnapshot> GetUsageAsync(AppSettings settings)
    {
        return await Task.Run(() =>
        {
            var snapshot = new TokenUsageSnapshot
            {
                ProviderId = Id,
                ProviderName = DisplayName,
                IconGlyph = IconGlyph,
                AccentColorHex = AccentColorHex,
                MaxContextTokens = settings.MaxContextTokens
            };

            if (!Directory.Exists(_brainDir))
            {
                snapshot.ConversationTitle = "Antigravity no detectado en el sistema";
                return snapshot;
            }

            try
            {
                // Find most recently modified conversation
                var transcripts = FindAllTranscripts();
                if (transcripts.Count == 0)
                {
                    snapshot.ConversationTitle = "No se encontraron conversaciones";
                    return snapshot;
                }

                var activeTranscript = transcripts[0];
                snapshot.ConversationId = GetConversationIdFromPath(activeTranscript.FullName);
                snapshot.LastActivityTime = activeTranscript.LastWriteTime;
                snapshot.IsActiveNow = (DateTime.Now - activeTranscript.LastWriteTime).TotalMinutes < 2;

                // Lookup title from SQLite database
                snapshot.ConversationTitle = GetConversationTitle(snapshot.ConversationId) ?? "Conversación de Antigravity";

                // Parse active conversation
                ParseTranscriptFile(activeTranscript.FullName, snapshot, isCurrentSession: true);

                // Aggregate today's metrics across other conversations
                var today = DateTime.Today;
                foreach (var fi in transcripts)
                {
                    if (fi.FullName.Equals(activeTranscript.FullName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (fi.LastWriteTime.Date >= today)
                    {
                        ParseTranscriptForTodayOnly(fi.FullName, snapshot);
                    }
                }
            }
            catch (Exception ex)
            {
                snapshot.ConversationTitle = $"Error al leer métricas: {ex.Message}";
            }

            return snapshot;
        });
    }

    public async Task<List<ConversationItem>> GetRecentConversationsAsync(int count = 5)
    {
        return await Task.Run(() =>
        {
            var list = new List<ConversationItem>();
            if (!File.Exists(_dbPath))
                return list;

            try
            {
                var connStr = new SqliteConnectionStringBuilder
                {
                    DataSource = _dbPath,
                    Mode = SqliteOpenMode.ReadOnly
                }.ToString();

                using var conn = new SqliteConnection(connStr);
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT conversation_id, preview, title, step_count, last_modified_time 
                    FROM conversation_summaries 
                    ORDER BY last_modified_time DESC 
                    LIMIT @limit;";
                cmd.Parameters.AddWithValue("@limit", count);

                using var reader = cmd.ExecuteReader();
                bool isFirst = true;
                while (reader.Read())
                {
                    var id = reader.IsDBNull(0) ? "" : reader.GetString(0);
                    var preview = reader.IsDBNull(1) ? "" : reader.GetString(1);
                    var title = reader.IsDBNull(2) ? "" : reader.GetString(2);
                    var steps = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
                    var modStr = reader.IsDBNull(4) ? "" : reader.GetString(4);

                    DateTime.TryParse(modStr, out var lastMod);
                    var displayTitle = !string.IsNullOrWhiteSpace(title) 
                        ? title 
                        : (!string.IsNullOrWhiteSpace(preview) ? preview : "Conversación " + id[..Math.Min(8, id.Length)]);

                    // Estimate context size for this conversation
                    long contextTokens = EstimateConversationContext(id);

                    list.Add(new ConversationItem
                    {
                        Id = id,
                        Title = displayTitle,
                        StepCount = steps,
                        LastModified = lastMod != default ? lastMod.ToLocalTime() : DateTime.Now,
                        ContextTokens = contextTokens,
                        IsCurrent = isFirst
                    });

                    isFirst = false;
                }
            }
            catch { }

            return list;
        });
    }

    private List<FileInfo> FindAllTranscripts()
    {
        var list = new List<FileInfo>();
        try
        {
            var dirs = Directory.GetDirectories(_brainDir);
            foreach (var d in dirs)
            {
                var transcriptPath = Path.Combine(d, ".system_generated", "logs", "transcript.jsonl");
                if (File.Exists(transcriptPath))
                {
                    list.Add(new FileInfo(transcriptPath));
                }
            }
            list.Sort((a, b) => b.LastWriteTimeUtc.CompareTo(a.LastWriteTimeUtc));
        }
        catch { }
        return list;
    }

    private static string GetConversationIdFromPath(string transcriptPath)
    {
        try
        {
            // e.g. C:\Users\TI\.gemini\antigravity\brain\<id>\.system_generated\logs\transcript.jsonl
            var parts = transcriptPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Equals("brain", StringComparison.OrdinalIgnoreCase) && i + 1 < parts.Length)
                {
                    return parts[i + 1];
                }
            }
        }
        catch { }
        return string.Empty;
    }

    private string? GetConversationTitle(string conversationId)
    {
        if (string.IsNullOrEmpty(conversationId) || !File.Exists(_dbPath))
            return null;

        try
        {
            var connStr = new SqliteConnectionStringBuilder
            {
                DataSource = _dbPath,
                Mode = SqliteOpenMode.ReadOnly
            }.ToString();

            using var conn = new SqliteConnection(connStr);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT title, preview FROM conversation_summaries WHERE conversation_id = @id LIMIT 1;";
            cmd.Parameters.AddWithValue("@id", conversationId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var title = reader.IsDBNull(0) ? "" : reader.GetString(0);
                var preview = reader.IsDBNull(1) ? "" : reader.GetString(1);
                if (!string.IsNullOrWhiteSpace(title)) return title;
                if (!string.IsNullOrWhiteSpace(preview)) return preview;
            }
        }
        catch { }
        return null;
    }

    private void ParseTranscriptFile(string path, TokenUsageSnapshot snapshot, bool isCurrentSession)
    {
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fs, Encoding.UTF8);

            string? line;
            var today = DateTime.Today;
            int stepCount = 0;

            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                stepCount++;

                try
                {
                    using var doc = JsonDocument.Parse(line);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("input_tokens", out var inEl))
                    {
                        long inp = inEl.GetInt64();
                        long outTokens = root.TryGetProperty("output_tokens", out var outEl) ? outEl.GetInt64() : 0;
                        long cache = root.TryGetProperty("cache_read_tokens", out var cacheEl) ? cacheEl.GetInt64() : 0;

                        if (isCurrentSession)
                        {
                            snapshot.LastTurnInputTokens = inp;
                            snapshot.LastTurnCacheTokens = cache;
                            snapshot.LastTurnOutputTokens = outTokens;

                            // Active context in model is input (uncached) + cache_read + output
                            snapshot.CurrentContextTokens = inp + cache + outTokens;

                            snapshot.SessionTotalInputTokens += inp;
                            snapshot.SessionTotalCacheTokens += cache;
                            snapshot.SessionTotalOutputTokens += outTokens;
                        }

                        // Check date for today metrics
                        if (root.TryGetProperty("created_at", out var createdEl))
                        {
                            var dtStr = createdEl.GetString();
                            if (!string.IsNullOrEmpty(dtStr) && DateTime.TryParse(dtStr, out var dt))
                            {
                                if (dt.ToLocalTime().Date == today)
                                {
                                    snapshot.TodayInputTokens += inp;
                                    snapshot.TodayCacheTokens += cache;
                                    snapshot.TodayOutputTokens += outTokens;
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            snapshot.StepCount = stepCount;
        }
        catch { }
    }

    private void ParseTranscriptForTodayOnly(string path, TokenUsageSnapshot snapshot)
    {
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fs, Encoding.UTF8);

            string? line;
            var today = DateTime.Today;

            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    using var doc = JsonDocument.Parse(line);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("input_tokens", out var inEl))
                    {
                        long inp = inEl.GetInt64();
                        long outTokens = root.TryGetProperty("output_tokens", out var outEl) ? outEl.GetInt64() : 0;
                        long cache = root.TryGetProperty("cache_read_tokens", out var cacheEl) ? cacheEl.GetInt64() : 0;

                        if (root.TryGetProperty("created_at", out var createdEl))
                        {
                            var dtStr = createdEl.GetString();
                            if (!string.IsNullOrEmpty(dtStr) && DateTime.TryParse(dtStr, out var dt))
                            {
                                if (dt.ToLocalTime().Date == today)
                                {
                                    snapshot.TodayInputTokens += inp;
                                    snapshot.TodayCacheTokens += cache;
                                    snapshot.TodayOutputTokens += outTokens;
                                }
                            }
                        }
                    }
                }
                catch { }
            }
        }
        catch { }
    }

    private long EstimateConversationContext(string conversationId)
    {
        try
        {
            var p = Path.Combine(_brainDir, conversationId, ".system_generated", "logs", "transcript.jsonl");
            if (!File.Exists(p)) return 0;

            using var fs = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fs, Encoding.UTF8);

            string? line;
            long latestContext = 0;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                try
                {
                    using var doc = JsonDocument.Parse(line);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("input_tokens", out var inEl))
                    {
                        long inp = inEl.GetInt64();
                        long cache = root.TryGetProperty("cache_read_tokens", out var cEl) ? cEl.GetInt64() : 0;
                        latestContext = inp + cache;
                    }
                }
                catch { }
            }
            return latestContext;
        }
        catch
        {
            return 0;
        }
    }
}

