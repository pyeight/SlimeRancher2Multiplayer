using Il2CppMonomiPark.SlimeRancher.Dialogue.CommStation;
using SR2MP.Packets.World;

namespace SR2MP.Shared.Managers;

internal static class NetworkConversationManager
{
    private static Dictionary<string, FixedConversation>? conversationsByName;

    private static Dictionary<string, FixedConversation> ConversationsByName
    {
        get
        {
            if (conversationsByName != null)
                return conversationsByName;

            conversationsByName = new Dictionary<string, FixedConversation>();
            foreach (var conversation in Resources.FindObjectsOfTypeAll<FixedConversation>())
                conversationsByName[conversation.name] = conversation;

            return conversationsByName;
        }
    }

    private static FixedConversation? FindConversation(string name)
        => ConversationsByName.TryGetValue(name, out var conversation) ? conversation : null;
    
    internal static void SendConversationPlayed(string conversationName)
        => Main.SendToAllOrServer(new ConversationPlayedPacket { ConversationName = conversationName });
    
    internal static void ApplyConversationPlayed(string conversationName)
    {
        var conversation = FindConversation(conversationName);
        if (conversation?.HasBeenPlayed() != false)
            return;

        HandlingPacket = true;
        conversation.RecordPlayed();
        HandlingPacket = false;
    }
    
    internal static List<string?> GetPlayedConversations()
    {
        var names = new List<string?>();

        foreach (var conversation in ConversationsByName.Values)
            if (conversation.HasBeenPlayed())
                names.Add(conversation.name);

        return names;
    }
}
