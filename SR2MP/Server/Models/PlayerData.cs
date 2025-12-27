namespace SR2MP.Server.Models;

public sealed class PlayerData
{
    public string PlayerId { get; set; }
    public string PlayerName { get; set; }
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; }

    public PlayerData(string playerId = "", string playerName = "Undefined name", Vector3 position = new Vector3(), Quaternion rotation = new Quaternion())
    {
        PlayerId = playerId;
        PlayerName = playerName;
        Position = position;
        Rotation = rotation;
    }
}
