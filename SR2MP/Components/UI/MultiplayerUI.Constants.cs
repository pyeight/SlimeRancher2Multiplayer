namespace SR2MP.Components.UI;

internal sealed partial class MultiplayerUI
{
    private const float ButtonHeight = 25f;
    private const float InputHeight = 25f;
    private const float SpacerHeight = 7.5f;
    private const float HorizontalSpacing = 2f;
    private const float WindowWidth = 400f;
    private const float WindowHeight = 450f;

    private const float ChatHeaderHeight = 15f;
    private const float ChatWidth = 400;
    private const float ChatHeight = 325f;
    private const int MaxChatLines = 25;
    private const int MaxChatMessageLength = 1000;

    internal const byte SystemMessageNormal = 0;
    internal const byte SystemMessageConnect = 1;
    internal const byte SystemMessageDisconnect = 2;
    internal const byte SystemMessageClose = 3;

    private const string ColorSystemNormal = "#4D95CB";
    private const string ColorSystemConnect = "#00FF00";
    private const string ColorSystemDisconnect = "#FFA500";
    private const string ColorSystemClose = "#FF0000";
}