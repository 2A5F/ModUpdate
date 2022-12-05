using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModUpdater.Model;

public class ModTask
{
    /// <summary>
    /// mod 的相对 mods 文件夹的路径
    /// </summary>
    public string Path { get; set; } = null!;
    /// <summary>
    /// 跨平台的路径描述
    /// </summary>
    public string[] RawPath { get; set; } = null!;
    /// <summary>
    /// 要执行的操作
    /// </summary>
    public ModOper Oper { get; set; }
}
