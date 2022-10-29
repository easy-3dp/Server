using System.Net.Sockets;

namespace Server
{
    internal class MinerClient
    {
        public TcpClient client;
        public string    ip;
        public DateTime  lastPing;

        public MinerClient(TcpClient client)
        {
            this.client = client;
            this.lastPing = DateTime.Now;

            try { ip = client.Client.RemoteEndPoint.ToString(); }
            catch { ip = "unknow"; }
        }

        public async void Send(byte[] id_meta, byte[] metaBytes)
        {
            try
            {
                await client.GetStream().WriteAsync(id_meta);
                await client.GetStream().WriteAsync(metaBytes);
            }
            catch
            {
            }
        }
    }
}
