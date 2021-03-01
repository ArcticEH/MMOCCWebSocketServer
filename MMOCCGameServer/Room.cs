using System;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Newtonsoft.Json;

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

        public int RoomId { get; private set; }

        public List<Player> playersInRoom = new List<Player>();
        public List<Cell> cellsInRoom = new List<Cell>(); // Currently unused

        // Hard coded room info
        public Cell SpawnCell;
        public int SpawnCellNumber;

        public Room(string roomName, RoomType roomType, int roomId, Cell spawnCell)
        {
            RoomName = roomName;
            RoomType = roomType;
            RoomId = roomId;
            SpawnCell = spawnCell;
        }

        public List<Player> GetPlayersInRoom()
        {
            return playersInRoom;
        }

        public void SpawnPlayerInRoom(string playerId)
        {
            // Get player from player connections
            Player spawnPlayer = Server.playerConnections.Where(player => player.Id.Equals(playerId)).First();

            // Set players attributes
            spawnPlayer.RoomId = RoomId;
            spawnPlayer.cellNumber = SpawnCellNumber;
            spawnPlayer.startingCell = SpawnCell;
            spawnPlayer.sortingCellNumber = SpawnCellNumber;
            spawnPlayer.destinationCell = SpawnCell;
            spawnPlayer.xPosition = SpawnCell.X;
            spawnPlayer.yPosition = SpawnCell.Y;
            spawnPlayer.cellPath.Clear();
             
            playersInRoom.Add(spawnPlayer);

            // Send spawn message to all players in room
            foreach (Player player in playersInRoom)
            {
                // Send to all players in room
                SpawnResponse existingSpawnData = new SpawnResponse
                {
                    playerId = spawnPlayer.Id,
                    playerName = spawnPlayer.playerName,
                    cellNumber = spawnPlayer.cellNumber,
                    playerNumber = spawnPlayer.PlayerNumber,
                    xPosition = spawnPlayer.xPosition,
                    yPosition = spawnPlayer.yPosition,
                    sortingCellNumber = spawnPlayer.sortingCellNumber
                };
                MessageContainer mc = new MessageContainer(MessageType.SpawnResponse, JsonConvert.SerializeObject(existingSpawnData));
                //byte[] newPlayerBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(mc));
                Server.chatWebSocket.SendMessage(player.Id, mc);
            }

            // Send all players in room to player who made request
            foreach (Player player in playersInRoom)
            {
                if (player.Id.Equals(playerId)) { continue; }
                Console.WriteLine("sending out existing player to spawned player");
                SpawnResponse existingSpawnData = new SpawnResponse
                {
                    playerId = player.Id,
                    playerName = player.playerName,
                    cellNumber = player.cellNumber,
                    playerNumber = player.PlayerNumber,
                    xPosition = player.xPosition,
                    yPosition = player.yPosition,
                    sortingCellNumber = player.sortingCellNumber
                };
                MessageContainer mc = new MessageContainer(MessageType.SpawnResponse, JsonConvert.SerializeObject(existingSpawnData));
                //byte[] otherPlayerBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(mc));
                Server.chatWebSocket.SendMessage(spawnPlayer.Id, mc);
            }
        }

        public void DespawnPlayerInRoom(string playerId) 
        {
            // Check if player is in room
            Player player = playersInRoom.Where(player => player.Id.Equals(playerId)).First();
            if (player is null) { return;  }

            // Remove from room
            player.RoomId = -1;
            player.isMoving = false;
            player.ticksInMovement = 0;
            playersInRoom.Remove(player);

            DespawnData despawnData = new DespawnData
            {
                Id = playerId
            };

            MessageContainer messageContainer = new MessageContainer(MessageType.DespawnData, JsonConvert.SerializeObject(despawnData));

            // Send to all players in room to despawn
            foreach(Player otherPlayer in playersInRoom)
            {
                Server.chatWebSocket.SendMessage(otherPlayer.Id, messageContainer);
            }

        }


        // Update all player positions in room
        public void UpdatePlayerPositions()
        {
            foreach (Player playerToUpdate in playersInRoom)
            {
                // Check if player is moving
                if (playerToUpdate.cellPath.Count == 0 && !playerToUpdate.isMoving) { continue; }

                // Check if player room number is still this room
                if (playerToUpdate.RoomId != RoomId) { continue;  }

                // Update position on server
                Vector3 nextPosition;

                if (playerToUpdate.ticksInMovement == 0)
                {
                    // Start moving to next cell
                    playerToUpdate.isMoving = true;
                    playerToUpdate.ticksInMovement++;
                    playerToUpdate.destinationCell = playerToUpdate.cellPath.Dequeue();

                    if (playerToUpdate.startingCell.Number == playerToUpdate.destinationCell.Number)
                    {
                        playerToUpdate.destinationCell = playerToUpdate.cellPath.Dequeue();
                    }
                    nextPosition = Vector3.Lerp(playerToUpdate.startingCell.ConvertToVector3(), playerToUpdate.destinationCell.ConvertToVector3(), playerToUpdate.ticksInMovement / Player.movementSpeed);
                    playerToUpdate.cellNumber = playerToUpdate.destinationCell.Number;

                }
                else if (playerToUpdate.ticksInMovement > 0 && playerToUpdate.ticksInMovement < Player.movementSpeed)
                {
                    // Continue moving to a cell
                    playerToUpdate.ticksInMovement++;
                    nextPosition = Vector3.Lerp(playerToUpdate.startingCell.ConvertToVector3(), playerToUpdate.destinationCell.ConvertToVector3(), playerToUpdate.ticksInMovement / Player.movementSpeed);

                    // if halfway then switch cell number
                    if (playerToUpdate.ticksInMovement == Player.movementSpeed / 2)
                    {
                        playerToUpdate.sortingCellNumber = playerToUpdate.destinationCell.Number;
                    }
                }
                else
                {
                    // Finish moving to cell
                    playerToUpdate.isMoving = false;
                    nextPosition = Vector3.Lerp(playerToUpdate.startingCell.ConvertToVector3(), playerToUpdate.destinationCell.ConvertToVector3(), playerToUpdate.ticksInMovement / Player.movementSpeed);

                    playerToUpdate.startingCell = playerToUpdate.destinationCell;
                    playerToUpdate.ticksInMovement = 0;
                }

                // Calculate which direction the player is facing.
                CalculatePlayerFacingDirection(playerToUpdate);

                // Set next values
                playerToUpdate.xPosition = nextPosition.X;
                playerToUpdate.yPosition = nextPosition.Y;

                // Create data to send to all clients
                MovementDataUpdate movementData = new MovementDataUpdate
                {
                    playerId = playerToUpdate.Id,
                    cellNumber = playerToUpdate.cellNumber,
                    xPosition = playerToUpdate.xPosition,
                    yPosition = playerToUpdate.yPosition,
                    sortingCellNumber = playerToUpdate.sortingCellNumber,
                    facingDirection = playerToUpdate.facingDirection
                };
                MessageContainer messageContainer = new MessageContainer(MessageType.MovementDataUpdate, JsonConvert.SerializeObject(movementData));

                // Send to all players in movement
                foreach (Player playerToSend in playersInRoom)
                {
                    Server.chatWebSocket.SendMessage(playerToSend.Id, messageContainer);
                }

            }
        }

        private static void CalculatePlayerFacingDirection(Player playerToUpdate)
        {
            if ((playerToUpdate.xPosition > playerToUpdate.destinationCell.X) && (playerToUpdate.startingCell.Y > playerToUpdate.destinationCell.Y))
            {
                playerToUpdate.facingDirection = FacingDirection.left;
            }
            else if ((playerToUpdate.xPosition < playerToUpdate.destinationCell.X) && (playerToUpdate.startingCell.Y > playerToUpdate.destinationCell.Y))
            {
                playerToUpdate.facingDirection = FacingDirection.down;
            }
            else if ((playerToUpdate.xPosition > playerToUpdate.destinationCell.X) && (playerToUpdate.startingCell.Y < playerToUpdate.destinationCell.Y))
            {
                playerToUpdate.facingDirection = FacingDirection.up;
            }
            else if ((playerToUpdate.xPosition < playerToUpdate.destinationCell.X) && (playerToUpdate.startingCell.Y < playerToUpdate.destinationCell.Y))
            {
                playerToUpdate.facingDirection = FacingDirection.right;
            }
        }
    }
}
