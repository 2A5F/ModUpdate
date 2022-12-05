using System;

namespace ModUpdater.Model
{
    public class ConfigModel
    {
        /// <summary>
        /// 获取配置的服务器地址
        /// </summary>
        public string Server { get; set; }
        /// <summary>
        /// 端口
        /// </summary>
        public ushort Port { get; set; }
    }
}
