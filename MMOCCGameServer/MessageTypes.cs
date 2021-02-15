
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// All the possible websocket message types
[Serializable]
public enum MessageType
{
    NewServerConnection,
    NewSpawn,
    ExistingSpawn,
    Despawn,
    Movement
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
public class SpawnData
{
    public string playerId;
    public int playerNumber;
}


[Serializable]
public class MovementData
{
    public string playerId;
    public int destinationCellNumber;

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
public class ExistingSpawnData
{
    public string Id;
    public int playerNumber;
    public int cellNumber;
}

[Serializable]
public class DespawnData
{
    public string Id;
}






