namespace SR2MP.Components.UI;

internal sealed partial class MultiplayerUI
{
    private string hostPortInput = "1919";
    private string hostIpInput = string.Empty;

    private string hostAutoJoinCode = string.Empty;
    private string hostAutoError = string.Empty;
    private string hostAutoCopyStatus = string.Empty;
    private bool hostAutoInProgress;

    private string hostManualJoinCode = string.Empty;
    private string hostManualError = string.Empty;
    private string hostManualCopyStatus = string.Empty;

    private void DrawHostSection()
    {
        DrawText("Host a world:");
        DrawTabRow(ref hostTab, "Automatic", "Manual Code", "Manual Simple");

        if (hostTab == 0)
            DrawHostAutomatic();
        else if (hostTab == 1)
            DrawHostManualCode();
        else
            DrawHostManualSimple();
    }

    private void DrawHostManualCode()
    {
        DrawText("Tunnel IP:", 2);
        hostIpInput = DrawSafeTextInput("host_ip", CalculateInputLayout(6, 2, 1), hostIpInput);
        DrawText("Tunnel Port:", 2);
        hostPortInput = DrawSafeTextInput("host_port", CalculateInputLayout(6, 2, 1), hostPortInput);

        if (!string.IsNullOrWhiteSpace(hostManualError))
            DrawText(hostManualError);

        if (hostIpInput == "127.0.0.1" && !DevMode)
        {
            DrawText("Invalid IP. Must not be 127.0.0.1");
            DrawText("If you are using PlayIt, You have to use the IP and port from the left side of the app.");
        }

        if (hostIpInput.Length == 0)
            DrawText("Invalid IP. Must not be empty");

        if (ushort.TryParse(hostPortInput, out var hostPort))
        {
            GUI.enabled = !hostAutoInProgress;
            if (GUI.Button(CalculateButtonLayout(6), hostAutoInProgress ? "Starting Server..." : "Start Server"))
                TryHostManual(hostIpInput, hostPort);
            GUI.enabled = true;
        }
        else
        {
            DrawText("Invalid port. Must be a number from 1 to 65535.");
        }
    }

    private void DrawHostAutomatic()
    {
        if (hostAutoInProgress)
            DrawText("Attempting UPnP...");

        if (!string.IsNullOrWhiteSpace(hostAutoError))
            DrawText(hostAutoError);

        GUI.enabled = !hostAutoInProgress;
        if (GUI.Button(CalculateButtonLayout(6), hostAutoInProgress ? "Starting Server..." : "Start Server"))
            StartAutoHost();
        GUI.enabled = true;
    }

    private void DrawHostManualSimple()
    {
        DrawText("Local Port:", 2);
        hostPortInput = DrawSafeTextInput("host_port", CalculateInputLayout(6, 2, 1), hostPortInput);

        if (!string.IsNullOrWhiteSpace(hostManualError))
            DrawText(hostManualError);

        if (ushort.TryParse(hostPortInput, out var hostPort))
        {
            GUI.enabled = !hostAutoInProgress;
            if (GUI.Button(CalculateButtonLayout(6), "Start Server"))
                Host(hostPort);
            GUI.enabled = true;
        }
        else
        {
            DrawText("Invalid port. Must be a number from 1 to 65535.");
        }
    }

    private void HostingScreen()
    {
        DrawText("Hosting on port: " + (Main.StreamerMode ? "Streamer Mode" : Main.Server.Port));

        DrawHostingJoinCode();

        if (GUI.Button(CalculateButtonLayout(6), "Resync All"))
            Main.Server.ReSyncManager.SynchronizeAll();

        if (GUI.Button(CalculateButtonLayout(6), "Stop Server"))
            Main.Server.Close();

        DrawText("All players:");
        foreach (var player in PlayerManager.GetAllPlayers())
            DrawText(!string.IsNullOrEmpty(player.Username) ? player.Username : "Invalid username.");
    }

    private void DrawHostingJoinCode()
    {
        if (hostTab == 2) return;

        var joinCode = !string.IsNullOrWhiteSpace(hostAutoJoinCode) ? hostAutoJoinCode : hostManualJoinCode;

        if (string.IsNullOrWhiteSpace(joinCode))
        {
            DrawText("Join code unavailable");
            return;
        }

        DrawText("Join code:");

        GUI.enabled = false;
        DrawSafeTextInput("join_code_view", CalculateInputLayout(6, 2, 1), Main.StreamerMode ? "Streamer Mode" : joinCode);
        GUI.enabled = true;

        if (GUI.Button(CalculateButtonLayout(6), "Copy Join Code"))
        {
            GUIUtility.systemCopyBuffer = joinCode;

            if (!string.IsNullOrWhiteSpace(hostAutoJoinCode))
                hostAutoCopyStatus = "Join code copied.";
            else
                hostManualCopyStatus = "Join code copied.";
        }

        var copyStatus = !string.IsNullOrWhiteSpace(hostAutoJoinCode) ? hostAutoCopyStatus : hostManualCopyStatus;
        if (!string.IsNullOrWhiteSpace(copyStatus))
            DrawText(copyStatus);
    }
}