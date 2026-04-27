namespace SR2MP.Components.UI;

internal sealed partial class MultiplayerUI
{
    private bool multiplayerUIHidden;

    private string usernameInput = "Player";
    private bool allowCheatsInput;

    private string activeInputId = string.Empty;
    private bool suppressNextChar;

    private GUIStyle boxStyle = new() { focused = { textColor = new Color32(205, 255, 205, 255) } };
    
    private string DrawSafeTextInput(string id, Rect rect, string value, int maxLength = 64, bool numbersOnly = false)
    {
        var current = Event.current;
        var displayValue = string.IsNullOrEmpty(value) && activeInputId != id ? "Click to type" : value;
        
        GUI.Box(rect, displayValue, boxStyle);

        if (current.type == EventType.MouseDown)
        {
            if (rect.Contains(current.mousePosition))
            {
                activeInputId = id;
                suppressNextChar = true;
                current.Use();
            }
            else if (activeInputId == id)
            {
                activeInputId = string.Empty;
            }
        }

        if (activeInputId != id)
            return value;

        if (current.type == EventType.KeyDown)
        {
            switch (current.keyCode)
            {
                case KeyCode.V:
                    if (current.control)
                        value += GUIUtility.systemCopyBuffer;
                    return value;
                case KeyCode.C:
                    if (current.control)
                        GUIUtility.systemCopyBuffer = value;
                    return value;
                case KeyCode.X:
                    if (current.control)
                        GUIUtility.systemCopyBuffer = value;
                    return "";
                
                case KeyCode.Backspace:
                    if (!string.IsNullOrEmpty(value))
                        value = value[..^1];
                    current.Use();
                    return value;

                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                case KeyCode.Escape:
                    activeInputId = string.Empty;
                    current.Use();
                    return value;
            }

            if (suppressNextChar)
            {
                suppressNextChar = false;
                current.Use();
                return value;
            }


            if (current.character != '\0' && !char.IsControl(current.character))
            {
                if (!numbersOnly || char.IsDigit(current.character))
                {
                    if (value.Length < maxLength)
                        value += current.character;
                }

                current.Use();
            }
        }

        return value;
    }

    private void FirstTimeScreen()
    {
        var valid = true;

        DrawText("Please select an username to play multiplayer.");

        DrawText("Username:", 2);
        usernameInput = DrawSafeTextInput("username", CalculateInputLayout(6, 2, 1), usernameInput, 32);
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
        DrawText("Username:", 2);
        usernameInput = GUI.TextField(CalculateInputLayout(6, 2, 1), usernameInput);

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