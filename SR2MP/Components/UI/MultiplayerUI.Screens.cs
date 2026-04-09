namespace SR2MP.Components.UI;

internal sealed partial class MultiplayerUI
{
    private bool multiplayerUIHidden;

    private string usernameInput = "Player";
    private bool allowCheatsInput;

    private void FirstTimeScreen()
    {
        var valid = true;

        DrawText("Please select an username to play multiplayer.");

        DrawText("Username:", 2, 0);
        usernameInput = GUI.TextField(CalculateInputLayout(6, 2, 1), usernameInput);

        if (string.IsNullOrWhiteSpace(usernameInput))
        {
            DrawText("You must set an Username first.");
            valid = false;
        }

        if (!valid) return;
        if (!GUI.Button(CalculateButtonLayout(6), "Save settings")) return;

        firstTime = false;
        Main.SetConfigValue("internal_setup_ui", false);
        Main.SetConfigValue("username", usernameInput);
    }

    private void SettingsScreen()
    {
        DrawText("Username:", 2, 0);
        usernameInput = GUI.TextField(CalculateInputLayout(6, 2, 1), usernameInput);

        DrawText("Allow Cheats:", 2, 0);
        if (GUI.Button(CalculateButtonLayout(6, 2, 1), allowCheatsInput.ToStringYesOrNo()))
            allowCheatsInput = !allowCheatsInput;

        if (string.IsNullOrWhiteSpace(usernameInput))
        {
            DrawText("You must set an Username.");
            return;
        }

        if (!GUI.Button(CalculateButtonLayout(6), "Save")) return;

        Main.SetConfigValue("username", usernameInput);
        Main.SetConfigValue("allow_cheats", allowCheatsInput);
        viewingSettings = false;
    }

    private void MainMenuScreen()
    {
        if (GUI.Button(CalculateButtonLayout(6), "Settings"))
            viewingSettings = true;

        DrawText("You must be in a save to host or connect!");
        DrawText("Make sure you join an EMPTY save before connecting, this save file WILL BE RESET.");
    }

    private void InGameScreen()
    {
        if (GUI.Button(CalculateButtonLayout(6), "Settings"))
            viewingSettings = true;

        mainTab = DrawMainTabRow("Join", "Host", mainTab);

        if (mainTab == MainTab.Join)
            DrawJoinSection();
        else
            DrawHostSection();
    }

    private void UnimplementedScreen()
    {
        DrawText("This screen hasn't been implemented yet.");
    }
}