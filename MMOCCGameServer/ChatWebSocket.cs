using System;
using System.Collections.Generic;
using System.Text;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MMOCCGameServer
{
    [Serializable]
    public enum MessageType
    {
        NewServerConnection,
        Spawn
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
    public class MovementRequestData : MessageData { };

    /////////////////////// START OF CHAT WEBSOCKET ///////////////////////////////////
    public class ChatWebSocket : WebSocketBehavior
    {
        public ChatWebSocket() { }

        protected override void OnOpen()
        {
            Player newPlayer = new Player(this.ID, "default", Server.playerConnections.Count + 1, "default", 0);
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

        ////////////// PART OF JSON STUFF //////////////////////////////////
        public void MessageDecoder(string message)
        {
            MessageContainer decodedMessage = (MessageContainer)JsonSerializer.Deserialize(message, typeof(MessageContainer), null);

            if (decodedMessage.MessageType == MessageType.Spawn) // Handle if player spawned
            {
                foreach (Player player in Server.publicRooms[0].playersInRoom)
                {
                    MessageContainer newSpawnMessageContainer = new MessageContainer(MessageType.Spawn, decodedMessage.Data);

                    Sessions.SendTo(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(newSpawnMessageContainer)), player.Id);
                }
            }
        }
    }



    

    
}
