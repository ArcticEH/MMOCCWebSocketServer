using System.Collections.Generic;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Text;
using System.Text.Json;
using System;

namespace MMOCCGameServer
{
    public class ChatWebSocket : WebSocketBehavior
    {
        public ChatWebSocket() { }

        protected override void OnOpen()
        {
            Player newPlayer = new Player(this.ID, "default", Server.issuePlayerNumber, "default", 0);
            Server.issuePlayerNumber++;
            Server.playerConnections.Add(newPlayer); // add player to connected players list.
            
            Server.publicRooms[0].playersInRoom.Add(newPlayer); // add player to default public room.
            MessageContainer newMessageContainer = new MessageContainer(MessageType.NewServerConnection, JsonSerializer.Serialize(newPlayer));
            Sessions.SendTo(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(newMessageContainer)), this.ID);

            // retrieve other player connections and send them to new client to spawn.

            foreach (Player player in Server.publicRooms[0].playersInRoom)
            {
                MessageContainer newSpawnMessageContainer = new MessageContainer(MessageType.Spawn, JsonSerializer.Serialize(player));

                Sessions.SendTo(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(newSpawnMessageContainer)), this.ID);
            }
        }

        protected override void OnClose(CloseEventArgs e)
        {
            Server.RemovePlayerConnection(this.ID);
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            Console.WriteLine(Encoding.UTF8.GetString(e.RawData));
            string decodedString = Encoding.UTF8.GetString(e.RawData);
            MessageDecoder(decodedString);
        }

        public void MessageDecoder(string message)
        {
            MessageContainer decodedMessage = JsonSerializer.Deserialize<MessageContainer>(message);

            switch (decodedMessage.MessageType) // Handle if player spawned
            {
                case MessageType.Spawn:
                    {
                        foreach (Player player in Server.publicRooms[0].playersInRoom)
                        {
                            MessageContainer newSpawnMessageContainer = new MessageContainer(MessageType.Spawn, decodedMessage.Data);

                            Sessions.SendTo(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(newSpawnMessageContainer)), player.Id);
                        }

                        break;
                    }

                case MessageType.Movement:
                    {
                        foreach (Player player in Server.publicRooms[0].playersInRoom)
                        {
                            MovementData newMovementData = JsonSerializer.Deserialize<MovementData>(decodedMessage.Data);
                            // Send to other players
                            MessageContainer newMovementMessageContainer = new MessageContainer(MessageType.Movement, decodedMessage.Data);
                            Sessions.SendTo(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(newMovementMessageContainer)), player.Id);
                        }

                        break;
                    }

                case MessageType.UpdateInformation:
                    {
                        Console.WriteLine(decodedMessage.Data);

                        PlayerInformation newData = JsonSerializer.Deserialize<PlayerInformation>(decodedMessage.Data, null);

                        foreach (Player player in Server.publicRooms[0].playersInRoom)
                        {
                            if (player.PlayerNumber == newData.PlayerNumber)
                            {
                                player.SetOnCell(newData.OnCell);
                            }
                        }

                        break;
                    }
            }
        }
    }



    /////////////////////////// BEYOND HERE ARE MESSAGE TYPES AND CLASSES ///////////////////////////////
    

    public enum MessageType
    {
        NewServerConnection,
        Spawn,
        Movement,
        UpdateInformation
    }

    public class MessageContainer
    {
        public MessageType MessageType { get; set; }
        public string Data { get; set; }

        public MessageContainer(MessageType newMessageType, string newData)
        {
            MessageType = newMessageType;
            Data = newData;
        }

        public MessageContainer() { } // blank constructor for deserializer.
    }

    public abstract class MessageData { }

    public class MovementData : MessageData
    {
        public int playerNumber;
        public int destinationCellNumber;

        public MovementData(int playerRequestingMovement, int destination)
        {
            playerNumber = playerRequestingMovement;
            destinationCellNumber = destination;
        }

        public MovementData() { }
    }

    public class PlayerInformation
    {
        
        public string PlayerName { get; set; }
        public int PlayerNumber { get; set; }
        public string Id { get; set; }
        public string InRoom { get; set; }
        public int OnCell { get; set; }

        // getters & setters

        public string GetPlayerName()
        {
            return PlayerName;
        }

        public int GetPlayerNumber()
        {
            return PlayerNumber;
        }

        public void SetPlayerName(string newName)
        {
            PlayerName = newName;
        }

        public void SetPlayerNumber(int newNumber)
        {
            PlayerNumber = newNumber;
        }

        
        public PlayerInformation(string playerName, int playerNumber, string id, string inRoom, int onCell) // constructor
        {
            PlayerName = playerName;
            PlayerNumber = playerNumber;
            Id = id;
            InRoom = inRoom;
            OnCell = onCell;
        }

        public PlayerInformation()
        {

        }
    }






}
