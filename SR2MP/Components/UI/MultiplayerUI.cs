using System.Net;
using MelonLoader;
using SR2E;
using SR2E.Utils;

namespace SR2MP.Components.UI;

// TODO: Make UI in an asset bundle so that it can be SR2 styled, among other things.
[RegisterTypeInIl2Cpp(false)]
public sealed class MultiplayerUI : MonoBehaviour
{
    #region State Enums
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
    #endregion
    
    #region Constants
    public const float TextHeight = 25f;
    public const float ButtonHeight = 25f;
    public const float InputHeight = 25f;
    public const float SpacerHeight = 7.5f;
    public const float HorizontalSpacing = 2f;
    public const float WindowWidth = 275f;
    public const float WindowHeight = 450f;
    #endregion

    #region Variables
    public MenuState state = MenuState.Hidden;
    
    private bool viewingSettings = false;
    private bool firstTime = true;
    private bool viewingHelp = false;
    
    private string usernameInput = "Player";
    private string ipInput = "127.0.0.1";
    private string portInput = "";
    private string hostPortInput = "1919";

    private Rect previousLayoutRect;
    private int previousLayoutHorizontalIndex;
    #endregion

    #region Initialization And Destruction
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
    #endregion

    #region GUI Controller
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
    #endregion

    #region Screens
    // Please only add the most important options here, this is for first time setup,
    // and most options should be changed in SR2E or the main settings menu.
    private void FirstTimeScreen()
    {
        bool valid = true;
        
        GUI.Label(CalculateTextLayout(6),"Please fill in the options to play multiplayer.");

        GUI.Label(CalculateTextLayout(6, 1, 2, 0),"Username:");
        
        usernameInput = GUI.TextField(CalculateInputLayout(6, 2, 1), usernameInput);

        if (string.IsNullOrWhiteSpace(usernameInput))
        {
            GUI.Label(CalculateTextLayout(6), "You must set an Username first.");
            valid = false;
        }

        if (valid)
        {
            if (GUI.Button(CalculateButtonLayout(6), "Save"))
            {
                firstTime = false;
                Main.SetConfigValue("internal_setup_ui", false);
                Main.SetConfigValue("username", usernameInput);
            }
        }
    }
    private void SettingsScreen()
    {
        bool valid = true;
        
        GUI.Label(CalculateTextLayout(6, 1, 2, 0),"Username:");
        usernameInput = GUI.TextField(CalculateInputLayout(6, 2, 1), usernameInput);

        if (string.IsNullOrWhiteSpace(usernameInput))
        {
            GUI.Label(CalculateTextLayout(6), "You must set an Username.");
            valid = false;
        }

        if (valid)
        {
            if (GUI.Button(CalculateButtonLayout(6), "Save"))
            {
                Main.SetConfigValue("username", usernameInput);
                viewingSettings = false;
            }
        }
    }
    
    // Use this when the mod properly handles joining from the main menu.
    private void MainMenuScreenUnused()
    {
        GUILayout.BeginVertical();
        
        if (GUILayout.Button("Settings"))
            viewingSettings = true;
        
        if (GUILayout.Button("Help"))
            viewingHelp = true;

        GUILayout.TextArea("Connect:");
        
        GUILayout.BeginHorizontal();
        GUILayout.TextArea("IP");
        ipInput = GUILayout.TextField(ipInput);
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        GUILayout.TextArea("Port");
        portInput = GUILayout.TextField(portInput);
        GUILayout.EndHorizontal();

        var validPort = ushort.TryParse(portInput, out var port);
        if (validPort)
        {
            if (GUILayout.Button("Connect")) { }
        }
        else
        {
            GUILayout.TextArea("Invalid port. Must be a number from 1 to 65535. Make sure your pc doesn't use the port anywhere else.");
        }
        
        GUILayout.TextArea("You must be in a save to host!");
        
        GUILayout.EndVertical();
    }
    private void MainMenuScreen()
    {
        if (GUI.Button(CalculateButtonLayout(6), "Settings"))
            viewingSettings = true;
        
        if (GUI.Button(CalculateButtonLayout(6), "Help"))
            viewingHelp = true;
        
        GUI.Label(CalculateTextLayout(6), "You must be in a save to host or connect!");
        GUI.Label(CalculateTextLayout(6, 2), "Make sure you join a save you DO NOT care about losing when you connect, OR back it up.");
    }
    private void InGameScreen()
    {
        if (GUI.Button(CalculateButtonLayout(6), "Settings"))
            viewingSettings = true;
        
        if (GUI.Button(CalculateButtonLayout(6), "Help"))
            viewingHelp = true;

        GUI.Label(CalculateTextLayout(6), "Connect:");
        
        GUI.Label(CalculateTextLayout(6, 1, 2, 0), "IP");
        ipInput = GUI.TextField(CalculateInputLayout(6, 2, 1), ipInput);
        
        GUI.Label(CalculateTextLayout(6, 1, 2, 0), "Port");
        portInput = GUI.TextField(CalculateInputLayout(6, 2, 1), portInput);

        var validPort = ushort.TryParse(portInput, out var port);
        if (validPort)
        {
            if (GUI.Button(CalculateButtonLayout(6), "Connect")) 
                Connect(ipInput, port);
        }
        else
        {
            GUI.Label(CalculateTextLayout(6, 2), "Invalid port. Must be a number from 1 to 65535.");
        }
        
        GUI.Label(CalculateTextLayout(6), "Host:");

        GUI.Label(CalculateTextLayout(6, 1, 2, 0), "Port");
        hostPortInput = GUI.TextField(CalculateInputLayout(6, 2, 1), hostPortInput);
   
        var validHostPort = ushort.TryParse(hostPortInput, out var hostPort);
        if (validHostPort)
        {
            if (GUI.Button(CalculateButtonLayout(6), "Host"))
                Host(hostPort);
        }
        else
        {
            GUI.Label(CalculateTextLayout(6, 2), "Invalid port. Must be a number from 1 to 65535. Make sure your pc doesn't use the port anywhere else.");
        }
    }
    private void UnimplementedScreen()
    {
        GUI.Label(CalculateTextLayout(6), "This screen hasn't been implemented yet.");
    }
    #endregion

    #region State Controller
    private bool GetIsLoading()
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
   
    #endregion

    #region Layout Controllers
    private Rect CalculateTextLayout(float originalX, int lines = 1, int horizontalShare = 1, int horizontalIndex = 0)
    {
        var maxWidth = WindowWidth - (HorizontalSpacing * 2);
        
        float x = originalX + HorizontalSpacing;
        float y = previousLayoutRect.y;
        float w = (maxWidth / horizontalShare);
        float h = TextHeight * lines;

        //if (horizontalShare != 1)
        //    w -= HorizontalSpacing * horizontalShare;
        
        x += horizontalIndex * w;
        if (horizontalIndex <= previousLayoutHorizontalIndex)
            y += previousLayoutRect.height + SpacerHeight;
        
        var result = new Rect(x, y, w, h);

        previousLayoutHorizontalIndex = horizontalIndex;
        previousLayoutRect = result;

        return result;
    }
    private Rect CalculateInputLayout(float originalX, int horizontalShare = 1, int horizontalIndex = 0)
    {
        var maxWidth = WindowWidth - (HorizontalSpacing * 2);
        
        float x = originalX + HorizontalSpacing;
        float y = previousLayoutRect.y;
        float w = (maxWidth / horizontalShare);
        float h = InputHeight;

        //if (horizontalShare != 1)
        //    w -= HorizontalSpacing * horizontalShare;
        
        x += horizontalIndex * w;
        if (horizontalIndex <= previousLayoutHorizontalIndex)
            y += previousLayoutRect.height + SpacerHeight;
        
        var result = new Rect(x, y, w, h);

        previousLayoutHorizontalIndex = horizontalIndex;
        previousLayoutRect = result;

        return result;
    }
    private Rect CalculateButtonLayout(float originalX, int horizontalShare = 1, int horizontalIndex = 0)
    {
        var maxWidth = WindowWidth - (HorizontalSpacing * 2);
        
        float x = originalX + HorizontalSpacing;
        float y = previousLayoutRect.y;
        float w = (maxWidth / horizontalShare);
        float h = ButtonHeight;

        //if (horizontalShare != 1)
        //    w -= HorizontalSpacing * horizontalShare;
        
        x += horizontalIndex * w;
        if (horizontalIndex <= previousLayoutHorizontalIndex)
            y += previousLayoutRect.height + SpacerHeight;
        
        var result = new Rect(x, y, w, h);

        previousLayoutHorizontalIndex = horizontalIndex;
        previousLayoutRect = result;

        return result;
    }
    #endregion

    #region Mod Controller
    public void Host(ushort port)
    {
        Server.Server server;
        MenuEUtil.CloseOpenMenu();
        server = Main.Server;
        server.Start(port, true);
    }
    public void Connect(string ip, ushort port)
    {
        MenuEUtil.CloseOpenMenu();
        
        if (ip.StartsWith("[") && ip.EndsWith("]"))
        {
            ip = ip[1..^1];
        }
        
        try
        {
            var addresses = Dns.GetHostAddresses(ip);
            if (addresses.Length > 0)
            {
                ip = addresses[0].ToString();
            }
            else
            {
                SrLogger.LogWarning("IP address incorrect!", SrLogTarget.Both);
            }
        }
        catch
        {
            SrLogger.LogWarning("IP address could not be resolved! (are you connected to the internet?)", SrLogTarget.Both);
        }

        Main.Client.Connect(ip, port);
    }
    #endregion
}