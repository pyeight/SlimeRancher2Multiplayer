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

    private MainTab DrawMainTabRow(string leftLabel, string rightLabel, MainTab tab)
    {
        if (GUI.Toggle(CalculateButtonLayout(6, 2), tab == MainTab.Join, leftLabel, GUI.skin.button))
            tab = MainTab.Join;
        if (GUI.Toggle(CalculateButtonLayout(6, 2, 1), tab == MainTab.Host, rightLabel, GUI.skin.button))
            tab = MainTab.Host;
        return tab;
    }

    private JoinTab DrawJoinTabRow(string leftLabel, string rightLabel, JoinTab tab)
    {
        if (GUI.Toggle(CalculateButtonLayout(6, 2), tab == JoinTab.Code, leftLabel, GUI.skin.button))
            tab = JoinTab.Code;
        if (GUI.Toggle(CalculateButtonLayout(6, 2, 1), tab == JoinTab.Manual, rightLabel, GUI.skin.button))
            tab = JoinTab.Manual;
        return tab;
    }

    private HostTab DrawHostTabRow(string leftLabel, string middleLabel, string rightLabel, HostTab tab)
    {
        if (GUI.Toggle(CalculateButtonLayout(6, 3), tab == HostTab.Automatic, leftLabel, GUI.skin.button))
            tab = HostTab.Automatic;
        if (GUI.Toggle(CalculateButtonLayout(6, 3, 1), tab == HostTab.ManualCode, middleLabel, GUI.skin.button))
            tab = HostTab.ManualCode;
        if (GUI.Toggle(CalculateButtonLayout(6, 3, 2), tab == HostTab.ManualSimple, rightLabel, GUI.skin.button))
            tab = HostTab.ManualSimple;
        return tab;
    }
}