namespace SR2MP.Components.UI;

internal sealed partial class MultiplayerUI
{
    private string hostIpInput = string.Empty;
    private string hostTunnelPortInput = string.Empty;
    private string hostLocalPortInput = string.Empty;
    
    private bool hostAutoInProgress;
    
    private string hostJoinCode = string.Empty;
    private string hostJoinCodeCopyStatus = string.Empty;
    
    private string hostAutoError = string.Empty;
    private string hostManualError = string.Empty;

    private void DrawHostSection()
    {
        DrawText("Host a world:");
        DrawTabRow(ref hostTab, "Automatic", "Manual");

        if (hostTab == 0)
            DrawHostAutomatic();
        else if (hostTab == 1)
            DrawHostManualSimple();
        else if (hostTab == 2)
            DrawHostManualCode();
    }

    internal void TriggerManualCode() => hostTab = 2;
    private void DrawHostManualCode()
    {
        DrawText("Tunnel IP:", 2);
        hostIpInput = DrawSafeTextInput("host_ip", CalculateInputLayout(6, 2, 1), hostIpInput);
        DrawText("Tunnel Port:", 2);
        hostTunnelPortInput = DrawSafeTextInput("host_tunnel_port", CalculateInputLayout(6, 2, 1), hostTunnelPortInput);
        DrawText("Local Port:", 2);
        hostLocalPortInput = DrawSafeTextInput("host_local_port", CalculateInputLayout(6, 2, 1), hostLocalPortInput);

        if (!string.IsNullOrWhiteSpace(hostManualError))
            DrawText(hostManualError);

        if (hostIpInput == "127.0.0.1" && !DevMode)
        {
            DrawText("Invalid IP. Must not be 127.0.0.1");
            DrawText("If you are using PlayIt, You have to use the IP and port from the left side of the app.");
        }

        if (hostIpInput.Length == 0)
            DrawText("Invalid IP. Must not be empty");

        var ipValid = hostIpInput.Length > 0 && (hostIpInput != "127.0.0.1" || DevMode);
        var localPortValid = ushort.TryParse(hostLocalPortInput, out var hostPort);
        var tunnelPortValid = ushort.TryParse(hostTunnelPortInput, out var tunnelPort);

        if (!localPortValid)
            DrawText("Invalid local port. Must be a number from 1 to 65535.");

        if (!tunnelPortValid)
            DrawText("Invalid tunnel port. Must be a number from 1 to 65535.");

        GUI.enabled = ipValid && localPortValid && tunnelPortValid && !hostAutoInProgress;
        if (GUI.Button(CalculateButtonLayout(6), hostAutoInProgress ? "Starting Server..." : "Start Server"))
            TryHostManual(hostIpInput, tunnelPort, hostPort);
        GUI.enabled = true;
    }

    private void DrawHostAutomatic()
    {
        if (hostAutoInProgress)
            DrawText("Attempting Auto Host...");

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
        hostLocalPortInput = DrawSafeTextInput("host_port", CalculateInputLayout(6, 2, 1), hostLocalPortInput);

        // todo: separate manual code and manual simple, if manual code errors,
        // actually no need really, if people use manualcode command, they know what they're doing
        if (!string.IsNullOrWhiteSpace(hostManualError))
            DrawText(hostManualError);

        if (ushort.TryParse(hostLocalPortInput, out var hostPort))
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
        if (hostTab == 1) return;

        if (string.IsNullOrWhiteSpace(hostJoinCode))
        {
            DrawText("Join code unavailable");
            return;
        }

        DrawText("Join code:");

        GUI.enabled = false;
        DrawSafeTextInput("join_code_view", CalculateInputLayout(6, 2, 1), Main.StreamerMode ? "Streamer Mode" : hostJoinCode);
        GUI.enabled = true;

        if (GUI.Button(CalculateButtonLayout(6), "Copy Join Code"))
        {
            GUIUtility.systemCopyBuffer = hostJoinCode;

            if (!string.IsNullOrWhiteSpace(hostJoinCode))
                hostJoinCodeCopyStatus = "Join code copied.";
        }
        
        if (!string.IsNullOrWhiteSpace(hostJoinCodeCopyStatus))
            DrawText(hostJoinCodeCopyStatus);
    }
}