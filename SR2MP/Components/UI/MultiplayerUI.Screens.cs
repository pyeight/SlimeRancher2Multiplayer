using System;
using UnityEngine;

namespace SR2MP.Components.UI;

internal sealed partial class MultiplayerUI
{
    private bool multiplayerUIHidden;

    private string usernameInput = "Player";
    private bool allowCheatsInput;

    private string activeInputId = string.Empty;
    private bool suppressNextChar;

    private string DrawSafeTextInput(string id, Rect rect, string value, int maxLength = 64, bool numbersOnly = false)
    {
        var e = Event.current;
        bool focused = activeInputId == id;
        string displayValue = string.IsNullOrEmpty(value) && !focused ? "Click to type..." : value;

        GUI.Box(rect, focused ? $"> {displayValue}_" : displayValue);

        if (e.type == EventType.MouseDown)
        {
            if (rect.Contains(e.mousePosition))
            {
                activeInputId = id;
                suppressNextChar = true;
                e.Use();
            }
            else if (focused)
            {
                activeInputId = string.Empty;
            }
        }

        if (!focused)
            return value;

        if (e.type == EventType.KeyDown)
        {
            switch (e.keyCode)
            {
                case KeyCode.Backspace:
                    if (!string.IsNullOrEmpty(value))
                        value = value[..^1];
                    e.Use();
                    return value;

                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                case KeyCode.Escape:
                    activeInputId = string.Empty;
                    e.Use();
                    return value;
            }

            if (suppressNextChar)
            {
                suppressNextChar = false;
                e.Use();
                return value;
            }

            char c = e.character;

            if (c != '\0' && !char.IsControl(c))
            {
                if (!numbersOnly || char.IsDigit(c))
                {
                    if (value.Length < maxLength)
                        value += c;
                }

                e.Use();
            }
        }

        return value;
    }

    private void FirstTimeScreen()
    {
        var valid = true;

        DrawText("Please select an username to play multiplayer.");

        DrawText("Username:", 2, 0);
        usernameInput = DrawSafeTextInput(
            "first_username",
            CalculateInputLayout(6, 2, 1),
            usernameInput,
            24
        );

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
        bool validUsername = true;

        DrawText("Username:", 2, 0);
        usernameInput = DrawSafeTextInput(
            "settings_username",
            CalculateInputLayout(6, 2, 1),
            usernameInput,
            24
        );

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