namespace ModUpdater.Model;

public enum Oper : byte
{
    /// <summary>
    /// 关闭连接
    /// </summary>
    Close,
    /// <summary>
    /// 获取 mod 列表 ; 服务器返回 长度 (int) 内容 (json 字符串)
    /// </summary>
    GetModList,
    /// <summary>
    /// 下载 ; 需要尾随 长度 (int) 文件名 (json 字符串) ; 服务器返回 长度 (long) 数据（字节流） 
    /// </summary>
    Download,
}
