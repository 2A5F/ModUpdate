namespace ModUpdater.Model;

public class ModInfo
{
    /// <summary>
    /// mod 的相对 mods 文件夹的路径
    /// </summary>
    public string[] Path { get; set; } = null!;
    /// <summary>
    /// 要执行的操作
    /// </summary>
    public ModOper Oper { get; set; }
}

public enum ModOper
{
    /// <summary>
    /// 更新
    /// </summary>
    Update = 1,
    /// <summary>
    /// 禁用
    /// </summary>
    Disable,
}
