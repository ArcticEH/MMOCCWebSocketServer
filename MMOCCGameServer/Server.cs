using System;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace MMOCCGameServer
{
    public static class Server
    {
        public static int issuePlayerNumber = 0;

        public static List<Player> playerConnections = new List<Player>();

        public static List<Room> publicRooms = new List<Room>();

        public static WebSocketServer webSocketServer;

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
            publicRooms.Add(new Room("default", RoomType.Public));

            Console.ReadKey();
        }

        public static void AddPlayerConnection(Player newPlayer)
        {
            playerConnections.Add(newPlayer);
        }

        public static void RemovePlayerConnection(String id)
        {
            playerConnections.Remove(playerConnections.Where(p => p.Id == id).FirstOrDefault());
            publicRooms[0].playersInRoom.Remove(publicRooms[0].playersInRoom.Where(p => p.Id == id).FirstOrDefault());
        }
    }
}
