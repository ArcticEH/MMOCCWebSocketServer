using System;
using System.Collections.Generic;
using System.Text;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft;

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
            MessageDecoder(Encoding.UTF8.GetString(e.RawData));
        }

        public void MessageDecoder(string message)
        {
            MessageContainer decodedMessage = JsonSerializer.Deserialize<MessageContainer>(message);

            if (decodedMessage.MessageType == MessageType.Spawn) // Handle if player spawned
            {
                foreach (Player player in Server.publicRooms[0].playersInRoom)
                {
                    MessageContainer newSpawnMessageContainer = new MessageContainer(MessageType.Spawn, decodedMessage.Data);

                    Sessions.SendTo(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(newSpawnMessageContainer)), player.Id);
                }
            }
            else if (decodedMessage.MessageType == MessageType.Movement)
            {
                foreach (Player player in Server.publicRooms[0].playersInRoom)
                {
                    MovementData newMovementData = JsonSerializer.Deserialize<MovementData>(decodedMessage.Data);
                    // Send to other players
                    MessageContainer newMovementMessageContainer = new MessageContainer(MessageType.Movement, decodedMessage.Data);
                    Sessions.SendTo(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(newMovementMessageContainer)), player.Id);
                }
            }
            else if (decodedMessage.MessageType == MessageType.UpdateInformation)
            {
                Console.WriteLine(decodedMessage.Data);

                PlayerInformation newData = Newtonsoft.Json.JsonConvert.DeserializeObject<PlayerInformation>(decodedMessage.Data);

                Console.WriteLine("ATTEMPT TO CHANGE TO: " + newData.OnCell);

                foreach(Player player in Server.publicRooms[0].playersInRoom)
                {
                    if (player.PlayerNumber == newData.PlayerNumber)
                    {
                        Console.WriteLine("BEFORE: " + player.OnCell);
                        player.SetOnCell(newData.OnCell);
                        Console.WriteLine("AFTER " + player.OnCell);
                    }
                }

                
            }
        }
    }



    /////////////////////////// BEYOND HERE ARE MESSAGE TYPES AND CLASSES ///////////////////////////////
    

    [Serializable]
    public enum MessageType
    {
        NewServerConnection,
        Spawn,
        Movement,
        UpdateInformation
    }

    [Serializable]
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

    [Serializable]
    public abstract class MessageData { }


    [Serializable]
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

    [Serializable]
    public class PlayerInformation : MessageData
    {
        public string PlayerName { get; private set; }
        public int PlayerNumber { get; private set; }
        public string Id { get; private set; }
        public string InRoom { get; private set; }
        public int OnCell { get; private set; }

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

        public PlayerInformation() { }
    }






}
