using System;
using ClientCore;
using ClientGUI;
using ClientUpdater;
using DTAClient.Domain;
using Localization;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Generic;

/// <summary>
/// The update window, displaying the update progress to the user.
/// </summary>
public class UpdateWindow : XNAWindow
{
    private const double DOT_TIME = 0.66;

    private const int MAX_DOTS = 5;

    private static readonly object Locker = new();

    private double currentDotTime = 0.0;

    private string currFileName = string.Empty;

    private int currFilePercentage = 0;

    private int dotCount = 0;

    private bool infoUpdated = false;

    private bool isStartingForceUpdate;

    private XNALabel lblCurrentFile;

    private XNALabel lblCurrentFileProgressPercentageValue;

    private XNALabel lblDescription;

    private XNALabel lblTotalProgressPercentageValue;

    private XNALabel lblUpdaterStatus;

    private XNAProgressBar prgCurrentFile;

    private XNAProgressBar prgTotal;

    private TaskbarProgress tbp;

    private int totalPercentage = 0;

    public UpdateWindow(WindowManager windowManager)
        : base(windowManager)
    {
    }

    private delegate void FileDownloadCompletedDelegate(string archiveName);

    private delegate void UpdateProgressChangedDelegate(string fileName, int filePercentage, int totalPercentage);

    public event EventHandler UpdateCancelled;

    public event EventHandler UpdateCompleted;

    public event EventHandler<UpdateFailureEventArgs> UpdateFailed;

    public override void Draw(GameTime gameTime)
    {
        base.Draw(gameTime);

        float xOffset = 3.0f;

        for (int i = 0; i < dotCount; i++)
        {
            Rectangle wrect = lblUpdaterStatus.RenderRectangle();
            Renderer.DrawStringWithShadow(
                ".",
                lblUpdaterStatus.FontIndex,
                new Vector2(wrect.Right + xOffset, wrect.Bottom - 15.0f),
                lblUpdaterStatus.TextColor);
            xOffset += 3.0f;
        }
    }

    public void ForceUpdate()
    {
        isStartingForceUpdate = true;
        lblDescription.Text = string.Format("Force updating {0} to latest version...".L10N("UI:Main:ForceUpdateToLatest"), MainClientConstants.GameNameShort);
        lblUpdaterStatus.Text = "Connecting".L10N("UI:Main:UpdateStatusConnecting");
        Updater.CheckForUpdates();
    }

    public override void Initialize()
    {
        Name = "UpdateWindow";
        ClientRectangle = new Rectangle(0, 0, 446, 270);
        BackgroundTexture = AssetLoader.LoadTexture("updaterbg.png");

        lblDescription = new XNALabel(WindowManager)
        {
            Text = string.Empty,
            ClientRectangle = new Rectangle(12, 9, 0, 0),
            Name = "lblDescription"
        };

        XNALabel lblCurrentFileProgressPercentage = new(WindowManager)
        {
            Text = "Progress percentage of current file:".L10N("UI:Main:CurrentFileProgressPercentage"),
            ClientRectangle = new Rectangle(12, 90, 0, 0),
            Name = "lblCurrentFileProgressPercentage"
        };

        lblCurrentFileProgressPercentageValue = new XNALabel(WindowManager)
        {
            Text = "0%",
            ClientRectangle = new Rectangle(409, lblCurrentFileProgressPercentage.Y, 0, 0),
            Name = "lblCurrentFileProgressPercentageValue"
        };

        prgCurrentFile = new XNAProgressBar(WindowManager)
        {
            Name = "prgCurrentFile",
            Maximum = 100,
            ClientRectangle = new Rectangle(12, 110, 422, 30),

            //prgCurrentFile.BorderColor = UISettings.WindowBorderColor;
            SmoothForwardTransition = true,
            SmoothTransitionRate = 10
        };

        lblCurrentFile = new XNALabel(WindowManager)
        {
            Name = "lblCurrentFile",
            ClientRectangle = new Rectangle(12, 142, 0, 0)
        };

        XNALabel lblTotalProgressPercentage = new(WindowManager)
        {
            Text = "Total progress percentage:".L10N("UI:Main:TotalProgressPercentage"),
            ClientRectangle = new Rectangle(12, 170, 0, 0),
            Name = "lblTotalProgressPercentage"
        };

        lblTotalProgressPercentageValue = new XNALabel(WindowManager)
        {
            Text = "0%",
            ClientRectangle = new Rectangle(409, lblTotalProgressPercentage.Y, 0, 0),
            Name = "lblTotalProgressPercentageValue"
        };

        prgTotal = new XNAProgressBar(WindowManager)
        {
            Name = "prgTotal",
            Maximum = 100,
            ClientRectangle = new Rectangle(12, 190, prgCurrentFile.Width, prgCurrentFile.Height)
        };

        //prgTotal.BorderColor = UISettings.WindowBorderColor;
        lblUpdaterStatus = new XNALabel(WindowManager)
        {
            Name = "lblUpdaterStatus",
            Text = "Preparing".L10N("UI:Main:StatusPreparing"),
            ClientRectangle = new Rectangle(12, 240, 0, 0)
        };

        XNAClientButton btnCancel = new(WindowManager)
        {
            ClientRectangle = new Rectangle(301, 240, UIDesignConstants.ButtonWidth133, UIDesignConstants.ButtonHeight),
            Text = "Cancel".L10N("UI:Main:ButtonCancel")
        };
        btnCancel.LeftClick += BtnCancel_LeftClick;

        AddChild(lblDescription);
        AddChild(lblCurrentFileProgressPercentage);
        AddChild(lblCurrentFileProgressPercentageValue);
        AddChild(prgCurrentFile);
        AddChild(lblCurrentFile);
        AddChild(lblTotalProgressPercentage);
        AddChild(lblTotalProgressPercentageValue);
        AddChild(prgTotal);
        AddChild(lblUpdaterStatus);
        AddChild(btnCancel);

        base.Initialize(); // Read theme settings from INI

        CenterOnParent();

        Updater.FileIdentifiersUpdated += Updater_FileIdentifiersUpdated;
        Updater.OnUpdateCompleted += Updater_OnUpdateCompleted;
        Updater.OnUpdateFailed += Updater_OnUpdateFailed;
        Updater.UpdateProgressChanged += Updater_UpdateProgressChanged;
        Updater.LocalFileCheckProgressChanged += Updater_LocalFileCheckProgressChanged;
        Updater.OnFileDownloadCompleted += Updater_OnFileDownloadCompleted;

        if (UpdateWindow.IsTaskbarSupported())
            tbp = new TaskbarProgress();
    }

    public void SetData(string newGameVersion)
    {
        lblDescription.Text = string.Format(
            ("Please wait while {0} is updated to version {1}." + Environment.NewLine +
                "This window will automatically close once the update is complete." + Environment.NewLine + Environment.NewLine +
                "The client may also restart after the update has been downloaded.").L10N("UI:Main:UpdateVersionPleaseWait"),
            MainClientConstants.GameNameShort,
            newGameVersion);
        lblUpdaterStatus.Text = "Preparing".L10N("UI:Main:StatusPreparing");
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        lock (Locker)
        {
            HandleUpdateProgressChange();
        }

        currentDotTime += gameTime.ElapsedGameTime.TotalSeconds;
        if (currentDotTime > DOT_TIME)
        {
            currentDotTime = 0.0;
            dotCount++;
            if (dotCount > MAX_DOTS)
                dotCount = 0;
        }
    }

    private static bool IsTaskbarSupported()
    {
        return MainClientConstants.OSId is OSVersion.WIN7 or OSVersion.WIN810;
    }

    private void BtnCancel_LeftClick(object sender, EventArgs e)
    {
        if (!isStartingForceUpdate)
            Updater.StopUpdate();

        CloseWindow();
    }

    private void CloseWindow()
    {
        isStartingForceUpdate = false;

        if (UpdateWindow.IsTaskbarSupported())
            tbp.SetState(WindowManager.GetWindowHandle(), TaskbarProgress.TaskbarStates.NoProgress);

        UpdateCancelled?.Invoke(this, EventArgs.Empty);
    }

    private void HandleFileDownloadCompleted(string archiveName)
    {
        lblUpdaterStatus.Text = "Unpacking archive".L10N("UI:Main:UnpackingArchive");
    }

    private void HandleUpdateCompleted()
    {
        if (UpdateWindow.IsTaskbarSupported())
            tbp.SetState(WindowManager.GetWindowHandle(), TaskbarProgress.TaskbarStates.NoProgress);

        UpdateCompleted?.Invoke(this, EventArgs.Empty);
    }

    private void HandleUpdateFailed(string updateFailureErrorMessage)
    {
        if (UpdateWindow.IsTaskbarSupported())
            tbp.SetState(WindowManager.GetWindowHandle(), TaskbarProgress.TaskbarStates.NoProgress);

        UpdateFailed?.Invoke(this, new UpdateFailureEventArgs(updateFailureErrorMessage));
    }

    private void HandleUpdateProgressChange()
    {
        if (!infoUpdated)
            return;

        infoUpdated = false;

        prgCurrentFile.Value = currFilePercentage < 0 || currFilePercentage > prgCurrentFile.Maximum ? 0 : currFilePercentage;

        prgTotal.Value = totalPercentage < 0 || totalPercentage > prgTotal.Maximum ? 0 : totalPercentage;

        lblCurrentFileProgressPercentageValue.Text = prgCurrentFile.Value.ToString() + "%";
        lblTotalProgressPercentageValue.Text = prgTotal.Value.ToString() + "%";
        lblCurrentFile.Text = "Current file:".L10N("UI:Main:CurrentFile") + " " + currFileName;
        lblUpdaterStatus.Text = "Downloading files".L10N("UI:Main:DownloadingFiles");

        /*/ TODO Improve the updater
         * When the updater thread in DTAUpdater.dll has completed the update, it will
         * restart the client right away without giving the UI thread a chance to
         * finish its tasks and free resources in a proper way.
         * Because of that, this function is sometimes executed when
         * the game window has already been hidden / removed, and the code below
         * will then crash the client, causing the user to see a KABOOM message
         * along with the succesful update, which is likely quite confusing for the user.
         * The try-catch is a dirty temporary workaround.
         * /*/
        try
        {
            if (UpdateWindow.IsTaskbarSupported())
            {
                tbp.SetState(WindowManager.GetWindowHandle(), TaskbarProgress.TaskbarStates.Normal);
                tbp.SetValue(WindowManager.GetWindowHandle(), prgTotal.Value, prgTotal.Maximum);
            }
        }
        catch
        {
        }
    }

    private void UpdateFileProgress(int value)
    {
        prgCurrentFile.Value = value;
        lblCurrentFileProgressPercentageValue.Text = value + "%";
    }

    private void Updater_FileIdentifiersUpdated()
    {
        if (!isStartingForceUpdate)
            return;

        if (Updater.VersionState == VersionState.UNKNOWN)
        {
            XNAMessageBox.Show(WindowManager, "Force Update Failure".L10N("UI:Main:ForceUpdateFailureTitle"), "Checking for updates failed.".L10N("UI:Main:ForceUpdateFailureText"));
            AddCallback(new Action(CloseWindow), null);
            return;
        }
        else if (Updater.VersionState == VersionState.OUTDATED && Updater.ManualUpdateRequired)
        {
            UpdateCancelled?.Invoke(this, EventArgs.Empty);
            AddCallback(new Action(CloseWindow), null);
            return;
        }

        SetData(Updater.ServerGameVersion);
        Updater.StartUpdate();
        isStartingForceUpdate = false;
    }

    private void Updater_LocalFileCheckProgressChanged(int checkedFileCount, int totalFileCount)
    {
        AddCallback(
            new Action<int>(UpdateFileProgress),
            checkedFileCount * 100 / totalFileCount);
    }

    private void Updater_OnFileDownloadCompleted(string archiveName)
    {
        AddCallback(new FileDownloadCompletedDelegate(HandleFileDownloadCompleted), archiveName);
    }

    private void Updater_OnUpdateCompleted()
    {
        AddCallback(new Action(HandleUpdateCompleted), null);
    }

    private void Updater_OnUpdateFailed(Exception ex)
    {
        AddCallback(new Action<string>(HandleUpdateFailed), ex.Message);
    }

    private void Updater_UpdateProgressChanged(string currFileName, int currFilePercentage, int totalPercentage)
    {
        lock (Locker)
        {
            infoUpdated = true;
            this.currFileName = currFileName;
            this.currFilePercentage = currFilePercentage;
            this.totalPercentage = totalPercentage;
        }
    }
}