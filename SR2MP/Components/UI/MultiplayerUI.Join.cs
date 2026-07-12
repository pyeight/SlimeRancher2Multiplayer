namespace SR2MP.Components.UI;

internal sealed partial class MultiplayerUI
{
    private string joinIpInput = string.Empty;
    private string joinPortInput = string.Empty;
    private string joinCodeInput = string.Empty;

    private string joinCodeError = string.Empty;
    private string joinManualError = string.Empty;

    private void DrawJoinSection()
    {
        DrawText("Join a world:");
        DrawTabRow(ref joinTab, "Code", "Manual");

        if (joinTab == 0)
            DrawJoinByCode();
        else
            DrawJoinManual();
    }

    private void DrawJoinByCode()
    {
        DrawText("Join code:", 2);
        joinCodeInput = DrawSafeTextInput("join_code", CalculateInputLayout(6, 2, 1), joinCodeInput);

        if (!string.IsNullOrWhiteSpace(joinCodeError))
            DrawText(joinCodeError);

        if (GUI.Button(CalculateButtonLayout(6), "Join"))
            TryJoinWithCode();
    }

    private void DrawJoinManual()
    {
        DrawText("IP:", 2);
        joinIpInput = DrawSafeTextInput("tunnel_ip", CalculateInputLayout(6, 2, 1), joinIpInput);
        DrawText("Port:", 2);
        joinPortInput = DrawSafeTextInput("tunnel_port", CalculateInputLayout(6, 2, 1), joinPortInput);

        if (!string.IsNullOrWhiteSpace(joinManualError))
            DrawText(joinManualError);

        if (joinIpInput == "127.0.0.1" && !DevMode)
        {
            DrawText("Invalid IP. Must not be 127.0.0.1");
            DrawText("If you are using PlayIt, You have to use the IP and port from the left side of the app.");
        }

        if (joinIpInput.Length == 0)
            DrawText("Invalid IP. Must not be empty");

        if (ushort.TryParse(joinPortInput, out var port))
        {
            if (GUI.Button(CalculateButtonLayout(6), "Join"))
                TryJoinManual(joinIpInput, port);
        }
        else
        {
            DrawText("Invalid port: Must be a number from 1 to 65535.");
        }
    }
    
    private void ConnectingScreen()
    {
        DrawText("Trying to connect to the server...");
    }

    private void ConnectedScreen()
    {
        DrawText("You are connected to the server.");

        if (GUI.Button(CalculateButtonLayout(6), "Request resync"))
            Main.Server.ReSyncManager.RequestResync();

        if (GUI.Button(CalculateButtonLayout(6), "Disconnect"))
            Main.Client.Disconnect();

        DrawText("All players:");

        foreach (var player in PlayerManager.GetAllPlayers())
            DrawText(!string.IsNullOrEmpty(player.Username) ? player.Username : "Invalid username.");
    }
}