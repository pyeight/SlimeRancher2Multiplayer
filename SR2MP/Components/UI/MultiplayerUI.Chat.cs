using MelonLoader;

namespace SR2MP.Components.UI;

public sealed partial class MultiplayerUI
{
    private bool chatHidden = false;
    private List<ChatMessage> chatMessages = new List<ChatMessage>();
    private int linesInUse;
    
    public sealed class ChatMessage
    {
        public string message;
        public string playerName;
        public long time;
        public int lines;
    }

    public void RegisterChatMessage(string message, string playerName, long time)
    {
        chatMessages.Add(new ChatMessage()
        {
            message = message,
            playerName = playerName,
            time = time,
            lines = DetectLineCount($"<{playerName}> {message}")
        });
    }

    private void CalculateLinesInUse()
    {
        linesInUse = 0;
        foreach (var chatMessage in chatMessages)
        {
            linesInUse += chatMessage.lines;
        }
    }
    
    private void ClearOldChatMessages()
    {
        CalculateLinesInUse();
        
        var count = linesInUse;

        if (count > MaxChatLines)
        {
            var over = count - MaxChatLines;

            for (var i = 0; i < over;)
            {
                var chatMessage = chatMessages[0];
                
                i += chatMessage.lines;
                
                chatMessages.RemoveAt(0);
            }
        }
    }
    
    private void DrawChatMessage(ChatMessage message)
    {
        GUI.Label(CalculateChatTextLayout(6, message.lines), $"<{message.playerName}> {message.message}");
    }
    
    private void DrawChat()
    {
        if (state == MenuState.DisconnectedMainMenu) return;
        if (chatHidden) return;
        
        GUI.Box(new Rect(6, Screen.height / 2, ChatWidth, ChatHeight), "Chat (F5 to hide)");
        
        ClearOldChatMessages();
        
        foreach (var chatMessage in chatMessages)
            DrawChatMessage(chatMessage);
    }
}