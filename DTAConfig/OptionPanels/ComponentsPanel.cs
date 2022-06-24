﻿using System;
using System.Collections.Generic;
using System.IO;
using ClientCore;
using ClientGUI;
using ClientUpdater;
using Localization;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAConfig.OptionPanels;

internal class ComponentsPanel : XNAOptionsPanel
{
    private readonly List<XNAClientButton> installationButtons = new();

    public ComponentsPanel(WindowManager windowManager, UserINISettings iniSettings)
        : base(windowManager, iniSettings)
    {
    }
    private bool downloadCancelled = false;

    public override void Initialize()
    {
        base.Initialize();

        Name = "ComponentsPanel";

        int componentIndex = 0;

        if (Updater.CustomComponents == null)
            return;

        foreach (CustomComponent c in Updater.CustomComponents)
        {
            string buttonText = "Not Available".L10N("UI:DTAConfig:NotAvailable");

            if (File.Exists(ProgramConstants.GamePath + c.LocalPath))
            {
                buttonText = "Uninstall".L10N("UI:DTAConfig:ButtonUninstall");

                if (c.LocalIdentifier != c.RemoteIdentifier)
                    buttonText = "Update".L10N("UI:DTAConfig:ButtonUpdate");
            }
            else
            {
                if (!string.IsNullOrEmpty(c.RemoteIdentifier))
                {
                    buttonText = "Install".L10N("UI:DTAConfig:ButtonInstall");
                }
            }

            XNAClientButton btn = new(WindowManager)
            {
                Name = "btn" + c.ININame,
                ClientRectangle = new Rectangle(
                    Width - 145,
                12 + (componentIndex * 35), UIDesignConstants.BUTTONWIDTH133, UIDesignConstants.BUTTONHEIGHT),
                Text = buttonText,
                Tag = c
            };
            btn.LeftClick += Btn_LeftClick;

            XNALabel lbl = new(WindowManager)
            {
                Name = "lbl" + c.ININame,
                ClientRectangle = new Rectangle(12, btn.Y + 2, 0, 0),
                Text = c.GUIName
            };

            AddChild(btn);
            AddChild(lbl);

            installationButtons.Add(btn);

            componentIndex++;
        }

        Updater.FileIdentifiersUpdated += Updater_FileIdentifiersUpdated;
    }

    public override void Load()
    {
        base.Load();

        UpdateInstallationButtons();
    }

    public void InstallComponent(int id)
    {
        XNAClientButton btn = installationButtons[id];
        btn.AllowClick = false;

        CustomComponent cc = (CustomComponent)btn.Tag;

        cc.DownloadFinished += cc_DownloadFinished;
        cc.DownloadProgressChanged += cc_DownloadProgressChanged;
        cc.DownloadComponent();
    }

    public void CancelAllDownloads()
    {
        Logger.Log("Cancelling all custom component downloads.");

        downloadCancelled = true;

        if (Updater.CustomComponents == null)
            return;

        foreach (CustomComponent cc in Updater.CustomComponents)
        {
            if (cc.IsBeingDownloaded)
                cc.StopDownload();
        }
    }

    private void Updater_FileIdentifiersUpdated()
        => UpdateInstallationButtons();

    private void UpdateInstallationButtons()
    {
        if (Updater.CustomComponents == null)
            return;

        int componentIndex = 0;

        foreach (CustomComponent c in Updater.CustomComponents)
        {
            if (!c.Initialized || c.IsBeingDownloaded)
            {
                installationButtons[componentIndex].AllowClick = false;
                componentIndex++;
                continue;
            }

            string buttonText = "Not Available".L10N("UI:DTAConfig:NotAvailable");
            bool buttonEnabled = false;

            if (File.Exists(ProgramConstants.GamePath + c.LocalPath))
            {
                buttonText = "Uninstall".L10N("UI:DTAConfig:Uninstall");
                buttonEnabled = true;

                if (c.LocalIdentifier != c.RemoteIdentifier)
                    buttonText = "Update".L10N("UI:DTAConfig:Update") + $" ({ComponentsPanel.GetSizeString(c.RemoteSize)})";
            }
            else
            {
                if (!string.IsNullOrEmpty(c.RemoteIdentifier))
                {
                    buttonText = "Install".L10N("UI:DTAConfig:Install") + $" ({ComponentsPanel.GetSizeString(c.RemoteSize)})";
                    buttonEnabled = true;
                }
            }

            installationButtons[componentIndex].Text = buttonText;
            installationButtons[componentIndex].AllowClick = buttonEnabled;

            componentIndex++;
        }
    }

    private void Btn_LeftClick(object sender, EventArgs e)
    {
        XNAClientButton btn = (XNAClientButton)sender;

        CustomComponent cc = (CustomComponent)btn.Tag;

        if (cc.IsBeingDownloaded)
            return;

        if (File.Exists(ProgramConstants.GamePath + cc.LocalPath))
        {
            if (cc.LocalIdentifier == cc.RemoteIdentifier)
            {
                File.Delete(ProgramConstants.GamePath + cc.LocalPath);
                btn.Text = "Install".L10N("UI:DTAConfig:Install") + $" ({ComponentsPanel.GetSizeString(cc.RemoteSize)})";
                return;
            }

            btn.AllowClick = false;

            cc.DownloadFinished += cc_DownloadFinished;
            cc.DownloadProgressChanged += cc_DownloadProgressChanged;
            cc.DownloadComponent();
        }
        else
        {
            XNAMessageBox msgBox = new(WindowManager, "Confirmation Required".L10N("UI:DTAConfig:UpdateConfirmRequiredTitle"),
                string.Format(
                    ("To enable {0} the Client will download the necessary files to your game directory." +
                Environment.NewLine + Environment.NewLine + "This will take an additional {1} of disk space (size of the download is {2}), and the download may last" +
                Environment.NewLine +
                "from a few minutes to multiple hours depending on your Internet connection speed." +
                Environment.NewLine + Environment.NewLine +
                "You will not be able to play during the download. Do you want to continue?").L10N("UI:DTAConfig:UpdateConfirmRequiredText"),
                cc.GUIName, ComponentsPanel.GetSizeString(cc.RemoteSize), ComponentsPanel.GetSizeString(cc.Archived ? cc.RemoteArchiveSize : cc.RemoteSize)), XNAMessageBoxButtons.YesNo)
            {
                Tag = btn
            };
            msgBox.Show();
            msgBox.YesClickedAction = MsgBox_YesClicked;
        }
    }

    private void MsgBox_YesClicked(XNAMessageBox messageBox)
    {
        XNAClientButton btn = (XNAClientButton)messageBox.Tag;
        btn.AllowClick = false;

        CustomComponent cc = (CustomComponent)btn.Tag;

        cc.DownloadFinished += cc_DownloadFinished;
        cc.DownloadProgressChanged += cc_DownloadProgressChanged;
        cc.DownloadComponent();
    }

    /// <summary>
    /// Called whenever a custom component download's progress is changed.
    /// </summary>
    /// <param name="c">The CustomComponent object.</param>
    /// <param name="percentage">The current download progress percentage.</param>
    private void cc_DownloadProgressChanged(CustomComponent c, int percentage)
    {
        WindowManager.AddCallback(new Action<CustomComponent, int>(HandleDownloadProgressChanged), c, percentage);
    }

    private void HandleDownloadProgressChanged(CustomComponent cc, int percentage)
    {
        percentage = Math.Min(percentage, 100);

        XNAClientButton btn = installationButtons.Find(b => object.ReferenceEquals(b.Tag, cc));

        btn.Text = cc.Archived && percentage == 100
            ? "Unpacking...".L10N("UI:DTAConfig:Unpacking")
            : "Downloading...".L10N("UI:DTAConfig:Downloading") + " " + percentage + "%";
    }

    /// <summary>
    /// Called whenever a custom component download is finished.
    /// </summary>
    /// <param name="c">The CustomComponent object.</param>
    /// <param name="success">True if the download succeeded, otherwise false.</param>
    private void cc_DownloadFinished(CustomComponent c, bool success)
    {
        WindowManager.AddCallback(new Action<CustomComponent, bool>(HandleDownloadFinished), c, success);
    }

    private void HandleDownloadFinished(CustomComponent cc, bool success)
    {
        cc.DownloadFinished -= cc_DownloadFinished;
        cc.DownloadProgressChanged -= cc_DownloadProgressChanged;

        XNAClientButton btn = installationButtons.Find(b => object.ReferenceEquals(b.Tag, cc));
        btn.AllowClick = true;

        if (!success)
        {
            if (!downloadCancelled)
            {
                XNAMessageBox.Show(WindowManager, "Optional Component Download Failed".L10N("UI:DTAConfig:OptionalComponentDownloadFailedTitle"),
                    string.Format(
                        ("Download of optional component {0} failed." + Environment.NewLine +
                    "See client.log for details." + Environment.NewLine + Environment.NewLine +
                    "If this problem continues, please contact your mod's authors for support.").L10N("UI:DTAConfig:OptionalComponentDownloadFailedText"),
                    cc.GUIName));
            }

            btn.Text = "Install".L10N("UI:DTAConfig:Install") + $" ({ComponentsPanel.GetSizeString(cc.RemoteSize)})";

            if (File.Exists(ProgramConstants.GamePath + cc.LocalPath))
                btn.Text = "Update".L10N("UI:DTAConfig:Update") + $" ({ComponentsPanel.GetSizeString(cc.RemoteSize)})";
        }
        else
        {
            XNAMessageBox.Show(WindowManager, "Download Completed".L10N("UI:DTAConfig:DownloadCompleteTitle"),
                string.Format("Download of optional component {0} completed succesfully.".L10N("UI:DTAConfig:DownloadCompleteText"), cc.GUIName));
            btn.Text = "Uninstall".L10N("UI:DTAConfig:Uninstall");
        }
    }

    public void Open()
    {
        downloadCancelled = false;
    }

    private static string GetSizeString(long size)
    {
        if (size < 1048576)
        {
            return (size / 1024) + " KB";
        }
        else
        {
            return (size / 1048576) + " MB";
        }
    }
}