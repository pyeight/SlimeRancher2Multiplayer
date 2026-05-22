using Il2CppInterop.Runtime.Attributes;
using SR2MP.Packets;

namespace SR2MP.Components.UI;

internal sealed partial class MultiplayerUI
{
    private bool chatHidden = true;
    private readonly List<ChatMessage> chatMessages = new();
    private readonly Queue<Action> pendingMessageRegistrations = new();
    private readonly HashSet<string> processedMessageIds = new();

    private string chatInput = string.Empty;
    private bool isChatFocused;

    private bool shouldUnfocusChat;
    private bool internalChatToggle;
    private bool shouldFocusChat;
    private bool disabledInput;

    private sealed class ChatMessage
    {
        public string message;
        public string playerName;
        public long time;
        public int lines;
        public string messageId;
        public bool isSystemMessage;
        public byte systemMessageType;
    }

    public void RegisterChatMessage(string message, string playerName, string messageId)
        => RegisterMessageInternal(message, playerName, messageId, false, 0);

    public void RegisterSystemMessage(string message, string messageId, byte type)
        => RegisterMessageInternal(message, "SYSTEM", messageId, true, type);

    private void RegisterMessageInternal(string message, string displayName, string messageId, bool isSystem, byte systemType)
    {
        if (processedMessageIds.Contains(messageId)) return;

        pendingMessageRegistrations.Enqueue(() =>
        {
            var trimmedMessage = message.Trim();
            var dateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var formattedMessage = $"[{DateTimeOffset.FromUnixTimeSeconds(dateTime).ToLocalTime():HH:mm:ss}] {displayName}: {trimmedMessage}";
            var (lines, _) = CalculateMessageHeight(formattedMessage);

            chatMessages.Add(new ChatMessage
            {
                message = trimmedMessage,
                playerName = displayName,
                lines = lines,
                time = dateTime,
                messageId = messageId,
                isSystemMessage = isSystem,
                systemMessageType = systemType
            });

            processedMessageIds.Add(messageId);

            if (processedMessageIds.Count <= 1000) return;

            // Snapshot to avoid mutating the set while iterating
            var toRemove = processedMessageIds.Take(500).ToList();
            foreach (var id in toRemove)
                processedMessageIds.Remove(id);
        });
    }

    private void ProcessPendingMessages()
    {
        while (pendingMessageRegistrations.TryDequeue(out var registration))
            registration?.Invoke();
    }

    private int CalculateTotalLinesInUse()
    {
        var total = 0;
        foreach (var message in chatMessages)
            total += message.lines;
        return total;
    }

    private void TrimOldMessages()
    {
        var totalLines = CalculateTotalLinesInUse();

        while (totalLines > MaxChatLines && chatMessages.Count > 0)
        {
            totalLines -= chatMessages[0].lines;
            chatMessages.RemoveAt(0);
        }
    }

    private static (int lines, float height) CalculateMessageHeight(string text)
    {
        var style = GUI.skin.label;
        const float maxWidth = ChatWidth - (HorizontalSpacing * 2);
        var height = style.CalcHeight(new GUIContent(text), maxWidth);
        var lineCount = Mathf.CeilToInt(height / style.lineHeight);
        return (lineCount, height);
    }

    [HideFromIl2Cpp]
    private void RenderChatMessage(ChatMessage message)
    {
        var timeString = DateTimeOffset.FromUnixTimeSeconds(message.time).ToLocalTime().ToString("HH:mm:ss");

        string formattedMessage;
        if (message.isSystemMessage)
        {
            var systemColor = message.systemMessageType switch
            {
                SystemMessageConnect => ColorSystemConnect,
                SystemMessageDisconnect => ColorSystemDisconnect,
                SystemMessageClose => ColorSystemClose,
                _ => ColorSystemNormal
            };
            formattedMessage = $"<color={systemColor}>[{timeString}] SYSTEM: {message.message}</color>";
        }
        else
        {
            formattedMessage = $"[{timeString}] {message.playerName}: {message.message}";
        }

        GUI.Label(CalculateChatMessageRect(formattedMessage), formattedMessage);
    }

    private Rect CalculateChatMessageRect(string text)
    {
        const float maxWidth = ChatWidth - (HorizontalSpacing * 2);
        var (_, height) = CalculateMessageHeight(text);

        var rect = new Rect(
            6 + HorizontalSpacing,
            previousLayoutChatRect.y + previousLayoutChatRect.height,
            maxWidth,
            height
        );

        previousLayoutChatRect = rect;
        return rect;
    }

    private void SendChatMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        message = message.Trim();

        var messageId = $"{Main.Username}_{message.GetHashCode()}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

        RegisterChatMessage(message, Main.Username, messageId);

        Main.SendToAllOrServer(new ChatMessagePacket
        {
            Message = message,
            Username = Main.Username,
            MessageID = messageId,
            MessageType = 0
        });
    }

    private void ClearChatInput() => chatInput = string.Empty;

    public void ClearChatMessages()
    {
        chatMessages.Clear();
        processedMessageIds.Clear();
    }

    public void ClearAndWelcome()
    {
        ClearChatMessages();
        RegisterSystemMessage(
            "Welcome to Ranching Together!",
            $"SYSTEM_WELCOME_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            SystemMessageNormal
        );
    }

    private void FocusChat() => SetChatFocusPending(true);

    private void UnfocusChat() => SetChatFocusPending(false);

    private void SetChatFocusPending(bool focus)
    {
        shouldFocusChat = focus;
        shouldUnfocusChat = !focus;
    }

    private void ProcessFocusRequests()
    {
        if (shouldFocusChat && Event.current.type == EventType.Repaint)
        {
            shouldFocusChat = false;

            if (!disabledInput)
            {
                isChatFocused = true;
                activeInputId = "chat_input";
                DisableInput();
                disabledInput = true;
            }
        }
        else if (shouldUnfocusChat)
        {
            shouldUnfocusChat = false;

            if (disabledInput)
            {
                isChatFocused = false;
                activeInputId = string.Empty;
                EnableInput();
                disabledInput = false;
            }
        }
    }

    private void DrawChat()
    {
        if (state == MenuState.DisconnectedMainMenu || chatHidden) return;

        var chatY = Screen.height / 2f;

        GUI.Box(new Rect(6, chatY, ChatWidth, ChatHeight), "Chat (F5 to toggle)");

        ProcessPendingMessages();
        TrimOldMessages();

        previousLayoutChatRect = new Rect(6, chatY + ChatHeaderHeight, ChatWidth, 0);

        foreach (var message in chatMessages)
            RenderChatMessage(message);

        chatInput = DrawSafeTextInput(
            "chat_input",
            new Rect(
                6 + HorizontalSpacing,
                chatY + ChatHeight - InputHeight - 5,
                ChatWidth - (HorizontalSpacing * 2),
                InputHeight
            ),
            chatInput,
            MaxChatMessageLength,
            numbersOnly: false,
            isChat: true
        );

        ProcessFocusRequests();
    }
}