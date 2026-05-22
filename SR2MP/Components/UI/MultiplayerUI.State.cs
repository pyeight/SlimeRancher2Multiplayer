using Starlight;

namespace SR2MP.Components.UI;

internal sealed partial class MultiplayerUI
{
    private byte mainTab = 0;
    private byte hostTab = 0;
    private byte joinTab = 0;

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
        PlayIt,
        SyncState,
        DiscordSupport,
    }

    public MenuState state = MenuState.Hidden;

    private bool viewingSettings;
    private bool firstTime = true;
    private bool viewingHelp;
    private bool chatShown;
    private MenuState previousState = MenuState.Hidden;

    private bool GetIsLoading()
    {
        return SystemContext.Instance.SceneLoader.CurrentSceneGroup.name is
            "StandaloneStart" or "CompanyLogo" or "LoadScene";
    }

    private MenuState GetState()
    {
        if (multiplayerUIHidden) return MenuState.Hidden;

        var inGame = ContextShortcuts.inGame;
        var loading = GetIsLoading();
        var connected = Main.Client.IsConnected;
        var hosting = Main.Server.IsRunning;

        if (loading) return MenuState.Hidden;
        if (firstTime) return MenuState.SettingsInitial;
        if (viewingSettings) return MenuState.SettingsMain;
        if (viewingHelp) return MenuState.SettingsHelp;
        if (connected) return MenuState.ConnectedClient;
        if (hosting) return MenuState.ConnectedHost;

        return inGame ? MenuState.DisconnectedInGame : MenuState.DisconnectedMainMenu;
    }

    private void UpdateChatVisibility()
    {
        var isInGame = state is MenuState.DisconnectedInGame or MenuState.ConnectedClient or MenuState.ConnectedHost;
        var isMainMenu = state == MenuState.DisconnectedMainMenu;

        if (isMainMenu)
        {
            chatHidden = true;
            chatShown = false;
            internalChatToggle = false;
            return;
        }

        if (internalChatToggle) return;

        if (isInGame && !chatShown)
        {
            chatHidden = false;
            chatShown = true;

            if (previousState is MenuState.DisconnectedMainMenu or MenuState.Hidden)
                ClearAndWelcome();
        }
        else if (!isInGame)
        {
            chatShown = false;
        }

        previousState = state;
    }
}