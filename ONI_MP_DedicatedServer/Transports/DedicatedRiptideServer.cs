using Riptide;
using Riptide.Utils;

namespace ONI_MP_DedicatedServer.Transports
{
    public class DedicatedRiptideServer : DedicatedTransportServer
    {
        public Server? _server;

        public DedicatedRiptideServer()
        {
            RiptideLogger.Initialize(Console.WriteLine, false);
        }

        public override void Start()
        {
            if (IsRunning())
                return;

            string ip = ServerConfiguration.Instance.Config.Ip;
            int port = ServerConfiguration.Instance.Config.Port;

            _server = new Server("ONI Together: Dedicated Server");
            _server.MessageReceived += OnServerMessageReceived;
            _server.Start((ushort)port, 1, useMessageHandlers: false);
            Console.WriteLine($"Started server on {ip}:{port}");
        }

        private void OnServerMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Console.WriteLine("Recieved message");
        }

        public override void Stop()
        {
            if (!IsRunning())
                return;

            _server.Stop();
        }

        public override bool IsRunning()
        {
            if (_server == null)
                return false;

            return _server.IsRunning;
        }
    }
}
