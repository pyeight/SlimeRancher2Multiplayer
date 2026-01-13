namespace SR2MP.Components.UI;

public sealed partial class MultiplayerUI
{
    private bool hidden = false;
    private string usernameInput = "Player";
    private string ipInput = "127.0.0.1";
    private string portInput = "";
    private string hostPortInput = "1919";

    private bool allowCheatsInput = false;

    // Please only add the most important options here, this is for first time setup,
    // and most options should be changed in SR2E or the main settings menu.
    private void FirstTimeScreen()
    {
        bool valid = true;

        DrawText("Please fill in the options to play multiplayer.");

        DrawText("Username:", 2, 0);

        usernameInput = GUI.TextField(CalculateInputLayout(6, 2, 1), usernameInput);

        if (string.IsNullOrWhiteSpace(usernameInput))
        {
            DrawText("You must set an Username first.");
            valid = false;
        }

        if (!valid)
            return;
        if (!GUI.Button(CalculateButtonLayout(6), "Save"))
            return;
        firstTime = false;
        Main.SetConfigValue("internal_setup_ui", false);
        Main.SetConfigValue("username", usernameInput);
    }
    private void SettingsScreen()
    {
        bool valid = true;

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
            valid = false;
        }

        if (!valid)
            return;
        if (!GUI.Button(CalculateButtonLayout(6), "Save"))
            return;
        Main.SetConfigValue("username", usernameInput);
        Main.SetConfigValue("allow_cheats", allowCheatsInput);
        viewingSettings = false;
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

        //if (GUI.Button(CalculateButtonLayout(6), "Help"))
        //    viewingHelp = true;

        DrawText("You must be in a save to host or connect!");
        DrawText("Make sure you join an EMPTY save before connecting, this save file WILL BE RESET.");
    }
    private void InGameScreen()
    {
        if (GUI.Button(CalculateButtonLayout(6), "Settings"))
            viewingSettings = true;

        //if (GUI.Button(CalculateButtonLayout(6), "Help"))
        //    viewingHelp = true;

        DrawText("Connect:");

        DrawText("IP", 2, 0);
        ipInput = GUI.TextField(CalculateInputLayout(6, 2, 1), ipInput);

        DrawText("Port", 2, 0);
        portInput = GUI.TextField(CalculateInputLayout(6, 2, 1), portInput);

        var validPort = ushort.TryParse(portInput, out var port);
        if (validPort)
        {
            if (GUI.Button(CalculateButtonLayout(6), "Connect"))
                Connect(ipInput, port);
        }
        else
        {
            DrawText("Invalid port. Must be a number from 1 to 65535.");
        }

        DrawText("Host:");

        DrawText("Port", 2, 0);
        hostPortInput = GUI.TextField(CalculateInputLayout(6, 2, 1), hostPortInput);

        var validHostPort = ushort.TryParse(hostPortInput, out var hostPort);
        if (validHostPort)
        {
            if (GUI.Button(CalculateButtonLayout(6), "Host"))
                Host(hostPort);
        }
        else
        {
            DrawText("Invalid port. Must be a number from 1 to 65535. Make sure your pc doesn't use the port anywhere else.");
        }
    }
    private void UnimplementedScreen()
    {
        DrawText("This screen hasn't been implemented yet.");
    }
    private void HostingScreen()
    {
        DrawText("You are the host.");
        DrawText($"Hosting on port: {Main.Server.Port}");
        DrawText("If you are using PlayIt, you must host on the port you set as Local Port on PlayIt.");

        DrawText("Players:");
        foreach (var player in playerManager.GetAllPlayers())
        {
            DrawText(player.Username);
            //if (GUI.Button(CalculateButtonLayout(6, 2, 1), "Kick"))
            //    Kick(player.PlayerId);
        }
    }
    private void ConnectedScreen()
    {
        DrawText("You are the client.");
        DrawText("Players:");
        foreach (var player in playerManager.GetAllPlayers())
        {
            DrawText(player.Username);
        }
    }
}