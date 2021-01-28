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
        ChatMessage,
        MovementRequest
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
    public class ChatMessageData : MessageData
    {
        public float PlayerXLocation { get; set; }
        public string ChatMessage { get; set; }

        public ChatMessageData(float newPlayerXLocation, string newChatMessage)
        {
            PlayerXLocation = newPlayerXLocation;
            ChatMessage = newChatMessage;
        }

        public ChatMessageData() { } // blank constructor for deserializer.
    }

    public class MovementRequestData : MessageData { };

    /////////////////////// START OF CHAT WEBSOCKET ///////////////////////////////////
    public class ChatWebSocket : WebSocketBehavior
    {
        public ChatWebSocket()
        {
  
        }

        protected override void OnOpen()
        {
            Server.AddPlayerConnection(this.ID, "default");
              
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
            JsonSerializerOptions options = new JsonSerializerOptions();

            MessageContainer decodedMessage = (MessageContainer)JsonSerializer.Deserialize(message, typeof(MessageContainer), options);

            if (decodedMessage.MessageType == MessageType.ChatMessage) // Handle if Message is a Chat Message
            {
                string newMessageContainerJSON = JsonSerializer.Serialize(decodedMessage);

                Console.WriteLine(newMessageContainerJSON);

                Sessions.Broadcast(Encoding.UTF8.GetBytes(newMessageContainerJSON));
            }
        }
    }



    

    
}
