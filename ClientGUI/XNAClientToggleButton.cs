using System;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace ClientGUI;

/// <summary>
/// This is a combination of a checkbox and a standard button. You must specify the Checked and
/// Unchecked Textures to render for each button state.
/// </summary>
public class XNAClientToggleButton : XNAButton
{
    private bool _checked;

    private ToolTip _toolTip;

    private string _toolTipText;

    public XNAClientToggleButton(WindowManager windowManager)
                    : base(windowManager)
    {
    }

    public bool Checked
    {
        get => _checked;
        set
        {
            _checked = value;
            UpdateIdleTexture();
        }
    }

    public Texture2D CheckedTexture { get; set; }

    public Texture2D UncheckedTexture { get; set; }

    public override void Initialize()
    {
        if (CheckedTexture == null)
            throw new ArgumentNullException(nameof(CheckedTexture));

        if (UncheckedTexture == null)
            throw new ArgumentNullException(nameof(UncheckedTexture));

        UpdateIdleTexture();

        if (HoverSoundEffect == null)
            HoverSoundEffect = new EnhancedSoundEffect("button.wav");

        base.Initialize();

        _toolTip = new ToolTip(WindowManager, this);
        SetToolTipText(_toolTipText);

        if (Width == 0)
            Width = IdleTexture.Width;
    }

    public void SetToolTipText(string text)
    {
        _toolTipText = text ?? string.Empty;
        if (_toolTip != null)
            _toolTip.Text = _toolTipText;
    }

    private void UpdateIdleTexture()
    {
        IdleTexture = _checked ? CheckedTexture : UncheckedTexture;
    }
}