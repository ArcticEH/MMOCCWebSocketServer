using System;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Threading;
using System.Numerics;

namespace MMOCCGameServer
{

    public static class Server
    {
        public static int issuePlayerNumber = 0;

        public static List<Player> playerConnections = new List<Player>();

        public static List<Room> publicRooms = new List<Room>();

        public static WebSocketServer webSocketServer;

        public static ChatWebSocket chatWebSocket;

        public const int TICKS_PER_SEC = 30;

        public const float MS_PER_TICK = 1000f / TICKS_PER_SEC;

        public static void Main(string[] args)
        {
            // Create websocket server
            webSocketServer = new WebSocketServer(9000);

            // Add chat web socket as web socket service
            webSocketServer.AddWebSocketService<ChatWebSocket>("/Chat");

            // Start the server 
            webSocketServer.Start();
            Console.WriteLine("Started server on port 9000");

            // Create Public Rooms
            publicRooms.Add(new Room("Welcome", RoomType.Public));

            // Start the update loop
            UpdateLoop();

           // Console.ReadKey();
        }

        public static void AddPlayerConnection(Player newPlayer)
        {
            playerConnections.Add(newPlayer);
        }

        public static void AddPlayerToRoom(string playerId, Guid roomId)
        {
            Room room = publicRooms.Where(room => room.RoomId.Equals(roomId)).First();
            Player player = playerConnections.Where(player => player.Id.Equals(playerId.ToString())).First();
            room.playersInRoom.Add(player);
            player.RoomId = room.RoomId;
        }

        public static void RemovePlayerFromRoom(string playerId)
        {
            Player player = playerConnections.Where(player => player.Id.Equals(playerId.ToString())).First();
            Room room = publicRooms.Where(room => room.RoomId.Equals(player.RoomId)).First();     
            room.playersInRoom.Add(player);
            player.RoomId = default;
        }

        public static void RemovePlayerConnection(String id)
        {
            playerConnections.Remove(playerConnections.Where(p => p.Id == id).FirstOrDefault());
            publicRooms[0].playersInRoom.Remove(publicRooms[0].playersInRoom.Where(p => p.Id == id).FirstOrDefault());
        }

        public static void UpdateLoop()
        {
            Console.WriteLine($"Update loop started on main thread. Running at {TICKS_PER_SEC} ticks per second.");
            DateTime nextLoop = DateTime.Now;

            while(true)
            {
                while(nextLoop <= DateTime.Now)
                {
                    UpdatePlayerPositions();
                    nextLoop = nextLoop.AddMilliseconds(MS_PER_TICK);

                    if (nextLoop > DateTime.Now)
                    {
                        Thread.Sleep(nextLoop - DateTime.Now);
                    }
                }
            }
        }


        public static void UpdatePlayerPositions()
        {
            // Update player positions in all rooms
            foreach(Room room in publicRooms)
            {
                room.UpdatePlayerPositions();
            }


        }
    }
}
