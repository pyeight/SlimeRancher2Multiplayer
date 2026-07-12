namespace SR2MP.Components.UI;

internal sealed partial class MultiplayerUI
{
    private bool multiplayerUIHidden;

    private string usernameInput = "Player";
    private string usernameColorInput = "FFFFFF";
    private bool allowCheatsInput;

    private void FirstTimeScreen()
    {
        var valid = true;

        DrawText("Please select an username to play multiplayer.");

        DrawText("Username:", 2);
        usernameInput = DrawSafeTextInput("username", CalculateInputLayout(6, 2, 1), usernameInput, 32);

        DrawUsernameColorInput();

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
        Main.SetConfigValue("username_color", usernameColorInput);
    }

    private void SettingsScreen()
    {
        DrawText("Username:", 2);
        usernameInput = DrawSafeTextInput("username", CalculateInputLayout(6, 2, 1), usernameInput, 32);

        DrawUsernameColorInput();

        DrawText("Allow Cheats:", 2);
        if (GUI.Button(CalculateButtonLayout(6, 2, 1), allowCheatsInput.ToStringYesOrNo()))
            allowCheatsInput = !allowCheatsInput;

        if (string.IsNullOrWhiteSpace(usernameInput))
        {
            DrawText("You must set an Username.");
            return;
        }

        if (!GUI.Button(CalculateButtonLayout(6), "Save")) return;

        Main.SetConfigValue("username", usernameInput);
        Main.SetConfigValue("username_color", usernameColorInput);
        Main.SetConfigValue("allow_cheats", allowCheatsInput);
        viewingSettings = false;
    }

    private void DrawUsernameColorInput()
    {
        DrawText("Username Color (hex):", 3);
        usernameColorInput = DrawSafeTextInput("username_color", CalculateInputLayout(6, 3, 1), usernameColorInput, 6);

        if (GUI.Button(CalculateButtonLayout(6, 3, 2), "Randomize"))
            usernameColorInput = Extensions.RandomHexColor();

        var preview = Extensions.IsValidHexColor(usernameColorInput)
            ? $"<color=#{usernameColorInput}>{(string.IsNullOrWhiteSpace(usernameInput) ? "Player" : usernameInput)}</color>"
            : "Enter a 6-digit hex color, e.g. FF8800";

        DrawText(preview);
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

        DrawTabRow(ref mainTab, "Join", "Host");

        if (mainTab == 0)
            DrawJoinSection();
        else
            DrawHostSection();
    }

    private void UnimplementedScreen()
    {
        DrawText("This screen hasn't been implemented yet.");
    }
}