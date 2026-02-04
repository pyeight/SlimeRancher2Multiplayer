using System.Net;
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
        MenuEUtil.CloseOpenMenu();

        if (ip.StartsWith("[") && ip.EndsWith("]"))
        {
            ip = ip[1..^1];
        }

        try
        {
            var addresses = Dns.GetHostAddresses(ip);
            if (addresses.Length > 0)
            {
                ip = addresses[0].ToString();
            }
            else
            {
                SrLogger.LogWarning("IP address incorrect!", SrLogTarget.Both);
            }
        }
        catch
        {
            SrLogger.LogWarning("IP address could not be resolved! (are you connected to the internet?)", SrLogTarget.Both);
        }

        Main.Client.Connect(ip, port);

        Main.SetConfigValue("recent_ip", ipInput);
        Main.SetConfigValue("recent_port", portInput);
    }

    public static void Kick(string player)
    {
        // TODO: Implement kick functionality
    }

    private void Update()
    {
        HandleUIToggle();
        HandleChatToggle();
        HandleChatInput();
    }

    private static void DisableInput()
    {
        GameContext.Instance.InputDirector._mainGame.Map.Disable();
    }

    private static void EnableInput()
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
        if (!KeyCode.F5.OnKeyDown())
            return;
        if (isChatFocused)
        {
            UnfocusChat();
        }

        chatHidden = !chatHidden;
        internalChatToggle = true;

        if (!chatHidden || !disabledInput)
            return;
        EnableInput();
        disabledInput = false;
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
    }
}