using ModUpdater.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ModUpdater
{
    public partial class Config : Form
    {
        public string config_file_path;
        public ConfigModel config;

        public Config(string config_file_path)
        {
            this.config_file_path = config_file_path;
            InitializeComponent();
        }

        private void ButtonOk_Click(object sender, EventArgs e)
        {
            try
            {
                config = new ConfigModel()
                {
                    Server = ServerAddr.Text,
                    Port = ushort.TryParse(ServerPort.Text, out var port) ? port : default,
                };
                Task.Run(async () =>
                {
                    using var fs = File.Open(config_file_path, FileMode.Create);
                    await JsonSerializer.SerializeAsync(fs, config, new JsonSerializerOptions() { WriteIndented = true });
                    await fs.FlushAsync();
                }).Wait();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "保存配置文件失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
                return;
            }

            Close();
        }
    }
}
