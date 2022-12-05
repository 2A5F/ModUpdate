using ModUpdater.Model;
using ModUpdater.Streams;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ModUpdater
{
    public partial class Main : Form
    {
        string mods_dir;
        ConfigModel config;
        TcpClient client;
        NetworkStream stream;
        ModTask[] modTasks;

        public Main(ConfigModel config, string mods_dir)
        {
            this.config = config;
            this.mods_dir = mods_dir;
            InitializeComponent();
        }

        private void Main_Load(object sender, EventArgs e)
        {
            Task.Run(async () =>
            {
                if (!await ConnectToServerAsync()) return;
                if (!await GetModListAsync()) return;
                Invoke(() =>
                {
                    if (modTasks.Length == 0)
                    {
                        Tips.Text = "没有需要更新的 mod";
                        ButtonOk.Visible = true;
                    }
                    else
                    {
                        Hide();
                        var exec = new Exec(mods_dir, modTasks, config);
                        exec.FormClosed += (_, _) =>
                        {
                            Close();
                            stream.WriteByte((byte)Oper.Close);
                            client.Close();
                        };
                        exec.DoExec(client, stream);
                    }
                });
            }).GetAwaiter();
        }

        async Task<bool> ConnectToServerAsync()
        {
            try
            {
                Invoke(() => Tips.Text = "正在连接服务器");
                client = new();
                await client.ConnectAsync(config.Server, config.Port);
                stream = client.GetStream();
            }
            catch (Exception ex)
            {
                Invoke(() =>
                {
                    Hide();
                    MessageBox.Show(ex.ToString(), "连接服务器失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Application.Exit();
                });
                return false;
            }
            return true;
        }

        async Task<bool> GetModListAsync()
        {
            try
            {
                Invoke(() => Tips.Text = "正在获取 mod 列表");
                stream.WriteByte((byte)Oper.GetModList);
                var op_buf = new byte[sizeof(int)];
                await stream.ReadAsync(op_buf, 0, sizeof(int));
                var len = BitConverter.ToInt32(op_buf, 0);
                var infos = await JsonSerializer.DeserializeAsync<ModInfo[]>(new ReadOnlySubStream(stream, len));
                modTasks = infos.Select(info => new ModTask()
                {
                    Path = Path.Combine(info.Path),
                    RawPath = info.Path,
                    Oper = info.Oper,
                }).ToArray();
            }
            catch (Exception ex)
            {
                Invoke(() =>
                {
                    Hide();
                    MessageBox.Show(ex.ToString(), "获取 mod 列表失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Application.Exit();
                });
                return false;
            }
            return true;
        }

    }
}
