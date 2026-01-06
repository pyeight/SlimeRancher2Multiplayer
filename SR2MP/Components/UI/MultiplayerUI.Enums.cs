namespace SR2MP.Components.UI;

public sealed partial class MultiplayerUI
{
    public enum MenuState : byte
    {
        Hidden,
        DisconnectedMainMenu,
        DisconnectedInGame,
        ConnectedClient,
        ConnectedHost,
        SettingsInitial,
        SettingsMain,
        SettingsHelp,
        Kicked,
        Error,
    }
    public enum ErrorType : byte
    {
        None,
        UnknownError,
        InvalidIP,
        IPNotFound,
    }
    public enum HelpTopic : byte
    {
        Root,
        Playit,
        SyncState,
        DiscordSupport,
    }
}