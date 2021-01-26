using System;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace MMOCCGameServer
{
    public enum RoomType
    {
        Public,
        Private
    }

    public class Room
    {
        public string RoomName { get; private set; }
        public RoomType RoomType { get; private set; }
        private List<Player> playersInRoom = new List<Player>();

        public Room(string roomName, RoomType roomType)
        {
            RoomName = roomName;
            RoomType = roomType;
        }

        public List<Player> GetPlayersInRoom()
        {
            return playersInRoom;
        }
    }
}