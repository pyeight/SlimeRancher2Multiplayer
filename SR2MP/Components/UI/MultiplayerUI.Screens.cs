namespace SR2MP.Components.UI;

public sealed partial class MultiplayerUI
{
    private bool multiplayerUIHidden;
    private string usernameInput = "Player";
    private string ipInput = string.Empty;
    private string portInput = string.Empty;
    private string hostPortInput = "1919";
    private bool allowCheatsInput;

    private void FirstTimeScreen()
    {
        bool valid = true;

        DrawText("Please select an username to play multiplayer.");

        DrawText("Username:", 2);
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

        DrawText("Username:", 2);
        usernameInput = GUI.TextField(CalculateInputLayout(6, 2, 1), usernameInput);

        DrawText("Allow Cheats:", 2);
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

        DrawText("Join a world:");

        DrawText("IP", 2);
        ipInput = GUI.TextField(CalculateInputLayout(6, 2, 1), ipInput);

        DrawText("Port", 2);
        portInput = GUI.TextField(CalculateInputLayout(6, 2, 1), portInput);

        var validPort = ushort.TryParse(portInput, out var port);
        if (validPort)
        {
            if (GUI.Button(CalculateButtonLayout(6), "Connect"))
                Connect(ipInput, port);
        }
        else
        {
            DrawText("Invalid port: Must be a number from 1 to 65535.");
        }

        DrawText("Host a world:");

        DrawText("Port", 2);
        hostPortInput = GUI.TextField(CalculateInputLayout(6, 2, 1), hostPortInput);

        var validHostPort = ushort.TryParse(hostPortInput, out var hostPort);
        if (validHostPort)
        {
            if (GUI.Button(CalculateButtonLayout(6), "Host"))
                Host(hostPort);
        }
        else
        {
            DrawText("Invalid port. Must be a number from 1 to 65535.");
            DrawText("Make sure your pc doesn't use the port anywhere else.");
        }
    }

    private void UnimplementedScreen()
    {
        DrawText("This screen hasn't been implemented yet.");
    }

    private void HostingScreen()
    {
        DrawText($"You are the hosting on port: {Main.Server.Port}");
        DrawText("All players:");

        var players = playerManager.GetAllPlayers();

        foreach (var player in players)
        {
            DrawText(!string.IsNullOrEmpty(player.Username) ? player.Username : "Invalid username.");
        }
    }

    private void ConnectedScreen()
    {
        DrawText("You are connected to the server.");
        DrawText("All players:");

        var players = playerManager.GetAllPlayers();
        foreach (var player in players)
        {
            DrawText(!string.IsNullOrEmpty(player.Username) ? player.Username : "Invalid username.");
        }
    }
}