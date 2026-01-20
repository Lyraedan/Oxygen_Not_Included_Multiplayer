using System;
using System.Collections.Generic;
using System.Threading;
using ONI_MP_DedicatedServer;
using ONI_MP_DedicatedServer.ONI;
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
        private static DedicatedTransportServer? server;
        private static SaveFile? saveFile;

        public struct Command
        {
            public string Name;
            public string Description;
            public System.Action Execute;
        }

        private static readonly Dictionary<string, Command> commands = new Dictionary<string, Command>();
        private static bool stopped = true;

        /// <summary>
        /// THIS IS PURELY EXPERIMENTAL!
        /// This is essentially a listening server, it doesn't run the simulation. It listens for network traffic and relays it to the clients.
        /// It will need to be informed about things like the save file etc.
        /// A save file is uploaded by the host, a client connects and downloads that save file, the first client is considered the master and their state is what overwrites the dedi save
        /// If a save action happens on the master, upload it to the dedi, if the master disconnects with clients present, the next client sends the save state to the dedi and it overwrites it with that one
        ///
        /// This is purely conceptual
        /// 
        /// Maybe it'll be better to hold the save file in Memory and use that then only save locally if the server shuts down
        /// 
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Console.WriteLine("ONI Together: Dedicated Server starting...");

            server = SetupTransport();
            stopped = false;

            RegisterCommands();

            try
            {
                string savePath = Path.Combine(ServerConfiguration.ConfigDirectory, ServerConfiguration.Instance.Config.SaveFile);
                saveFile = SaveFile.FromFile($"{savePath}.sav");
                server.Start();

                Console.WriteLine("\nType \"help\" to view a list of commands.");

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
            } catch (Exception ex)
            {
                Console.WriteLine($"Failed to start server: {ex.Message}");
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
                    command.Execute.Invoke();
                }

                if (stopped)
                    break;
            }
        }

        static void RegisterCommands()
        {
            RegisterCommand(new Command
            {
                Name = "quit",
                Description = "Stops the dedicated server",
                Execute = () =>
                {
                    Console.WriteLine("Stopping server...");
                    stopped = true;
                }
            });

            BindExistingCommandTo("stop", "quit");

            RegisterCommand(new Command
            {
                Name = "help",
                Description = "Displays all available commands",
                Execute = () =>
                {
                    Console.WriteLine("Available commands:");
                    foreach (var cmd in commands.Values)
                    {
                        Console.WriteLine($" - {cmd.Name} : {cmd.Description}");
                    }
                }
            });
        }

        public static void RegisterCommand(Command command)
        {
            commands[command.Name.ToLowerInvariant()] = command;
        }

        public static void BindExistingCommandTo(string newBinding, string commandToBindTo)
        {
            if (!commands.TryGetValue(commandToBindTo.ToLowerInvariant(), out var existing))
            {
                Console.WriteLine($"Failed to bind {newBinding} to {commandToBindTo}");
                return;
            }

            RegisterCommand(new Command
            {
                Name = newBinding,
                Description = existing.Description,
                Execute = existing.Execute
            });
        }

        public static DedicatedTransportServer SetupTransport()
        {
            switch (transport) {
                case Transports.Riptide:
                    return new DedicatedRiptideServer();
                default:
                    return new DedicatedRiptideServer();
            }
        }
    }
}
