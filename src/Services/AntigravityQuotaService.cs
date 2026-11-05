using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Dockus.Models;

namespace Dockus.Services;

public class AntigravityQuotaService
{
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(5) };
    private int _cachedPort;
    private string _cachedToken = string.Empty;

    public async Task<AntigravityQuotaSummary> FetchQuotaSummaryAsync(bool forceRefresh = false)
    {
        var summary = new AntigravityQuotaSummary();

        var hub = FindActiveHub();
        if (hub == null)
        {
            summary.IsConnected = false;
            return summary;
        }

        var (port, token) = hub.Value;
        _cachedPort = port;
        _cachedToken = token;

        var url = $"http://127.0.0.1:{port}/exa.language_server_pb.LanguageServerService/RetrieveUserQuotaSummary";

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Add("x-codeium-csrf-token", token);
            var bodyJson = JsonSerializer.Serialize(new { forceRefresh });
            req.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

            var resp = await _httpClient.SendAsync(req);
            if (resp.IsSuccessStatusCode)
            {
                var content = await resp.Content.ReadAsStringAsync();
                ParseQuotaResponse(content, summary);
                summary.IsConnected = true;
                summary.LastFetched = DateTime.Now;
            }
        }
        catch
        {
            summary.IsConnected = false;
        }

        return summary;
    }

    private static (int port, string token)? FindActiveHub()
    {
        try
        {
            var daemonDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".gemini",
                "antigravity",
                "daemon"
            );

            if (!Directory.Exists(daemonDir)) return null;

            var files = Directory.GetFiles(daemonDir, "hub_*.json")
                                 .Select(f => new FileInfo(f))
                                 .OrderByDescending(f => f.LastWriteTimeUtc)
                                 .ToList();

            foreach (var file in files)
            {
                try
                {
                    var json = File.ReadAllText(file.FullName);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    int pid = root.GetProperty("pid").GetInt32();

                    bool isAlive = false;
                    try
                    {
                        var proc = Process.GetProcessById(pid);
                        isAlive = !proc.HasExited;
                    }
                    catch { }

                    if (isAlive)
                    {
                        int port = root.GetProperty("httpPort").GetInt32();
                        string token = root.GetProperty("csrfToken").GetString() ?? "";
                        return (port, token);
                    }
                }
                catch { }
            }

            // Fallback to most recent file if process check failed (e.g. permission)
            if (files.Count > 0)
            {
                var json = File.ReadAllText(files[0].FullName);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                int port = root.GetProperty("httpPort").GetInt32();
                string token = root.GetProperty("csrfToken").GetString() ?? "";
                return (port, token);
            }
        }
        catch { }

        return null;
    }

    private static void ParseQuotaResponse(string json, AntigravityQuotaSummary summary)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            JsonElement responseEl = root;
            if (root.TryGetProperty("response", out var respProp))
            {
                responseEl = respProp;
            }

            if (responseEl.TryGetProperty("description", out var descProp))
            {
                summary.OverallDescription = descProp.GetString() ?? "";
            }

            if (responseEl.TryGetProperty("groups", out var groupsArr) && groupsArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var groupEl in groupsArr.EnumerateArray())
                {
                    var group = new QuotaGroup
                    {
                        DisplayName = groupEl.TryGetProperty("displayName", out var gName) ? gName.GetString() ?? "" : "",
                        Description = groupEl.TryGetProperty("description", out var gDesc) ? gDesc.GetString() ?? "" : ""
                    };

                    if (groupEl.TryGetProperty("buckets", out var bucketsArr) && bucketsArr.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var bEl in bucketsArr.EnumerateArray())
                        {
                            var bucket = new QuotaBucket
                            {
                                BucketId = bEl.TryGetProperty("bucketId", out var bId) ? bId.GetString() ?? "" : "",
                                DisplayName = bEl.TryGetProperty("displayName", out var bName) ? bName.GetString() ?? "" : "",
                                Description = bEl.TryGetProperty("description", out var bDesc) ? bDesc.GetString() ?? "" : "",
                                Window = bEl.TryGetProperty("window", out var bWin) ? bWin.GetString() ?? "" : "",
                                RemainingFraction = bEl.TryGetProperty("remainingFraction", out var bRem) ? bRem.GetDouble() : 1.0
                            };

                            if (bEl.TryGetProperty("resetTime", out var bReset))
                            {
                                var rStr = bReset.GetString();
                                if (!string.IsNullOrEmpty(rStr) && DateTime.TryParse(rStr, out var rDt))
                                {
                                    bucket.ResetTime = rDt;
                                }
                            }

                            group.Buckets.Add(bucket);
                        }
                    }

                    summary.Groups.Add(group);
                }
            }
        }
        catch { }
    }
}

