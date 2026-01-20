using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONI_MP_DedicatedServer.Transports
{
    public abstract class DedicatedTransportServer
    {
        public abstract void Start();

        public abstract void Stop();

        public abstract bool IsRunning();
    }
}
