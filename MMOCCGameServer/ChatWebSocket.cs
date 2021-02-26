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

            // Add player with session information (not yet logged in)
            Player newPlayer = new Player
            {
                PlayerNumber = ++Player.numberOfPlayers,
                Id = this.ID           
            };

            Server.AddPlayerConnection(newPlayer);
            //Server.AddPlayerToRoom(newPlayer.Id, Server.publicRooms[0].RoomId); // for now just automatically add to welcome room

            // Send back them network player
            NewServerConnectionData newServerConnectionData = new NewServerConnectionData
            {
                PlayerNumber = newPlayer.PlayerNumber,
                Id = newPlayer.Id,
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
                case MessageType.Login:
                    Login login = JsonConvert.DeserializeObject<Login>(messageContainer.MessageData);

                    // TODO: Have actual login

                    // Add player name to player
                    Player loginPlayer = Server.playerConnections.Where(player => player.Id.Equals(login.playerId)).First();
                    loginPlayer.playerName = login.PlayerName;

                    // Create success message
                    LoginResponse loginResponse = new LoginResponse()
                    {
                        isSuccess = true,
                        message = "Successful login on server"
                    };

                    MessageContainer loginResponseMc = new MessageContainer(MessageType.LoginResponse, JsonConvert.SerializeObject(loginResponse));
                    byte[] loginResponseBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(loginResponseMc));
                    Sessions.SendTo(loginResponseBytes, login.playerId);
                    Console.WriteLine($"Sending back login response to player {login.playerId}");
                    break;

                case MessageType.SpawnRequest:
                    SpawnRequest spawnData = JsonConvert.DeserializeObject<SpawnRequest>(messageContainer.MessageData); ;

                    // Get player spawning and room
                    Player spawnPlayer = Server.playerConnections.Where(player => player.Id.Equals(spawnData.playerId)).First();
                    // Join room
                    Server.AddPlayerToRoom(spawnData.playerId, spawnData.roomId);
                    Room roomJoined = Server.publicRooms.Where(room => room.RoomId == spawnData.roomId).First();

                    // Add properties for player position to be the rooms spawn position
                    spawnPlayer.RoomId = spawnData.roomId;
                    spawnPlayer.sortingCellNumber = roomJoined.SpawnCellNumber;
                    spawnPlayer.startingCell = roomJoined.SpawnCell;
                    spawnPlayer.xPosition = roomJoined.SpawnCell.X;
                    spawnPlayer.yPosition = roomJoined.SpawnCell.Y;

                                    

                    // Create new spawn message to send out
                    byte[] byteArray = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(messageContainer));

                    // Get room player is located in
                    Player spawningPlayer = Server.playerConnections.Where(player => player.Id.Equals(spawnData.playerId)).First();

                    Room room = Server.publicRooms.Where(room => room.RoomId.Equals(spawningPlayer.RoomId)).First();

                    // Send spawn message to all players in room
                    foreach(Player player in room.playersInRoom)
                    {
                        // Send to all players in room
                        SpawnResponse existingSpawnData = new SpawnResponse
                        {
                            playerId = spawningPlayer.Id,
                            cellNumber = spawningPlayer.cellNumber,
                            playerNumber = spawningPlayer.PlayerNumber,
                            xPosition = spawningPlayer.xPosition,
                            yPosition = spawningPlayer.yPosition,
                            sortingCellNumber = spawningPlayer.sortingCellNumber
                        };
                        MessageContainer mc = new MessageContainer(MessageType.SpawnResponse, JsonConvert.SerializeObject(existingSpawnData));
                        byte[] newPlayerBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(mc));
                        Sessions.SendTo(newPlayerBytes, player.Id);
                    }

                    // Send all players in room to player who made request
                    foreach(Player player in room.playersInRoom)
                    {
                        if (player.Id.Equals(spawnData.playerId)) { continue; }
                        Console.WriteLine("sending out existing player to spawned player");
                        SpawnResponse existingSpawnData = new SpawnResponse
                        {
                            playerId = player.Id,
                            cellNumber = player.cellNumber,
                            playerNumber = player.PlayerNumber,
                            xPosition = player.xPosition,
                            yPosition = player.yPosition,
                            sortingCellNumber = player.sortingCellNumber
                        };
                        MessageContainer mc = new MessageContainer(MessageType.SpawnResponse, JsonConvert.SerializeObject(existingSpawnData));
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

                    Room RoomMessageDestination = Server.publicRooms.Where(Room => Room.RoomId == newInRoomChatMessageData.roomId).FirstOrDefault();

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
