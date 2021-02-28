
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// All the possible websocket message types
[Serializable]
public enum MessageType
{
    NewServerConnection,
    Login,
    LoginResponse,
    SpawnRequest,
    SpawnResponse,
    DespawnRequest,
    DespawnData,
    MovementDataUpdate,
    MovementDataRequest,
    InRoomChatMessage
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
public class Login
{
    public string PlayerName;
    public string playerId;
}

[Serializable]
public class LoginResponse
{
    public bool isSuccess;
    public string message;
}

[Serializable]
public class SpawnRequest
{
    public string playerId;
    public int playerNumber;
    public int roomId;
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
    public int PlayerNumber;
    public string Id;
}

[Serializable]
public class SpawnResponse
{
    public string playerId;
    public string playerName;
    public int playerNumber;
    public int cellNumber;
    public int sortingCellNumber;
    public float xPosition;
    public float yPosition;
    public FacingDirection facingDirection;
}

[Serializable]
public class DespawnRequest
{
    public string Id;
    public int RoomId;
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
    public int roomId;
    public float messageXLocation;
}

// Additional Enums
[Serializable]
public enum FacingDirection
{
    left,
    right,
    up,
    down
}


