using System;
using System.Collections.Generic;
using System.Text;

namespace MMOCCGameServer
{
    [Serializable]
    public class Player
    {
        public string PlayerName { get; private set; }
        public int PlayerNumber { get; private set; }
        public string Id { get; private set; }
        public string InRoom { get; private set; }
        public int OnCell { get; private set; }

        public Player(string id, string name, int number, string inRoom, int onCell)
        {
            this.PlayerName = name;
            this.PlayerNumber = number;
            this.Id = id;
            InRoom = inRoom;
            OnCell = onCell;
        }

        public Player() { }
    }
}
