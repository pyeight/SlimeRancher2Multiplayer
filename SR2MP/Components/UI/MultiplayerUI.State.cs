using SR2E;

namespace SR2MP.Components.UI;

public sealed partial class MultiplayerUI
{
    private bool viewingSettings = false;
    private bool firstTime = true;
    private bool viewingHelp = false;

    public MenuState state = MenuState.Hidden;
    private bool chatShown = false;
    private MenuState previousState = MenuState.Hidden;

    private static bool GetIsLoading()
    {
        switch (SystemContext.Instance.SceneLoader.CurrentSceneGroup.name)
        {
            case "StandaloneStart":
            case "CompanyLogo":
            case "LoadScene":
                return true;
        }

        return false;
    }

    private MenuState GetState()
    {
        if (multiplayerUIHidden) return MenuState.Hidden;

        var inGame = ContextShortcuts.inGame;
        var loading = GetIsLoading();
        var connected = Main.Client.IsConnected;
        var hosting = Main.Server.IsRunning();

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
        bool isInGame = state is MenuState.DisconnectedInGame or MenuState.ConnectedClient or MenuState.ConnectedHost;

        bool isMainMenu = state == MenuState.DisconnectedMainMenu;

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

        previousState = state;
    }
}