using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONI_MP.Networking.Relay.Lan
{
    public class LanServer : RelayServer
    {
        public enum TransportType
        {
            DIRECT,
            STUN // TODO: Research
        }

        public override void Start()
        {
        }

        public override void Stop()
        {
        }

        public override void Update()
        {
        }

        public override void CloseConnections()
        {
        }

        public override void OnMessageRecieved()
        {
        }

        public override void Prepare()
        {
        }
    }
}
