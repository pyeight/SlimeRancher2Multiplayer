namespace SR2MP.Components.UI;

public sealed partial class MultiplayerUI
{
    private Rect previousLayoutRect;
    private Rect previousLayoutChatRect;
    private int previousLayoutHorizontalIndex;

    private (int lines, float height) DetectHeight(string text)
    {
        var style = GUI.skin.label;

        var height = style.CalcHeight(new GUIContent(text), ChatWidth);
        return (Mathf.CeilToInt(height / style.lineHeight), height);
    }
    
    private Rect CalculateChatTextLayout(float originalX, string text)
    {
        var maxWidth = ChatWidth;
        var textHeight = DetectHeight(text);
        float x = originalX + HorizontalSpacing;
        float y = previousLayoutChatRect.y;
        float w = maxWidth;
        float h = textHeight.height;// * textHeight.lines;
        
        y += previousLayoutChatRect.height;
        
        var result = new Rect(x, y, w, h);

        previousLayoutChatRect = result;

        return result;
    }

    private void DrawText(string text, int horizontalShare = 1, int horizontalIndex = 0)
    {
        GUI.Label(CalculateTextLayout(6, text, horizontalShare, horizontalIndex), text);
    }
    
    private Rect CalculateTextLayout(float originalX, string text, int horizontalShare = 1, int horizontalIndex = 0)
    {
        var maxWidth = WindowWidth - (HorizontalSpacing * 2);
        var textHeight = DetectHeight(text);
        float x = originalX + HorizontalSpacing;
        float y = previousLayoutRect.y;
        float w = (maxWidth / horizontalShare);
        float h = textHeight.height * textHeight.lines;

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