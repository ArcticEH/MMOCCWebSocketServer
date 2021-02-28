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

            Player player = Server.playerConnections.Where(player => player.Id.Equals(this.ID)).First();

            // Remove from server
            Server.RemovePlayerConnection(this.ID);

            if (player.RoomId == -1) { return; } // For now just remove them

            // Remove player from room
            Room room = Server.publicRooms.Where(room => room.RoomId == player.RoomId).First();

            room.playersInRoom.Remove(player);

            // Create player message to despawn
            DespawnData despawnData = new DespawnData
            {
                Id = this.ID
            };

            MessageContainer messageContainer = new MessageContainer(MessageType.DespawnData, JsonConvert.SerializeObject(despawnData));
            byte[] despawnMessage = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(messageContainer));
            foreach(Player roomPlayer in room.playersInRoom)
            {
                Sessions.SendTo(despawnMessage, roomPlayer.Id);
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
                    Room roomJoined = Server.publicRooms.Where(room => room.RoomId == spawnData.roomId).First();
                    roomJoined.SpawnPlayerInRoom(spawnData.playerId);
                    break;

                case MessageType.DespawnRequest:
                    Console.WriteLine("Received despawn request");
                    // Find player who is despawning/leaving room
                    DespawnRequest despawnRequest = JsonConvert.DeserializeObject<DespawnRequest>(messageContainer.MessageData);

                    // Find room they claim to be in
                    Room occupiedRoom = Server.publicRooms.Where(room => room.RoomId.Equals(despawnRequest.RoomId)).First();
                    occupiedRoom.DespawnPlayerInRoom(despawnRequest.Id);

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
