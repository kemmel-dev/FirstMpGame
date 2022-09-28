
using UnityEngine;

[System.Serializable]
public class ContainerPackage
{
    public ContainerPackage(int _packageType, string _packageData)
    {
        packageType = _packageType;
        packageData = _packageData;
    }

    public int packageType;
    public string packageData;
}

[System.Serializable]
public class HandshakePackage
{
    public int tick;
    public int[] ids;
    public Vector2[] positions;
}

[System.Serializable]
public class PositionPackage
{
    public int tick;
    public int id;
    public Vector2 pos;
}