using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Server
{
    internal class Program
    {
        static HashSet<MinerClient> clients = new HashSet<MinerClient>();
        static byte[] metaBytes;

        const byte id_ping = 0;
        const byte id_pong = 1;
        const byte id_meta = 2;
        const byte id_push = 3;
        const byte id_speed = 4;
        static readonly byte[] ids_ping = { id_ping };
        static readonly byte[] ids_pong = { id_pong };
        static readonly byte[] ids_meta = { id_meta };
        static readonly byte[] ids_push = { id_push };
        static readonly byte[] ids_speed= { id_speed };

        static StringBuilder poolSb = new StringBuilder();

        class Args
        {
            public string url = string.Empty;
            public int node_port = 9933;
            public int port = 9999;
            public string pool_id = string.Empty;
            //public string member_id = string.Empty;
            //public string key = string.Empty;
            public int interval = 200;
            public bool isSolo = false;
        }
        static readonly Args args1 = new Args();

        static void Main(string[] args)
        {
            ArgsParser(args, args1);
            if(args1.url == string.Empty)
                args1.url = $"http://127.0.0.1:{args1.node_port}";

            args1.isSolo = args1.pool_id == string.Empty /*|| args1.member_id == string.Empty || args1.key == string.Empty*/;
            if (args1.isSolo) Console.WriteLine($"[{DateTime.Now}] mode: SOLO");
            else Console.WriteLine($"[{DateTime.Now}] mode: POOL");

            if (args1.isSolo) metaBytes = new byte[96];
            else metaBytes = new byte[209];

            NodeMeta();
            NodeServer();
            MinerTimeout();
            while (true)
            {
                Console.ReadLine();
            }
        }

        static void ArgsParser(string[] args, Args args1)
        {
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].Trim())
                {
                    default: throw new Exception("不支持的参数" + args[i]);
                    case "--node-url": args1.url = args[++i]; break;
                    case "--node-port": args1.node_port = (int)uint.Parse(args[++i]); break;
                    case "--port": args1.port = (int)uint.Parse(args[++i]); break;
                    case "--pool-id": args1.pool_id = args[++i]; break;
                    //case "--member-id": args1.member_id = args[++i]; break;
                    //case "--key": args1.key = args[++i]; break;
                    case "--interval": args1.interval = (int)uint.Parse(args[++i]); break;
                }
            }
        }

        // miner <-> server
        static async void NodeServer()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, args1.port);
            listener.Start();
            Console.WriteLine($"[{DateTime.Now}] Server listening on {args1.port}");
            while (true)
            {
                try
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    Console.WriteLine($"[{DateTime.Now}] ⛏️  : got a connnect: " + client.Client.RemoteEndPoint);
                    client.ReceiveTimeout = 20000;
                    client.SendTimeout    = 20000;
                    client.Client.NoDelay = true;

                    MinerClient miner = new MinerClient(client);
                    miner.Send(ids_meta, metaBytes);

                    lock (clients)
                        clients.Add(miner);

                    ClientRecive(miner);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[{DateTime.Now}] ⛏️  : connnect error: {e.InnerException?.Message ?? e.Message}.");
                }
            }
        }

        // miner <- server <- node
        static async void NodeMeta()
        {
            string? old = null;
            string? now = null;
            while (true)
            {
                try
                {
                    string json = "";

                    using (var web = new WebClient())
                    {
                        web.Headers["Content-Type"] = "application/json; charset=utf-8";

                        if (args1.isSolo)
                        {
                            json = await web.UploadStringTaskAsync(args1.url, "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"poscan_getMeta\"}");
                        }
                        else
                        {
                            json = await web.UploadStringTaskAsync(args1.url, $"{{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"poscan_getMiningParams\",\"params\":[\"{args1.pool_id}\"]}}");
                        }
                    }

                    var jo = JObject.Parse(json);

                    if (jo.ContainsKey("error"))
                    {
                        Console.WriteLine($"[{DateTime.Now}] GetMeta: {jo["error"]["message"]}.");
                        now = null;
                    }
                    else
                    {
                        if (args1.isSolo)
                        {
                            now = (string?)jo["result"];
                        }
                        else
                        {
                            poolSb.Clear();
                            poolSb.Append(((string?)jo["result"][0])[2..].PadLeft(64, '0'));
                            poolSb.Append(((string?)jo["result"][1])[2..].PadLeft(64, '0'));

                            var str = ((string?)jo["result"][2])[2..].PadLeft(64, '0');
                            for (int i = str.Length - 2; i >= 0; i -= 2)
                            {
                                poolSb.Append(str[i..(i + 2)]);
                            }

                            str = ((string?)jo["result"][3])[2..].PadLeft(64, '0');
                            for (int i = str.Length - 2; i >= 0; i -= 2)
                            {
                                poolSb.Append(str[i..(i + 2)]);
                            }

                            poolSb.Append(((string?)jo["result"][4])[2..].PadLeft(64, '0'));

                            now = poolSb.ToString();
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[{DateTime.Now}] GetMeta: {e.InnerException?.Message ?? e.Message}");
                    now = null;
                }

                if (now != old)
                {
                    old = now;

                    Console.WriteLine($"[{DateTime.Now}] NewMeta: {now}");

                    if (string.IsNullOrEmpty(now))
                    {
                        Array.Clear(metaBytes);
                    }
                    else
                    {
                        Hex.Decode(now, metaBytes);
                        if (!args1.isSolo)
                        {
                            Encoding.ASCII.GetBytes(args1.pool_id, metaBytes.AsSpan(160..));
                        }
                    }

                    lock (clients)
                    {
                        foreach (var client in clients)
                        {
                            client.Send(ids_meta, metaBytes);
                        }
                    }
                }

                await Task.Delay(args1.interval);
            }
        }

        // miner -> server -> node
        static async void ClientRecive(MinerClient miner)
        {
            byte[] buffer = new byte[1000 * 1024];

            string ip;
            try { ip = miner.client.Client.RemoteEndPoint.ToString(); }
            catch { ip = "unknow"; }

            while (true)
            {
                try
                {
                    int len = await miner.client.GetStream().ReadAsync(buffer.AsMemory(0, 1));
                    if (len == 0)
                    {
                        Console.WriteLine($"[{DateTime.Now}] ⛏️  [{ip}] recive len 0.");
                        CloseClient(miner);
                        return;
                    }

                    switch (buffer[0])
                    {
                        default:
                            Console.WriteLine($"[{DateTime.Now}] ⛏️  [{ip}] recived error id {buffer[0]}.");
                            CloseClient(miner);
                            return;
                        case id_ping:
                            miner.lastPing = DateTime.Now;
                            await miner.client.GetStream().WriteAsync(ids_pong);
                            break;
                        case id_push:
                            len = 0;
                            do { len += await miner.client.GetStream().ReadAsync(buffer.AsMemory(len..buffer.Length)); }
                            while (buffer[len - 1] != 0);

                            try
                            {
                                Console.WriteLine($"[{DateTime.Now}] ⛏️  [{ip}] recived push.");

                                var push = Encoding.ASCII.GetString(buffer.AsSpan(0, len-1));
                                using (var web = new WebClient())
                                {
                                    web.Headers["Content-Type"] = "application/json; charset=utf-8";
                                    var result = await web.UploadStringTaskAsync(args1.url, push);
                                    Console.WriteLine($"[{DateTime.Now}] 📡  [{ip}] Node echo:{result}.");
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine($"[{DateTime.Now}][{args1.url}] push to node error: {e.InnerException?.Message ?? e.Message}.");
                            }

                            break;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[{DateTime.Now}] ⛏️  [{ip}] got error: {e.InnerException?.Message ?? e.Message}.");
                    CloseClient(miner);
                    return;
                }
            }



        }

        static async void MinerTimeout()
        {
            while (true)
            {
                await Task.Delay(10000);
                lock (clients)
                {
                    var dt = DateTime.Now.AddSeconds(-20);
                    var temp = clients.Where(p => p.lastPing < dt).ToList();
                    clients.ExceptWith(temp);
                    foreach (var client in temp)
                    {
                        string ip;
                        try { ip = client.client.Client.RemoteEndPoint.ToString(); }
                        catch { ip = "unknow"; }

                        client.client.Close();
                        Console.WriteLine($"[{DateTime.Now}] [{ip}] timeout.");
                    }
                }
            }
        }


        static void CloseClient(MinerClient client)
        {
            lock (clients)
            {
                clients.Remove(client);
                client.client.Close();
            }
        }



    }
}