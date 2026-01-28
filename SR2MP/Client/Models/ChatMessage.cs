namespace SR2MP.Client.Models;

public sealed class ChatMessage
{
    public string PlayerId { get; set; }
    public string Message { get; set; }
    public bool IsOwnMessage { get; set; }

    public ChatMessage(string playerId, string message, DateTime timestamp, bool isOwnMessage = false)
    {
        PlayerId = playerId;
        Message = message;
        IsOwnMessage = isOwnMessage;
    }
}