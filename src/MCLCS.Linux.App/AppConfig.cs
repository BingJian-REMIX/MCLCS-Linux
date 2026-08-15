using System;
using System.IO;

namespace MCLCS.Linux.App;

/// <summary>
/// 全局配置路径：用户级数据根目录（主题偏好等落盘位置）。
/// Linux 下映射到 ~/.config/MCLCS。
/// </summary>
public static class AppConfig
{
    /// <summary>用户数据根目录（自动创建）。</summary>
    public static string DataRoot
    {
        get
        {
            var root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MCLCS");
            try { Directory.CreateDirectory(root); } catch { }
            return root;
        }
    }
}
