using Xunit;

// 皮肤编辑器 fixture 会初始化 headless App（改全局 ThemeManager/Application 状态），
// 与其余测试并行会相互干扰（Theme 相关断言偶发失败）。测试量小，统一串行。
[assembly: CollectionBehavior(DisableTestParallelization = true)]
