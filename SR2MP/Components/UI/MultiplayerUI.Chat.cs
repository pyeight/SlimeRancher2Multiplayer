using SR2E.Utils;
using SR2MP.Packets;

namespace SR2MP.Components.UI;

public sealed partial class MultiplayerUI
{
    private bool chatHidden = true;
    private List<ChatMessage> chatMessages = new();
    private Queue<Action> pendingMessageRegistrations = new();
    private HashSet<string> processedMessageIds = new();
    
    private string chatInput = "";
    private bool isChatFocused;
    private bool wasChatFocused;
    private const string ChatInputName = "ChatInput";
    
    private bool shouldUnfocusChat;
    private bool internalChatToggle;
    private bool shouldFocusChat;

    private bool disabledInput = false;
    
    public sealed class ChatMessage
    {
        public string message;
        public string playerName;
        public long time;
        public int lines;
        public string messageId;
    }

    public void RegisterChatMessage(string message, string playerName, string messageId)
    {
        messageId = $"{playerName}_{message.GetHashCode()}";
        
        if (processedMessageIds.Contains(messageId))
        {
            return;
        }

        pendingMessageRegistrations.Enqueue(() =>
        {
            var trimmedMessage = message.Trim();
            var dateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var timeString = dateTime.ToString("HH:mm:ss");
            var formattedMessage = $"[{timeString}] {playerName}: {trimmedMessage}";
            var lineInfo = CalculateMessageHeight(formattedMessage);
            
            chatMessages.Add(new ChatMessage
            {
                message = trimmedMessage,
                playerName = playerName,
                lines = lineInfo.lines,
                time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                messageId = messageId
            });
            
            processedMessageIds.Add(messageId);
            
            if (processedMessageIds.Count > 1000)
            {
                var toRemove = processedMessageIds.Take(500).ToList();
                foreach (var id in toRemove)
                {
                    processedMessageIds.Remove(id);
                }
            }
        });
    }

    private void ProcessPendingMessages()
    {
        while (pendingMessageRegistrations.TryDequeue(out var registration))
        {
            registration?.Invoke();
        }
    }

    private int CalculateTotalLinesInUse()
    {
        int total = 0;
        foreach (var message in chatMessages)
        {
            total += message.lines;
        }
        return total;
    }
    
    private void TrimOldMessages()
    {
        int totalLines = CalculateTotalLinesInUse();

        while (totalLines > MaxChatLines && chatMessages.Count > 0)
        {
            var oldestMessage = chatMessages[0];
            totalLines -= oldestMessage.lines;
            chatMessages.RemoveAt(0);
        }
    }

    private (int lines, float height) CalculateMessageHeight(string text)
    {
        var style = GUI.skin.label;
        var maxWidth = ChatWidth - (HorizontalSpacing * 2);
        var height = style.CalcHeight(new GUIContent(text), maxWidth);
        var lineCount = Mathf.CeilToInt(height / style.lineHeight);
        return (lineCount, height);
    }

    private void RenderChatMessage(ChatMessage message)
    {
        var dateTime = DateTimeOffset.FromUnixTimeSeconds(message.time).ToLocalTime();
        var timeString = dateTime.ToString("HH:mm:ss");
        var formattedMessage = $"[{timeString}] {message.playerName}: {message.message}";
        GUI.Label(CalculateChatMessageRect(formattedMessage), formattedMessage);
    }

    private Rect CalculateChatMessageRect(string text)
    {
        var maxWidth = ChatWidth - (HorizontalSpacing * 2);
        var messageInfo = CalculateMessageHeight(text);
        
        float x = 6 + HorizontalSpacing;
        float y = previousLayoutChatRect.y + previousLayoutChatRect.height;
        float w = maxWidth;
        float h = messageInfo.height;
        
        var rect = new Rect(x, y, w, h);
        previousLayoutChatRect = rect;
        
        return rect;
    }

    private void SendChatMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        message = message.Trim();
        
        string messageId = $"{Main.Username}_{message.GetHashCode()}";
        
        RegisterChatMessage(message, Main.Username, messageId);
        
        Main.SendToAllOrServer(new ChatMessagePacket()
        { 
            Message = message,
            Username = Main.Username,
            MessageID = messageId
        });
    }

    private void ClearChatInput()
    {
        chatInput = "";
    }

    public void ClearChatMessages()
    {
        chatMessages.Clear();
        processedMessageIds.Clear();
        
        RegisterChatMessage("Welcome to SR2MP!", "SYSTEM", "SYSTEM");
    }

    private void FocusChat()
    {
        shouldFocusChat = true;
        shouldUnfocusChat = false;
    }

    private void UnfocusChat()
    {
        shouldUnfocusChat = true;
        shouldFocusChat = false;
    }

    private void ProcessFocusRequests()
    {
        if (shouldFocusChat && Event.current.type == EventType.Repaint)
        {
            GUI.FocusControl(ChatInputName);
            shouldFocusChat = false;
            
            if (!disabledInput)
            {
                DisableInput();
                disabledInput = true;
            }
        }
        else if (shouldUnfocusChat)
        {
            GUI.FocusControl(null);
            shouldUnfocusChat = false;
            
            if (disabledInput)
            {
                EnableInput();
                disabledInput = false;
            }
        }
    }

    private void UpdateChatFocusState()
    {
        string currentChatFocus = GUI.GetNameOfFocusedControl();
        bool wasPreviouslyFocused = isChatFocused;
        isChatFocused = currentChatFocus == ChatInputName;
        
        if (isChatFocused && !wasPreviouslyFocused)
        {
            if (!disabledInput)
            {
                DisableInput();
                disabledInput = true;
            }
        }
        else if (!isChatFocused && wasPreviouslyFocused)
        {
            if (disabledInput)
            {
                EnableInput();
                disabledInput = false;
            }
        }

        wasChatFocused = isChatFocused;
    }

    private void DrawChat()
    {
        if (state == MenuState.DisconnectedMainMenu || chatHidden) return;
        
        float chatY = Screen.height / 2;
        
        GUI.Box(new Rect(6, chatY, ChatWidth, ChatHeight), 
                "Chat (F5 to toggle)");
        
        ProcessPendingMessages();
        TrimOldMessages();
        
        previousLayoutChatRect = new Rect(6, chatY + ChatHeaderHeight, ChatWidth, 0);
        
        foreach (var message in chatMessages)
        {
            RenderChatMessage(message);
        }
        
        GUI.SetNextControlName(ChatInputName);
        
        if (string.IsNullOrEmpty(chatInput) && !isChatFocused)
        {
            GUIStyle placeholderStyle = new(GUI.skin.textField);
            placeholderStyle.normal.textColor = Color.gray;
            
            GUI.Label(
                new Rect(6 + HorizontalSpacing, 
                         chatY + ChatHeight - InputHeight - 5, 
                         ChatWidth - (HorizontalSpacing * 2), 
                         InputHeight),
                "Press Enter to chat...",
                placeholderStyle
            );
        }
        
        string newChatInput = GUI.TextField(
            new Rect(6 + HorizontalSpacing, 
                     chatY + ChatHeight - InputHeight - 5, 
                     ChatWidth - (HorizontalSpacing * 2), 
                     InputHeight), 
            chatInput,
            MaxChatMessageLength
        );
        
        chatInput = newChatInput;
        
        UpdateChatFocusState();
        ProcessFocusRequests();
    }
}