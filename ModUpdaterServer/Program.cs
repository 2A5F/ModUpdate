using ModUpdater.Model;
using ModUpdater.Streams;
using ModUpdaterServer.Model;
using Serilog;
using System.Buffers;
using System.IO.Enumeration;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/.log", rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true)
    .CreateLogger();

const string ConfigFileName = "ModUpdaterServer.json";
const string ModListFileName = "modlist.json";
const string Mods = "mods";
const string TmpMods = ".tmp/mods";

const string SyncExt = ".jar";
const string DelExt = ".jar.del";
const string DelRawExt = ".del";

const string HelpCmd = "help";
const string SyncCmd = "sync";
const string ExitCmd = "exit";

var current_dir = Directory.GetCurrentDirectory();

var config_file_path = Path.Combine(current_dir, ConfigFileName);

ConfigModel config = null!;

Console.Clear();

if (!File.Exists(config_file_path))
{
    Console.WriteLine("未发现配置文件，正在进行初次配置");
    Console.WriteLine("");

    config = new ConfigModel();

    Console.WriteLine("请输入端口号");
    for (; ; )
    {
        var line = Console.ReadLine();
        if (ushort.TryParse(line, out var port))
        {
            config.Port = port;
            break;
        }
        else
        {
            Console.WriteLine("输入格式有误，请重新输入");
        }
    }

    using var fs = File.Open(config_file_path, FileMode.Create);
    await JsonSerializer.SerializeAsync(fs, config, new SerPackJsonContext(new JsonSerializerOptions() { WriteIndented = true }).ConfigModel);
    await fs.FlushAsync();
}
else
{
    using var fs = File.OpenRead(config_file_path);
    config = (await JsonSerializer.DeserializeAsync(fs, SerPackJsonContext.Default.ConfigModel))!;
}

Console.Clear();

PrintHelp();
void PrintHelp()
{
    Console.WriteLine($"========================= 使用提示 =========================");
    Console.WriteLine($"");
    Console.WriteLine($"1. 在 {Mods} 文件夹里放置 mod （不要和 mc 服务器的 mods 放一起）");
    Console.WriteLine($"2. mod 文件名后缀是 {SyncExt} 的会让客户端下载");
    Console.WriteLine($"3. mod 文件名后缀是 {DelExt} 的会让客户端禁用");
    Console.WriteLine($"4. 将生成 {ModListFileName} 缓存 mod 列表（仅供查看，修改此文件不会更新列表）");
    Console.WriteLine($"5. 需要下载的 mod 将会被复制到 {TmpMods} 文件夹下以便修改 mod 列表");
    Console.WriteLine($"6. 在控制台输入 {SyncCmd} 来刷新 mod 列表缓存");
    Console.WriteLine($"7. 在控制台输入 {ExitCmd} 退出同步服务");
    Console.WriteLine($"");
}

var mods_dir = Path.Combine(current_dir, Mods);
var tmp_mods_dir = Path.Combine(current_dir, TmpMods);
var modlist_file_path = Path.Combine(current_dir, ModListFileName);

Directory.CreateDirectory(mods_dir);
Directory.CreateDirectory(tmp_mods_dir);

Dictionary<string, ModInfo> modlist = null!;

await Sync(false);
async Task Sync(/* 是否输出控制台提示 */ bool tip)
{
    if (tip) Log.Information("加载 mod 列表中");
    // 检查 mods 文件夹，生成 mod 列表信息
    modlist = new Dictionary<string, ModInfo>(new FileSystemEnumerable<KeyValuePair<string, ModInfo>>(
        directory: mods_dir!,
        transform: (ref FileSystemEntry entry) =>
        {
            var path = entry.ToFullPath();
            var oper = path.EndsWith(DelExt) ? ModOper.Disable : ModOper.Update;
            var rel_path = Path.GetRelativePath(mods_dir, path);
            if (oper == ModOper.Disable) rel_path = Path.GetFileNameWithoutExtension(rel_path);
            return new KeyValuePair<string, ModInfo>(rel_path, new ModInfo()
            {
                Path = rel_path.Split(Path.DirectorySeparatorChar),
                Oper = oper,
            });
        },
        options: new EnumerationOptions()
        {
            RecurseSubdirectories = true
        })
    {
        ShouldIncludePredicate = (ref FileSystemEntry entry) => !entry.IsDirectory && Path.GetExtension(entry.ToFullPath()) is SyncExt or DelRawExt,
    }.DistinctBy(kv => kv.Key));

    // 写入 modlist.json
    using var fs = File.Open(modlist_file_path, FileMode.Create);
    var saved_modlist = modlist.GroupBy(kv => kv.Value.Oper).ToDictionary(g => g.Key.ToString().ToLower(), g => g.Select(kv => kv.Key));
    await JsonSerializer.SerializeAsync(fs, saved_modlist, new JsonSerializerOptions() { WriteIndented = true });

    Directory.Delete(tmp_mods_dir, true);
    Directory.CreateDirectory(tmp_mods_dir);

    // 把需要更新的 mod 复制到暂存文件夹
    await Task.WhenAll(modlist.AsParallel()
         .Select(async kv =>
         {
             if (kv.Value.Oper != ModOper.Update) return;
             var source = Path.Combine(mods_dir, kv.Key);
             var target = Path.Combine(tmp_mods_dir, kv.Key);
             Directory.CreateDirectory(Path.GetDirectoryName(target)!);

             using var source_fs = File.OpenRead(source);
             using var target_fs = File.Open(target, FileMode.Create);

             await source_fs.CopyToAsync(target_fs);
             await target_fs.FlushAsync();

         }).ToArray());


    if (tip) Log.Information("已完成 mod 列表加载");
}

var ip = new IPEndPoint(IPAddress.Any, config.Port);
var server = new TcpListener(ip);
server.Start();

Log.Information("服务在 {ip} 上启动", ip);

var server_task = Task.Run(async () =>
{
    for (; ; )
    {
        try
        {
            var client = await server.AcceptTcpClientAsync();
            Task.Run(async () =>
            {
                try
                {
                    await ClientService(client);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{msg}", ex.Message);
                };
            }).GetAwaiter();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "{msg}", ex.Message);
        }
    }
});

async Task ClientService(TcpClient client)
{
    var con_id = Guid.NewGuid();
    var remote = client.Client.RemoteEndPoint;
    Log.Information("已与 {ep} 建立连接, 连接 id: {id}", remote, con_id);
    var op_buf = ArrayPool<byte>.Shared.Rent(16);
    using var stream = client.GetStream();
    try
    {
        for (; ; )
        {
            #region 读取操作

            var rec_time_out_token_source = new CancellationTokenSource();
            var rec_ok_token_source = new CancellationTokenSource();

            var rec_task = stream.ReadAsync(op_buf.AsMemory(0, 1), rec_time_out_token_source.Token).AsTask();
            var rec_time_out_task = Task.Delay(new TimeSpan(0, 5, 0), rec_ok_token_source.Token); // 5 分钟超时

            if (rec_task != await Task.WhenAny(rec_task, rec_time_out_task))
            {
                rec_time_out_token_source.Cancel();
                Log.Warning("{ep} ({id}) 长时间无响应", remote, con_id);
                client.Close();
                return;
            }
            rec_ok_token_source.Cancel();

            var rec_count = await rec_task;
            if (rec_count == 0)
            {
                Log.Warning("{ep} ({id}) 发送了 0 字节", remote, con_id);
                client.Close();
                return;
            }
            var oper = (Oper)op_buf[0];

            #endregion

            switch (oper)
            {
                case Oper.Close:
                    client.Close();
                    return;
                case Oper.GetModList:
                    {
                        Log.Information("{ep} ({id}) 请求了 mod 列表", remote, con_id);
                        using var ms = new MemoryStream();
                        await JsonSerializer.SerializeAsync(ms, modlist.Values, ModInfoSerPackJsonContext.Default.IEnumerableModInfo);
                        BitConverter.TryWriteBytes(op_buf.AsSpan(0, sizeof(int)), (int)ms.Length);
                        await stream.WriteAsync(op_buf.AsMemory(0, sizeof(int)));
                        ms.Position = 0;
                        await ms.CopyToAsync(stream);
                    }
                    break;
                case Oper.Download:
                    {
                        var c = await stream.ReadAsync(op_buf.AsMemory(0, sizeof(int)));
                        var len = BitConverter.ToInt32(op_buf.AsSpan(0, sizeof(int)));
                        if (c != sizeof(int) || len <= 0)
                        {
                            Log.Warning("{ep} ({id}) 发送了错误的数据", remote, con_id);
                            client.Close();
                            return;
                        }
                        var path_parts = await JsonSerializer.DeserializeAsync(new ReadOnlySubStream(stream, len), ModInfoSerPackJsonContext.Default.StringArray);
                        var path = Path.Combine(path_parts!);
                        if (!modlist.TryGetValue(path, out var info))
                        {
                            Log.Warning("{ep} ({id}) 发送了错误的数据", remote, con_id);
                            client.Close();
                            return;
                        }
                        var real_path = Path.Combine(tmp_mods_dir, path);
                        using var fs = File.OpenRead(real_path);
                        Log.Information("{ep} ({id}) 请求下载 {name}", remote, con_id, path);
                        BitConverter.TryWriteBytes(op_buf.AsSpan(0, sizeof(long)), fs.Length);
                        await stream.WriteAsync(op_buf.AsMemory(0, sizeof(long)));
                        await fs.CopyToAsync(stream);
                    }
                    break;
                default:
                    Log.Warning("{ep} ({id}) 发送了未知的操作 {oper}", remote, con_id, oper);
                    client.Close();
                    return;
            }
        }
    }
    finally
    {
        ArrayPool<byte>.Shared.Return(op_buf);
        Log.Information("已与 {ep} ({id}) 断开连接", remote, con_id);
    }
}

var cmd_task = Task.Run(async () =>
{
    for (; ; )
    {
        var cmd = Console.ReadLine();
        switch (cmd)
        {
            case HelpCmd:
                PrintHelp();
                break;
            case SyncCmd:
                await Sync(true);
                break;
            case ExitCmd:
                await Log.CloseAndFlushAsync();
                Environment.Exit(0);
                break;
            default:
                Console.WriteLine("未知指令，输入 help 查看帮助");
                break;
        }
    }
});

await Task.WhenAll(server_task, cmd_task);
