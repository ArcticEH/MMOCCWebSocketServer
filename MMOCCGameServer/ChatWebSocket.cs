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

        public ChatWebSocket() {

        }

        protected override void OnOpen()
        {
            // Give server reference to this websocket when it is created
            Server.chatWebSocket = this;

            Console.WriteLine($"Received new player connection - {ID}");

            // Add player to connection
            Player newPlayer = new Player
            {
                playerName = "Hi",
                PlayerNumber = ++Player.numberOfPlayers,
                Id = this.ID,
                startingCell = new Cell()
                {
                    Number = 0,
                    X = 0,
                    Y = 28
                },
                destinationCell = new Cell()
                {
                    Number = 0,
                    X = 0,
                    Y = 28
                },
                cellNumber = 0

            };

            Server.AddPlayerConnection(newPlayer);
            Server.AddPlayerToRoom(newPlayer.Id, Server.publicRooms[0].RoomId); // for now just automatically add to welcome room

            // Send back them network player
            NewServerConnectionData newServerConnectionData = new NewServerConnectionData
            {
                PlayerName = newPlayer.playerName,
                PlayerNumber = newPlayer.PlayerNumber,
                Id = newPlayer.Id,
                Room = newPlayer.RoomId.ToString()
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

                case MessageType.MovementDataRequest:
                    // Find player to change destination cell
                    MovementDataRequest movementDataRequest = JsonConvert.DeserializeObject<MovementDataRequest>(messageContainer.MessageData);
                    Player playerMovement = Server.playerConnections.Where(playerConnection => playerConnection.Id.Equals(movementDataRequest.playerId)).FirstOrDefault();

                    // Set new values
                    playerMovement.cellPath = new Queue<Cell>();

                    for (int i = 0; i < movementDataRequest.cellNumberPath.Length; i++)
                    {
                        playerMovement.cellPath.Enqueue(new Cell()
                        {
                            Number = movementDataRequest.cellNumberPath[i],
                            X = movementDataRequest.cellPathXValues[i],
                            Y = movementDataRequest.cellPathYValues[i]
                        });
                    }
                    break;

                case MessageType.InRoomChatMessage:

                    InRoomChatMessageData newInRoomChatMessageData = JsonConvert.DeserializeObject<InRoomChatMessageData>(messageContainer.MessageData);

                    Room RoomMessageDestination = Server.publicRooms.Where(Room => Room.RoomName == newInRoomChatMessageData.roomName).FirstOrDefault();

                    foreach(Player player in RoomMessageDestination.playersInRoom)
                    {
                        SendMessage(player.Id, messageContainer);
                    }

                    break;
            }
        }

        public void SendMessage(string id, MessageContainer messageContainer)
        {
            Sessions.SendTo(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(messageContainer)), id);
        }

    }




}
