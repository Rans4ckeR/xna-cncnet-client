﻿using System;
using System.Collections.Generic;
using System.Linq;
using ClientCore;
using DTAClient.Domain.Multiplayer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using PlayerInfo = DTAClient.Domain.Multiplayer.PlayerInfo;

namespace DTAClient.DXGUI.Multiplayer.GameLobby;

/// <summary>
/// A player location indicator for the map preview.
/// </summary>
public partial class PlayerLocationIndicator : XNAControl
{
    private const float TEXTURE_SCALE = 0.25f;

    private readonly double backgroundAlphaRate = 0.1;

    private readonly List<MultiplayerColor> mpColors;

    private readonly List<PlayerText> pText = new();

    private readonly string[] teamIds = new[] { string.Empty }
        .Concat(ProgramConstants.TEAMS.Select(team => $"[{team}]")).ToArray();

    private double angle;

    private double backgroundAlpha = 0.0;

    private Texture2D baseTexture;

    private Texture2D hoverTexture;

    private bool isHoveredOn = false;

    private int lineHeight;

    private Color nameBackgroundColor;

    private Color nameBorderColor;

    private Vector2 textSize;

    private int textXPosition;

    private Texture2D usedTexture;

    public PlayerLocationIndicator(
        WindowManager windowManager,
        List<MultiplayerColor> mpColors,
        Color nameBackgroundColor,
        Color nameBorderColor,
        XNAContextMenu contextMenu)
        : base(windowManager)
    {
        this.mpColors = mpColors;
        this.nameBackgroundColor = nameBackgroundColor;
        this.nameBorderColor = nameBorderColor;
        ContextMenu = contextMenu;
        HoverRemapColor = Color.White;
    }

    public double AngularVelocity { get; set; } = 0.015;

    public bool BackgroundShown { get; set; }

    public int FontIndex { get; set; }

    public Color HoverRemapColor { get; set; }

    public List<PlayerInfo> Players { get; set; } = new();

    public double ReversedAngularVelocity { get; set; } = -0.0075;

    public Texture2D WaypointTexture { get; set; }

    private XNAContextMenu ContextMenu { get; set; }

    public override void Draw(GameTime gameTime)
    {
        Point p = GetWindowPoint();
        Rectangle displayRectangle = new(p.X, p.Y, Width, Height);

        int y = displayRectangle.Y + (((int)(baseTexture.Height * TEXTURE_SCALE) - lineHeight) / 2);

        int i = 0;
        foreach (PlayerInfo pInfo in Players)
        {
            Color textColor = Color.White;
            if (pInfo.ColorId > 0)
                textColor = mpColors[pInfo.ColorId - 1].XnaColor;

            if (backgroundAlpha > 0.0)
            {
                int rectangleWidth = 0;
                int rectangleCoordX = 0;
                if (pText[i].TextOnRight)
                {
                    rectangleCoordX = displayRectangle.Center.X;
                    rectangleWidth = (int)textSize.X + textXPosition + (displayRectangle.Width / 2) + 5;
                }
                else
                {
                    rectangleWidth = (int)textSize.X + (displayRectangle.Width / 2) + 5;
                    rectangleCoordX = displayRectangle.Center.X - rectangleWidth;
                }

                Renderer.FillRectangle(
                    new Rectangle(rectangleCoordX, y, rectangleWidth, lineHeight),
                    new Color(
                        nameBackgroundColor.R,
                        nameBackgroundColor.G,
                        nameBackgroundColor.B,
                        (int)(nameBackgroundColor.A * backgroundAlpha)));

                Renderer.DrawRectangle(
                    new Rectangle(rectangleCoordX, y, rectangleWidth, lineHeight),
                    new Color(nameBorderColor.R, nameBorderColor.G, nameBorderColor.B, (int)(nameBorderColor.A * backgroundAlpha)));
            }

            Renderer.DrawStringWithShadow(
                pText[i].Text,
                FontIndex,
                new Vector2(
                    displayRectangle.Right + textXPosition,
                    y),
                textColor);

            y += lineHeight;
            i++;
        }

        Vector2 origin = new(usedTexture.Width / 2, usedTexture.Height / 2);

        Renderer.DrawTexture(
            usedTexture,
            new Vector2(displayRectangle.Center.X + 1.5f, displayRectangle.Center.Y + 1f),
            (float)angle,
            origin,
            new Vector2(TEXTURE_SCALE),
            Color.Black);

        Color remapColor = Color.White;
        Color hoverRemapColor = HoverRemapColor;
        if (Players.Count == 1 && Players[0].ColorId > 0)
        {
            remapColor = mpColors[Players[0].ColorId - 1].XnaColor;
            hoverRemapColor = remapColor;
        }

        if (isHoveredOn ||
            (ContextMenu.Tag == Tag && ContextMenu.Visible))
        {
            Renderer.DrawTexture(
                usedTexture,
                new Vector2(displayRectangle.Center.X + 0.5f, displayRectangle.Center.Y),
                (float)angle,
                origin,
                new Vector2(TEXTURE_SCALE + 0.1f),
                hoverRemapColor);
        }

        Renderer.DrawTexture(
            usedTexture,
            new Vector2(displayRectangle.Center.X + 0.5f, displayRectangle.Center.Y),
            (float)angle,
            origin,
            new Vector2(TEXTURE_SCALE),
            remapColor);

        if (WaypointTexture != null)
        {
            // Non-premultiplied blending makes the indicators look sharper for some reason TODO
            // figure out why
            Renderer.PushSettings(new SpriteBatchSettings(SpriteSortMode.Deferred, BlendState.NonPremultiplied, null));

            Renderer.DrawTexture(
                WaypointTexture,
                new Vector2(displayRectangle.Center.X + 0.5f, displayRectangle.Center.Y),
                0f,
                new Vector2(WaypointTexture.Width / 2, WaypointTexture.Height / 2),
                new Vector2(1f, 1f),
                Color.White);

            Renderer.PopSettings();
        }

        base.Draw(gameTime);
    }

    public override void Initialize()
    {
        base.Initialize();

        baseTexture = AssetLoader.LoadTexture("slocindicator.png");
        hoverTexture = AssetLoader.LoadTexture("slocindicatorh.png");
        ClientRectangle = baseTexture.Bounds;
        lineHeight = (int)Renderer.GetTextDimensions("@", FontIndex).Y + 1;

        usedTexture = baseTexture;
    }

    public override void OnMouseEnter()
    {
        //usedTexture = hoverTexture;
        isHoveredOn = true;

        base.OnMouseEnter();
    }

    public override void OnMouseLeave()
    {
        //usedTexture = baseTexture;
        isHoveredOn = false;

        base.OnMouseLeave();
    }

    public void Refresh()
    {
        textSize = Vector2.Zero;
        pText.Clear();

        foreach (PlayerInfo pInfo in Players)
        {
            string text = pInfo.Name;
            if (pInfo.TeamId > 0)
                text = teamIds[pInfo.TeamId] + " " + pInfo.Name;

            if (text == null)
                return;

            Vector2 pInfoSize = Renderer.GetTextDimensions(text, FontIndex);

            if (pInfoSize.X > textSize.X)
                textSize = new Vector2(pInfoSize.X, Players.Count * (pInfoSize.Y + 1));

            textXPosition = 3;

            bool textOnRight = true;

            if (Right + textXPosition + (int)textSize.X > Parent.Width)
            {
                textXPosition = -(int)textSize.X - 3 - (int)(baseTexture.Width * TEXTURE_SCALE);
                text = pInfo.TeamId > 0 ? pInfo.Name + " " + teamIds[pInfo.TeamId] : pInfo.Name;
                textOnRight = false;
            }

            pText.Add(new PlayerText(text, textOnRight));
        }
    }

    public void SetPosition(Point p)
    {
        int width = (int)(baseTexture.Width * TEXTURE_SCALE);
        int height = (int)(baseTexture.Height * TEXTURE_SCALE);

        ClientRectangle = new Rectangle(
            p.X - (width / 2),
            p.Y - (height / 2),
            width,
            height);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        double frameTimeCoefficient = gameTime.ElapsedGameTime.TotalMilliseconds / 10.0;

        angle += Players.Count > 0 ? ReversedAngularVelocity * frameTimeCoefficient : AngularVelocity * frameTimeCoefficient;

        usedTexture = Players.Count > 0 ? hoverTexture : baseTexture;

        backgroundAlpha = BackgroundShown ? Math.Min(backgroundAlpha + backgroundAlphaRate, 1.0) : Math.Max(backgroundAlpha - backgroundAlphaRate, 0.0);
    }

    protected override void OnVisibleChanged(object sender, EventArgs args)
    {
        base.OnVisibleChanged(sender, args);

        backgroundAlpha = 0.0;
    }
}