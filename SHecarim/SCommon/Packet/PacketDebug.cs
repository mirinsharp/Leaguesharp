﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;

namespace SCommon.Packet
{
    public static class PacketDebug
    {
        private static byte[] s_NetworkId;
        private static bool s_debugSent;
        private static bool s_debugReceived;
        private static bool s_debugReceivedOnlyWithMyNetId;
        private static List<int> s_blockedOpcodes;
        private static int s_debugOnlyOpcode;

        /// <summary>
        /// Starts Packet Debugging
        /// </summary>
        /// <param name="debugSent">if <c>true</c>, debugs sent packets</param>
        /// <param name="debugReceived">if <c>true</c>, debugs received packets</param>
        /// <param name="onlyWithMyNetId">if <c>true</c>, debugs packets which has own hero's network id</param>
        public static void Start(bool debugSent, bool debugReceived, bool onlyWithMyNetId = false)
        {
            s_debugSent = debugSent;
            s_debugReceived = debugReceived;
            s_debugReceivedOnlyWithMyNetId = onlyWithMyNetId;
            s_blockedOpcodes = new List<int>();
            s_debugOnlyOpcode = 0xFFFF;

            if (s_debugSent)
                Game.OnSendPacket += Game_OnSendPacket;

            if (s_debugReceived)
                Game.OnProcessPacket += Game_OnProcessPacket;

            s_NetworkId = BitConverter.GetBytes(ObjectManager.Player.NetworkId);

            Game.OnChat += Game_OnChat;
        }
        
        /// <summary>
        /// Stops Packet Debugging
        /// </summary>
        public static void Stop()
        {
            if (s_debugSent)
                Game.OnSendPacket -= Game_OnSendPacket;

            if (s_debugReceived)
                Game.OnProcessPacket -= Game_OnProcessPacket;

            Game.OnChat -= Game_OnChat;
        }

        /// <summary>
        /// The event when called a packet sent to server
        /// </summary>
        /// <param name="args"></param>
        private static void Game_OnSendPacket(GamePacketEventArgs args)
        {
            if (s_blockedOpcodes.Contains(BitConverter.ToInt16(args.PacketData, 0)))
                return;
            if (s_debugOnlyOpcode != 0xFFFF && s_debugOnlyOpcode != BitConverter.ToInt16(args.PacketData, 0))
                return;

            Console.WriteLine("[C->S][Opcode:0x{0:X4} ({0})][ProtocolFlag:{1}][MyNetId:{3:X8} ({2})]", BitConverter.ToInt16(args.PacketData, 0), args.ProtocolFlag, ObjectManager.Player.NetworkId, BitConverter.ToInt32(BitConverter.GetBytes(ObjectManager.Player.NetworkId).Reverse().ToArray(), 0));
            HexDump(args.PacketData, 2, args.PacketData.Length - 2);
        }

        /// <summary>
        /// The event when called a packet received from server
        /// </summary>
        /// <param name="args"></param>
        private static void Game_OnProcessPacket(GamePacketEventArgs args)
        {
            if (s_blockedOpcodes.Contains(BitConverter.ToInt16(args.PacketData, 0)))
                return;

            if (s_debugOnlyOpcode != 0xFFFF && s_debugOnlyOpcode != BitConverter.ToInt16(args.PacketData, 0))
                return;

            if (!s_debugReceivedOnlyWithMyNetId || search(args.PacketData, s_NetworkId) != -1)
            {
                Console.WriteLine("[{4}][Opcode:0x{0:X4} ({0})][ProtocolFlag:{1}][MyNetId:{3:X8} ({2})]", BitConverter.ToInt16(args.PacketData, 0), args.ProtocolFlag, ObjectManager.Player.NetworkId, BitConverter.ToInt32(BitConverter.GetBytes(ObjectManager.Player.NetworkId).Reverse().ToArray(), 0), args.Channel);
                Console.WriteLine(HexDump(args.PacketData, 2, args.PacketData.Length - 2));

                Console.WriteLine();
            }
        }

        /// <summary>
        /// Game.OnChat event for the command line
        /// </summary>
        /// <param name="args">The args.</param>
        private static void Game_OnChat(GameChatEventArgs args)
        {
            if(args.Sender.IsMe)
            {
                args.Process = false;
                string[] commands = args.Message.Split(' ');
                switch (commands[0])
                {
                    case "blockopcode":
                        s_blockedOpcodes.Add(int.Parse(commands[1], System.Globalization.NumberStyles.HexNumber));
                        Game.PrintChat("opcode {0} has blocked", commands[1]);
                        break;
                    case "unblockopcode":
                        s_blockedOpcodes.Remove(int.Parse(commands[1], System.Globalization.NumberStyles.HexNumber));
                        Game.PrintChat("opcode {0} has unblocked", commands[1]);
                        break;
                    case "showblockedopcodes":
                        foreach (var opcode in s_blockedOpcodes)
                            Game.PrintChat("{0:X2}", opcode);
                        break;
                    case "setdebugme":
                        if(commands[1] != "0" && commands[1] != "1")
                        {
                            Game.PrintChat("setdebugme only accepets 1 or 0");
                            return;
                        }
                        s_debugReceivedOnlyWithMyNetId = Convert.ToBoolean(Convert.ToInt32(commands[1]));
                        if (s_debugReceivedOnlyWithMyNetId)
                            Game.PrintChat("Only debugging packets with my net id");
                        else
                            Game.PrintChat("Debugging all packets");
                        break;
                    case "clearconsole":
                        Console.Clear();
                        Game.PrintChat("console clear");
                        break;
                    case "unblockallopcodes":
                        s_blockedOpcodes.Clear();
                        Game.PrintChat("Clear all blocked opcodes");
                        break;
                    case "setdebegopcode":
                        s_debugOnlyOpcode = int.Parse(commands[1], System.Globalization.NumberStyles.HexNumber);
                        Game.PrintChat("Only debugging opcode {0} (to disable enter same command with FFFF)", commands[1]);
                        break;
                    default:
                        args.Process = true;
                        break;
                }
            }
        }

        private static int search(byte[] haystack, byte[] needle)
        {
            for (int i = 0; i <= haystack.Length - needle.Length; i++)
            {
                if (match(haystack, needle, i))
                {
                    return i;
                }
            }
            return -1;
        }

        private static bool match(byte[] haystack, byte[] needle, int start)
        {
            if (needle.Length + start > haystack.Length)
            {
                return false;
            }
            else
            {
                for (int i = 0; i < needle.Length; i++)
                {
                    if (needle[i] != haystack[i + start])
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        private static string HexDump(byte[] buffer)
        {
            return HexDump(buffer, 0, buffer.Length);
        }

        private static string HexDump(byte[] buffer, int offset, int count)
        {
            const int bytesPerLine = 16;
            StringBuilder output = new StringBuilder();
            StringBuilder ascii_output = new StringBuilder();
            int length = count;
            if (length % bytesPerLine != 0)
            {
                length += bytesPerLine - length % bytesPerLine;
            }
            for (int x = 0; x <= length; ++x)
            {
                if (x % bytesPerLine == 0)
                {
                    if (x > 0)
                    {
                        output.AppendFormat("  {0}{1}", ascii_output.ToString(), Environment.NewLine);
                        ascii_output.Clear();
                    }
                    if (x != length)
                    {
                        output.AppendFormat("{0:d10}   ", x);
                    }
                }
                if (x < count)
                {
                    output.AppendFormat("{0:X2} ", buffer[offset + x]);
                    char ch = (char)buffer[offset + x];
                    if (!Char.IsControl(ch))
                    {
                        ascii_output.AppendFormat("{0}", ch);
                    }
                    else
                    {
                        ascii_output.Append(".");
                    }
                }
                else
                {
                    output.Append("   ");
                    ascii_output.Append(".");
                }
            }
            return output.ToString();
        }
    }
}
