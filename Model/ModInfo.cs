using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

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

public class ModInfoSerPack
{
    public Dictionary<string, ModInfo> dict { get; set; } = null!;
    public IEnumerable<ModInfo> modInfos { get; set; } = null!;
}

[JsonSerializable(typeof(ModInfoSerPack), GenerationMode = JsonSourceGenerationMode.Metadata)]
public partial class ModInfoSerPackJsonContext : JsonSerializerContext { }
