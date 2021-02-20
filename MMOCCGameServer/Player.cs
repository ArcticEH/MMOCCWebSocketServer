using System;
using System.Collections.Generic;
using System.Text;

namespace MMOCCGameServer
{
    public class Player
    {
        static public int numberOfPlayers = 0;

        public string playerName;
        public int PlayerNumber;
        public string Id;

        // Position Info
        public int cellNumber = 0;
        public float xPosition = 0;
        public float yPosition = 0;

        // Room information
        public string Room;

        //public Player(string playerName, int playerNumber, string id, string room)
        //{
        //    this.playerName = playerName;
        //    PlayerNumber = playerNumber;
        //    Id = id;
        //    Room = room;


        //}
    }
}
