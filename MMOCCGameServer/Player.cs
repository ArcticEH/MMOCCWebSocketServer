using System;
using System.Collections.Generic;
using System.Text;

namespace MMOCCGameServer
{
    public class Player
    {
        // Static vars
        static public int numberOfPlayers = 0;
        static public int movementSpeed = 20; // Must be a number divisible by 2 to know when to switch sorting layers

        // Player Information
        public string playerName;
        public int PlayerNumber;
        public string Id;

        // Room information
        public Guid RoomId;


        // Position Info
        public int cellNumber = 0;
        public int sortingCellNumber = 0;
        public float xPosition = 0;
        public float yPosition = 0;
        public float ticksInMovement = 0;
        public Queue<Cell> cellPath = new Queue<Cell>();
        public Cell destinationCell;
        public Cell startingCell;
        public bool isMoving;
    }
}
