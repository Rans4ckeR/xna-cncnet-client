namespace DTAClient.DXGUI.Multiplayer.GameLobby;

public partial class PlayerLocationIndicator
{
    private sealed class PlayerText
    {
        public PlayerText(string text, bool textOnRight)
        {
            Text = text;
            TextOnRight = textOnRight;
        }

        public string Text { get; set; }

        public bool TextOnRight { get; set; }
    }
}