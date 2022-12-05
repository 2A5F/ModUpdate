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
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ModUpdater;
public partial class Exec : Form
{
    string mods_dir;
    ModTask[] modTasks;
    ConfigModel config;

    public Exec(string mods_dir, ModTask[] modTasks, ConfigModel config)
    {
        this.mods_dir = mods_dir;
        this.modTasks = modTasks;
        this.config = config;
        InitializeComponent();
    }
    private void StopButton_Click(object sender, EventArgs e)
    {
        Close();
    }

    public void DoExec(TcpClient client, NetworkStream stream)
    {
        #region 连接并发度管理

        var the_lock = new object();
        var con_channel = Channel.CreateBounded<(TcpClient client, NetworkStream stream)>(Environment.ProcessorCount);
        con_channel.Writer.TryWrite((client, stream));
        int con_count = 1;

        async Task<(TcpClient client, NetworkStream stream)?> CreateConnect(Label tips)
        {
            try
            {
                if (tips != null) Invoke(() => tips.Text = "正在连接服务器");
                var client = new TcpClient();
                await client.ConnectAsync(config.Server, config.Port);
                var stream = client.GetStream();
                return (client, stream);
            }
            catch (Exception ex)
            {
                Invoke(() =>
                {
                    if (tips != null) Invoke(() => tips.Text = "连接服务器失败");
                    MessageBox.Show(ex.ToString(), "连接服务器失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                });
                return null;
            }
        }

        async ValueTask<(TcpClient client, NetworkStream stream)?> GetConnect(Label tips)
        {
            lock (the_lock)
            {
                if (con_channel.Reader.TryRead(out var con)) return con;
                if (con_count >= Environment.ProcessorCount) goto wait;
                con_count++;
            }

            return await CreateConnect(tips);


        wait:
            return await con_channel.Reader.ReadAsync();
        }

        async Task BackConnect((TcpClient client, NetworkStream stream) con)
        {
            if (con.client.Connected)
            {
                con_channel.Writer.TryWrite(con);
            }
            else
            {
                var new_con = await CreateConnect(null);
                if (new_con == null)
                {
                    MessageBox.Show("遇到无法处理的情况", "崩溃了", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Application.Exit();
                }
                con_channel.Writer.TryWrite(con);
            }
        }

        #endregion

        #region 构造表格

        TheTable.RowCount = modTasks.Length + 1;
        var tasks = modTasks.Select((t, row) =>
         {
             TheTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
             var oper = new StringBuilder();
             switch (t.Oper)
             {
                 case ModOper.Update:
                     oper.Append("更新 ");
                     oper.Append(t.Path);
                     break;
                 case ModOper.Disable:
                     oper.Append("禁用 ");
                     oper.Append(t.Path);
                     break;
                 default:
                     oper.Append("未知操作");
                     break;
             }

             var oper_label = new Label()
             {
                 Text = oper.ToString(),
                 Margin = new Padding(5, 5, 5, 0),
                 AutoSize = true,
             };
             switch (t.Oper)
             {
                 case ModOper.Update:
                     oper_label.ForeColor = Color.ForestGreen;
                     break;
                 case ModOper.Disable:
                     oper_label.ForeColor = Color.DarkRed;
                     break;
             }
             TheTable.Controls.Add(oper_label);
             TheTable.SetRow(oper_label, row + 1);
             TheTable.SetColumn(oper_label, 0);

             var progress_label = new Label()
             {
                 Text = "未开始",
                 Margin = new Padding(5, 5, 5, 0),
             };
             TheTable.Controls.Add(progress_label);
             TheTable.SetRow(progress_label, row + 1);
             TheTable.SetColumn(progress_label, 1);

             ProgressBar progress_bar = null;
             if (t.Oper == ModOper.Update)
             {
                 progress_bar = new ProgressBar
                 {
                     Width = 150
                 };
                 TheTable.Controls.Add(progress_bar);
                 TheTable.SetRow(progress_bar, row + 1);
                 TheTable.SetColumn(progress_bar, 2);
             }

             return (t, oper_label, progress_label, progress_bar);
         }).ToArray();

        #endregion

        Show();

        #region 运行任务

        var task_pg_lock = new object();
        var task_pg = 0;

        Invoke(() => TheProgressBar.Maximum = tasks.Length);

        void FinishTask()
        {
            Interlocked.Increment(ref task_pg);
            Invoke(() =>
            {
                TheProgressBar.Value = task_pg;
                TheProgressTip.Text = $"进度：{task_pg} / {tasks.Length}";
            });
        }

        Task.Run(async () =>
        {
            await Task.WhenAll(tasks.AsParallel().Select(async ti =>
            {
                try
                {
                    switch (ti.t.Oper)
                    {
                        case ModOper.Update:
                            {
                                var path = Path.Combine(mods_dir, ti.t.Path);
                                var del_path = path + ".disabled";
                                if (File.Exists(path))
                                {
                                    Invoke(() => ti.progress_label.Text = "无需操作");
                                    return;
                                }
                                if (File.Exists(del_path))
                                {
                                    File.Move(del_path, path);
                                    Invoke(() => ti.progress_label.Text = "完成");
                                    return;
                                }
                                Directory.CreateDirectory(Path.GetDirectoryName(path));
                                Invoke(() => ti.progress_bar.Style = ProgressBarStyle.Marquee);
                                var con = await GetConnect(ti.progress_label);
                                if (con == null) return;
                                try
                                {
                                    var client = con.Value.client;
                                    var stream = con.Value.stream;
                                    Invoke(() => ti.progress_label.Text = "请求中");
                                    stream.WriteByte((byte)Oper.Download);
                                    {
                                        using var ms = new MemoryStream();
                                        await JsonSerializer.SerializeAsync(ms, ti.t.RawPath);
                                        var op_buf = BitConverter.GetBytes((int)ms.Length);
                                        await stream.WriteAsync(op_buf, 0, sizeof(int));
                                        ms.Position = 0;
                                        await ms.CopyToAsync(stream);
                                    }
                                    {
                                        var op_buf = new byte[sizeof(long)];
                                        await stream.ReadAsync(op_buf, 0, sizeof(long));
                                        var len = BitConverter.ToInt64(op_buf, 0);
                                        Invoke(() =>
                                        {
                                            ti.progress_label.Text = "下载中 0%";
                                            ti.progress_bar.Style = ProgressBarStyle.Continuous;
                                        });
                                        var read_stream = new ReadOnlySubStream(stream, len);
                                        read_stream.OnUpdatePosition += p => Invoke(() =>
                                        {
                                            var rat = len == 0 ? 100 : (int)((decimal)p / len * 100);
                                            var pg = ti.progress_bar.Value = rat;
                                            ti.progress_label.Text = $"下载中 {pg}%";
                                        });
                                        using var fs = File.Open(path, FileMode.Create);
                                        await read_stream.CopyToAsync(fs);
                                        await fs.FlushAsync();
                                        Invoke(() => ti.progress_label.Text = "完成");
                                    }
                                }
                                finally
                                {
                                    await BackConnect(con.Value);
                                }
                            }
                            break;
                        case ModOper.Disable:
                            {
                                var path = Path.Combine(mods_dir, ti.t.Path);
                                var new_path = path + ".disabled";
                                if (File.Exists(path))
                                {
                                    File.Move(path, new_path);
                                    Invoke(() => ti.progress_label.Text = "完成");
                                }
                                else
                                {
                                    Invoke(() => ti.progress_label.Text = "无需操作");
                                }
                            }
                            break;
                        default:
                            Invoke(() => ti.progress_label.Text = "");
                            return;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    FinishTask();
                }
            }).ToArray());

            Invoke(() =>
            {
                StopButton.Text = "完成";
            });
        
        }).GetAwaiter();

        #endregion
    }
}
