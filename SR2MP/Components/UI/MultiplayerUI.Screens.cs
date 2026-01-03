namespace SR2MP.Components.UI;

public sealed partial class MultiplayerUI
{
    private string usernameInput = "Player";
    private string ipInput = "127.0.0.1";
    private string portInput = "";
    private string hostPortInput = "1919";
    
    // Please only add the most important options here, this is for first time setup,
    // and most options should be changed in SR2E or the main settings menu.
    private void FirstTimeScreen()
    {
        bool valid = true;
        
        GUI.Label(CalculateTextLayout(6),"Please fill in the options to play multiplayer.");

        GUI.Label(CalculateTextLayout(6, 1, 2, 0),"Username:");
        
        usernameInput = GUI.TextField(CalculateInputLayout(6, 2, 1), usernameInput);

        if (string.IsNullOrWhiteSpace(usernameInput))
        {
            GUI.Label(CalculateTextLayout(6), "You must set an Username first.");
            valid = false;
        }

        if (valid)
        {
            if (GUI.Button(CalculateButtonLayout(6), "Save"))
            {
                firstTime = false;
                Main.SetConfigValue("internal_setup_ui", false);
                Main.SetConfigValue("username", usernameInput);
            }
        }
    }
    private void SettingsScreen()
    {
        bool valid = true;
        
        GUI.Label(CalculateTextLayout(6, 1, 2, 0),"Username:");
        usernameInput = GUI.TextField(CalculateInputLayout(6, 2, 1), usernameInput);

        if (string.IsNullOrWhiteSpace(usernameInput))
        {
            GUI.Label(CalculateTextLayout(6), "You must set an Username.");
            valid = false;
        }

        if (valid)
        {
            if (GUI.Button(CalculateButtonLayout(6), "Save"))
            {
                Main.SetConfigValue("username", usernameInput);
                viewingSettings = false;
            }
        }
    }
    
    // Use this when the mod properly handles joining from the main menu.
    private void MainMenuScreenUnused()
    {
        GUILayout.BeginVertical();
        
        if (GUILayout.Button("Settings"))
            viewingSettings = true;
        
        if (GUILayout.Button("Help"))
            viewingHelp = true;

        GUILayout.TextArea("Connect:");
        
        GUILayout.BeginHorizontal();
        GUILayout.TextArea("IP");
        ipInput = GUILayout.TextField(ipInput);
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        GUILayout.TextArea("Port");
        portInput = GUILayout.TextField(portInput);
        GUILayout.EndHorizontal();

        var validPort = ushort.TryParse(portInput, out var port);
        if (validPort)
        {
            if (GUILayout.Button("Connect")) { }
        }
        else
        {
            GUILayout.TextArea("Invalid port. Must be a number from 1 to 65535. Make sure your pc doesn't use the port anywhere else.");
        }
        
        GUILayout.TextArea("You must be in a save to host!");
        
        GUILayout.EndVertical();
    }
    private void MainMenuScreen()
    {
        if (GUI.Button(CalculateButtonLayout(6), "Settings"))
            viewingSettings = true;
        
        if (GUI.Button(CalculateButtonLayout(6), "Help"))
            viewingHelp = true;
        
        GUI.Label(CalculateTextLayout(6), "You must be in a save to host or connect!");
        GUI.Label(CalculateTextLayout(6, 2), "Make sure you join a save you DO NOT care about losing when you connect, OR back it up.");
    }
    private void InGameScreen()
    {
        if (GUI.Button(CalculateButtonLayout(6), "Settings"))
            viewingSettings = true;
        
        if (GUI.Button(CalculateButtonLayout(6), "Help"))
            viewingHelp = true;

        GUI.Label(CalculateTextLayout(6), "Connect:");
        
        GUI.Label(CalculateTextLayout(6, 1, 2, 0), "IP");
        ipInput = GUI.TextField(CalculateInputLayout(6, 2, 1), ipInput);
        
        GUI.Label(CalculateTextLayout(6, 1, 2, 0), "Port");
        portInput = GUI.TextField(CalculateInputLayout(6, 2, 1), portInput);

        var validPort = ushort.TryParse(portInput, out var port);
        if (validPort)
        {
            if (GUI.Button(CalculateButtonLayout(6), "Connect")) 
                Connect(ipInput, port);
        }
        else
        {
            GUI.Label(CalculateTextLayout(6, 2), "Invalid port. Must be a number from 1 to 65535.");
        }
        
        GUI.Label(CalculateTextLayout(6), "Host:");

        GUI.Label(CalculateTextLayout(6, 1, 2, 0), "Port");
        hostPortInput = GUI.TextField(CalculateInputLayout(6, 2, 1), hostPortInput);
   
        var validHostPort = ushort.TryParse(hostPortInput, out var hostPort);
        if (validHostPort)
        {
            if (GUI.Button(CalculateButtonLayout(6), "Host"))
                Host(hostPort);
        }
        else
        {
            GUI.Label(CalculateTextLayout(6, 2), "Invalid port. Must be a number from 1 to 65535. Make sure your pc doesn't use the port anywhere else.");
        }
    }
    private void UnimplementedScreen()
    {
        GUI.Label(CalculateTextLayout(6), "This screen hasn't been implemented yet.");
    }
}