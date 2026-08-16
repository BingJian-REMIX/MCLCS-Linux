using MCLCS.Core.Download;
using MCLCS.Core.Profiles;
using Xunit;

namespace MCLCS.Linux.Tests;

/// <summary>
/// 锁定「下载源偏好驱动 MirrorPolicy 重排」（设置 → 下载 链接 core）：
/// MirrorFirst 时 BMCLAPI 候选在前，OfficialFirst 时官方源候选在前。
/// </summary>
public class MirrorPolicyTests
{
    [Fact]
    public void VersionManifestUrls_MirrorFirst_PutsBmclapiFirst()
    {
        MirrorPolicy.Preference = DownloadSourcePreference.MirrorFirst;
        var urls = MirrorPolicy.VersionManifestUrls().ToList();
        Assert.StartsWith("https://bmclapi2.bangbang93.com", urls[0]);
        Assert.StartsWith("https://piston-meta.mojang.com", urls[1]);
    }

    [Fact]
    public void VersionManifestUrls_OfficialFirst_PutsOfficialFirst()
    {
        MirrorPolicy.Preference = DownloadSourcePreference.OfficialFirst;
        try
        {
            var urls = MirrorPolicy.VersionManifestUrls().ToList();
            Assert.StartsWith("https://piston-meta.mojang.com", urls[0]);
            Assert.StartsWith("https://bmclapi2.bangbang93.com", urls[1]);
        }
        finally
        {
            MirrorPolicy.Preference = DownloadSourcePreference.MirrorFirst;
        }
    }

    [Fact]
    public void LibraryUrls_HonorsPreference()
    {
        MirrorPolicy.Preference = DownloadSourcePreference.OfficialFirst;
        try
        {
            var urls = MirrorPolicy.LibraryUrls("a/b.jar").ToList();
            Assert.StartsWith("https://libraries.minecraft.net", urls[0]);
        }
        finally
        {
            MirrorPolicy.Preference = DownloadSourcePreference.MirrorFirst;
        }
    }
}
