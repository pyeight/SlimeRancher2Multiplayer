using SR2E;

namespace SR2MP.Components.UI;

public sealed partial class MultiplayerUI
{
    private bool viewingSettings;
    private bool firstTime = true;
    private bool viewingHelp;

    public MenuState state = MenuState.Hidden;

    private static bool GetIsLoading() => SystemContext.Instance.SceneLoader.CurrentSceneGroup.name is "StandaloneStart" or "CompanyLogo" or "LoadScene";

    private MenuState GetState()
    {
        if (hidden) return MenuState.Hidden;

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
}