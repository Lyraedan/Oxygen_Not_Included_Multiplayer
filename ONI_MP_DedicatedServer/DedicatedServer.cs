using System;
using System.Collections.Generic;
using System.Threading;
using ONI_MP_DedicatedServer.Transports;

namespace ONI_MP.DedicatedServer
{
    public class DedicatedServer
    {
        public enum Transports
        {
            Riptide = 0
        }

        public static Transports transport = Transports.Riptide;
        private static TransportServer? server;

        private static readonly Dictionary<string, System.Action> commands = new();
        private static bool stopped = true;

        /// <summary>
        /// THIS IS PURELY EXPERIMENTAL!
        /// This is essentially a listening server, it doesn't run the simulation. It listens for network traffic and relays it to the clients.
        /// It will need to be informed about things like the save file etc.
        /// A save file is uploaded by the host, a client connects and downloads that save file, the first client is considered the master and their state is what overwrites the dedi save
        /// If a save action happens on the master, upload it to the dedi, if the master disconnects with clients present, the next client sends the save state to the dedi and it overwrites it with that one
        ///
        /// This is purely conceptual
        /// </summary>
        /// <param name="args"></param>

        static void Main(string[] args)
        {
            Console.WriteLine("ONI Multiplayer Dedicated Server starting...");

            server = SetupTransport();
            stopped = false;

            RegisterCommands();

            server.Start();

            var inputThread = new Thread(ReadConsole)
            {
                IsBackground = true
            };
            inputThread.Start();

            while (server.IsRunning())
            {
                if (stopped)
                {
                    server.Stop();
                    break;
                }

                Thread.Sleep(50);
            }

            Console.WriteLine("Server stopped. Press Enter to close.");
            Console.ReadLine();
        }

        static void ReadConsole()
        {
            if (server == null)
                return;

            while (server.IsRunning())
            {
                var line = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (commands.TryGetValue(line.ToLowerInvariant(), out var command))
                {
                    command.Invoke();
                }

                if (stopped)
                    break;
            }
        }

        static void RegisterCommands()
        {
            RegisterCommand("quit", () =>
            {
                Console.WriteLine("Stopping server...");
                stopped = true;
            });

            BindExistingCommandTo("stop", "quit");
        }

        public static void RegisterCommand(string command, System.Action execution)
        {
            commands[command.ToLowerInvariant()] = execution;
        }

        public static void BindExistingCommandTo(string newBinding, string commandToBindTo)
        {
            if (!commands.TryGetValue(commandToBindTo.ToLowerInvariant(), out var execution))
            {
                Console.WriteLine($"Failed to bind {newBinding} to {commandToBindTo}");
                return;
            }

            RegisterCommand(newBinding, execution);
        }

        public static TransportServer SetupTransport()
        {
            switch (transport) {
                case Transports.Riptide:
                    return new RiptideServer();
                default:
                    return new RiptideServer();
            }
        }
    }
}
