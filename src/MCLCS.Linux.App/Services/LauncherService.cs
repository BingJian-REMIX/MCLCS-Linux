using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MCLCS.Core.Download;
using MCLCS.Core.Installers;
using MCLCS.Core.Models;
using MCLCS.Core.Utils;

namespace MCLCS.Linux.App.Services;

/// <summary>
/// 启动器后端服务（对齐 MCLCS-WPF 的 LauncherService）：整合下载页所需的全部能力，
/// 向下委托 Core 层的下载原语（ModrinthClient / PixelmapClient / ModpackInstaller / 版本安装器 / MapInstaller / ExtraResourceInstaller）。
/// 下载页 ViewModel 只与本服务打交道，不直接依赖具体 Core 实现，便于与 WPF 保持一致。
/// </summary>
public class LauncherService : ILogger
{
    /// <summary>单例实例。</summary>
    public static LauncherService Instance { get; } = new(GameConstants.DefaultGameRoot);

    /// <summary>游戏根目录（来自 GameConstants，供安装器落地）。</summary>
    public string GameRoot { get; }

    /// <summary>共享 HttpClient（含 User-Agent 与下载器复用）。</summary>
    public HttpClient ApiClient { get; }

    private readonly IDownloader _downloader;

    /// <summary>像素茶艺（PixelMap）地图站客户端（下载页 → 地图）。</summary>
    public PixelmapClient Pixelmap { get; }

    /// <summary>当前可用的整合包在线源（Modrinth 免 Key 常驻）。</summary>
    public IReadOnlyList<IModpackSource> ModpackSources { get; }

    public LauncherService(string gameRoot)
    {
        GameRoot = gameRoot;
        ApiClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        // 规格 2.2：地图站要求 User-Agent 为 MCLCS/版本号
        ApiClient.DefaultRequestHeaders.UserAgent.TryParseAdd(
            $"MCLCS/{GameConstants.LauncherVersion} (Linux; +{GameConstants.CnbRepoUrl})");

        _downloader = new HttpDownloader(ApiClient, 8, this);
        Pixelmap = new PixelmapClient(ApiClient);
        ModpackSources = new IModpackSource[] { new ModrinthModpackSource(ApiClient) };
    }

    /// <summary>按 Id 取得整合包源（未知 Id 回退到 Modrinth）。</summary>
    private IModpackSource GetModpackSource(string? id) =>
        ModpackSources.FirstOrDefault(s => s.Id == (id ?? "")) ?? ModpackSources[0];

    // ---- 版本列表 ----

    public async Task<List<string>> GetVanillaVersionsAsync()
    {
        try
        {
            var json = await MirrorPolicy.GetStringWithFallback(MirrorPolicy.VersionManifestUrls(), ApiClient);
            var manifest = System.Text.Json.JsonSerializer.Deserialize<VersionManifest>(json);
            return manifest?.Versions.Select(v => v.Id).ToList() ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    /// <summary>返回完整版本清单条目（含类型 / 发布时间），供下载页 Minecraft 子页列举与分类。</summary>
    public async Task<List<VersionEntry>> GetVanillaVersionsDetailedAsync()
    {
        try
        {
            var json = await MirrorPolicy.GetStringWithFallback(MirrorPolicy.VersionManifestUrls(), ApiClient);
            var manifest = System.Text.Json.JsonSerializer.Deserialize<VersionManifest>(json);
            return manifest?.Versions ?? new List<VersionEntry>();
        }
        catch
        {
            return new List<VersionEntry>();
        }
    }

    // ---- Modrinth 搜索 / 下载 ----

    public async Task<List<ModrinthHit>> SearchModsAsync(string query, string? gameVersion, LoaderType loader, ModrinthProjectType type)
    {
        var client = new ModrinthClient(ApiClient);
        var r = await client.SearchAsync(query, gameVersion, loader, type);
        return r.Hits;
    }

    public async Task<bool> DownloadModAsync(string projectId, string targetDir, string? gameVersion, LoaderType loader,
        IProgress<double>? progress = null, CancellationToken ct = default)
    {
        var client = new ModrinthClient(ApiClient);
        var versions = await client.GetVersionsAsync(projectId, ct);
        ModrinthFile? file = null;
        foreach (var v in versions)
        {
            var f = client.SelectBestFile(v, gameVersion, loader);
            if (f is not null) { file = f; break; }
        }
        if (file is null) return false;

        Directory.CreateDirectory(targetDir);
        var dest = Path.Combine(targetDir, file.FileName);
        var item = new DownloadItem(new[] { file.Url }, dest, file.Hashes.Sha1, file.Size);
        await _downloader.DownloadAsync(item, progress, ct);
        return true;
    }

    // ---- 整合包 ----

    public async Task<List<ModpackItem>> SearchModpacksAsync(string? keyword, string? gameVersion, string? loader, string? sourceId, CancellationToken ct)
    {
        var source = GetModpackSource(sourceId);
        if (!source.IsAvailable) return new List<ModpackItem>();
        return await source.SearchAsync(keyword, gameVersion, loader, 24, 0, ct);
    }

    public async Task<ModpackDetail?> GetModpackDetailAsync(string? sourceId, string id, CancellationToken ct)
    {
        var source = GetModpackSource(sourceId);
        if (!source.IsAvailable) return null;
        return await source.GetDetailAsync(id, ct);
    }

    public async Task<ModpackInstallResult?> InstallModpackVersionAsync(
        string? sourceId, ModpackVersion version, bool isolated, string? preferredName,
        IProgress<double>? progress, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(version.FileUrl)) return null;
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".mrpack");
        try
        {
            await _downloader.DownloadAsync(new DownloadItem(new[] { version.FileUrl }, tmp, version.Sha1), progress, ct);
            var installer = new ModpackInstaller(GameRoot, ApiClient, _downloader, this);
            return await installer.InstallAsync(tmp, isolated, preferredName, null, ct);
        }
        finally
        {
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { /* 忽略 */ }
        }
    }

    public async Task<ModpackInstallResult?> InstallModpackAsync(
        string? sourceId, string projectId, string? gameVersion, string? preferredName,
        IProgress<double>? progress, CancellationToken ct)
    {
        var detail = await GetModpackDetailAsync(sourceId, projectId, ct);
        if (detail is null || detail.Versions.Count == 0) return null;
        var version = detail.Versions.FirstOrDefault(v =>
                          string.IsNullOrEmpty(gameVersion) || string.Equals(v.GameVersion, gameVersion, StringComparison.OrdinalIgnoreCase))
                      ?? detail.Versions[0];
        return await InstallModpackVersionAsync(sourceId, version, isolated: true, preferredName, progress, ct);
    }

    // ---- 版本安装（下载页 → Minecraft 下载）----

    public async Task<string?> InstallVersionAsync(string mcVersion, string loader,
        IProgress<double>? progress = null, CancellationToken ct = default)
    {
        switch (loader.ToLowerInvariant())
        {
            case "fabric":
                return await new FabricInstaller(GameRoot, ApiClient, _downloader, this).InstallAsync(mcVersion, null, ct);
            case "forge":
                return await new ForgeInstaller(GameRoot, ApiClient, _downloader, this).InstallAsync(mcVersion, null, ct);
            case "neoforge":
                return await new NeoForgeInstaller(GameRoot, ApiClient, _downloader, this).InstallAsync(mcVersion, null, ct);
            case "quilt":
                return await new QuiltInstaller(GameRoot, ApiClient, _downloader, this).InstallAsync(mcVersion, null, ct);
            default:
                await new VanillaInstaller(GameRoot, ApiClient, _downloader, this).InstallAsync(mcVersion, null, ct);
                return mcVersion;
        }
    }

    // ---- 地图 ----

    public async Task<bool> DownloadMapAsync(string slug, IProgress<double>? progress, CancellationToken ct)
    {
        var detail = await Pixelmap.GetDetailAsync(slug, ct);
        if (detail is null || !detail.CanDownload) return false;
        var item = PixelmapClient.ToDownloadItem(detail, GameRoot);
        if (item is null) return false;

        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + "_.zip");
        try
        {
            await _downloader.DownloadAsync(item, progress, ct);
            var result = MapInstaller.Install(tmp, GameRoot, slug);
            return result.Ok;
        }
        finally
        {
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { /* 忽略 */ }
        }
    }

    public async Task<ExtraResourceInstallResult?> DownloadMapExtraAsync(PixelMapDetail detail, IProgress<double>? progress, CancellationToken ct)
    {
        var item = PixelmapClient.ToExtraDownloadItem(detail, GameRoot);
        if (item is null) return null;

        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + "_.zip");
        try
        {
            await _downloader.DownloadAsync(item, progress, ct);
            return ExtraResourceInstaller.Install(tmp, GameRoot, detail.Title);
        }
        finally
        {
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { /* 忽略 */ }
        }
    }

    /// <summary>实现 ILogger：安装器日志当前仅丢弃（进度由 IProgress 回调驱动）。</summary>
    public void Log(string message) { }
}
