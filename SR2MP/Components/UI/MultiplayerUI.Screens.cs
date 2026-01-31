namespace SR2MP.Components.UI;

public sealed partial class MultiplayerUI
{
    private enum MainTab
    {
        Join,
        Host
    }

    private enum JoinTab
    {
        Code,
        Manual
    }

    private enum HostTab
    {
        Automatic,
        Manual
    }

    private bool multiplayerUIHidden;
    private string usernameInput = "Player";
    private string ipInput = "";
    private string portInput = "";
    private string hostPortInput = "1919";
    private string hostIpInput = "";
    private string joinCodeInput = "";
    private string joinCodeError = "";
    private string joinManualError = "";
    private string hostAutoJoinCode = "";
    private string hostAutoError = "";
    private string hostAutoCopyStatus = "";
    private string hostManualJoinCode = "";
    private string hostManualError = "";
    private string hostManualCopyStatus = "";
    private bool hostAutoInProgress;
    private bool connectCheckInProgress;
    private bool connectCheckAwaitingResponse;
    private string connectCheckIp = "";
    private ushort connectCheckPort;
    private int connectCheckAttempts;
    private float connectCheckDeadline;
    private const float ConnectCheckTimeoutSeconds = 5f;
    private const int ConnectCheckMaxAttempts = 3;
    private MainTab mainTab = MainTab.Join;
    private JoinTab joinTab = JoinTab.Code;
    private HostTab hostTab = HostTab.Automatic;
    private bool allowCheatsInput;

    private void FirstTimeScreen()
    {
        bool valid = true;

        DrawText("Please select an username to play multiplayer.");

        DrawText("Username:", 2, 0);
        usernameInput = GUI.TextField(CalculateInputLayout(6, 2, 1), usernameInput);

        if (string.IsNullOrWhiteSpace(usernameInput))
        {
            DrawText("You must set an Username first.");
            valid = false;
        }

        if (!valid) return;
        if (!GUI.Button(CalculateButtonLayout(6), "Save settings")) return;
        
        firstTime = false;
        Main.SetConfigValue("internal_setup_ui", false);
        Main.SetConfigValue("username", usernameInput);
    }

    private void SettingsScreen()
    {
        bool validUsername = true;

        DrawText("Username:", 2, 0);
        usernameInput = GUI.TextField(CalculateInputLayout(6, 2, 1), usernameInput);

        DrawText("Allow Cheats:", 2, 0);
        if (GUI.Button(CalculateButtonLayout(6, 2, 1), allowCheatsInput.ToStringYesOrNo()))
        {
            allowCheatsInput = !allowCheatsInput;
        }

        if (string.IsNullOrWhiteSpace(usernameInput))
        {
            DrawText("You must set an Username.");
            validUsername = false;
        }

        if (!validUsername) return;
        if (!GUI.Button(CalculateButtonLayout(6), "Save")) return;
        
        Main.SetConfigValue("username", usernameInput);
        Main.SetConfigValue("allow_cheats", allowCheatsInput);
        viewingSettings = false;
    }

    private void MainMenuScreen()
    {
        if (GUI.Button(CalculateButtonLayout(6), "Settings"))
            viewingSettings = true;

        DrawText("You must be in a save to host or connect!");
        DrawText("Make sure you join an EMPTY save before connecting, this save file WILL BE RESET.");
    }

    private void InGameScreen()
    {
        if (GUI.Button(CalculateButtonLayout(6), "Settings"))
            viewingSettings = true;

        mainTab = DrawMainTabRow("Join", "Host", mainTab);

        if (mainTab == MainTab.Join)
        {
            DrawText("Join a world:");
            joinTab = DrawJoinTabRow("Code", "Manual", joinTab);
            if (joinTab == JoinTab.Code)
            {
                DrawText("Join code");
                joinCodeInput = GUI.TextField(CalculateInputLayout(6), joinCodeInput);
                if (!string.IsNullOrWhiteSpace(joinCodeError))
                    DrawText(joinCodeError);

                var joinLabel = connectCheckInProgress ? "Joining..." : "Join";
                GUI.enabled = !connectCheckInProgress;
                if (GUI.Button(CalculateButtonLayout(6), joinLabel))
                    TryJoinWithCode();
                GUI.enabled = true;
            }
            else
            {
                DrawText("IP", 2, 0);
                ipInput = GUI.TextField(CalculateInputLayout(6, 2, 1), ipInput);

                DrawText("Port", 2, 0);
                portInput = GUI.TextField(CalculateInputLayout(6, 2, 1), portInput);

                if (!string.IsNullOrWhiteSpace(joinManualError))
                    DrawText(joinManualError);

                var validPort = ushort.TryParse(portInput, out var port);
                if (validPort)
                {
                    var joinLabel = connectCheckInProgress ? "Joining..." : "Join";
                    GUI.enabled = !connectCheckInProgress;
                    if (GUI.Button(CalculateButtonLayout(6), joinLabel))
                        TryJoinManual(ipInput, port);
                    GUI.enabled = true;
                }
                else
                {
                    DrawText("Invalid port: Must be a number from 1 to 65535.");
                }
            }
        }
        else
        {
            DrawText("Host a world:");
            hostTab = DrawHostTabRow("Automatic", "Manual", hostTab);

            if (hostTab == HostTab.Automatic)
            {
                if (hostAutoInProgress)
                {
                    DrawText("Attempting UPnP...");
                }

                if (!string.IsNullOrWhiteSpace(hostAutoError))
                    DrawText(hostAutoError);

                GUI.enabled = !hostAutoInProgress;
                if (GUI.Button(CalculateButtonLayout(6), hostAutoInProgress ? "Hosting..." : "Host (Automatic)"))
                {
                    if (!hostAutoInProgress)
                        StartAutoHost();
                }
                GUI.enabled = true;
            }
            else
            {
                DrawText("IP", 2, 0);
                hostIpInput = GUI.TextField(CalculateInputLayout(6, 2, 1), hostIpInput);

                DrawText("Port", 2, 0);
                hostPortInput = GUI.TextField(CalculateInputLayout(6, 2, 1), hostPortInput);

                if (!string.IsNullOrWhiteSpace(hostManualError))
                    DrawText(hostManualError);

                var validHostPort = ushort.TryParse(hostPortInput, out var hostPort);
                if (validHostPort)
                {
                    GUI.enabled = !hostAutoInProgress;
                    if (GUI.Button(CalculateButtonLayout(6), hostAutoInProgress ? "Hosting..." : "Host"))
                        TryHostManual(hostIpInput, hostPort);
                    GUI.enabled = true;
                }
                else
                {
                    DrawText("Invalid port. Must be a number from 1 to 65535.");
                }
            }
        }
    }

    private void UnimplementedScreen()
    {
        DrawText("This screen hasn't been implemented yet.");
    }

    private void HostingScreen()
    {
        DrawText($"You are the hosting on port: {Main.Server.Port}");
        var joinCode = !string.IsNullOrWhiteSpace(hostAutoJoinCode) ? hostAutoJoinCode : hostManualJoinCode;
        if (!string.IsNullOrWhiteSpace(joinCode))
        {
            DrawText("Join code");
            GUI.enabled = false;
            GUI.TextField(CalculateInputLayout(6), joinCode);
            GUI.enabled = true;

            if (GUI.Button(CalculateButtonLayout(6), "Copy Join Code"))
            {
                GUIUtility.systemCopyBuffer = joinCode;
                if (!string.IsNullOrWhiteSpace(hostAutoJoinCode))
                    hostAutoCopyStatus = "Join code copied.";
                else
                    hostManualCopyStatus = "Join code copied.";
            }

            if (!string.IsNullOrWhiteSpace(hostAutoJoinCode) && !string.IsNullOrWhiteSpace(hostAutoCopyStatus))
                DrawText(hostAutoCopyStatus);
            if (!string.IsNullOrWhiteSpace(hostManualJoinCode) && !string.IsNullOrWhiteSpace(hostManualCopyStatus))
                DrawText(hostManualCopyStatus);
        }
        else
        {
            DrawText("Join code unavailable.");
        }
        DrawText("All players:");

        var players = playerManager.GetAllPlayers();

        foreach (var player in players)
        {
            if (!string.IsNullOrEmpty(player.Username))
            {
                DrawText(player.Username);
            }
            else
            {
                DrawText("Invalid username.");
            }
        }
    }

    private void ConnectedScreen()
    {
        DrawText("You are connected to the server.");
        DrawText("All players:");

        var players = playerManager.GetAllPlayers();
        foreach (var player in players)
        {
            if (!string.IsNullOrEmpty(player.Username))
            {
                DrawText(player.Username);
            }
            else
            {
                DrawText("Invalid username.");
            }
        }
    }
}
