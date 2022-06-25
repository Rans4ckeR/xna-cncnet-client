using System;
using ClientCore;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace ClientGUI;

/// <summary>
/// A tool tip.
/// </summary>
public class ToolTip : XNAControl
{
    private readonly XNAControl masterControl;

    private TimeSpan cursorTime = TimeSpan.Zero;

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolTip" /> class. Creates a new tool tip and
    /// attaches it to the given control.
    /// </summary>
    /// <param name="windowManager">The window manager.</param>
    /// <param name="masterControl">The control to attach the tool tip to.</param>
    public ToolTip(WindowManager windowManager, XNAControl masterControl!!)
        : base(windowManager)
    {
        this.masterControl = masterControl;
        masterControl.MouseEnter += MasterControl_MouseEnter;
        masterControl.MouseLeave += MasterControl_MouseLeave;
        masterControl.MouseMove += MasterControl_MouseMove;
        masterControl.EnabledChanged += MasterControl_EnabledChanged;
        InputEnabled = false;
        DrawOrder = int.MaxValue;
        GetParentControl(masterControl.Parent).AddChild(this);
        Visible = false;
    }

    public override float Alpha { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether if set to true - makes tooltip not appear and
    /// instantly hides it if currently shown.
    /// </summary>
    public bool Blocked { get; set; }

    public bool IsMasterControlOnCursor { get; set; }

    public override string Text
    {
        get => base.Text;
        set
        {
            base.Text = value;
            Vector2 textSize = Renderer.GetTextDimensions(base.Text, ClientConfiguration.Instance.ToolTipFontIndex);
            Width = (int)textSize.X + (ClientConfiguration.Instance.ToolTipMargin * 2);
            Height = (int)textSize.Y + (ClientConfiguration.Instance.ToolTipMargin * 2);
        }
    }

    /// <summary>
    /// Sets the tool tip's location, checking that it doesn't exceed the window's bounds.
    /// </summary>
    /// <param name="location">The point at location coordinates.</param>
    public void DisplayAtLocation(Point location)
    {
        X = location.X + Width > WindowManager.RenderResolutionX ?
            WindowManager.RenderResolutionX - Width : location.X;
        Y = location.Y - Height < 0 ? 0 : location.Y - Height;
    }

    public override void Draw(GameTime gameTime)
    {
        Renderer.FillRectangle(
            ClientRectangle,
            UISettings.ActiveSettings.BackgroundColor * Alpha);
        Renderer.DrawRectangle(
            ClientRectangle,
            UISettings.ActiveSettings.AltColor * Alpha);
        Renderer.DrawString(
            Text,
            ClientConfiguration.Instance.ToolTipFontIndex,
            new Vector2(X + ClientConfiguration.Instance.ToolTipMargin, Y + ClientConfiguration.Instance.ToolTipMargin),
            UISettings.ActiveSettings.AltColor * Alpha,
            1.0f);
    }

    public override void Update(GameTime gameTime)
    {
        if (Blocked)
        {
            Alpha = 0f;
            Visible = false;
            return;
        }

        if (IsMasterControlOnCursor)
        {
            cursorTime += gameTime.ElapsedGameTime;

            if (cursorTime > TimeSpan.FromSeconds(ClientConfiguration.Instance.ToolTipDelay))
            {
                Alpha += ClientConfiguration.Instance.ToolTipAlphaRatePerSecond * (float)gameTime.ElapsedGameTime.TotalSeconds;
                Visible = true;
                if (Alpha > 1.0f)
                    Alpha = 1.0f;
                return;
            }
        }

        Alpha -= ClientConfiguration.Instance.ToolTipAlphaRatePerSecond * (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (Alpha < 0f)
        {
            Alpha = 0f;
            Visible = false;
        }
    }

    private static Point SumPoints(Point p1, Point p2)
#if XNA // This is also needed for XNA compatibility
        => new(p1.X + p2.X, p1.Y + p2.Y);
#else
        => p1 + p2;
#endif

    private XNAControl GetParentControl(XNAControl parent)
    {
        if (parent is XNAWindow)
            return parent as XNAWindow;
        else if (parent is INItializableWindow)
            return parent as INItializableWindow;
        else if (parent.Parent != null)
            return GetParentControl(parent.Parent);
        else
            return parent;
    }

    private void MasterControl_EnabledChanged(object sender, EventArgs e)
        => Enabled = masterControl.Enabled;

    private void MasterControl_MouseEnter(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(Text))
            return;

        DisplayAtLocation(ToolTip.SumPoints(
            WindowManager.Cursor.Location,
            new Point(ClientConfiguration.Instance.ToolTipOffsetX, ClientConfiguration.Instance.ToolTipOffsetY)));
        IsMasterControlOnCursor = true;
    }

    private void MasterControl_MouseLeave(object sender, EventArgs e)
    {
        IsMasterControlOnCursor = false;
        cursorTime = TimeSpan.Zero;
    }

    private void MasterControl_MouseMove(object sender, EventArgs e)
    {
        if (!Visible && !string.IsNullOrEmpty(Text))
        {
            // Move the tooltip if the cursor has moved while staying on the control area and we're invisible
            DisplayAtLocation(ToolTip.SumPoints(
                WindowManager.Cursor.Location,
                new Point(ClientConfiguration.Instance.ToolTipOffsetX, ClientConfiguration.Instance.ToolTipOffsetY)));
        }
    }
}