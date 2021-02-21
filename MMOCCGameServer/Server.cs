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
            publicRooms.Add(new Room("default", RoomType.Public));

            // Start the update loop
            UpdateLoop();

           // Console.ReadKey();
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
            Console.WriteLine("Updating player positions");
            webSocketServer.WebSocketServices.TryGetServiceHost("/Chat", out WebSocketServiceHost host);

            foreach (Player playerToUpdate in playerConnections)
            {
                // Check if player is moving
                if (playerToUpdate.cellPath.Count == 0 && !playerToUpdate.isMoving) { continue; }

                // Update position on server
                Vector3 nextPosition;

                if (playerToUpdate.ticksInMovement == 0)
                {

                    // Start moving to next cell
                    playerToUpdate.isMoving = true;
                    playerToUpdate.ticksInMovement++;
                    playerToUpdate.destinationCell = playerToUpdate.cellPath.Dequeue();
                    if (playerToUpdate.startingCell.Number == playerToUpdate.destinationCell.Number)
                    {
                        playerToUpdate.destinationCell = playerToUpdate.cellPath.Dequeue();
                    }
                    nextPosition = Vector3.Lerp(playerToUpdate.startingCell.ConvertToVector3(), playerToUpdate.destinationCell.ConvertToVector3(), playerToUpdate.ticksInMovement / 30f);
                    playerToUpdate.cellNumber = playerToUpdate.destinationCell.Number;
                }
                else if (playerToUpdate.ticksInMovement > 0 && playerToUpdate.ticksInMovement < 30)
                {
                    // Continue moving to a cell
                    playerToUpdate.ticksInMovement++;
                    nextPosition = Vector3.Lerp(playerToUpdate.startingCell.ConvertToVector3(), playerToUpdate.destinationCell.ConvertToVector3(), playerToUpdate.ticksInMovement / 30f);

                    // if halfway then switch cell number
                    if (playerToUpdate.ticksInMovement == 15)
                    {
                        playerToUpdate.sortingCellNumber = playerToUpdate.destinationCell.Number;
                    }
                }
                else
                {
                    // Finish moving to cell
                    playerToUpdate.isMoving = false;
                    nextPosition = Vector3.Lerp(playerToUpdate.startingCell.ConvertToVector3(), playerToUpdate.destinationCell.ConvertToVector3(), playerToUpdate.ticksInMovement / 30f);

                    playerToUpdate.startingCell = playerToUpdate.destinationCell;
                    playerToUpdate.ticksInMovement = 0;
                }

                // Set next values
                playerToUpdate.xPosition = nextPosition.X;
                playerToUpdate.yPosition = nextPosition.Y;

                // Create data to send to all clients
                MovementDataUpdate movementData = new MovementDataUpdate
                {
                    playerId = playerToUpdate.Id,
                    cellNumber = playerToUpdate.cellNumber,
                    xPosition = playerToUpdate.xPosition,
                    yPosition = playerToUpdate.yPosition,
                    sortingCellNumber = playerToUpdate.sortingCellNumber
                };
                MessageContainer messageContainer = new MessageContainer(MessageType.MovementDataUpdate, JsonConvert.SerializeObject(movementData));

                // Send to all players in movement
                foreach (Player playerToSend in playerConnections)
                {
                    chatWebSocket.SendMessage(playerToSend.Id, messageContainer);               
                }

            }
        }
    }
}
