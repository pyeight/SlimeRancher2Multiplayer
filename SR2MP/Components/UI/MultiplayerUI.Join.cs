namespace SR2MP.Components.UI;

public sealed partial class MultiplayerUI
{
    private string ipInput = string.Empty;
    private string portInput = string.Empty;
    private string joinCodeInput = string.Empty;

    private string joinCodeError = string.Empty;
    private string joinManualError = string.Empty;

    private JoinTab joinTab = JoinTab.Code;

    private void DrawJoinSection()
    {
        DrawText("Join a world:");
        joinTab = DrawJoinTabRow("Code", "Manual", joinTab);

        if (joinTab == JoinTab.Code)
            DrawJoinByCode();
        else
            DrawJoinManual();
    }

    private void DrawJoinByCode()
    {
        DrawText("Join code:");
        joinCodeInput = GUI.TextField(CalculateInputLayout(6), joinCodeInput);

        if (!string.IsNullOrWhiteSpace(joinCodeError))
            DrawText(joinCodeError);

        if (GUI.Button(CalculateButtonLayout(6), "Join"))
            TryJoinWithCode();
    }

    private void DrawJoinManual()
    {
        DrawText("Tunnel IP:", 2, 0);
        ipInput = GUI.TextField(CalculateInputLayout(6, 2, 1), ipInput);

        DrawText("Tunnel Port:", 2, 0);
        portInput = GUI.TextField(CalculateInputLayout(6, 2, 1), portInput);

        if (!string.IsNullOrWhiteSpace(joinManualError))
            DrawText(joinManualError);

        if (ipInput == "127.0.0.1" && !devMode)
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
            Main.Server.reSyncManager.RequestResync();

        if (GUI.Button(CalculateButtonLayout(6), "Disconnect"))
            Main.Client.Disconnect();

        DrawText("All players:");

        foreach (var player in playerManager.GetAllPlayers())
            DrawText(!string.IsNullOrEmpty(player.Username) ? player.Username : "Invalid username.");
    }
}