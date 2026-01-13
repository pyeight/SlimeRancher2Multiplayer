namespace SR2MP.Components.UI;

public sealed partial class MultiplayerUI
{
    private Rect previousLayoutRect;
    private Rect previousLayoutChatRect;
    private int previousLayoutHorizontalIndex;

    private int DetectLineCount(string text)
    {
        var style = GUI.skin.label;

        var height = style.CalcHeight(new GUIContent(text), ChatWidth);
        return Mathf.CeilToInt(height / style.lineHeight) - 1;
    }
    
    private Rect CalculateChatTextLayout(float originalX, int lines)
    {
        var maxWidth = ChatWidth;

        float x = originalX + HorizontalSpacing;
        float y = previousLayoutChatRect.y;
        float w = maxWidth;
        float h = TextHeight * lines;
        
        y += previousLayoutRect.height;
        
        var result = new Rect(x, y, w, h);

        previousLayoutChatRect = result;

        return result;
    }

    private Rect CalculateTextLayout(float originalX, int lines = 1, int horizontalShare = 1, int horizontalIndex = 0)
    {
        var maxWidth = WindowWidth - (HorizontalSpacing * 2);

        float x = originalX + HorizontalSpacing;
        float y = previousLayoutRect.y;
        float w = (maxWidth / horizontalShare);
        float h = TextHeight * lines;

        //if (horizontalShare != 1)
        //    w -= HorizontalSpacing * horizontalShare;

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
        var maxWidth = WindowWidth - (HorizontalSpacing * 2);

        float x = originalX + HorizontalSpacing;
        float y = previousLayoutRect.y;
        float w = (maxWidth / horizontalShare);
        float h = InputHeight;

        //if (horizontalShare != 1)
        //    w -= HorizontalSpacing * horizontalShare;

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
        var maxWidth = WindowWidth - (HorizontalSpacing * 2);

        float x = originalX + HorizontalSpacing;
        float y = previousLayoutRect.y;
        float w = (maxWidth / horizontalShare);
        float h = ButtonHeight;

        //if (horizontalShare != 1)
        //    w -= HorizontalSpacing * horizontalShare;

        x += horizontalIndex * w;
        if (horizontalIndex <= previousLayoutHorizontalIndex)
            y += previousLayoutRect.height + SpacerHeight;

        var result = new Rect(x, y, w, h);

        previousLayoutHorizontalIndex = horizontalIndex;
        previousLayoutRect = result;

        return result;
    }
}