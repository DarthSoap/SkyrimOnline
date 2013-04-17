﻿using Lidgren.Network;
using Skyrim.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Skyrim.MasterServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Dictionary<long, Object[]> registeredHosts = new Dictionary<long, Object[]>();

            NetPeerConfiguration config = new NetPeerConfiguration("masterserver");
            config.SetMessageTypeEnabled(NetIncomingMessageType.UnconnectedData, true);
            config.Port = Skyrim.API.MasterServer.MasterServerPort;

            NetPeer peer = new NetPeer(config);
            peer.Start();

            // keep going until ESCAPE is pressed
            Console.WriteLine("Press ESC to quit");
            while (!Console.KeyAvailable || Console.ReadKey().Key != ConsoleKey.Escape)
            {
                NetIncomingMessage msg;
                while ((msg = peer.ReadMessage()) != null)
                {
                    switch (msg.MessageType)
                    {
                        case NetIncomingMessageType.UnconnectedData:
                            // by design, the first byte always indicates action
                            switch ((MasterServerMessageType)msg.ReadByte())
                            {
                                case MasterServerMessageType.RegisterHost:

                                    // It's a host wanting to register its presence
                                    var id = msg.ReadInt64(); // server unique identifier
                                    var name = msg.ReadString();
                                    var population = msg.ReadUInt16();
                                    var maxPopulation = msg.ReadUInt16();

                                    Console.WriteLine("Got registration for host " + id);
                                    registeredHosts[id] = new Object[]
									{
										msg.ReadIPEndPoint(), // internal
										msg.SenderEndPoint, // external
                                        name,
                                        population,
                                        maxPopulation,
                                        NetTime.Now
									};
                                    break;

                                case MasterServerMessageType.RequestHostList:
                                    // It's a client wanting a list of registered hosts
                                    Console.WriteLine("Sending list of " + registeredHosts.Count + " hosts to client " + msg.SenderEndPoint);
                                    List<long> toRemove = new List<long>();
                                    foreach (var kvp in registeredHosts)
                                    {
                                        if ((double)kvp.Value[5] + 130.0 < NetTime.Now)
                                        {
                                            toRemove.Add(kvp.Key);
                                            continue;
                                        }
                                        // send registered host to client
                                        NetOutgoingMessage om = peer.CreateMessage();
                                        om.Write(kvp.Key);
                                        om.Write((string)kvp.Value[2]);
                                        om.Write((UInt16)kvp.Value[3]);
                                        om.Write((UInt16)kvp.Value[4]);
                                        peer.SendUnconnectedMessage(om, msg.SenderEndPoint);
                                    }

                                    foreach (var kvp in toRemove)
                                    {
                                        registeredHosts.Remove(kvp);
                                    }

                                    break;
                                case MasterServerMessageType.RequestIntroduction:
                                    // It's a client wanting to connect to a specific (external) host
                                    IPEndPoint clientInternal = msg.ReadIPEndPoint();
                                    long hostId = msg.ReadInt64();
                                    string token = msg.ReadString();

                                    Console.WriteLine(msg.SenderEndPoint + " requesting introduction to " + hostId + " (token " + token + ")");

                                    // find in list
                                    Object[] elist;
                                    if (registeredHosts.TryGetValue(hostId, out elist))
                                    {
                                        // found in list - introduce client and host to eachother
                                        Console.WriteLine("Sending introduction...");
                                        peer.Introduce(
                                            (IPEndPoint)elist[0], // host internal
                                            (IPEndPoint)elist[1], // host external
                                            clientInternal, // client internal
                                            msg.SenderEndPoint, // client external
                                            token // request token
                                        );
                                    }
                                    else
                                    {
                                        Console.WriteLine("Client requested introduction to nonlisted host!");
                                    }
                                    break;
                            }
                            break;

                        case NetIncomingMessageType.DebugMessage:
                        case NetIncomingMessageType.VerboseDebugMessage:
                        case NetIncomingMessageType.WarningMessage:
                        case NetIncomingMessageType.ErrorMessage:
                            // print diagnostics message
                            Console.WriteLine(msg.ReadString());
                            break;
                    }
                }
            }

            peer.Shutdown("shutting down");
        }
    }
}