using System.Diagnostics;
using ClientCore;
using ClientGUI;
using Localization;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Generic;

/// <summary>
/// A notification that asks the user to accept the CnCNet privacy policy.
/// </summary>
internal class PrivacyNotification : XNAWindow
{
    public PrivacyNotification(WindowManager windowManager)
        : base(windowManager)
    {
        // DrawMode = ControlDrawMode.UNIQUE_RENDER_TARGET;
    }

    public override void Initialize()
    {
        Name = nameof(PrivacyNotification);
        Width = WindowManager.RenderResolutionX;

        XNALabel lblDescription = new(WindowManager);
        lblDescription.Name = nameof(lblDescription);
        lblDescription.X = UIDesignConstants.EMPTYSPACESIDES;
        lblDescription.Y = UIDesignConstants.EMPTYSPACETOP;
        lblDescription.Text = Renderer.FixText(
            "By using the client you agree to the CnCNet Terms & Conditions as well as the CnCNet Privacy Policy. Privacy-related options can be configured in the client options.".L10N("UI:Main:TOSText"),
            lblDescription.FontIndex, WindowManager.RenderResolutionX - (UIDesignConstants.EMPTYSPACESIDES * 2)).Text;
        AddChild(lblDescription);

        XNALabel lblMoreInformation = new(WindowManager);
        lblMoreInformation.Name = nameof(lblMoreInformation);
        lblMoreInformation.X = lblDescription.X;
        lblMoreInformation.Y = lblDescription.Bottom + UIDesignConstants.CONTROLVERTICALMARGIN;
        lblMoreInformation.Text = "More information:".L10N("UI:Main:TOSMoreInfo") + " ";
        AddChild(lblMoreInformation);

        XNALinkLabel lblTermsAndConditions = new(WindowManager);
        lblTermsAndConditions.Name = nameof(lblTermsAndConditions);
        lblTermsAndConditions.X = lblMoreInformation.Right + UIDesignConstants.CONTROLHORIZONTALMARGIN;
        lblTermsAndConditions.Y = lblMoreInformation.Y;
        lblTermsAndConditions.Text = "https://cncnet.org/terms-and-conditions";
        lblTermsAndConditions.LeftClick += (s, e) =>
        {
            using Process _ = Process.Start(new ProcessStartInfo
            {
                FileName = lblTermsAndConditions.Text,
                UseShellExecute = true
            });
        };
        AddChild(lblTermsAndConditions);

        XNALinkLabel lblPrivacyPolicy = new(WindowManager);
        lblPrivacyPolicy.Name = nameof(lblPrivacyPolicy);
        lblPrivacyPolicy.X = lblTermsAndConditions.Right + UIDesignConstants.CONTROLHORIZONTALMARGIN;
        lblPrivacyPolicy.Y = lblMoreInformation.Y;
        lblPrivacyPolicy.Text = "https://cncnet.org/privacy-policy";
        lblPrivacyPolicy.LeftClick += (s, e) =>
        {
            using Process _ = Process.Start(new ProcessStartInfo
            {
                FileName = lblPrivacyPolicy.Text,
                UseShellExecute = true
            });
        };
        AddChild(lblPrivacyPolicy);

        XNALabel lblExplanation = new(WindowManager);
        lblExplanation.Name = nameof(lblExplanation);
        lblExplanation.X = UIDesignConstants.EMPTYSPACESIDES;
        lblExplanation.Y = lblMoreInformation.Bottom + (UIDesignConstants.CONTROLVERTICALMARGIN * 2);
        lblExplanation.Text = "No worries, we're not actually using your data for anything evil, but we have to display this message due to regulations.".L10N("UI:Main:TOSExplanation");
        lblExplanation.TextColor = UISettings.ActiveSettings.SubtleTextColor;
        AddChild(lblExplanation);

        XNAClientButton btnOK = new(WindowManager);
        btnOK.Name = nameof(btnOK);
        btnOK.Width = 75;
        btnOK.Y = lblExplanation.Y;
        btnOK.X = WindowManager.RenderResolutionX - btnOK.Width - UIDesignConstants.CONTROLHORIZONTALMARGIN;
        btnOK.Text = "Got it".L10N("UI:Main:TOSButtonOK");
        AddChild(btnOK);
        btnOK.LeftClick += (s, e) =>
        {
            UserINISettings.Instance.PrivacyPolicyAccepted.Value = true;
            UserINISettings.Instance.SaveSettings();

            // AlphaRate = -0.2f;
            Disable();
        };

        Height = btnOK.Bottom + UIDesignConstants.EMPTYSPACEBOTTOM;
        Y = WindowManager.RenderResolutionY - Height;

        base.Initialize();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (Alpha <= 0.0)
            Disable();
    }
}