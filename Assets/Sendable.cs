
[System.Serializable]
public class Sendable
{
    public int packageType;
    public string packageData;
}

[System.Serializable]
public class HandshakeSendable : Sendable
{
    public int[] ids;
    public (float x,float y)[] positions;
}

[System.Serializable]
public class GameSendable : Sendable
{
    public int tick;
    public int id;
    public float x, y;
}