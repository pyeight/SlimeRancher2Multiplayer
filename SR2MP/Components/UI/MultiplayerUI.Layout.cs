using Il2CppInterop.Runtime.Attributes;

namespace SR2MP.Components.UI;

internal sealed partial class MultiplayerUI
{
    private Rect previousLayoutRect;
    private Rect previousLayoutChatRect;
    private int previousLayoutHorizontalIndex;

    private void DrawText(string text, int horizontalShare = 1, int horizontalIndex = 0)
    {
        GUI.Label(CalculateTextLayout(6, text, horizontalShare, horizontalIndex), text);
    }
    
    private string activeInputId = string.Empty;
    private bool justUnfocusedInput;
    private bool suppressNextChar;

    private string DrawSafeTextInput(string id, Rect rect, string value, int maxLength = 64, bool numbersOnly = false, bool isChat = false)
    {
        var current = Event.current;
        var displayValue = string.IsNullOrEmpty(value) && activeInputId != id
            ? (isChat ? "Enter to Chat" : "Click to Type")
            : value;

        if (activeInputId == id)
            GUI.skin.box.normal.textColor = new Color32(255, 255, 185, 255);

        GUI.Box(rect, displayValue);

        GUI.skin.box.normal.textColor = Color.white;

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
                case KeyCode.Backspace:
                    if (!string.IsNullOrEmpty(value))
                        value = value[..^1];
                    current.Use();
                    return value;
                
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    if (!isChat)
                    {
                        activeInputId = string.Empty;
                        justUnfocusedInput = true;
                        current.Use();
                    }
                    return value;

                case KeyCode.Escape:
                    activeInputId = string.Empty;
                    current.Use();
                    return value;

                case KeyCode.X:
                    if (current.control)
                    {
                        GUIUtility.systemCopyBuffer = value;
                        return "";
                    }

                    break;

                case KeyCode.V:
                    if (current.control)
                    {
                        value += GUIUtility.systemCopyBuffer;
                        return value;
                    }

                    break;

                case KeyCode.C:
                    if (current.control)
                    {
                        GUIUtility.systemCopyBuffer = value;
                        return value;
                    }

                    break;
            }

            if (suppressNextChar)
            {
                suppressNextChar = false;
                current.Use();
                return value;
            }

            if (current.character == '\0' || char.IsControl(current.character))
                return value;

            if ((!numbersOnly || char.IsDigit(current.character)) && value.Length < maxLength)
            {
                value += current.character;
            }

            current.Use();
        }

        return value;
    }

    private Rect CalculateTextLayout(float originalX, string text, int horizontalShare = 1, int horizontalIndex = 0)
    {
        const float maxWidth = WindowWidth - (HorizontalSpacing * 2);
        var style = GUI.skin.label;
        var height = style.CalcHeight(new GUIContent(text), maxWidth / horizontalShare);

        var x = originalX + HorizontalSpacing;
        var y = previousLayoutRect.y;
        var w = maxWidth / horizontalShare;
        var h = height;

        x += horizontalIndex * w;

        if (horizontalIndex <= previousLayoutHorizontalIndex)
            y += previousLayoutRect.height + SpacerHeight;

        var result = new Rect(x, y, w, h);

        previousLayoutHorizontalIndex = horizontalIndex;
        previousLayoutRect = result;

        return result;
    }

    private Rect CalculateInputLayout(float originalX, int horizontalShare = 1, int horizontalIndex = 0)
    {
        const float maxWidth = WindowWidth - (HorizontalSpacing * 2);

        var x = originalX + HorizontalSpacing;
        var y = previousLayoutRect.y;
        var w = maxWidth / horizontalShare;
        const float h = InputHeight;

        x += horizontalIndex * w;

        if (horizontalIndex <= previousLayoutHorizontalIndex)
            y += previousLayoutRect.height + SpacerHeight;

        var result = new Rect(x, y, w, h);

        previousLayoutHorizontalIndex = horizontalIndex;
        previousLayoutRect = result;

        return result;
    }

    private Rect CalculateButtonLayout(float originalX, int horizontalShare = 1, int horizontalIndex = 0)
    {
        const float maxWidth = WindowWidth - (HorizontalSpacing * 2);

        var x = originalX + HorizontalSpacing;
        var y = previousLayoutRect.y;
        var w = maxWidth / horizontalShare;
        const float h = ButtonHeight;

        x += horizontalIndex * w;

        if (horizontalIndex <= previousLayoutHorizontalIndex)
            y += previousLayoutRect.height + SpacerHeight;

        var result = new Rect(x, y, w, h);

        previousLayoutHorizontalIndex = horizontalIndex;
        previousLayoutRect = result;

        return result;
    }

    [HideFromIl2Cpp]
    private void DrawTabRow(ref byte selected, params string[] labels)
    {
        for (byte i = 0; i < labels.Length; i++)
            if (GUI.Toggle(CalculateButtonLayout(6, labels.Length, i), selected == i, labels[i], GUI.skin.button))
                selected = i;
    }
}