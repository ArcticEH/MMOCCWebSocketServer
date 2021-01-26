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
        static List<Player> playerConnections = new List<Player>();

        static List<Room> publicRooms = new List<Room>();

        static WebSocketServer webSocketServer;

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
            publicRooms.Add(new Room("First Room", RoomType.Public));

            Console.ReadKey();
        }

        public static void AddPlayerConnection(String id, String playerName)
        {
            playerConnections.Add(new Player(playerName, id));
        }

        public static void RemovePlayerConnection(String id)
        {
            playerConnections.Remove(playerConnections.Where(p => p.Id == id).FirstOrDefault());
        }
    }
}
