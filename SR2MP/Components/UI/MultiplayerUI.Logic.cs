using System.Net;
using SR2MP.Shared.Utils;
using SR2E.Utils;
using UnityEngine.InputSystem.Utilities;

namespace SR2MP.Components.UI;

public sealed partial class MultiplayerUI
{
    public void Host(ushort port)
    {
        MenuEUtil.CloseOpenMenu();
        Main.Server.Start(port, true);
        Main.SetConfigValue("host_port", hostPortInput);
    }

    public void Connect(string ip, ushort port)
    {
        BeginConnectWithOnlineCheck(ip, port);
    }

    private void TryJoinWithCode()
    {
        joinCodeError = string.Empty;

        if (!JoinCode.TryDecode(joinCodeInput, out var address, out var port, out var error))
        {
            joinCodeError = error;
            return;
        }

        ipInput = address.ToString();
        portInput = port.ToString();
        Connect(ipInput, port);
    }

    private void TryJoinManual(string ip, ushort port)
    {
        joinManualError = string.Empty;

        if (!IPAddress.TryParse(ip, out _))
        {
            joinManualError = "Invalid IP address.";
            return;
        }

        Connect(ip, port);
    }

    private void TryHostManual(string ip, ushort port)
    {
        hostManualError = string.Empty;
        hostManualJoinCode = string.Empty;
        hostManualCopyStatus = string.Empty;
        hostAutoJoinCode = string.Empty;
        hostAutoCopyStatus = string.Empty;

        if (!IPAddress.TryParse(ip, out var address))
        {
            hostManualError = "Invalid IP address.";
            return;
        }

        Host(port);
        hostManualJoinCode = JoinCode.Encode(address, port);
    }

    private void StartAutoHost()
    {
        if (hostAutoInProgress)
            return;

        hostAutoInProgress = true;
        hostAutoError = string.Empty;
        hostAutoJoinCode = string.Empty;
        hostAutoCopyStatus = string.Empty;
        hostManualJoinCode = string.Empty;
        hostManualCopyStatus = string.Empty;

        AutoHostUtility.BeginAutoHost(result =>
        {
            if (!result.Success)
            {
                hostAutoError = result.ErrorMessage;
                hostAutoInProgress = false;
                return;
            }

            hostPortInput = result.Port.ToString();
            Host(result.Port);
            hostAutoJoinCode = result.JoinCode;
            hostAutoInProgress = false;
        });
    }

    public void Kick(string player) 
    { 
        // TODO: Implement kick functionality
    }

    private void Update()
    {
        HandleUIToggle();
        HandleChatToggle();
        HandleChatInput();
        UpdateConnectCheck();
    }

    private void DisableInput()
    {
        GameContext.Instance.InputDirector._mainGame.Map.Disable();
    }

    private void EnableInput()
    {
        GameContext.Instance.InputDirector._mainGame.Map.Enable();
    }
    
    private void HandleUIToggle()
    {
        if (KeyCode.F4.OnKeyDown() && !isChatFocused)
        {
            multiplayerUIHidden = !multiplayerUIHidden;
        }
    }

    private void HandleChatToggle()
    {
        if (KeyCode.F5.OnKeyDown())
        {
            if (isChatFocused)
            {
                UnfocusChat();
            }
            
            chatHidden = !chatHidden;
            internalChatToggle = true;
            
            if (chatHidden && disabledInput)
            {
                EnableInput();
                disabledInput = false;
            }
        }
    }

    private void HandleChatInput()
    {
        if (chatHidden || state == MenuState.DisconnectedMainMenu) return;

        bool enterPressed = KeyCode.Return.OnKeyDown() || KeyCode.KeypadEnter.OnKeyDown();
        bool escapePressed = KeyCode.Escape.OnKeyDown();

        if (isChatFocused)
        {
            if (enterPressed)
            {
                if (!string.IsNullOrWhiteSpace(chatInput))
                {
                    SendChatMessage(chatInput.Trim());
                }
                ClearChatInput();
                UnfocusChat();
            }
            else if (escapePressed)
            {
                ClearChatInput();
                UnfocusChat();
            }
        }
        else
        {
            if (enterPressed)
            {
                FocusChat();
            }
        }
    }

    private void AdjustInputValues()
    {
        ipInput = ipInput.WithAllWhitespaceStripped();
        portInput = portInput.WithAllWhitespaceStripped();
        hostPortInput = hostPortInput.WithAllWhitespaceStripped();
        hostIpInput = hostIpInput.WithAllWhitespaceStripped();
        joinCodeInput = joinCodeInput.WithAllWhitespaceStripped();
    }

    private void BeginConnectWithOnlineCheck(string ip, ushort port)
    {
        if (connectCheckInProgress)
            return;

        if (Main.Server.IsRunning())
        {
            SrLogger.LogWarning("You are already hosting a server, to connect to someone else, restart your game.");
            return;
        }

        if (Main.Client.IsConnected)
        {
            SrLogger.LogMessage("You are already connected to a Server!", SrLogTarget.Both);
            return;
        }

        connectCheckIp = ResolveIp(ip);
        connectCheckPort = port;
        connectCheckAttempts = 0;
        connectCheckInProgress = true;
        connectCheckAwaitingResponse = false;

        StartConnectCheckAttempt();
    }

    private void StartConnectCheckAttempt()
    {
        connectCheckAttempts++;
        connectCheckAwaitingResponse = true;
        connectCheckDeadline = UnityEngine.Time.realtimeSinceStartup + ConnectCheckTimeoutSeconds;

        Main.Client.QueryServerInfo(connectCheckIp, connectCheckPort, _ =>
        {
            if (!connectCheckInProgress)
                return;

            connectCheckAwaitingResponse = false;
            connectCheckInProgress = false;
            ConnectDirect(connectCheckIp, connectCheckPort);
        });
    }

    private void UpdateConnectCheck()
    {
        if (!connectCheckInProgress || !connectCheckAwaitingResponse)
            return;

        if (UnityEngine.Time.realtimeSinceStartup < connectCheckDeadline)
            return;

        connectCheckAwaitingResponse = false;

        if (connectCheckAttempts >= ConnectCheckMaxAttempts)
        {
            connectCheckInProgress = false;
            int randomComponent = UnityEngine.Random.Range(0, 999999999);
            RegisterSystemMessage(
                "Connection failed: server info timed out.",
                $"SYSTEM_CONNECT_TIMEOUT_{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{randomComponent}",
                SystemMessageDisconnect
            );
            SrLogger.LogWarning("Connection failed: server info timed out.", SrLogTarget.Both);
            return;
        }

        StartConnectCheckAttempt();
    }

    private void ConnectDirect(string ip, ushort port)
    {
        MenuEUtil.CloseOpenMenu();

        ip = ResolveIp(ip);

        Main.Client.Connect(ip, port);

        Main.SetConfigValue("recent_ip", ipInput);
        Main.SetConfigValue("recent_port", portInput);
    }

    private string ResolveIp(string ip)
    {
        if (ip.StartsWith("[") && ip.EndsWith("]"))
        {
            ip = ip[1..^1];
        }

        if (IPAddress.TryParse(ip, out _))
        {
            return ip;
        }

        try
        {
            var addresses = Dns.GetHostAddresses(ip);
            if (addresses.Length > 0)
            {
                return addresses[0].ToString();
            }

            SrLogger.LogWarning("IP address incorrect!", SrLogTarget.Both);
        }
        catch
        {
            SrLogger.LogWarning("IP address could not be resolved! (are you connected to the internet?)", SrLogTarget.Both);
        }

        return ip;
    }
}
