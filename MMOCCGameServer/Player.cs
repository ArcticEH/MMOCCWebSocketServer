using System;
using System.Collections.Generic;
using System.Text;

namespace MMOCCGameServer
{
    public class Player
    {
        public string Name { get; private set; }
        public string Id { get; private set; }

        public Player(string id, string name)
        {
            this.Name = name;
            this.Id = Id;
        }  
    }
}
