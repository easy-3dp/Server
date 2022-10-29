using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Server
{
    internal class Program
    {
        const string hashrateUrl = "http://120.46.172.54:6666/upload";
        static string url;
        static int port;
        static HashSet<MinerClient> clients = new HashSet<MinerClient>();
        static byte[] metaBytes = new byte[96];

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

        static void Main(string[] args)
        {
            //args = new string[] { "9933", "9999" };
            url = $"http://127.0.0.1:{args[0]}";
            //url = $"http://192.168.31.129:9933";
            port = int.Parse(args[1]);
            NodeMeta();
            NodeServer();
            MinerTimeout();
            while (true)
            {
                Console.ReadLine();
            }
        }

        // miner <-> server
        static async void NodeServer()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            Console.WriteLine($"[{DateTime.Now}] Server listening on {port}");
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
                    continue;
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
                        json = await web.UploadStringTaskAsync(url, "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"poscan_getMeta\"}");
                    }

                    var jo = JObject.Parse(json);
                    now = (string?)jo["result"];
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[{DateTime.Now}][{url}] GetMeta: {e.InnerException?.Message ?? e.Message}.");
                    now = null;
                }

                if (now != old)
                {
                    old = now;

                    if (string.IsNullOrEmpty(now))
                    {
                        Array.Clear(metaBytes);
                    }
                    else
                    {
                        Hex.Decode(now, metaBytes);
                    }

                    lock (clients)
                    {
                        foreach (var client in clients)
                        {
                            client.Send(ids_meta, metaBytes);
                        }
                    }
                }

                await Task.Delay(200);
            }
        }

        // miner -> server -> node
        static async void ClientRecive(MinerClient miner)
        {
            byte[] body_buffer = new byte[100 * 1024];
            byte[] id_buffer   = new byte[1];

            string ip;
            try { ip = miner.client.Client.RemoteEndPoint.ToString(); }
            catch { ip = "unknow"; }

            while (true)
            {
                try
                {
                    int len = await miner.client.GetStream().ReadAsync(id_buffer.AsMemory(0, 1));
                    if (len == 0)
                    {
                        Console.WriteLine($"[{DateTime.Now}] ⛏️  [{ip}] recive len 0.");
                        CloseClient(miner);
                        return;
                    }

                    switch (id_buffer[0])
                    {
                        default:
                            Console.WriteLine($"[{DateTime.Now}] ⛏️  [{ip}] recived error id {id_buffer[0]}.");
                            CloseClient(miner);
                            return;
                        case id_ping:
                            miner.lastPing = DateTime.Now;
                            await miner.client.GetStream().WriteAsync(ids_pong);
                            break;
                        case id_push:
                            len = 0;
                            do { len += await miner.client.GetStream().ReadAsync(body_buffer.AsMemory(len..body_buffer.Length)); }
                            while (body_buffer[len - 1] != 0);

                            try
                            {
                                Console.WriteLine($"[{DateTime.Now}] ⛏️  [{ip}] recived push.");

                                var push = Encoding.ASCII.GetString(body_buffer.AsSpan(0, len-1));
                                using (var web = new WebClient())
                                {
                                    web.Headers["Content-Type"] = "application/json; charset=utf-8";
                                    var r = await web.UploadStringTaskAsync(hashrateUrl, push);
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine($"[{DateTime.Now}][{url}] push to node error: {e.InnerException?.Message ?? e.Message}.");
                            }

                            break;
                        case id_speed:
                            len = 0;
                            do { len += await miner.client.GetStream().ReadAsync(body_buffer.AsMemory(len..body_buffer.Length)); }
                            while (body_buffer[len - 1] != 0);

                            try
                            {
                                var push = Encoding.ASCII.GetString(body_buffer.AsSpan(0, len - 1));
                                using (var web = new WebClient())
                                {
                                    web.Headers["Content-Type"] = "application/json; charset=utf-8";
                                    var r = await web.UploadStringTaskAsync(url, push);
                                }
                            }
                            catch
                            {
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