using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using IpcContractClientInterface;

using AppLog = IpcUtil.Logging;

namespace IpcCircularScan
{
    public partial class ShadingCorrectionDialog : Form
    {

        #region Variables

        /// <summary> Inherited Parent form </summary>
        protected IpcCircularScan.IpcCircularScanForm_goldenRatioSampling mParentForm = null;

        /// <summary> Inherited Channels </summary>
        private IpcContractClientInterface.Channels mChannels = null;

        /// <summary> Inherited Configuration </summary>
        public IpcCircularScan.Configuration mConfiguration = null;

        /// <summary> Dialog status </summary>
        private enum EDialogStatus { OK, Cancel, Error };
        private EDialogStatus mDialogStatus = EDialogStatus.OK;

        /// <summary> Number of images to average </summary>
        private int mNoImages = 1;

        // Original Manipulator Positions
        private float posX = 0;
        private float posY = 0;
        private float posMag = 0;
        private float posRot = 0;
        private float posImaging = 0;
        private float posTilt = 0;

        /// <summary> Path for shading corrections </summary>
        private string mPath = "";

        /// <summary> FileName for flat shading corrections </summary>
        private string mFilepathFlat = "";
        /// <summary> FileName for dark shading corrections </summary>
        private string mFilepathDark = "";

        ///// <summary> Flag for X-rays switched on </summary>
        //private bool mXraysOn = false;

        /// <summary> Manipulator Returned </summary>
        private bool mManipulatorReturned = false;

        #endregion Variables

        /// <summary>
        /// Constructor for ShadingCorrectionDialog
        /// </summary>
        /// <param name="aParentForm">Parent form</param>
        /// <param name="aChannels">Channels from parent form (private)</param>
        public ShadingCorrectionDialog(IpcCircularScan.IpcCircularScanForm_goldenRatioSampling aParentForm,
            IpcContractClientInterface.Channels aChannels)
        {
            // Assign the parent form
            mParentForm = aParentForm;

            // Assign the channels and configuration
            mChannels = aChannels;
            mConfiguration = mParentForm.mConfiguration;

            // Designer Initialisation
            InitializeComponent();

           

        }



        #region Background workers

        #region Shading Correction Background Worker

        private void backgroundWorker_ShadingCorrection_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                // Print Display log message;
                mParentForm.DisplayLog("Acquiring shading correction...");
                // Shading correction routine
                ShadingCorrection();

                // Wait for manipulator to return to original position
                backgroundWorker_ReturnManipulator.RunWorkerAsync();
                while (!mManipulatorReturned)
                    Thread.Sleep(100);

            }
            catch (Exception ex)
            {
                mDialogStatus = EDialogStatus.Error;
                AppLog.LogException(ex);
            }
        }

        private void backgroundWorker_ShadingCorrection_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // Final text based on Dialog status
            if (mDialogStatus == EDialogStatus.OK)
            {
                // Change action text
                labelAction.Text = "Correction acquired. Press Finish to exit.";

                // Display log text
                mParentForm.DisplayLog("Shading correction successfully acquired.");

                // Display paths for flat and dark image
                mParentForm.DisplayLog("Flat image:"+mFilepathFlat.ToString());
                mParentForm.DisplayLog("Dark image:" + mFilepathDark.ToString());
            }
            else
            {
                // Change action text
                labelAction.Text = "Correction aborted. Press Finish to exit.";

                // Display log text
                mParentForm.DisplayLog("Shading correction aborted.");
            }

            // Create output log file
            mParentForm.OutputLogFile(mPath + @"\ShadingCorrection");

            // Disable and hide cancel button
            buttonCancelInProcess.Enabled = false;
            buttonCancelInProcess.Visible = false;

            // Enable and show finish button
            buttonFinish.Enabled = true;
            buttonFinish.Visible = true;
        }
        
        #endregion Shading Correction Background Worker

        #region X-rays on Background Worker

        private void backgroundWorker_SwitchXrayOn_DoWork(object sender, DoWorkEventArgs e)
        {
            //// Set flag to false
            //mXraysOn = false;

            // Reset flag from previous use
            mParentForm.mXraysStable = false;
            // Check if X-rays are on and if so wait for stability
            if (mChannels.Xray.XRays.GenerationActual())
            {
                // Manually check if X-rays are stable. If already stable then no-flag will have been raised. 

                // Get a updated X-ray Entire Status
                mParentForm.mXrayEntireStatus = mChannels.Xray.GetXRayEntireStatus();
                // Find generation part of Entire Status
                mParentForm.mXrayGenerationStatus = mParentForm.mXrayEntireStatus.XRaysStatus.GenerationStatus.State;
                // If stable then manually set flag
                if (mParentForm.mXrayGenerationStatus == IpcContract.XRay.GenerationStatus.EXRayGenerationState.Success)
                    mParentForm.mXraysStable = true;
            }
            // Else if X-rays are not already on, then switch them on
            else
                mChannels.Xray.XRays.GenerationDemand(true);

            //Wait for the X-rays to stabilise before proceeding
            while (mParentForm.mXraysStable == false && mDialogStatus == EDialogStatus.OK)
                Thread.Sleep(200);
        }

        #endregion X-rays on Background Worker

        #region Manipulator return background worker

        private void backgroundWorker_ReturnManipulator_DoWork(object sender, DoWorkEventArgs e)
        {
            // set flag to false
            mManipulatorReturned = false;

            #region Manipulator return
            // Return Manipulator to original position

            // Set target positions to be original positions
            mChannels.Manipulator.Axis.Target(IpcContract.Manipulator.EAxisName.X, posX);
            mChannels.Manipulator.Axis.Target(IpcContract.Manipulator.EAxisName.Y, posY);
            mChannels.Manipulator.Axis.Target(IpcContract.Manipulator.EAxisName.Magnification, posMag);
            mChannels.Manipulator.Axis.Target(IpcContract.Manipulator.EAxisName.Tilt, posTilt);
            mChannels.Manipulator.Axis.Target(IpcContract.Manipulator.EAxisName.Rotate, posRot);
            mChannels.Manipulator.Axis.Target(IpcContract.Manipulator.EAxisName.Detector, posImaging);

            mParentForm.DisplayLog("Moving manipulator to original position.");

            // Move all axes at once
            mParentForm.MoveManipulator(IpcContract.Manipulator.EAxisName.All);

            mParentForm.DisplayLog("Manipulator now in original position.");
            #endregion Manipulator return

            // Set flag to true
            mManipulatorReturned = true;
        }

        private void backgroundWorker_ReturnManipulator_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

        }

        #endregion

        #endregion Background workers

        #region Dialog form functions

        #region Form loading closing functions

        private void ShadingCorrectionDialog_Load(object sender, EventArgs e)
        {
            // Load current positions into memory
            posX = mChannels.Manipulator.Axis.Position(IpcContract.Manipulator.EAxisName.X);
            posY = mChannels.Manipulator.Axis.Position(IpcContract.Manipulator.EAxisName.Y);
            posMag = mChannels.Manipulator.Axis.Position(IpcContract.Manipulator.EAxisName.Magnification);
            posRot = mChannels.Manipulator.Axis.Position(IpcContract.Manipulator.EAxisName.Rotate);
            posImaging = mChannels.Manipulator.Axis.Position(IpcContract.Manipulator.EAxisName.Detector);
            posTilt = mChannels.Manipulator.Axis.Position(IpcContract.Manipulator.EAxisName.Tilt);

            mParentForm.DisplayLog("Current manipulator position saved.");

            // Switch X-rays on using background worker
            backgroundWorker_SwitchXrayOn.RunWorkerAsync();

            // Place in live imaging mode
            mChannels.ImageProcessing.Image.Live();
        }

        private void ShadingCorrectionDialog_FormClosing(object sender, FormClosingEventArgs e)
        {

        }

        #endregion Form loading closing functions

        #region Form controls

        private void numericUpDown_NoImages_ValueChanged(object sender, EventArgs e)
        {
            mNoImages = Convert.ToInt32(numericUpDown_NoImages.Value);
        }

        #endregion Form controls

        #region Cancel buttons

        private void buttonCancelStatic_Click(object sender, EventArgs e)
        {
            // Switch X-rays off for safety
            mChannels.Xray.XRays.GenerationDemand(false);

            // Wait for manipulator to return to original position
            backgroundWorker_ReturnManipulator.RunWorkerAsync();

            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }

        private void buttonCancelInProcess_Click(object sender, EventArgs e)
        {
            mDialogStatus = EDialogStatus.Cancel;
        }

        #endregion Cancel buttons

        #region Progress buttons

        private void buttonContinue_Click(object sender, EventArgs e)
        {
            // Disable and hide continue button
            buttonContinue.Enabled = false;
            buttonContinue.Visible = false;

            // Enable and show Acquire button
            buttonAcquire.Enabled = true;
            buttonAcquire.Visible = true;

            // Change action text
            labelAction.Text = "Select number of images to average";

            // Display numeric up down control
            numericUpDown_NoImages.Enabled = true;
            numericUpDown_NoImages.Visible = true;

        }

        private void buttonAcquire_Click(object sender, EventArgs e)
        {
            // Disable Acquire button
            buttonAcquire.Enabled = false;

            // Disable and hide numeric up-down control
            numericUpDown_NoImages.Enabled = false;
            numericUpDown_NoImages.Visible = false;

            // Change action text
            labelAction.Text = "Acquiring correction using average of " + mNoImages.ToString() + " images";

            // Swap cancel buttons
            buttonCancelStatic.Enabled = false;
            buttonCancelStatic.Visible = false;
            buttonCancelInProcess.Enabled = true;
            buttonCancelInProcess.Visible = true;

            // Run background worker
            backgroundWorker_ShadingCorrection.RunWorkerAsync();
        }

        private void buttonFinish_Click(object sender, EventArgs e)
        {
            if (mDialogStatus == EDialogStatus.OK)
                this.DialogResult = System.Windows.Forms.DialogResult.OK;
            else
                this.DialogResult = System.Windows.Forms.DialogResult.Abort;
        }

        #endregion Progress buttons

        #endregion Dialog Form functions

        #region Shading Correction

        /// <summary>
        /// Function for shading correction
        /// </summary>
        private void ShadingCorrection()
        {
            #region Create suitable Directory for shading correction

            // Set directory path
            mPath = mConfiguration.ResultsDirectory + @"\ShadingCorrection_" +
                mChannels.Xray.XRays.KilovoltsDemand().ToString() + "kV" +
                mChannels.Xray.XRays.MicroampsDemand().ToString() + "uA" +
                    "_" + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss-tt");
            // Check if Directory exists, and if not then create one. 
            if (!Directory.Exists(mPath))
            {
                Directory.CreateDirectory(mPath);
            }

            // Display message
            mParentForm.DisplayLog("Directory for shading correction created at:");
            mParentForm.DisplayLog(mPath.ToString());

            #endregion Create suitable Directory for shading correction

            #region Flat Image

            #region X-rays on and stable
            // Turn X-rays on and wait for stability

            // Reset flag from previous use
            mParentForm.mXraysStable = false;
            // Check if X-rays are on and if so wait for stability
            if (mChannels.Xray.XRays.GenerationActual())
            {
                // Manually check if X-rays are stable. If already stable then no-flag will have been raised. 

                // Get a updated X-ray Entire Status
                mParentForm.mXrayEntireStatus = mChannels.Xray.GetXRayEntireStatus();
                // Find generation part of Entire Status
                mParentForm.mXrayGenerationStatus = mParentForm.mXrayEntireStatus.XRaysStatus.GenerationStatus.State;
                // If stable then manually set flag
                if (mParentForm.mXrayGenerationStatus == IpcContract.XRay.GenerationStatus.EXRayGenerationState.Success)
                    mParentForm.mXraysStable = true;
            }
            // Else if X-rays are not already on, then switch them on
            else
                mChannels.Xray.XRays.GenerationDemand(true);

            //Wait for the X-rays to stabilise before proceeding
            while (mParentForm.mXraysStable == false && mDialogStatus == EDialogStatus.OK)
                Thread.Sleep(200);

            // Only wait some more if process hasn't been cancelled already
            if (mDialogStatus == EDialogStatus.Cancel)
                return;
            else
            {
                // sleep some more
                Thread.Sleep(5000);
            }

            mParentForm.DisplayLog("Demand X-ray kV:\t" + mChannels.Xray.XRays.KilovoltsDemand().ToString() + "kV");
            mParentForm.DisplayLog("Demand X-ray uA:\t" + mChannels.Xray.XRays.MicroampsDemand().ToString() + "uA");
            mParentForm.DisplayLog("Exposure:\t" + mChannels.ImageProcessing.DetectorParameters.Exposure().ToString() + "ms");
            mParentForm.DisplayLog("Binning:\t" + Convert.ToInt32(Math.Pow(2.0, (double)mChannels.ImageProcessing.DetectorParameters.Binning())).ToString() + "x");
            mParentForm.DisplayLog("Accumulation:\t" + mChannels.ImageProcessing.DetectorParameters.Accumulation().ToString() + "x");

            #endregion X-rays on and stable


            #region Capture and save Flat image

            if (mDialogStatus == EDialogStatus.Cancel)
                return;
            else
            {
                // Capture average flat image
                mParentForm.ImageCapture(mNoImages, true);
                // Set filename
                string filename = @"Flat_" +
                    mChannels.Xray.XRays.KilovoltsDemand().ToString() + "kV" + 
                    mChannels.Xray.XRays.MicroampsDemand().ToString() + "uA" + @".tif";
                // Set filepath
                string filepath = mPath + @"\" + filename;
                // Save image
                mParentForm.ImageSave(filepath);
                // Display log message
                mParentForm.DisplayLog(@"Captured and saved " + filename);
                // Save Flat path
                mFilepathFlat = filepath;
            }

            #endregion Capture and save Flat image

            #endregion Flat image

            #region Dark Image

            #region X-rays off
            // Switch X-rays off
            mChannels.Xray.XRays.GenerationDemand(false);

            //Wait for the X-rays to stabilise before proceeding
            while (mParentForm.mXraysStable == true && mDialogStatus == EDialogStatus.OK)
                Thread.Sleep(200);

            // If cancel requested then immediately quit otherwise wait some more
            if (mDialogStatus == EDialogStatus.Cancel)
                return;
            else
            {
                // sleep some more
                Thread.Sleep(5000);
            }

            #endregion X-rays off

            #region Capture and save Dark image

            // Check if cancel requested, and if not then capture dark image
            if (mDialogStatus == EDialogStatus.Cancel)
                return;
            else
            {
                // Capture average dark image
                mParentForm.ImageCapture(mNoImages, true);
                // Set filename
                string filename = @"Dark_" +
                    mChannels.Xray.XRays.KilovoltsDemand().ToString() + "kV" +
                    mChannels.Xray.XRays.MicroampsDemand().ToString() + "uA" + @".tif";
                // Set filepath
                string filepath = mPath + @"\" + filename;
                // Save image
                mParentForm.ImageSave(filepath);
                // Display log message
                mParentForm.DisplayLog(@"Captured and saved " + filename);
                // Save Dark path
                mFilepathDark = filepath;
            }

            #endregion Capture and save Dark image

            #endregion Dark Image
        }

        #endregion Shading Correction






    }
}
