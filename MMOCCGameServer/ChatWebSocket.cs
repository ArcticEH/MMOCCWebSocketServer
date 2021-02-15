using System.Collections.Generic;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Text;
using System;
using System.IO;
//using System.Text.Json;
using Newtonsoft.Json;
using System.Linq;

namespace MMOCCGameServer
{

    public class ChatWebSocket : WebSocketBehavior
    {


        public ChatWebSocket() { }

        protected override void OnOpen()
        {
            Console.WriteLine($"Received new player connection - {ID}");

            // Add player to connection
            Player newPlayer = new Player
            {
                playerName = "Hi",
                PlayerNumber = ++Player.numberOfPlayers,
                Id = this.ID,
                Room = "1"

            };
            Server.AddPlayerConnection(newPlayer);

            // Send back them network player
            NewServerConnectionData newServerConnectionData = new NewServerConnectionData
            {
                PlayerName = newPlayer.playerName,
                PlayerNumber = newPlayer.PlayerNumber,
                Id = newPlayer.Id,
                Room = newPlayer.Room
            };

            //Create message container with serialized message
            string messageData = JsonConvert.SerializeObject(newServerConnectionData);
            MessageContainer messageContainer = new MessageContainer(MessageType.NewServerConnection, messageData);
            string messageContainerString = JsonConvert.SerializeObject(messageContainer);
            byte[] messageSendArray = Encoding.UTF8.GetBytes(messageContainerString);

            //Send connection message back
            Send(messageSendArray);

        }

        protected override void OnClose(CloseEventArgs e)
        {
            Console.WriteLine("Received close connection");

            // Remove from server
            Server.RemovePlayerConnection(this.ID);

            // Send player message to despawn
            DespawnData despawnData = new DespawnData
            {
                Id = this.ID
            };

            MessageContainer messageContainer = new MessageContainer(MessageType.Despawn, JsonConvert.SerializeObject(despawnData));
            byte[] despawnMessage = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(messageContainer));
            foreach(Player player in Server.playerConnections)
            {
                Sessions.SendTo(despawnMessage, player.Id);
            }
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            // Receive and decode message into message container
            Console.WriteLine(Encoding.UTF8.GetString(e.RawData));
            string json = Encoding.UTF8.GetString(e.RawData);
            MessageContainer messageContainer = JsonConvert.DeserializeObject<MessageContainer>(json);
            HandleMessage(messageContainer);
        }

        public void HandleMessage(MessageContainer messageContainer)
        {
            switch(messageContainer.MessageType)
            {
                case MessageType.NewSpawn:
                    SpawnData spawnData = JsonConvert.DeserializeObject<SpawnData>(messageContainer.MessageData); ;

                    // Verify the spawn data

                    // Create new spawn message to send out
                    byte[] byteArray = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(messageContainer));

                    // Send spawn message to all players
                    foreach(Player player in Server.playerConnections)
                    {
                        // Send to all players in session. TODO: Change to room once rooms are a thing
                        Sessions.SendTo(byteArray, player.Id);
                    }

                    // Send all players in room to player who made request
                    foreach(Player player in Server.playerConnections)
                    {
                        if (player.Id.Equals(spawnData.playerId)) { continue; }
                        Console.WriteLine("sending out existing player to spawned player");
                        ExistingSpawnData existingSpawnData = new ExistingSpawnData
                        {
                            Id = player.Id,
                            cellNumber = player.cellNumber,
                            playerNumber = player.PlayerNumber
                        };
                        MessageContainer mc = new MessageContainer(MessageType.ExistingSpawn, JsonConvert.SerializeObject(existingSpawnData));
                        byte[] otherPlayerBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(mc));
                        Sessions.SendTo(otherPlayerBytes, spawnData.playerId);
                    }
                    break;

                case MessageType.Movement:
                    // Find player to change destination cell
                    MovementData movementData = JsonConvert.DeserializeObject<MovementData>(messageContainer.MessageData);
                    Server.playerConnections.Where(playerConnection => playerConnection.Id.Equals(movementData.playerId)).FirstOrDefault().cellNumber = movementData.destinationCellNumber;

                    // Tell all players in room to move
                    Console.WriteLine("Sending out movement");
                    foreach(Player player in Server.playerConnections)
                    {                    
                        Sessions.SendTo(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(messageContainer)), player.Id);
                    }
                    break;
            }

           

        }

    }


}
