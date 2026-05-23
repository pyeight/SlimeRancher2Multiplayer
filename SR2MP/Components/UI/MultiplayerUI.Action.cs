using System.Net;
using Il2CppInterop.Runtime.Attributes;
using Starlight.Utils;
using SR2MP.Shared.Utils;
using UnityEngine.InputSystem.Utilities;

namespace SR2MP.Components.UI;

internal sealed partial class MultiplayerUI
{
    public void Host(ushort port)
    {
        MenuEUtil.CloseOpenMenu();
        Main.Server.Start(port, true);
        Main.SetConfigValue("host_port", hostLocalPortInput);
    }

    private void TryHostManual(string ip, ushort tunnelPort, ushort localPort)
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

        Host(localPort);
        hostManualJoinCode = JoinCode.Encode(address, tunnelPort);
    }

    private void StartAutoHost()
    {
        if (hostAutoInProgress) return;

        hostAutoInProgress = true;
        hostAutoError = string.Empty;
        hostAutoJoinCode = string.Empty;
        hostAutoCopyStatus = string.Empty;
        hostManualJoinCode = string.Empty;
        hostManualCopyStatus = string.Empty;

        AutoHost.BeginAutoHost(OnAutoHostCompleted);
    }

    [HideFromIl2Cpp]
    private void OnAutoHostCompleted(AutoHostResult result)
    {
        if (!result.Success)
        {
            hostAutoError = result.ErrorMessage;
            hostAutoInProgress = false;
            return;
        }

        hostLocalPortInput = result.Port.ToString();
        Host(result.Port);
        hostAutoJoinCode = result.JoinCode;
        hostAutoInProgress = false;
    }

    public void Connect(string ip, ushort port)
    {
        MenuEUtil.CloseOpenMenu();

        ip = ResolveIp(ip);

        Main.Client.Connect(ip, port);

        Main.SetConfigValue("recent_ip", ipInput);
        Main.SetConfigValue("recent_port", portInput);
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

        if (string.IsNullOrWhiteSpace(ip))
        {
            joinManualError = "Invalid IP address or hostname.";
            return;
        }

        var resolved = ResolveIp(ip);
        if (resolved is null)
        {
            joinManualError = "Could not resolve hostname.";
            return;
        }

        Connect(resolved, port);
    }

    private static string? ResolveIp(string ip)
    {
        if (ip.StartsWith("[") && ip.EndsWith("]"))
            ip = ip[1..^1];

        if (IPAddress.TryParse(ip, out _))
            return ip;

        try
        {
            var addresses = Dns.GetHostAddresses(ip);
            if (addresses.Length > 0)
                return addresses[0].ToString();

            SrLogger.LogWarning("Could not resolve hostname: no addresses returned.");
        }
        catch
        {
            SrLogger.LogWarning("Could not resolve hostname (are you connected to the internet?)");
        }

        return null;
    }

    public void Kick(string player)
    {
        // TODO: Implement kick functionality
    }

    private static void DisableInput() =>
        GameContext.Instance.InputDirector._mainGame.Map.Disable();

    private static void EnableInput() =>
        GameContext.Instance.InputDirector._mainGame.Map.Enable();

    private void HandleUIToggle()
    {
        if (KeyCode.F4.OnKeyDown() && !isChatFocused)
            multiplayerUIHidden = !multiplayerUIHidden;
    }

    private void HandleChatToggle()
    {
        if (!KeyCode.F5.OnKeyDown()) return;

        if (isChatFocused)
            UnfocusChat();

        chatHidden = !chatHidden;
        internalChatToggle = true;

        if (chatHidden && disabledInput)
        {
            EnableInput();
            disabledInput = false;
        }
    }

    private void HandleChatInput()
    {
        if (chatHidden || state == MenuState.DisconnectedMainMenu)
            return;

        if (justUnfocusedInput)
        {
            justUnfocusedInput = false;
            return;
        }

        var enterPressed = KeyCode.Return.OnKeyDown() || KeyCode.KeypadEnter.OnKeyDown();
        var escapePressed = KeyCode.Escape.OnKeyDown();

        if (!string.IsNullOrEmpty(activeInputId) && activeInputId != "chat_input")
            return;

        if (!isChatFocused && enterPressed)
        {
            FocusChat();
            activeInputId = "chat_input";
            return;
        }

        if (!isChatFocused)
            return;

        if (enterPressed)
        {
            if (!string.IsNullOrWhiteSpace(chatInput))
                SendChatMessage(chatInput.Trim());

            ClearChatInput();
            UnfocusChat();
            activeInputId = string.Empty;
        }
        else if (escapePressed)
        {
            ClearChatInput();
            UnfocusChat();
            activeInputId = string.Empty;
        }
    }

    private void AdjustInputValues()
    {
        ipInput = ipInput.WithAllWhitespaceStripped();
        portInput = portInput.WithAllWhitespaceStripped();
        hostLocalPortInput = hostLocalPortInput.WithAllWhitespaceStripped();
        hostIpInput = hostIpInput.WithAllWhitespaceStripped();
        hostTunnelPortInput = hostTunnelPortInput.WithAllWhitespaceStripped();
        joinCodeInput = joinCodeInput.WithAllWhitespaceStripped();
    }
}