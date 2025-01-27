﻿using ClientGUI;
using ClientCore.Extensions;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using ClientCore;
using System.Runtime.InteropServices;
using ClientUpdater;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;

namespace DTAClient.DXGUI.Generic
{
    using System.Globalization;

    /// <summary>
    /// The update window, displaying the update progress to the user.
    /// </summary>
    public class UpdateWindow : XNAWindow
    {
        public delegate void UpdateCancelEventHandler(object sender, EventArgs e);
        public event UpdateCancelEventHandler UpdateCancelled;

        public delegate void UpdateCompletedEventHandler(object sender, EventArgs e);
        public event UpdateCompletedEventHandler UpdateCompleted;

        public delegate void UpdateFailureEventHandler(object sender, UpdateFailureEventArgs e);
        public event UpdateFailureEventHandler UpdateFailed;

        private const double DOT_TIME = 0.66;
        private const int MAX_DOTS = 5;

        private static readonly Guid CLSID_TaskbarList = new(0x56FDF344, 0xFD6D, 0x11D0, 0x95, 0x8A, 0x00, 0x60, 0x97, 0xC9, 0xA0, 0x90);

        private bool disposedValue;

        public UpdateWindow(WindowManager windowManager)
            : base(windowManager)
        {
        }

        private XNALabel lblDescription;
        private XNALabel lblCurrentFileProgressPercentageValue;
        private XNALabel lblTotalProgressPercentageValue;
        private XNALabel lblCurrentFile;
        private XNALabel lblUpdaterStatus;

        private XNAProgressBar prgCurrentFile;
        private XNAProgressBar prgTotal;
        private ITaskbarList4 tbp;

        private bool isStartingForceUpdate;

        bool infoUpdated = false;

        string currFileName = string.Empty;
        int currFilePercentage = 0;
        int totalPercentage = 0;
        int dotCount = 0;
        double currentDotTime = 0.0;

        private static readonly object locker = new object();

        public override void Initialize()
        {
            Name = "UpdateWindow";
            ClientRectangle = new Rectangle(0, 0, 446, 270);
            BackgroundTexture = AssetLoader.LoadTexture("updaterbg.png");

            lblDescription = new XNALabel(WindowManager);
            lblDescription.Text = string.Empty;
            lblDescription.ClientRectangle = new Rectangle(12, 9, 0, 0);
            lblDescription.Name = "lblDescription";

            var lblCurrentFileProgressPercentage = new XNALabel(WindowManager);
            lblCurrentFileProgressPercentage.Text = "Progress percentage of current file:".L10N("Client:Main:CurrentFileProgressPercentage");
            lblCurrentFileProgressPercentage.ClientRectangle = new Rectangle(12, 90, 0, 0);
            lblCurrentFileProgressPercentage.Name = "lblCurrentFileProgressPercentage";

            lblCurrentFileProgressPercentageValue = new XNALabel(WindowManager);
            lblCurrentFileProgressPercentageValue.Text = "0%";
            lblCurrentFileProgressPercentageValue.ClientRectangle = new Rectangle(409, lblCurrentFileProgressPercentage.Y, 0, 0);
            lblCurrentFileProgressPercentageValue.Name = "lblCurrentFileProgressPercentageValue";

            prgCurrentFile = new XNAProgressBar(WindowManager);
            prgCurrentFile.Name = "prgCurrentFile";
            prgCurrentFile.Maximum = 100;
            prgCurrentFile.ClientRectangle = new Rectangle(12, 110, 422, 30);
            //prgCurrentFile.BorderColor = UISettings.WindowBorderColor;
            prgCurrentFile.SmoothForwardTransition = true;
            prgCurrentFile.SmoothTransitionRate = 10;

            lblCurrentFile = new XNALabel(WindowManager);
            lblCurrentFile.Name = "lblCurrentFile";
            lblCurrentFile.ClientRectangle = new Rectangle(12, 142, 0, 0);

            var lblTotalProgressPercentage = new XNALabel(WindowManager);
            lblTotalProgressPercentage.Text = "Total progress percentage:".L10N("Client:Main:TotalProgressPercentage");
            lblTotalProgressPercentage.ClientRectangle = new Rectangle(12, 170, 0, 0);
            lblTotalProgressPercentage.Name = "lblTotalProgressPercentage";

            lblTotalProgressPercentageValue = new XNALabel(WindowManager);
            lblTotalProgressPercentageValue.Text = "0%";
            lblTotalProgressPercentageValue.ClientRectangle = new Rectangle(409, lblTotalProgressPercentage.Y, 0, 0);
            lblTotalProgressPercentageValue.Name = "lblTotalProgressPercentageValue";

            prgTotal = new XNAProgressBar(WindowManager);
            prgTotal.Name = "prgTotal";
            prgTotal.Maximum = 100;
            prgTotal.ClientRectangle = new Rectangle(12, 190, prgCurrentFile.Width, prgCurrentFile.Height);
            //prgTotal.BorderColor = UISettings.WindowBorderColor;

            lblUpdaterStatus = new XNALabel(WindowManager);
            lblUpdaterStatus.Name = "lblUpdaterStatus";
            lblUpdaterStatus.Text = "Preparing".L10N("Client:Main:StatusPreparing");
            lblUpdaterStatus.ClientRectangle = new Rectangle(12, 240, 0, 0);

            var btnCancel = new XNAClientButton(WindowManager);
            btnCancel.ClientRectangle = new Rectangle(301, 240, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnCancel.Text = "Cancel".L10N("Client:Main:ButtonCancel");
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
#if !NETFRAMEWORK

            if (!OperatingSystem.IsWindowsVersionAtLeast(6, 1))
                return;
#endif

            HRESULT coCreateInstanceResult = PInvoke.CoCreateInstance(CLSID_TaskbarList, null, CLSCTX.CLSCTX_INPROC_SERVER, out ITaskbarList4 ppv);

            if (coCreateInstanceResult.Failed)
                throw Marshal.GetExceptionForHR(coCreateInstanceResult)!;

            tbp = ppv;
        }

        private void Updater_FileIdentifiersUpdated()
        {
            if (!isStartingForceUpdate)
                return;

            if (Updater.VersionState == VersionState.UNKNOWN)
            {
                XNAMessageBox.Show(WindowManager, "Force Update Failure".L10N("Client:Main:ForceUpdateFailureTitle"), "Checking for updates failed.".L10N("Client:Main:ForceUpdateFailureText"));
                AddCallback(CloseWindow);
                return;
            }
            else if (Updater.VersionState == VersionState.OUTDATED && Updater.ManualUpdateRequired)
            {
                UpdateCancelled?.Invoke(this, EventArgs.Empty);
                AddCallback(CloseWindow);
                return;
            }

            SetData(Updater.ServerGameVersion);
            Updater.StartUpdate();
            isStartingForceUpdate = false;
        }

        private void Updater_LocalFileCheckProgressChanged(int checkedFileCount, int totalFileCount)
        {
            AddCallback(() => UpdateFileProgress(checkedFileCount * 100 / totalFileCount));
        }

        private void UpdateFileProgress(int value)
        {
            prgCurrentFile.Value = value;
            lblCurrentFileProgressPercentageValue.Text = value + "%";
        }

        private void Updater_UpdateProgressChanged(string currFileName, int currFilePercentage, int totalPercentage)
        {
            lock (locker)
            {
                infoUpdated = true;
                this.currFileName = currFileName;
                this.currFilePercentage = currFilePercentage;
                this.totalPercentage = totalPercentage;
            }
        }

        private void HandleUpdateProgressChange()
        {
            if (!infoUpdated)
                return;

            infoUpdated = false;

            if (currFilePercentage < 0 || currFilePercentage > prgCurrentFile.Maximum)
                prgCurrentFile.Value = 0;
            else
                prgCurrentFile.Value = currFilePercentage;

            if (totalPercentage < 0 || totalPercentage > prgTotal.Maximum)
                prgTotal.Value = 0;
            else
                prgTotal.Value = totalPercentage;

            lblCurrentFileProgressPercentageValue.Text = prgCurrentFile.Value + "%";
            lblTotalProgressPercentageValue.Text = prgTotal.Value + "%";
            lblCurrentFile.Text = "Current file:".L10N("Client:Main:CurrentFile") + " " + currFileName;
            lblUpdaterStatus.Text = "Downloading files".L10N("Client:Main:DownloadingFiles");
#if !NETFRAMEWORK

            if (!OperatingSystem.IsWindowsVersionAtLeast(6, 1))
                return;
#endif

            /*/ TODO Improve the updater
             * When the updater thread in DTAUpdater.dll has completed the update, it will
             * restart the client right away without giving the UI thread a chance to
             * finish its tasks and free resources in a proper way.
             * Because of that, this function is sometimes executed when
             * the game window has already been hidden / removed, and the code below
             * will then crash the client, causing the user to see a KABOOM message
             * along with the successful update, which is likely quite confusing for the user.
             * The try-catch is a dirty temporary workaround.
             * /*/
            try
            {
                tbp.SetProgressState((HWND)WindowManager.GetWindowHandle(), TBPFLAG.TBPF_NORMAL);
                tbp.SetProgressValue((HWND)WindowManager.GetWindowHandle(), (ulong)prgTotal.Value, (ulong)prgTotal.Maximum);
            }
            catch (Exception ex)
            {
                ProgramConstants.LogException(ex);
            }
        }

        private void Updater_OnFileDownloadCompleted(string archiveName)
        {
            AddCallback(() => HandleFileDownloadCompleted(archiveName));
        }

        private void HandleFileDownloadCompleted(string archiveName)
        {
            lblUpdaterStatus.Text = "Unpacking archive".L10N("Client:Main:UnpackingArchive");
        }

        private void Updater_OnUpdateCompleted()
        {
            AddCallback(HandleUpdateCompleted);
        }

        private void HandleUpdateCompleted()
        {
#if !NETFRAMEWORK
            if (OperatingSystem.IsWindowsVersionAtLeast(6, 1))
#endif
                tbp.SetProgressState((HWND)WindowManager.GetWindowHandle(), TBPFLAG.TBPF_NOPROGRESS);

            UpdateCompleted?.Invoke(this, EventArgs.Empty);
        }

        private void Updater_OnUpdateFailed(Exception ex)
        {
            AddCallback(() => HandleUpdateFailed(ex.Message));
        }

        private void HandleUpdateFailed(string updateFailureErrorMessage)
        {
#if !NETFRAMEWORK
            if (OperatingSystem.IsWindowsVersionAtLeast(6, 1))
#endif
                tbp.SetProgressState((HWND)WindowManager.GetWindowHandle(), TBPFLAG.TBPF_NOPROGRESS);

            UpdateFailed?.Invoke(this, new UpdateFailureEventArgs(updateFailureErrorMessage));
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

#if !NETFRAMEWORK
            if (OperatingSystem.IsWindowsVersionAtLeast(6, 1))
#endif
                tbp.SetProgressState((HWND)WindowManager.GetWindowHandle(), TBPFLAG.TBPF_NOPROGRESS);

            UpdateCancelled?.Invoke(this, EventArgs.Empty);
        }

        public void SetData(string newGameVersion)
        {
            lblDescription.Text = string.Format(CultureInfo.CurrentCulture, "Please wait while {0} is updated to version {1}.\nThis window will automatically close once the update is complete.\n\nThe client may also restart after the update has been downloaded.".L10N("Client:Main:UpdateVersionPleaseWait"), ProgramConstants.GAME_NAME_SHORT, newGameVersion);
            lblUpdaterStatus.Text = "Preparing".L10N("Client:Main:StatusPreparing");
        }

        public void ForceUpdate()
        {
            isStartingForceUpdate = true;
            lblDescription.Text = string.Format(CultureInfo.CurrentCulture, "Force updating {0} to latest version...".L10N("Client:Main:ForceUpdateToLatest"), ProgramConstants.GAME_NAME_SHORT);
            lblUpdaterStatus.Text = "Connecting".L10N("Client:Main:UpdateStatusConnecting");
            Updater.CheckForUpdates();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            lock (locker)
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

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            float xOffset = 3.0f;

            for (int i = 0; i < dotCount; i++)
            {
                var wrect = lblUpdaterStatus.RenderRectangle();
                Renderer.DrawStringWithShadow(".", lblUpdaterStatus.FontIndex,
                    new Vector2(wrect.Right + xOffset, wrect.Bottom - 15.0f), lblUpdaterStatus.TextColor);
                xOffset += 3.0f;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
#if NETFRAMEWORK
                    if (tbp is not null)
#else
                    if (tbp is not null && OperatingSystem.IsWindowsVersionAtLeast(5))
#endif
                    {
                        PInvoke.CoUninitialize();

                        tbp = null;
                    }
                }

                disposedValue = true;
            }

            base.Dispose(disposing);
        }
    }

    public class UpdateFailureEventArgs : EventArgs
    {
        public UpdateFailureEventArgs(string reason)
        {
            this.reason = reason;
        }

        string reason = String.Empty;

        /// <summary>
        /// The returned error message from the update failure.
        /// </summary>
        public string Reason
        {
            get { return reason; }
        }
    }
}