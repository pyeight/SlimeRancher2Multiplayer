namespace SR2MP.Components.UI;

internal sealed partial class MultiplayerUI
{
    private string ipInput = string.Empty;
    private string portInput = string.Empty;
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
        DrawText("Tunnel IP:", 2);
        ipInput = DrawSafeTextInput("tunnel_ip", CalculateInputLayout(6, 2, 1), ipInput);
        DrawText("Tunnel Port:", 2);
        portInput = DrawSafeTextInput("tunnel_port", CalculateInputLayout(6, 2, 1), portInput);

        if (!string.IsNullOrWhiteSpace(joinManualError))
            DrawText(joinManualError);

        if (ipInput == "127.0.0.1" && !DevMode)
        {
            DrawText("Invalid IP. Must not be 127.0.0.1");
            DrawText("If you are using PlayIt, You have to use the IP and port from the left side of the app.");
        }

        if (ipInput.Length == 0)
            DrawText("Invalid IP. Must not be empty");

        if (ushort.TryParse(portInput, out var port))
        {
            if (GUI.Button(CalculateButtonLayout(6), "Join"))
                TryJoinManual(ipInput, port);
        }
        else
        {
            DrawText("Invalid port: Must be a number from 1 to 65535.");
        }
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