using ModUpdater.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.Json;
using System.Threading;
using Microsoft.VisualStudio.Threading;

namespace ModUpdater
{
    internal static class Program
    {
        const string ConfigFileName = "ModUpdater.json";

        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var mods_dir = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), ".minecraft", "mods");
            if (!Directory.Exists(mods_dir))
            {
                MessageBox.Show("未找到 .minecraft/mods 文件夹，请同步器放在启动器同级", "启动失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var config_file_path = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), ConfigFileName);
            if (File.Exists(config_file_path))
            {
                ConfigModel config = null;
                try
                {
                    Task.Run(async () =>
                    {
                        using var fs = File.OpenRead(config_file_path);
                        config = await JsonSerializer.DeserializeAsync<ConfigModel>(fs);
                    })
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
                        .Wait();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "加载配置文件失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return; 
                }
                Application.Run(new Main(config, mods_dir));
            }
            else
            {
                var c = new Config(config_file_path);
                Application.Run(c);
                if (c.config != null)
                {
                    Application.Run(new Main(c.config, mods_dir));
                }
            }
        }
    }
}
