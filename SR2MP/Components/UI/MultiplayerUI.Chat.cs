using MelonLoader;

namespace SR2MP.Components.UI;

public sealed partial class MultiplayerUI
{
    private bool chatHidden = false;
    private List<ChatMessage> chatMessages = new List<ChatMessage>();
    private int linesInUse;
    
    private Queue<Action> waitToRegister = new Queue<Action>();
    
    public sealed class ChatMessage
    {
        public string message;
        public string playerName;
        public long time;
        public int lines;
    }

    public void RegisterChatMessage(string message, string playerName, long time)
    {
        waitToRegister.Enqueue(() =>
        {
            chatMessages.Add(new ChatMessage()
            {
                message = message,
                playerName = playerName,
                time = time,
                lines = DetectHeight($"<{playerName}> {message}").lines
            });
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

    private void RegisterMessages()
    {
        while (waitToRegister.TryDequeue(out var messageAction))
        {
            messageAction();
        }
    }
    
    private void DrawChatMessage(ChatMessage message)
    {
        var msg = $"<{message.playerName}> {message.message}";
        GUI.Label(CalculateChatTextLayout(6, msg), msg);
    }
    
    private void DrawChat()
    {
        if (state == MenuState.DisconnectedMainMenu) return;
        if (chatHidden) return;
        
        GUI.Box(new Rect(6, Screen.height / 2, ChatWidth, ChatHeight), "Chat (F5 to toggle)");
        
        RegisterMessages();
        ClearOldChatMessages();
        
        foreach (var chatMessage in chatMessages)
            DrawChatMessage(chatMessage);
    }
}