using System.IO;
using MCLCS.Core.Profiles;
using Xunit;

namespace MCLCS.Linux.Tests;

/// <summary>
/// 锁定「更改游戏目录后存档跟随搬家」的核心行为（对齐 WPF 的 GameRoot 迁移语义）：
/// 旧目录的 mclcs_profiles.json 应迁移到新目录，且不能因找不到存档而清空用户设置。
/// </summary>
public class ProfileStoreTests
{
    [Fact]
    public void Migrate_MovesProfileFromOldDirToNewDir()
    {
        var oldDir = Path.Combine(Path.GetTempPath(), "mclcs_migrate_old_" + Path.GetRandomFileName());
        var newDir = Path.Combine(Path.GetTempPath(), "mclcs_migrate_new_" + Path.GetRandomFileName());
        try
        {
            ProfileStore.Save(new LauncherProfile { DefaultUsername = "MigratedUser", GameRoot = oldDir });
            var oldPath = Path.Combine(oldDir, "mclcs_profiles.json");
            var newPath = Path.Combine(newDir, "mclcs_profiles.json");
            Assert.True(File.Exists(oldPath));

            ProfileStore.Migrate(oldDir, newDir);

            // 新目录出现存档、旧目录副本消失
            Assert.True(File.Exists(newPath));
            Assert.False(File.Exists(oldPath));
            // 内容完整迁移，设置不丢
            Assert.Equal("MigratedUser", ProfileStore.Load(newDir).DefaultUsername);
        }
        finally
        {
            if (Directory.Exists(oldDir)) Directory.Delete(oldDir, true);
            if (Directory.Exists(newDir)) Directory.Delete(newDir, true);
        }
    }

    [Fact]
    public void Migrate_NewDirHasExistingProfile_KeepsNewAndDropsOld()
    {
        var oldDir = Path.Combine(Path.GetTempPath(), "mclcs_migrate_old2_" + Path.GetRandomFileName());
        var newDir = Path.Combine(Path.GetTempPath(), "mclcs_migrate_new2_" + Path.GetRandomFileName());
        try
        {
            ProfileStore.Save(new LauncherProfile { DefaultUsername = "Old", GameRoot = oldDir });
            ProfileStore.Save(new LauncherProfile { DefaultUsername = "New", GameRoot = newDir });

            ProfileStore.Migrate(oldDir, newDir);

            // 新目录已有配置不被覆盖，旧副本被清掉
            Assert.Equal("New", ProfileStore.Load(newDir).DefaultUsername);
            Assert.True(File.Exists(Path.Combine(newDir, "mclcs_profiles.json")));
            Assert.False(File.Exists(Path.Combine(oldDir, "mclcs_profiles.json")));
        }
        finally
        {
            if (Directory.Exists(oldDir)) Directory.Delete(oldDir, true);
            if (Directory.Exists(newDir)) Directory.Delete(newDir, true);
        }
    }

    [Fact]
    public void Migrate_SameDir_NoThrow()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mclcs_migrate_same_" + Path.GetRandomFileName());
        try
        {
            ProfileStore.Save(new LauncherProfile { GameRoot = dir });
            var ex = Record.Exception(() => ProfileStore.Migrate(dir, dir));
            Assert.Null(ex);
            Assert.True(File.Exists(Path.Combine(dir, "mclcs_profiles.json")));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }
}
