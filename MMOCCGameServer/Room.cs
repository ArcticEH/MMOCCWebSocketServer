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

        public Guid RoomId { get; private set; }

        public List<Player> playersInRoom = new List<Player>();
        public List<Cell> cellsInRoom = new List<Cell>(); // Currently unused

        public Room(string roomName, RoomType roomType)
        {
            RoomName = roomName;
            RoomType = roomType;
            RoomId = new Guid();
        }

        public List<Player> GetPlayersInRoom()
        {
            return playersInRoom;
        }

        // Update all player positions in room
        public void UpdatePlayerPositions()
        {
            foreach (Player playerToUpdate in playersInRoom)
            {
                // Check if player is moving
                if (playerToUpdate.cellPath.Count == 0 && !playerToUpdate.isMoving) { continue; }


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