using SR2E;

namespace SR2MP.Components.UI;

internal sealed partial class MultiplayerUI
{
    private bool viewingSettings;
    private bool firstTime = true;
    private bool viewingHelp;

    public MenuState State = MenuState.Hidden;
    public ErrorType ErrorState = ErrorType.None;

    private bool chatShown;
    private MenuState previousState = MenuState.Hidden;

    private static bool GetIsLoading()
    {
        return SystemContext.Instance.SceneLoader.CurrentSceneGroup.name is "StandaloneStart" or "CompanyLogo" or "LoadScene";
    }

    private MenuState GetState()
    {
        if (multiplayerUIHidden) return MenuState.Hidden;

        var inGame = ContextShortcuts.inGame;
        var loading = GetIsLoading();
        var connected = Main.Client.IsConnected;
        var hosting = Main.Server.IsRunning;

        if (!string.IsNullOrWhiteSpace(ConnectionFailedReason))
        {
            ErrorState = ErrorType.ConnectionDeny;
            return MenuState.Error;
        }

        ErrorState = ErrorType.None;

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
        var isInGame = State is MenuState.DisconnectedInGame or MenuState.ConnectedClient or MenuState.ConnectedHost;

        var isMainMenu = State == MenuState.DisconnectedMainMenu;

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

            if (previousState == MenuState.DisconnectedMainMenu || previousState == MenuState.Hidden)
            {
                ClearAndWelcome();
            }
        }

        previousState = State;
    }
}