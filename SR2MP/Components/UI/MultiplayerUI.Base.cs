using System.Net;
using MelonLoader;
using SR2E;
using SR2E.Utils;

namespace SR2MP.Components.UI;

// TODO: Make UI in an asset bundle so that it can be SR2 styled, among other things.
[RegisterTypeInIl2Cpp(false)]
public sealed partial class MultiplayerUI : MonoBehaviour
{
    public static MultiplayerUI Instance { get; private set; }

    private void Awake()
    {
        firstTime = Main.SetupUI;
        usernameInput = Main.Username;
        if (Instance)
        {
            SrLogger.LogError("Tried to create instance of MultiplayerUI, but it already exists!", SrLogTarget.Both);
            Destroy(this);
            return;
        }

        Instance = this;
    }

    // Not sure if OnDestroy is needed for the singleton, though it is IL2CPP stuff, so I don't want to deal with bugs.
    private void OnDestroy()
    {
        Instance = null!;
    }

    private void OnGUI()
    {
        state = GetState();

        if (state == MenuState.Hidden)
            return;

        previousLayoutRect = new Rect(6, 16, WindowWidth, 0);

        DrawWindow();
        //DrawChat();
    }

    private void DrawWindow()
    {
        GUI.Box(new Rect(6, 6, WindowWidth, WindowHeight), "SR2MP");
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
            default:
                UnimplementedScreen();
                break;
        }
    }
    private void DrawChat()
    {
        UnimplementedScreen();
    }

}