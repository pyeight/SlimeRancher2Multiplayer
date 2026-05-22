using MelonLoader;
using Starlight.Storage;
using Starlight.Utils;

namespace SR2MP.Components.UI;

// todo: Asset bundle
[InjectIntoIL]
internal sealed partial class MultiplayerUI : MonoBehaviour
{
    public static MultiplayerUI Instance { get; private set; }

    private bool didUnfocus;

    private void Awake()
    {
        firstTime = Main.SetupUI;
        usernameInput = Main.Username;
        allowCheatsInput = Main.AllowCheats;
        ipInput = Main.SavedConnectIP;
        portInput = Main.SavedConnectPort;
        hostPortInput = Main.SavedHostPort;

        if (Instance)
        {
            SrLogger.LogError("Tried to create instance of MultiplayerUI, but it already exists!");
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        Instance = null!;
    }

    private void Update()
    {
        HandleUIToggle();
        HandleChatToggle();
        HandleChatInput();
    }

    private void OnGUI()
    {
        GUI.skin.label.richText = true;

        if (Event.current.type == EventType.Layout)
        {
            state = GetState();
            UpdateChatVisibility();
        }

        previousLayoutRect = new Rect(6, 16, WindowWidth, 0);
        previousLayoutHorizontalIndex = 0;

        if (!MenuEUtil.isAnyMenuOpen && !MenuEUtil.isAnyPopUpOpen)
        {
            didUnfocus = false;
            DrawWindow();
            DrawChat();
        }
        else if (!didUnfocus)
        {
            UnfocusChat();
            didUnfocus = true;
        }
    }

    private void DrawWindow()
    {
        if (state == MenuState.Hidden) return;

        GUI.Box(new Rect(6, 6, WindowWidth, WindowHeight), "SR2MP (F4 to toggle)");

        switch (state)
        {
            case MenuState.SettingsInitial:
                FirstTimeScreen();
                break;
            case MenuState.SettingsMain:
                SettingsScreen();
                break;
            case MenuState.DisconnectedMainMenu:
                MainMenuScreen();
                break;
            case MenuState.DisconnectedInGame:
                InGameScreen();
                break;
            case MenuState.ConnectedClient:
                ConnectedScreen();
                break;
            case MenuState.ConnectedHost:
                HostingScreen();
                break;
            default:
                UnimplementedScreen();
                break;
        }

        AdjustInputValues();
    }
}