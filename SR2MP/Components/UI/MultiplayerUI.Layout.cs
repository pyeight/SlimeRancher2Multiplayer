namespace SR2MP.Components.UI;

public sealed partial class MultiplayerUI
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
}