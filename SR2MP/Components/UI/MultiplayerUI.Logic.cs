using System.Net;
using SR2E.Enums;
using SR2E.Utils;
using UnityEngine.InputSystem.Utilities;

namespace SR2MP.Components.UI;

public sealed partial class MultiplayerUI
{
    public void Host(ushort port)
    {
        Server.Server server;
        MenuEUtil.CloseOpenMenu();
        server = Main.Server;
        server.Start(port, true);
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

    public void Kick(string player) { }

    private void Update()
    {
        if (KeyCode.F4.OnKeyDown())
            hidden = !hidden;
        
        if (KeyCode.F5.OnKeyDown())
            chatHidden = !chatHidden;
    }

    private void AdjustInputValues()
    {
        ipInput = ipInput.WithAllWhitespaceStripped();
        portInput = portInput.WithAllWhitespaceStripped();
    }
}