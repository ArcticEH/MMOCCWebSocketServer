
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// All the possible websocket message types
[Serializable]
public enum MessageType
{
    NewServerConnection,
    SpawnRequest,
    SpawnResponse,
    Despawn,
    MovementDataUpdate,
    MovementDataRequest,
    InRoomChatMessage
}

[Serializable]
public enum FacingDirection
{
    left,
    right,
    up,
    down
}

// Message container is the actual object sent over the network
[Serializable]
public class MessageContainer
{
    public MessageType MessageType;
    public string MessageData;

    public MessageContainer(MessageType newMessageType, string newData)
    {
        MessageType = newMessageType;
        MessageData = newData;
    }

}

// Message Data Types
[Serializable]
public class SpawnRequest
{
    public string playerId;
    public int playerNumber;
}


[Serializable]
public class MovementDataUpdate
{
    public string playerId;
    public int cellNumber;
    public int sortingCellNumber;
    public float xPosition;
    public float yPosition;
    public FacingDirection facingDirection;
}

[Serializable]
public class MovementDataRequest
{
    public string playerId;
    public int[] cellNumberPath;
    public int[] cellPathXValues;
    public int[] cellPathYValues;
}

[Serializable]
public class NewServerConnectionData
{
    public string PlayerName;
    public int PlayerNumber;
    public string Id;
    public string Room;
}

[Serializable]
public class SpawnResponse
{
    public string playerId;
    public int playerNumber;
    public int cellNumber;
    public int sortingCellNumber;
    public float xPosition;
    public float yPosition;
    public FacingDirection facingDirection;
}

[Serializable]
public class DespawnData
{
    public string Id;
}

[Serializable]
public class InRoomChatMessageData
{
    public string chatMessage;
    public string roomName;
    public float messageXLocation;
}





