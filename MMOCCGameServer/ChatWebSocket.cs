using System;
using System.Collections.Generic;
using System.Text;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Text.Json;

namespace MMOCCGameServer
{
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
            
        }
    }
}
