using System.Net;
using Il2CppInterop.Runtime.Attributes;
using SR2E.Utils;
using SR2MP.Shared.Utils;
using UnityEngine.InputSystem.Utilities;

namespace SR2MP.Components.UI;

internal sealed partial class MultiplayerUI
{
    public void Host(ushort port)
    {
        MenuEUtil.CloseOpenMenu();
        Main.Server.Start(port, true);
        Main.SetConfigValue("host_port", hostPortInput);
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

    internal void StartAutoHost()
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

        hostPortInput = result.Port.ToString();
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

        if (!IPAddress.TryParse(ip, out _))
        {
            joinManualError = "Invalid IP address.";
            return;
        }

        Connect(ip, port);
    }

    private static string ResolveIp(string ip)
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

            SrLogger.LogWarning("IP address incorrect!");
        }
        catch
        {
            SrLogger.LogWarning("IP address could not be resolved! (are you connected to the internet?)");
        }

        return ip;
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
        if (chatHidden || state == MenuState.DisconnectedMainMenu) return;

        var enterPressed = KeyCode.Return.OnKeyDown() || KeyCode.KeypadEnter.OnKeyDown();
        var escapePressed = KeyCode.Escape.OnKeyDown();

        if (isChatFocused)
        {
            if (enterPressed)
            {
                if (!string.IsNullOrWhiteSpace(chatInput))
                    SendChatMessage(chatInput.Trim());

                ClearChatInput();
                UnfocusChat();
            }
            else if (escapePressed)
            {
                ClearChatInput();
                UnfocusChat();
            }
        }
        else if (enterPressed)
        {
            FocusChat();
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
}