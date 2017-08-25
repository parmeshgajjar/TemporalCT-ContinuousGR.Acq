
//**********************************************************************************************************************************************************//
//																																							//
//	The purpose of IpcDemo project is to show how Inspect-X can be programmed externally using Microsoft.NET platform, Inter Process Communication (IPC).	//
//	It should be used together with the Inspect-X Programming Manual.																						//
//	This example shows how to																																//
//		-	connect to Inspect-X,																															//
//		-	get data from Inspect-X,																														//
//		-	tell Inspect-X to do something.																													//
//	In particular the following functionalities are present:																								//
//		-	Heartbeat to Inspect-X																															//
//		-	Get beam data																																	//
//		-	Get manipulator position																														//
//		-	Set binning mode																																//
//		-	Set exposure																																	//
//		-	Set frames to be averaged																														//
//		-	Set number of projections as well as the start and end angle																					//
//		-	Home manipulator																																//
//		-	Move manipulator																																//				
//		-	Turn x-rays on/off																																//				
//		-	Take images																																		//
//		-	Save results			
//
//										
//**********************************************************************************************************************************************************//


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Globalization;
using System.Diagnostics;
using IpcContractClientInterface;

using AppLog = IpcUtil.Logging;

namespace IpcCircularScan
{
    public partial class IpcCircularScanForm : Form
    {

        /// <summary>Are we in design mode</summary>
        protected bool mDesignMode { get; private set; }

        #region Standard IPC Variables

        /// <summary>Collection of all IPC channels, this object always exists.</summary>
        private IpcContractClientInterface.Channels mChannels = new Channels();

        #endregion Standard IPC Variables

        # region Variables

        /// <summary> Status of the application </summary>
        public enum EApplicationState { Disconnected, Connected, Running }
        private EApplicationState mApplicationState;
        public Configuration mConfiguration;
        /// <summary> The path where the configuration data will be saved to </summary>
        public string mConfigurationDirectory = "";
        /// <summary> Project Name </summary>
        private string mProjectName = "";
        /// <summary> The number of heartbeats since connection </summary>
        private UInt32 mHeartBeats = 0;
        /// <summary>The list with the panels to show live connection with host</summary>
        private List<Panel> mHeartBeatPanelList;
        /// <summary> Flag to stop the test </summary>
        bool mStop = true;
        /// <summary>Flag to shut down if impossible to initialize application</summary>
        private bool mLoadedOK = false;
        /// <summary> Flag to signal if manipulator stopped moving </summary>
        public bool mManipulatorMoveComplete = false;
        /// <summary> Flag to signal if an image has been captured</summary>
        public bool mImageCaptureComplete = false;
        /// <summary> Flag to signal if image has been saved  </summary>
        public bool mImageSaveComplete = false;
        /// <summary> Flag to signal if manipulator has been homed </summary>
        private bool mManipulatorHomed = false;
        /// <summary> Flag to signal if x-rays are on and stable </summary>
        public bool mXraysStable = false;

        /// <summary> Entire Xray Status (for Inspect-X bug correction) </summary>
        public IpcContract.XRay.EntireStatus mXrayEntireStatus;
        /// <summary> Generation status </summary>
        public IpcContract.XRay.GenerationStatus.EXRayGenerationState mXrayGenerationStatus;
        /// <summary> Stability event counter </summary>
        private int mXraysStabilityCounter = 0;

        /// <summary> Lock Binning Flag </summary>
        private bool mLockTBBinning = false;
        /// <summary> Lock Exposure flag </summary>
        private bool mlockTBExposure = false;
        /// <summary> Lock accumulation flag </summary>
        private bool mLockTBAccumulation = false;

        /// <summary>Remember the last exposures seen!</summary>
        private int[] mExposures = null;
        /// <summary> Status of test </summary>
        public enum ETestResult { Succeeded, FailedStopped, FailedToCondition, FailedToCapture, Disconnected }

        /// <summary>Status of process</summary>
        public enum EStatus
        {
            /// <summary>Process state has no errors etc</summary>
            OK,
            /// <summary>Internal problem, process aborted unexpectedly</summary>
            InternalError,
            /// <summary>Process cancelled by external operator</summary>
            ExternalCancel
        }

        /// <summary>Abort state (if any)</summary>
        private EStatus mStatus = EStatus.OK;

        private ProjectScan mScan = new ProjectScan();
        private Thread mWorkingThread = null;

        /// <summary> ang file </summary>
        System.IO.StreamWriter mAngFile = null;

        /// <summary> Shading correction Dialog </summary>
        private IpcCircularScan.ShadingCorrectionDialog mShadingCorrectionDialog = null;

        // 
        // List of Axis label indices
        //
        // X = 1,
        // Y = 2,
        // Z = 3,
        // Magnification = 3,
        // Tilt = 4,
        // Rotate = 5,
        // Detector = 6,

        /// <summary> Array of Axis indexes 
        /// Ensure that the correct index is chosen for each description below </summary>
        private int[] mAxisListIndex = { 5 };
        /// <summary> Axis Labels </summary>
        private List<string> AxisListLabels = new List<string> { "Rotate" };

        /// <summary> Output log text that includes time and date stamps </summary>
        public string OutputLogText = null;

        #endregion Variables

        /// <summary>
        /// Class managing scan variables
        /// </summary>
        public class ProjectScan
        {
            public DateTime TimeStarted = DateTime.Now;
            public DateTime TimeFinished = DateTime.Now;
            public Int32 ImagesCaptured = 0;
            public DateTime LastImageTimestamp = DateTime.Now;
            public ETestResult Result = ETestResult.Succeeded;

            /// <summary> Formats output into a structured format ready to print on screen </summary>
            public string DumpValues()
            {
                string s =
                    "Scan State\r\n" +
                    "   Time Started      {0}\r\n" +
                    "   Time Finished     {1}\r\n" +
                    "   Images Captured   {2}\r\n" +
                    "   Last Image taken  {3}\r\n" +
                    "   Result            {4}";

                // These are NOT the standard time and date formats.
                return String.Format(s,
                    TimeStarted.ToString("yyyy-MM-dd HH-mm-ss-tt"),
                    TimeFinished.ToString("yyyy-MM-dd HH-mm-ss-tt"),
                    ImagesCaptured,
                    LastImageTimestamp.ToString("yyyy-MM-dd HH-mm-ss-tt"),
                    Result.ToString());
            }

            public ProjectScan Clone()
            {
                ProjectScan Scan = new ProjectScan();
                Scan.ImagesCaptured = ImagesCaptured;
                Scan.LastImageTimestamp = LastImageTimestamp;
                Scan.Result = Result;
                Scan.TimeFinished = TimeFinished;
                Scan.TimeStarted = TimeStarted;
                return Scan;
            }
        }

        /// <summary>
        /// Constructor for form
        /// </summary>
        public IpcCircularScanForm()
        {
            try
            {
                mDesignMode = (LicenseManager.CurrentContext.UsageMode == LicenseUsageMode.Designtime);

                InitializeComponent();
                if (mDesignMode)
                    return;

                //load the last saved settings
                mConfigurationDirectory = Path.GetDirectoryName(Application.ExecutablePath) + @"\" + Configuration.mFileName;
                mConfiguration = Configuration.Load(mConfigurationDirectory);

                // Tell normal logging who the parent window is.
                AppLog.SetParentWindow = this;
                AppLog.TraceInfo = true;
                AppLog.TraceDebug = true;

                // Initialize members
                mStop = true;
                mApplicationState = EApplicationState.Disconnected;
                mHeartBeatPanelList = new List<Panel>();
                mHeartBeatPanelList.Add(this.panelHeartBeat1);
                mHeartBeatPanelList.Add(this.panelHeartBeat2);
                mHeartBeatPanelList.Add(this.panelHeartBeat3);
                mHeartBeatPanelList.Add(this.panelHeartBeat4);
                mHeartBeatPanelList.Add(this.panelHeartBeat5);
                mHeartBeatPanelList.Add(this.panelHeartBeat6);
                mLoadedOK = true;

                // Enable the channels that will be controlled by this application.
                // For the generic IPC client this is all of them!
                // This just sets flags, it does not actually open the channels.
                mChannels.AccessApplication = true;
                mChannels.AccessXray = true;
                mChannels.AccessManipulator = true;
                mChannels.AccessImageProcessing = true;
                mChannels.AccessInspection = false;
                mChannels.AccessInspection2D = false;
                mChannels.AccessCT3DScan = false;
                mChannels.AccessCT2DScan = false;
            }
            catch (Exception ex)
            {
                AppLog.LogException(ex);
            }

        }

        //*********************************************************************
        /// <summary>
        /// Initialises the UI
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (mDesignMode)
                return;

            try
            {
                if (mLoadedOK)
                {
                    buttonDisconnect.Location = buttonConnect.Location;
                    buttonStop.Location = buttonStart.Location;
                    LayoutUI();
                }
                else
                {
                    MessageBox.Show("Error loading Application.\r\n Application now will close");
                    Close();
                }
            }
            catch (Exception ex)
            {
                AppLog.LogException(ex);
                Close();
            }
        }

        #region UI Elements

        #region Connect and Disconnect

        //***************************************************************
        /// <summary>
        /// Disconnects from Inspect-X 
        /// </summary>
        private void buttonDisconnect_Click(object sender, EventArgs e)
        {
            try
            {
                if (mChannels != null)
                {
                    if (ChannelsDetach())
                    {
                        DisplayLog("Disconnecting from Inspect-X...");
                        Thread.Sleep(1000); //Wait a little bit so any callbacks on other threads can complete gracefully
                        mApplicationState = EApplicationState.Disconnected;
                        mScan.Result = ETestResult.Disconnected;
                        LayoutUI();
                        DisplayLog("Disconnected");
                    }
                    else
                    {
                        throw new Exception("Cannot Disconnect from Inspect-X");
                    }
                }
            }
            catch (Exception ex)
            {
                mStatus = EStatus.InternalError;
                AppLog.LogException(ex);
            }
        }

        //*******************************************************************
        /// <summary>
        /// Connects to Inspect-X by attaching all the channels
        /// </summary>
        private void buttonConnect_Click(object sender, EventArgs e)
        {
            try
            {
                //connect to server and populate controls
                DisplayLog("Connecting to Inspect-X...");
                Channels.EConnectionState connectionState = ChannelsAttach();

                if (connectionState == Channels.EConnectionState.Connected) //have we connected successfully?
                {
                    IpcContractClientInterface.CommonMethods.ProductVersionCheck(this, mChannels);
                    InitialiseUI();
                    mHeartBeats = 0;
                    mApplicationState = EApplicationState.Connected; //update state to connected
                    DisplayLog("Connected to Inspect-X");
                }
                else
                {
                    MessageBox.Show("Error: Connecting to Inspect-X\r\nPlease make sure Inspect-X is running on this machine");
                }
            }
            catch (Exception ex)
            {
                AppLog.LogException(ex);
                mApplicationState = EApplicationState.Disconnected;
                mStatus = EStatus.InternalError;
            }
            finally
            {
                LayoutUI();
            }
        }

        #endregion Connect and Disconnect

        #region Start-Stop buttons

        //****************************************************************
        /// <summary>
        /// Creates the TestDirectory folder for the results, saves configuration settings to disk,
        /// Starts the actual test process
        /// </summary>
        private void buttonStart_Click(object sender, EventArgs e)
        {
            try
            {
                // Start test
                mWorkingThread = new Thread(WorkingThread);
                mWorkingThread.Name = "Main Working Thread";
                mWorkingThread.Start();
            }
            catch (Exception ex)
            {
                if (mWorkingThread != null)
                    mStop = true;
                mStatus = EStatus.InternalError;
                AppLog.LogException(ex);
            }
        }

        //*****************************************************************
        /// <summary>
        /// Ends the process by calling the User Abort function
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonStop_Click(object sender, EventArgs e)
        {
            try
            {
                UserAbort();
            }
            catch (Exception ex)
            { AppLog.LogException(ex); }
        }

        #endregion Start-Stop buttons

        #region User-Interface Interaction

        //*************************************************************
        /// <summary>
        /// Opens a folder browser dialog to choose where to save the results
        /// </summary>
        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            if (mDesignMode)
                return;

            try
            {
                FolderBrowserDialog fbd = new FolderBrowserDialog();
                fbd.SelectedPath = mConfiguration.ResultsDirectory;

                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    textBoxResultsDirectory.Text = fbd.SelectedPath;
                    mConfiguration.ResultsDirectory = fbd.SelectedPath;
                    LayoutUI();
                }
            }
            catch (Exception ex)
            {
                mStatus = EStatus.ExternalCancel;
                AppLog.LogException(ex);
            }
        }

        //*********************************************************************
        /// <summary>
        /// Sets the number of projections in the configuration when the UI changes
        /// </summary>
        private void numericUpDownNoOfProjections_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                mConfiguration.NoOfProjections = Convert.ToInt32(numericUpDownNoOfProjections.Value);
            }
            catch (Exception ex)
            {
                AppLog.LogException(ex);
            }
        }

        //*********************************************************************
        /// <summary>
        /// Sets the configuration according to the value on the UI
        /// </summary>
        private void trackBarBinning_Scroll(object sender, EventArgs e)
        {
            if (mChannels.ImageProcessing == null)
                return;
            if (mLockTBBinning)
                return;
            try
            {
                mLockTBBinning = true;

                int Value = (int)((TrackBar)sender).Value;
                mConfiguration.mBinning = Value;
                //convert index to value to display the binning mode
                labelBinning.Text = Convert.ToInt32(Math.Pow((double)2, (double)Value)).ToString() + " X";
                mChannels.ImageProcessing.DetectorParameters.Binning(mConfiguration.mBinning);
            }
            catch (Exception ex)
            {
                AppLog.LogException(ex);
            }
            finally
            {
                mLockTBBinning = false;
            }
        }

        //***********************************************************************
        /// <summary>
        /// Sets the configuration according to the value on the UI
        /// </summary>
        private void trackBarExpoure_Scroll(object sender, EventArgs e)
        {
            if (mChannels.ImageProcessing == null)
                return;
            if (mlockTBExposure)
                return;
            try
            {
                mlockTBExposure = true;
                int Value = (int)((TrackBar)sender).Value;
                if (mExposures != null && Value < mExposures.Length)
                {
                    Value = mExposures[Value]; //Index to value
                    mChannels.ImageProcessing.DetectorParameters.Exposure(Value);
                    mConfiguration.mExposure = Value;
                }
            }
            catch (Exception ex)
            {
                AppLog.LogException(ex);
            }
            finally
            {
                mlockTBExposure = false;
            }
        }

        //************************************************************************
        /// <summary>
        /// Sets the configuration according to the value on the UI
        /// </summary>
        private void trackBarAccumulation_Scroll(object sender, EventArgs e)
        {
            if (mChannels.ImageProcessing == null)
                return;
            if (mLockTBAccumulation)
                return;
            try
            {
                mLockTBAccumulation = true;
                int index = (int)((TrackBar)sender).Value;
                int value = 1 << index; // the values of accumulation are the powers of 2, up till 2^11 so this is an index to value conversion
                mChannels.ImageProcessing.DetectorParameters.Accumulation(value);
                labelAccumulation.Text = mChannels.ImageProcessing.DetectorParameters.Accumulation().ToString() + " X";
                mConfiguration.mAccumulation = value;
            }
            catch (Exception ex)
            {
                AppLog.LogException(ex);
            }
            finally
            {
                mLockTBAccumulation = false;
            }
        }

        //*********************************************************************
        /// <summary>
        /// Sets the flags for doing a full circle of rotation and updates the configuration
        /// </summary>
        private void checkBox360Degree_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (checkBox360Degree.Checked)
                {
                    panelAngle.Enabled = false;
                    mConfiguration.Degree360 = true;
                }
                else
                {
                    panelAngle.Enabled = true;
                    mConfiguration.Degree360 = false;
                }
            }
            catch (Exception ex)
            {
                AppLog.LogException(ex);
            }
        }

        //*****************************************************************************
        /// <summary>
        /// Sets the starting angle in the configuration according to the value on the UI
        /// </summary>
        private void numericUpDownOpeningAngle_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                mConfiguration.StartPosition = numericUpDownStartPosition.Value;
            }
            catch (Exception ex)
            {
                AppLog.LogException(ex);
            }
        }

        //*****************************************************************************
        /// <summary>
        /// Sets the closing angle in the configuration according to the value on the UI
        /// </summary>
        private void numericUpDownClosingAngle_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                mConfiguration.EndPosition = numericUpDownEndPosition.Value;
            }
            catch (Exception ex)
            {
                AppLog.LogException(ex);
            }
        }

        //*****************************************************************************
        /// <summary>
        /// Sets the number of frames to average
        /// </summary>
        private void numericUpDownNoImagesToAverage_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                mConfiguration.AverageFrames = Convert.ToInt32(numericUpDownNoImagesToAverage.Value);
            }
            catch (Exception ex)
            {
                AppLog.LogException(ex);
            }
        }

        //*****************************************************************************
        /// <summary>
        /// Sets whether the form is always shown on top
        /// </summary>
        private void checkBoxOnTop_CheckedChanged(object sender, EventArgs e)
        {
            this.TopMost = checkBoxTopMost.Checked;
        }

        //*****************************************************************************
        /// <summary>
        /// Sets the project name
        /// </summary>
        private void textBoxProjectName_TextChanged(object sender, EventArgs e)
        {
            if (mDesignMode)
                return;

            try
            {
                mProjectName = textBoxProjectName.Text;
                mConfiguration.ProjectName = mProjectName;
                LayoutUI();
            }
            catch (Exception ex)
            {
                mStatus = EStatus.ExternalCancel;
                AppLog.LogException(ex);
            }
        }

        //*****************************************************************************
        /// <summary>
        /// Launches acquisition of shading corrections
        /// </summary>
        private void buttonAcquireShadingCorrection_Click(object sender, EventArgs e)
        {
            try
            {
                if (mChannels != null)
                {
                    DisplayLog("Shading correction dialog launched");

                    // disable the current form whilst shading correction dialog is present
                    this.Enabled = false;

                    // load shading correction dialog
                    mShadingCorrectionDialog = new IpcCircularScan.ShadingCorrectionDialog(this, mChannels);
                    mShadingCorrectionDialog.TopLevel = true;
                    mShadingCorrectionDialog.DialogResult = System.Windows.Forms.DialogResult.None;
                    mShadingCorrectionDialog.ShowDialog();

                    Debug.Print("Result recieved: " + mShadingCorrectionDialog.DialogResult.ToString());

                    DisplayLog("Shading correction dialog closed");

                    // re-enable main user interfaces
                    this.Enabled = true;
                    this.TopLevel = true;

                    // Update UI
                    LayoutUI();
                }
            }
            catch (Exception ex) { AppLog.LogException(ex); }

        }

        #endregion User-Interface Interaction

        #region Initialise, UI setup and background UI functions

        //**********************************************************
        /// <summary>
        /// Sets up UI
        /// </summary>
        private void InitialiseUI()
        {
            // Initialise combobox
            comboBoxAxis.DataSource = AxisListLabels;

            // Look up axis from configuration
            int axisindex = (int)mConfiguration.Axis;
            int selectedindex = System.Array.IndexOf(mAxisListIndex, axisindex);
            comboBoxAxis.SelectedIndex = selectedindex;

            // Set axis limits
            SetAxisLimits();

            //load data from configuration
            textBoxResultsDirectory.Text = mConfiguration.ResultsDirectory;
            textBoxProjectName.Text = mConfiguration.ProjectName;
            numericUpDownNoOfProjections.Value = mConfiguration.NoOfProjections;
            numericUpDownStartPosition.Value = mConfiguration.StartPosition;
            numericUpDownNoImagesToAverage.Value = mConfiguration.AverageFrames;
            checkBox360Degree.Checked = mConfiguration.Degree360;
            if (mConfiguration.Degree360)
                numericUpDownEndPosition.Value = 0;
            else
            {
                decimal x = mConfiguration.EndPosition;
                decimal min = numericUpDownEndPosition.Minimum;
                decimal max = numericUpDownEndPosition.Maximum;
                if (x >= min && x <= max) // Check if in range (needed if axes are changing)
                    numericUpDownEndPosition.Value = mConfiguration.EndPosition;
                else // Else generically set to middle value
                    numericUpDownEndPosition.Value = Convert.ToDecimal(0.50 * (double)(min + max));
            }


            //load current data from Inspect-X
            labelActualKV.Text = mChannels.Xray.XRays.KilovoltsActual().ToString();
            labelActualMA.Text = mChannels.Xray.XRays.MicroampsActual().ToString();

            labelPosX.Text = mChannels.Manipulator.Axis.Position(IpcContract.Manipulator.EAxisName.X).ToString("0.000");
            labelPosY.Text = mChannels.Manipulator.Axis.Position(IpcContract.Manipulator.EAxisName.Y).ToString("0.000");
            labelPosZ.Text = mChannels.Manipulator.Axis.Position(IpcContract.Manipulator.EAxisName.Z).ToString("0.000");
            labelTilt.Text = mChannels.Manipulator.Axis.Position(IpcContract.Manipulator.EAxisName.Tilt).ToString("0.000");
            labelRotate.Text = mChannels.Manipulator.Axis.Position(IpcContract.Manipulator.EAxisName.Rotate).ToString("0.000");
        }

        private void SetAxisLimits()
        {
            // Retrieve start and end limits of axes
            float min = mChannels.Manipulator.Axis.TravelMin(mConfiguration.Axis);
            float max = mChannels.Manipulator.Axis.TravelMax(mConfiguration.Axis);

            // NumericUpDown start position
            numericUpDownStartPosition.Minimum = (decimal)min;
            numericUpDownStartPosition.Maximum = (decimal)max;

            // NumericUpDown End position
            numericUpDownEndPosition.Minimum = (decimal)min;
            numericUpDownEndPosition.Maximum = (decimal)max;

            // Layout User interface
            LayoutUI();
        }

        //************************************************************************
        /// <summary>Helper, setup a control with value and range. If the range
        /// has more than one value, enable adjustment control.</summary>
        private void SetupTrackBarElement(TrackBar aTrackBar, IpcContract.ImageProcessing.intValueAndRange aValue)
        {
            if (aValue == null)
            {
                aTrackBar.Enabled = false;
                return;
            }
            aTrackBar.Enabled = aValue.HasRange;
            InitialiseTrackBar(aTrackBar, aValue.Value, aValue.Min, aValue.Max);
            aTrackBar.TickFrequency = aValue.Step;
        }

        //***********************************************************************
        /// <summary>
        /// Initialises a trackbar with a value and expand range if necessary
        /// </summary>
        private void InitialiseTrackBar(TrackBar aTrackBar, int aValue, int aMin, int aMax)
        {
            // Make sure input is sensible
            if (aValue < aMin)
                aValue = aMin;
            if (aValue > aMax)
                aValue = aMax;

            // Expand range of control
            if (aMax > aTrackBar.Maximum)
                aTrackBar.Maximum = aMax;
            if (aMin < aTrackBar.Minimum)
                aTrackBar.Minimum = aMin;

            // Set new values.
            aTrackBar.Value = aValue;
            aTrackBar.Minimum = aMin;
            aTrackBar.Maximum = aMax;
        }

        //*********************************************************************
        /// <summary>
        ///Gets the entire status from Inspect-X
        /// </summary>
        private void GetStatus()
        {
            try
            {
                if (mChannels.ImageProcessing == null) return;

                IpcContract.ImageProcessing.EntireStatus Status = mChannels.ImageProcessing.GetStatus();
                if (Status != null)
                {
                    DecodeEntireStatus(Status);
                }
            }
            catch (Exception ex)
            {
                AppLog.LogException(ex);
            }
        }

        //***************************************************************
        private delegate void LayoutUIEventHandler();

        //***************************************************************
        /// <summary>Update UI depending on the application's state</summary>
        private void LayoutUI()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke((LayoutUIEventHandler)LayoutUI);
                return;
            }

            // Only enable 360 check box if axis is rotate axis, else disable
            if (mConfiguration.Axis == IpcContract.Manipulator.EAxisName.Rotate)
            {
                checkBox360Degree.Enabled = true;
                checkBox360Degree.Visible = true;
            }
            else
            {
                mConfiguration.Degree360 = false;
                checkBox360Degree.Enabled = false;
                checkBox360Degree.Visible = false;
            }

            if (mApplicationState == EApplicationState.Disconnected)
            {
                //communication panel
                buttonConnect.Enabled = true;
                buttonConnect.Visible = true;
                buttonDisconnect.Enabled = false;
                buttonDisconnect.Visible = false;
                panelHeartBeat1.BackColor = Color.Black;
                panelHeartBeat2.BackColor = Color.Black;
                panelHeartBeat3.BackColor = Color.Black;
                panelHeartBeat4.BackColor = Color.Black;
                panelHeartBeat5.BackColor = Color.Black;
                panelHeartBeat6.BackColor = Color.Black;

                //result directory panel
                panelResultsDirectory.Enabled = false;
                buttonBrowse.Enabled = false;

                //Project name panel
                panelProjectName.Enabled = false;

                //settings panel
                panelImageSettings.Enabled = false;

                //shading correction panel
                panelShadingCorrection.Enabled = false;

                //stop/start buttons
                buttonStart.Enabled = false;
                buttonStart.Visible = true;
                buttonStop.Enabled = false;
                buttonStop.Visible = false;
            }
            else if (mApplicationState == EApplicationState.Connected)
            {
                //communication panel
                panelCommunication.Enabled = true;
                buttonConnect.Enabled = false;
                buttonConnect.Visible = false;
                buttonDisconnect.Enabled = true;
                buttonDisconnect.Visible = true;

                //result directory panel
                panelResultsDirectory.Enabled = true;
                buttonBrowse.Enabled = true;

                //Project name panel
                panelProjectName.Enabled = true;

                //settings panel
                panelImageSettings.Enabled = true;

                //shading correction panel
                panelShadingCorrection.Enabled = true;


                //Angles panel
                if (mConfiguration.Degree360)
                {
                    panelAngle.Enabled = false;
                    checkBox360Degree.Checked = true;
                }
                else
                {
                    panelAngle.Enabled = true;
                    checkBox360Degree.Checked = false;
                }

                //stop/start buttons
                buttonStart.Enabled = SettingsOK() ? true : false;
                buttonStart.Visible = true;
                buttonStop.Enabled = false;
                buttonStop.Visible = false;
            }
            else if (mApplicationState == EApplicationState.Running)
            {
                panelCommunication.Enabled = true;
                buttonConnect.Visible = false;
                buttonConnect.Enabled = false;
                buttonDisconnect.Visible = true;
                buttonDisconnect.Enabled = false;

                panelResultsDirectory.Enabled = false;
                panelImageSettings.Enabled = false;
                panelProjectName.Enabled = false;
                panelShadingCorrection.Enabled = false;

                buttonStop.Visible = true;
                buttonStop.Enabled = true;
                buttonStart.Visible = false;
                buttonStart.Enabled = false;
            }
        }

        #endregion Initialise, UI setup and background UI functions

        #endregion UI Elements

        #region Channel connections

        //**********************************************************************
        /// <summary>Attach to channel and connect any event handlers</summary>
        /// <returns>Connection status</returns>
        private Channels.EConnectionState ChannelsAttach()
        {
            try
            {
                if (mChannels != null)
                {
                    Channels.EConnectionState State = mChannels.Connect();
                    if (State == Channels.EConnectionState.Connected)  // Open channels
                    {
                        // Attach event handlers (as required)

                        if (mChannels.Application != null)
                        {
                            mChannels.Application.mEventSubscriptionHeartbeat.Event +=
                                new EventHandler<CommunicationsChannel_Application.EventArgsHeartbeat>(EventHandlerHeartbeatApp);
                        }

                        if (mChannels.Xray != null)
                        {
                            mChannels.Xray.mEventSubscriptionHeartbeat.Event +=
                                new EventHandler<CommunicationsChannel_XRay.EventArgsHeartbeat>(EventHandlerHeartbeatXRay);
                            mChannels.Xray.mEventSubscriptionEntireStatus.Event +=
                                new EventHandler<CommunicationsChannel_XRay.EventArgsXRayEntireStatus>(EventHandlerXRayEntireStatus);
                        }

                        if (mChannels.Manipulator != null)
                        {
                            mChannels.Manipulator.mEventSubscriptionHeartbeat.Event +=
                                new EventHandler<CommunicationsChannel_Manipulator.EventArgsHeartbeat>(EventHandlerHeartbeatMan);
                            mChannels.Manipulator.mEventSubscriptionManipulatorMove.Event +=
                                new EventHandler<CommunicationsChannel_Manipulator.EventArgsManipulatorMoveEvent>(EventHandlerManipulatorMoveEvent);
                        }

                        if (mChannels.ImageProcessing != null)
                        {
                            mChannels.ImageProcessing.mEventSubscriptionHeartbeat.Event +=
                                new EventHandler<CommunicationsChannel_ImageProcessing.EventArgsHeartbeat>(EventHandlerHeartbeatIP);
                            mChannels.ImageProcessing.mEventSubscriptionImageProcessing.Event +=
                                new EventHandler<IpcContractClientInterface.CommunicationsChannel_ImageProcessing.EventArgsIPEvent>(EventHandlerImageProcessing);
                        }

                    }
                    return State;
                }
            }
            catch (Exception ex) { AppLog.LogException(ex); }
            return Channels.EConnectionState.Error;
        }

        //******************************************************************
        /// <summary>Detach channel and disconnect any event handlers</summary>
        /// <returns>true if OK</returns>
        private bool ChannelsDetach()
        {
            try
            {
                if (mChannels != null)
                {
                    // Detach event handlers
                    if (mChannels.Application != null)
                    {
                        mChannels.Application.mEventSubscriptionHeartbeat.Event -=
                            new EventHandler<CommunicationsChannel_Application.EventArgsHeartbeat>(EventHandlerHeartbeatApp);
                    }

                    if (mChannels.Xray != null)
                    {
                        mChannels.Xray.mEventSubscriptionHeartbeat.Event -=
                            new EventHandler<CommunicationsChannel_XRay.EventArgsHeartbeat>(EventHandlerHeartbeatXRay);
                        mChannels.Xray.mEventSubscriptionEntireStatus.Event -=
                            new EventHandler<CommunicationsChannel_XRay.EventArgsXRayEntireStatus>(EventHandlerXRayEntireStatus);
                    }

                    if (mChannels.Manipulator != null)
                    {
                        mChannels.Manipulator.mEventSubscriptionHeartbeat.Event -=
                            new EventHandler<CommunicationsChannel_Manipulator.EventArgsHeartbeat>(EventHandlerHeartbeatMan);
                        mChannels.Manipulator.mEventSubscriptionManipulatorMove.Event -=
                            new EventHandler<CommunicationsChannel_Manipulator.EventArgsManipulatorMoveEvent>(EventHandlerManipulatorMoveEvent);
                    }

                    if (mChannels.ImageProcessing != null)
                    {
                        mChannels.ImageProcessing.mEventSubscriptionHeartbeat.Event -=
                            new EventHandler<CommunicationsChannel_ImageProcessing.EventArgsHeartbeat>(EventHandlerHeartbeatIP);
                        mChannels.ImageProcessing.mEventSubscriptionImageProcessing.Event -=
                            new EventHandler<IpcContractClientInterface.CommunicationsChannel_ImageProcessing.EventArgsIPEvent>(EventHandlerImageProcessing);
                    }

                    Thread.Sleep(100); // A breather for events to finish!
                    return mChannels.Disconnect(); // Close channels
                }
            }
            catch (Exception ex) { AppLog.LogException(ex); }
            return false;
        }

        #endregion Channel connections

        #region Heartbeat from host

        //*********************************************************************
        /// <summary>
        /// Callback for the application heartbeat message
        /// </summary>
        void EventHandlerHeartbeatApp(object aSender, IpcContractClientInterface.CommunicationsChannel_Application.EventArgsHeartbeat e)
        {
            try
            {
                if (mChannels == null || mChannels.Application == null)
                    return;
                if (this.InvokeRequired)
                    this.BeginInvoke((MethodInvoker)delegate { EventHandlerHeartbeatApp(aSender, e); });
                else
                {
                    int heartbeats = (int)mHeartBeats % (mHeartBeatPanelList.Count + 1);
                    if (heartbeats == 0)
                    {
                        panelHeartBeat1.BackColor = Color.Black;
                        panelHeartBeat2.BackColor = Color.Black;
                        panelHeartBeat3.BackColor = Color.Black;
                        panelHeartBeat4.BackColor = Color.Black;
                        panelHeartBeat5.BackColor = Color.Black;
                        panelHeartBeat6.BackColor = Color.Black;
                    }
                    else
                    {
                        mHeartBeatPanelList[heartbeats - 1].BackColor = Color.Red;
                    }

                    mHeartBeats++;

                    if (mApplicationState == EApplicationState.Connected ||
                        mApplicationState == EApplicationState.Running)
                    {
                        UInt32 kV = mChannels.Xray.XRays.KilovoltsActual();
                        UInt32 uA = mChannels.Xray.XRays.MicroampsActual();
                        labelActualKV.Text = kV.ToString();
                        labelActualMA.Text = uA.ToString();
                    }
                    GetStatus();
                }
            }
            catch (ObjectDisposedException) { } // ignore
            catch (Exception ex) { AppLog.LogException(ex); }
        }

        void EventHandlerHeartbeatXRay(object aSender,
            IpcContractClientInterface.CommunicationsChannel_XRay.EventArgsHeartbeat e)
        { }

        void EventHandlerHeartbeatMan(object aSender,
            IpcContractClientInterface.CommunicationsChannel_Manipulator.EventArgsHeartbeat e)
        { }

        void EventHandlerHeartbeatIP(object aSender,
            IpcContractClientInterface.CommunicationsChannel_ImageProcessing.EventArgsHeartbeat e)
        { }

        void EventHandlerHeartbeatInspection(object aSender,
            IpcContractClientInterface.CommunicationsChannel_Inspection.EventArgsHeartbeat e)
        { }


        //***********************************************************************
        /// <summary>
        /// Breaks down the status into smaller chunks
        /// </summary>
        /// <param name="aStatus"></param>
        private void DecodeEntireStatus(IpcContract.ImageProcessing.EntireStatus aStatus)
        {
            try
            {
                if (aStatus != null)
                {
                    //we are only interested in the parameters' status, so carry on interpreting these
                    DecodeDetectorParametersStatus(aStatus.DetectorParametersStatus);
                }
            }
            catch (Exception ex)
            {
                AppLog.LogException(ex);
            }
        }

        //***************************************************************************************
        /// <summary>
        /// Breaks down the parameter status into its final data
        /// </summary>
        private void DecodeDetectorParametersStatus(IpcContract.ImageProcessing.DetectorParametersStatus aStatus)
        {
            try
            {
                if (aStatus != null)
                {
                    //we are only interested in the binning mode, accumulation and exposure values here
                    //track bars are created programmatically because their values are dependent on the camera type, on which binning mode is selected etc
                    SetupTrackBarElement(trackBarBinning, aStatus.BinningIndex);
                    SetupTrackBarElement(trackBarExposure, aStatus.ExposureIndex);
                    SetupTrackBarElement(trackBarAccumulation, aStatus.AccumulationIndex);

                    if (aStatus.ExposureIndex.StepValues != null)
                        mExposures = (int[])aStatus.ExposureIndex.StepValues.Clone();

                    //now set up the labels for each trackbar
                    int Value = 0;

                    //accumulation
                    mConfiguration.mAccumulation = mChannels.ImageProcessing.DetectorParameters.Accumulation();
                    labelAccumulation.Text = mConfiguration.mAccumulation.ToString() + " X";


                    //binning mode
                    Value = mChannels.ImageProcessing.DetectorParameters.Binning();
                    mConfiguration.mBinning = Value;
                    Value = Convert.ToInt32(Math.Pow((double)2, (double)Value));	// Binning returns exponent of 2
                    labelBinning.Text = Value.ToString() + " X";

                    // Exposure returns the actual value, not the INDEX of the value!
                    Value = mChannels.ImageProcessing.DetectorParameters.Exposure();
                    mConfiguration.mExposure = Value;
                    labelExposure.Text = Value.ToString();
                    if (aStatus.ExposureIndex.StepValues != null)
                    {
                        Value = aStatus.ExposureIndex.StepValues[aStatus.ExposureIndex.Value];
                        labelExposure.Text = Value.ToString() + " ms";
                    }
                    else
                        labelExposure.Text = Value.ToString() + " ms";
                }
            }
            catch (Exception ex)
            {
                AppLog.LogException(ex);
            }
        }


        #endregion Heartbeat from host

        #region Status from host

        //*****************************************************************************************
        /// <summary>
        /// Event handler for checking x-ray status
        /// </summary>
        void EventHandlerXRayEntireStatus(object aSender, IpcContractClientInterface.CommunicationsChannel_XRay.EventArgsXRayEntireStatus e)
        {
            try
            {
                if (mChannels == null || mChannels.Application == null)
                    return;
                if (this.InvokeRequired)
                    this.BeginInvoke((MethodInvoker)delegate { EventHandlerXRayEntireStatus(aSender, e); });
                else
                {
                    switch (e.EntireStatus.XRaysStatus.GenerationStatus.State)
                    {
                        case IpcContract.XRay.GenerationStatus.EXRayGenerationState.Success:
                            DisplayLog("X-rays are stable ");
                            mXraysStable = true;
                            break;
                        case IpcContract.XRay.GenerationStatus.EXRayGenerationState.WaitingForStability:
                            DisplayLog("Waiting for X-ray stability ");
                            mXraysStable = false;

                            // Increment stability counter;
                            mXraysStabilityCounter++;

                            // If stability counter is greater than 1 then must manually check update X-ray Entire status
                            if (mXraysStabilityCounter > 1)
                            {
                                // Manual loop to update X-ray Entire Status until "Success"

                                do
                                {
                                    DisplayLog("Waiting for X-ray stability ");
                                    // First sleep for a small amount of time to allow status updates
                                    Thread.Sleep(100);
                                    // Then get a updated X-ray Entire Status
                                    mXrayEntireStatus = mChannels.Xray.GetXRayEntireStatus();
                                    // Find generation part of Entire Status
                                    mXrayGenerationStatus = mXrayEntireStatus.XRaysStatus.GenerationStatus.State;
                                }
                                while (mXrayGenerationStatus != IpcContract.XRay.GenerationStatus.EXRayGenerationState.Success);

                                DisplayLog("X-rays are stable ");

                                // Once "Success" obtained then set mXraysStable flag to true
                                mXraysStable = true;

                                // Reset stability counter
                                mXraysStabilityCounter = 0;
                            }
                            break;
                        case IpcContract.XRay.GenerationStatus.EXRayGenerationState.NoXRayController:
                            DisplayLog("No X-ray controller ");
                            mXraysStable = false;
                            break;
                        case IpcContract.XRay.GenerationStatus.EXRayGenerationState.StabilityTimeout:
                            DisplayLog("X-ray timed out. Please try again ");
                            mXraysStable = false;
                            break;
                        case IpcContract.XRay.GenerationStatus.EXRayGenerationState.StabilityXRays:
                            break;
                        case IpcContract.XRay.GenerationStatus.EXRayGenerationState.SwitchedOff:
                            DisplayLog("X-rays switch off ");
                            mXraysStable = false;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.LogException(ex);
            }
        }

        //************************************************************
        /// <summary>
        /// callback for the application's manipulator moved event
        /// </summary>
        void EventHandlerManipulatorMoveEvent(object aSender, IpcContractClientInterface.CommunicationsChannel_Manipulator.EventArgsManipulatorMoveEvent e)
        {
            try
            {
                if (mChannels == null || mChannels.Application == null)
                    return;
                if (this.InvokeRequired)
                    this.BeginInvoke((MethodInvoker)delegate { EventHandlerManipulatorMoveEvent(aSender, e); });
                else
                {
                    //every time the manipulator moves update the UI with the latest positions
                    labelPosX.Text = mChannels.Manipulator.Axis.Position(IpcContract.Manipulator.EAxisName.X).ToString("0.000");
                    labelPosY.Text = mChannels.Manipulator.Axis.Position(IpcContract.Manipulator.EAxisName.Y).ToString("0.000");
                    labelPosZ.Text = mChannels.Manipulator.Axis.Position(IpcContract.Manipulator.EAxisName.Z).ToString("0.000");
                    labelRotate.Text = mChannels.Manipulator.Axis.Position(IpcContract.Manipulator.EAxisName.Rotate).ToString("0.000");
                    labelTilt.Text = mChannels.Manipulator.Axis.Position(IpcContract.Manipulator.EAxisName.Tilt).ToString("0.000");

                    Debug.Print(DateTime.Now.ToString("dd/MM/yyyy H:mm:ss.fff") + " : e.MoveEvent=" + e.MoveEvent.ToString());

                    switch (e.MoveEvent)
                    {
                        //display events in the log windows and set flags
                        case IpcContract.Manipulator.EMoveEvent.HomingStarted:
                            DisplayLog("Homing in process ");
                            break;
                        case IpcContract.Manipulator.EMoveEvent.HomingCompleted:
                            mManipulatorHomed = true;
                            DisplayLog("Homing Complete ");
                            break;
                        case IpcContract.Manipulator.EMoveEvent.ManipulatorStartedMoving:
                            DisplayLog("Manipulator started moving");
                            break;
                        case IpcContract.Manipulator.EMoveEvent.GoCompleted:
                            DisplayLog("Manipulator completed moving");
                            mManipulatorMoveComplete = true;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.LogException(ex);
            }

        }

        void EventHandlerIPEvent(object aSender,
            IpcContractClientInterface.CommunicationsChannel_ImageProcessing.EventArgsIPEvent e)
        { }



        //****************************************************************
        public delegate void OnImageCapturedEventHandler(ProjectScan test);

        //****************************************************************
        /// <summary>
        /// When an image is taken, write into the log window
        /// </summary>
        private void OnImageCaptured(ProjectScan test)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke((OnImageCapturedEventHandler)OnImageCaptured, new object[] { test });
                return;
            }
            String info = String.Format(
                "\r\n========================================================" +
                "\r\n     CAPTURE  {0}   " +
                "\r\n========================================================\r\n",
            test.ImagesCaptured.ToString("000"));
            info += test.DumpValues() + "\r\n";
            DisplayLog(info);
        }

        //*******************************************************************
        public delegate void OnTestEndedEventHandler(ProjectScan test);

        //*******************************************************************
        /// <summary>
        /// When the test ends, write into the log window
        /// </summary>
        private void OnTestEnded(ProjectScan test)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke((OnTestEndedEventHandler)OnTestEnded, new object[] { test });
                return;
            }

            String info =
                "\r\n=======================================================" +
                "\r\n TEST COMPLETED                    " +
                "\r\n=======================================================" +
                "\r\n" +
                test.DumpValues() +
                "\r\n=======================================================\r\n";

            DisplayLog(info);
        }


        //**************************************************************************
        /// <summary>
        /// Handler for image processing events
        /// </summary>
        private void EventHandlerImageProcessing(object aSender, IpcContractClientInterface.CommunicationsChannel_ImageProcessing.EventArgsIPEvent e)
        {
            if (mChannels == null || mChannels.ImageProcessing == null)
                return;
            try
            {
                Debug.Print(DateTime.Now.ToString("dd/MM/yyyy H:mm:ss.fff") + " : e.IPEvent.EventType=" + e.IPEvent.EventType.ToString());
                switch (e.IPEvent.EventType)
                {
                    case IpcContract.ImageProcessing.IPEvent.EEventType.AverageComplete:
                        mImageCaptureComplete = true;
                        break;
                    case IpcContract.ImageProcessing.IPEvent.EEventType.SaveImageComplete:
                        mImageSaveComplete = true;
                        break;
                }
            }
            catch (Exception ex)
            {
                AppLog.LogException(ex);
            }
        }

        #endregion Status from host

        #region Settings & Configuration

        //*********************************************************************
        /// <summary>
        /// check if results directory exists
        /// </summary>
        /// <returns></returns>
        private bool SettingsOK()
        {
            bool settingsOk = false;
            try
            {
                settingsOk = Directory.Exists(mConfiguration.ResultsDirectory);
            }
            catch (Exception ex)
            { AppLog.LogException(ex); }
            return settingsOk;
        }

        /// <summary>
        /// Transfer form values to configuration file
        /// </summary>
        private void UserInterfaceElements()
        {
            // Transfer important values from form to configuration file. 
            mConfiguration.Degree360 = checkBox360Degree.Checked;
            if (mConfiguration.Degree360 == true)
            {
                mConfiguration.StartPosition = 0;
                mConfiguration.EndPosition = 360;
            }
            else
            {
                mConfiguration.StartPosition = numericUpDownStartPosition.Value;
                mConfiguration.EndPosition = numericUpDownEndPosition.Value;
            }
            mConfiguration.NoOfProjections = Convert.ToInt32(numericUpDownNoOfProjections.Value);
            mConfiguration.mBinning = trackBarBinning.Value;
            mConfiguration.mAccumulation = Convert.ToInt32(Math.Pow(2.0, trackBarAccumulation.Value));
            mConfiguration.mExposure = trackBarExposure.Value;
            mConfiguration.AverageFrames = Convert.ToInt32(numericUpDownNoImagesToAverage.Value);
        }

        /// <summary>
        /// Invoke/Delegated function to transfer User values to configuration file. 
        /// </summary>
        private void TransferUserFormToConfiguration()
        {
            try
            {
                if (this.InvokeRequired)
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        UserInterfaceElements();
                    });
                else
                {
                    UserInterfaceElements();
                }
            }
            catch (Exception ex) { AppLog.LogException(ex); }
        }


        #endregion Settings

        #region Manipulator functions

        //**********************************************************************
        /// <summary>
        /// sets the target position and moves the manipulator to the target position 
        /// </summary>
        /// <param name="aAxisName">axes to do the moment</param>
        /// <param name="position">the position to go to</param>
        private void MoveManipulator(IpcContract.Manipulator.EAxisName aAxisName, float position)
        {
            if (mStatus != EStatus.OK)
                return;
            try
            {
                if (mChannels != null && mChannels.Manipulator.Axis.Programmable(aAxisName))
                {
                    mManipulatorMoveComplete = false;

                    DisplayLog("Moving Manipulator Axis " + aAxisName.ToString() + " to " + position.ToString());

                    //first set the target position you want to go to
                    mChannels.Manipulator.Axis.Target(aAxisName, position);
                    //move the manipulator to the target position
                    mChannels.Manipulator.Axis.Go(aAxisName);

                    //Wait for the manipulator to finish moving
                    while (mStatus == EStatus.OK && mManipulatorMoveComplete == false)
                        Thread.Sleep(100);

                    DisplayLog(aAxisName.ToString() + " axis position is " + mChannels.Manipulator.Axis.Position(aAxisName).ToString() + "\n");
                }
            }
            catch (Exception ex)
            {
                AppLog.LogException(ex);
                mStatus = EStatus.InternalError;
            }
        }

        //**********************************************************************
        /// <summary>
        /// Moves the manipulator to the target position 
        /// Useful for multiple axes at once
        /// Public function for use with shading correction module. 
        /// </summary>
        /// <param name="aAxisName">axes to do the moment</param>
        public void MoveManipulator(IpcContract.Manipulator.EAxisName aAxisName)
        {
            if (mStatus != EStatus.OK)
                return;
            try
            {
                // note there is no check to see whether axis is programmable
                if (mChannels != null)
                {
                    mManipulatorMoveComplete = false;

                    DisplayLog("Moving Manipulator Axis " + aAxisName.ToString());

                    //move the manipulator to the target position
                    mChannels.Manipulator.Axis.Go(aAxisName);

                    //Wait for the manipulator to finish moving
                    while (mStatus == EStatus.OK && mManipulatorMoveComplete == false)
                        Thread.Sleep(100);

                    DisplayLog(aAxisName.ToString() + " stopped moving\n");
                }
            }
            catch (Exception ex)
            {
                AppLog.LogException(ex);
                mStatus = EStatus.InternalError;
            }
        }

        //*******************************************************************
        /// <summary>
        /// Calculates the rotation degree based on the number of projections
        /// </summary>
        private decimal CalculateDegreeOfRotation()
        {
            // Decimal value for accuracy
            decimal degreeOfTurning = 0;

            if (mConfiguration.NoOfProjections != 0)
            {
                decimal angleOfInterest = CalulateAngleOfInterest();
                degreeOfTurning = Convert.ToDecimal(angleOfInterest / mConfiguration.NoOfProjections);
            }
            else
            {
                degreeOfTurning = 0;
            }
            mConfiguration.PositionalIncrement = degreeOfTurning;
            return degreeOfTurning;
        }

        //********************************************************************
        /// <summary>
        /// Calculates the error that is introduced when rounding
        /// </summary>
        private decimal CalculateErrorOfRotation(decimal degreeOfTurning, int numberOfProjections)
        {
            decimal error = 0;
            decimal angle = CalulateAngleOfInterest();

            if (degreeOfTurning * numberOfProjections < angle)
                error = angle - (degreeOfTurning * numberOfProjections);

            return error;
        }

        //**********************************************************************
        /// <summary>
        /// Calculates the angle of interest, which is the difference between the start and closing angle
        /// always clockwise direction!
        /// </summary>
        private decimal CalulateAngleOfInterest()
        {
            decimal angle;

            if (mConfiguration.Degree360 == true)
            {
                angle = 360;
            }
            else
            {
                if (mConfiguration.StartPosition < mConfiguration.EndPosition)
                {
                    angle = mConfiguration.EndPosition - mConfiguration.StartPosition;
                }
                else
                {
                    //start angle is to the left of zero, Closing angle is to the right of zero
                    //if start and end are the same, it will treat it as a whole circle
                    angle = (360 - mConfiguration.StartPosition) + mConfiguration.EndPosition;
                }
            }
            // Write to configuration file
            mConfiguration.TotalDisplacement = angle;
            // Return angle
            return angle;
        }

        #endregion Manipulator functions

        #region Image Processing functions
        //******************************************************************
        /// <summary>Capture image with N averages</summary>
        /// <param name="aRecursion">Number of averages</param>
        /// <param name="aShowProgressBar">Show progress bar if necessary!</param>
        public void ImageCapture(int aRecursion, bool aShowProgressBar)
        {
            if (mStatus != EStatus.OK)
                return; // Nothing to be done.
            if (mChannels == null)
                return; // No channels are open
            try
            {
                // reset flag
                mImageCaptureComplete = false;
                // capture average image
                mChannels.ImageProcessing.Image.Average(aRecursion, false);

                // Wait for it to finish
                while (mStatus == EStatus.OK && mImageCaptureComplete == false)
                    Thread.Sleep(25);
            }
            catch (Exception ex)
            {
                AppLog.LogException(ex);
                mStatus = EStatus.InternalError;
            }
        }

        //*************************************************************************
        /// <summary>Save image to test directory </summary>
        private void ImageSave()
        {
            String fileName = mConfiguration.ProjectDirectory + @"\" + mConfiguration.ProjectName + @"_" + (mScan.ImagesCaptured + 1).ToString("0000") + @".tif";
            if (mStatus != EStatus.OK)
                return; // Nothing to be done.
            if (mChannels == null)
                return; // Ops!
            try
            {
                if (File.Exists(fileName))
                    File.Delete(fileName);

                mImageSaveComplete = false;
                //save the current image displayed to disk
                mChannels.ImageProcessing.Image.SaveAsTiff(fileName, true, false, true);// monochrome, no tone curve, 16-bits

                //wait for it to finish
                while (mStatus == EStatus.OK && mImageSaveComplete == false)
                    Thread.Sleep(25);
            }
            catch (Exception ex)
            {
                AppLog.LogException(ex);
                mStatus = EStatus.InternalError;
            }
        }

        //*************************************************************************
        /// <summary>
        /// Save an image
        /// </summary>
        /// <param name="aFilename"> Filename for image to be saved </param>
        public void ImageSave(string aFilename)
        {
            if (mStatus != EStatus.OK)
                return; // Nothing to be done.
            if (mChannels == null)
                return; // Ops!
            try
            {
                if (File.Exists(aFilename))
                    File.Delete(aFilename);

                mImageSaveComplete = false;
                //save the current image displayed to disk
                mChannels.ImageProcessing.Image.SaveAsTiff(aFilename, true, false, true);// monochrome, no tone curve, 16-bits

                //wait for it to finish
                while (mStatus == EStatus.OK && mImageSaveComplete == false)
                    Thread.Sleep(25);
            }
            catch (Exception ex)
            {
                AppLog.LogException(ex);
                mStatus = EStatus.InternalError;
            }
        }


        #endregion Image Processing functions

        #region X-ray functions

        /// <summary>
        /// Routine for switching X-rays on and ensuring stability
        /// </summary>
        public void XraysOnStabilise()
        {
            // Reset flag from previous use
            mXraysStable = false;

            // Check if X-rays are on and if so wait for stability
            if (mChannels.Xray.XRays.GenerationActual())
            {
                // Manually check if X-rays are stable. If already stable then no-flag will have been raised. 

                // Get a updated X-ray Entire Status
                mXrayEntireStatus = mChannels.Xray.GetXRayEntireStatus();
                // Find generation part of Entire Status
                mXrayGenerationStatus = mXrayEntireStatus.XRaysStatus.GenerationStatus.State;
                // If stable then manually set flag
                if (mXrayGenerationStatus == IpcContract.XRay.GenerationStatus.EXRayGenerationState.Success)
                    mXraysStable = true;
            }
            // Else if X-rays are not already on, then switch them on
            else
                mChannels.Xray.XRays.GenerationDemand(true);

            //Wait for the X-rays to stabilise before proceeding
            while (mStatus == EStatus.OK && mXraysStable == false)
                Thread.Sleep(200);

        }

        #endregion X-ray functions

        #region Output

        /// <summary>
        /// Create directory
        /// </summary>
        public void CreateDirectory()
        {
            // Create folders for test results
            mConfiguration.ProjectDirectory = mConfiguration.ResultsDirectory + @"\" + mConfiguration.ProjectName + @"_" + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss-tt");

            if (!Directory.Exists(mConfiguration.ProjectDirectory))
            {
                Directory.CreateDirectory(mConfiguration.ProjectDirectory);
            }
            else
            {
                mConfiguration.ProjectDirectory += "2"; //just in case the folder already exists
                Directory.CreateDirectory(mConfiguration.ProjectDirectory);
            }
            Configuration.Save(mConfigurationDirectory, mConfiguration);
        }


        //*********************************************************************
        /// <summary>
        /// Dumps info onto text box
        /// </summary>
        /// <param name="info"></param>
        public void DisplayLog(String info)
        {
            if (this.InvokeRequired)
                this.Invoke((MethodInvoker)delegate
                {
                    textBoxLog.Text += info + "\r\n";
                    OutputLogText += DateTime.Now.ToString("dd/MM/yyyy H:mm:ss.fff") + " : " + info + "\r\n";
                    textBoxLog.SelectionStart = textBoxLog.Text.Length;
                    textBoxLog.ScrollToCaret();
                });
            else
            {

                textBoxLog.Text += info + "\r\n";
                OutputLogText += DateTime.Now.ToString("dd/MM/yyyy H:mm:ss.fff") + " : " + info + "\r\n";
                textBoxLog.SelectionStart = textBoxLog.Text.Length;
                textBoxLog.ScrollToCaret();
            }
        }

        /// <summary>
        /// Function for clearing the output display box. Note the Output Log is not cleared. 
        /// </summary>
        public void DisplayLogClear()
        {
            if (this.InvokeRequired)
                this.Invoke((MethodInvoker)delegate
                {
                    textBoxLog.Clear();
                });
            else
            {
                textBoxLog.Clear();
            }
        }

        /// <summary>
        /// Prints output onto a log file
        /// </summary>
        public void OutputLogFile()
        {
            try
            {
                // Filename
                string outputfilename = mConfiguration.ProjectDirectory + @"\" + mConfiguration.ProjectName + @".log";

                // open file
                System.IO.StreamWriter logfile = new StreamWriter(outputfilename, true);

                // Write to file
                logfile.Write(OutputLogText);

                //Close file
                logfile.Close();
            }
            catch (Exception ex)
            {
                AppLog.LogException(ex);
            }
        }

        /// <summary>
        /// Prints output onto a log file
        /// </summary>
        /// <param name="aFilename"> Filename (note that .log will automatically be appended)</param>
        public void OutputLogFile(string aFilename)
        {
            try
            {
                // Filename
                string outputfilename = aFilename + @".log";

                // open file
                System.IO.StreamWriter logfile = new StreamWriter(outputfilename, true);

                // Write to file
                logfile.Write(OutputLogText);

                //Close file
                logfile.Close();
            }
            catch (Exception ex)
            {
                AppLog.LogException(ex);
            }
        }

        #endregion Output

        #region Circular Scan

        /// <summary>
        /// Modulated Circular Scan Routine
        /// </summary>
        private void CircularScanRoutine()
        {
            // start a new test
            mStop = false;
            // This will record time started and reset the number of images captured
            mScan = new ProjectScan();

            #region Initialise variables

            // Transfer Values from user form to configuration file
            TransferUserFormToConfiguration();

            //initial position = start angle 
            float position = (float)mConfiguration.StartPosition;

            //angle to be rotated each time
            decimal degreeOfTurning = CalculateDegreeOfRotation();
            //correction when the angles don't add up to a full circle
            decimal calculationError = CalculateErrorOfRotation(degreeOfTurning, mConfiguration.NoOfProjections);
            // current rotate axis position
            float rotateaxisposition = 0;
            //flag for the manipulator homed
            mManipulatorHomed = mChannels.Manipulator.Axis.Homed(IpcContract.Manipulator.EAxisName.All);
            //flag for manipulator moving
            mManipulatorMoveComplete = false;

            //open .ang file for output
            string angfilename = mConfiguration.ProjectDirectory + @"\" + mConfiguration.ProjectName + @".ang";
            mAngFile = new System.IO.StreamWriter(angfilename, false);
            // write first line
            mAngFile.WriteLine("Proj\tAngle(deg)");

            #endregion

            #region Parameter output
            // Some initial output to log
            DisplayLog("####################################################");
            DisplayLog("Parameters for this scan are as follows:");
            DisplayLog("Manipulator X position:\t" + mChannels.Manipulator.Axis.Position(IpcContract.Manipulator.EAxisName.X).ToString());
            DisplayLog("Manipulator Y position:\t" + mChannels.Manipulator.Axis.Position(IpcContract.Manipulator.EAxisName.Y).ToString());
            DisplayLog("Manipulator Mag/Z position:\t" + mChannels.Manipulator.Axis.Position(IpcContract.Manipulator.EAxisName.Magnification).ToString());
            DisplayLog("Manipulator Tilt position:\t" + mChannels.Manipulator.Axis.Position(IpcContract.Manipulator.EAxisName.Tilt).ToString());
            DisplayLog("Manipulator Detector position:\t" + mChannels.Manipulator.Axis.Position(IpcContract.Manipulator.EAxisName.Detector).ToString());
            DisplayLog("Number of Projections:\t" + mConfiguration.NoOfProjections.ToString());
            DisplayLog("Starting angle:\t" + mConfiguration.StartPosition.ToString());
            DisplayLog("Rotation angle:\t" + mConfiguration.TotalDisplacement.ToString());
            DisplayLog("End angle:\t" + mConfiguration.EndPosition.ToString());
            DisplayLog("X-ray kV:\t" + mChannels.Xray.XRays.KilovoltsDemand().ToString() + "kV");
            DisplayLog("X-ray uA:\t" + mChannels.Xray.XRays.MicroampsDemand().ToString() + "uA");
            DisplayLog("Exposure:\t" + mChannels.ImageProcessing.DetectorParameters.Exposure().ToString() + "ms");
            DisplayLog("####################################################");
            #endregion Parameter output

            // Set operation mode to external
            mChannels.Xray.Controller.OperationMode(IpcContract.EOperationMode.External);

            // Place in live imaging mode
            mChannels.ImageProcessing.Image.Live();

            // Switch X-rays on and wait for stability
            XraysOnStabilise();

            #region Capture loop
            // Capture and XrayMonitoring loop
            while (!mStop && mStatus == EStatus.OK)
            {
                // If X-rays are not stable, then wait until they have returned to stability
                if (!mXraysStable)
                {
                    while (!mXraysStable)
                        Thread.Sleep(100);
                }
                // Check if Manipulator not homed
                if (!mManipulatorHomed)
                {
                    DialogResult dialogResult = MessageBox.Show("The manipulator axes have not been homed. The movement cannot be started until all axes have been homed. Do you want to home all axes?",
                        "Warning", MessageBoxButtons.YesNo);
                    if (dialogResult == DialogResult.Yes)
                    {
                        mChannels.Manipulator.Axis.Home(IpcContract.Manipulator.EAxisName.All, true, false);
                        //Check if the manipulator has been homed. If it hasn't then wait for it to do so
                        while (mStatus == EStatus.OK && mManipulatorHomed == false)
                            Thread.Sleep(100);
                    }
                    else if (dialogResult == DialogResult.No)
                    {
                        //without homing we cannot proceed so tidy up
                        mScan.Result = ETestResult.FailedStopped;
                        OnTestEnded(mScan.Clone());
                        mChannels.Xray.Controller.OperationMode(IpcContract.EOperationMode.Manual);
                        mChannels.Xray.XRays.GenerationDemand(false);
                        mApplicationState = EApplicationState.Connected;
                        LayoutUI();
                        return;
                    }
                }

                //go to next position
                MoveManipulator(mConfiguration.Axis, position);
                if (mStatus != EStatus.OK)
                    return;

                // grab the image
                ImageCapture(mConfiguration.AverageFrames, true);
                if (mStatus != EStatus.OK)
                    break;

                //save image
                ImageSave();
                if (mStatus != EStatus.OK)
                    break;

                mScan.LastImageTimestamp = DateTime.Now;
                ++mScan.ImagesCaptured;

                // look up current rotate axis position
                rotateaxisposition = mChannels.Manipulator.Axis.Position(mConfiguration.Axis);

                // print line to ang file
                mAngFile.WriteLine(mScan.ImagesCaptured + @": " + String.Format(rotateaxisposition.ToString(), "F3"));

                //is it the last rotation and do the rotations add up to 360?
                if (calculationError > 0 && mScan.ImagesCaptured == mConfiguration.NoOfProjections)
                    position = position + (float)degreeOfTurning + (float)calculationError; //it rotations don't add up to 360 then pad it to 360
                else
                    position += (float)degreeOfTurning;

                // is it the last image captured?
                if (mScan.ImagesCaptured > mConfiguration.NoOfProjections)
                {
                    mScan.Result = ETestResult.Succeeded;
                    mScan.TimeFinished = DateTime.Now;
                    mStop = true;
                }

                // Display Info of the capture
                OnImageCaptured(mScan.Clone());
            }
            #endregion Capture loop

            #region XtekCT file
            // Create XtekCT file
            XtekCT xtekctfile = new XtekCT(mChannels, mConfiguration);
            xtekctfile.CreateXtekCTFile();
            #endregion XtekCT file

            //tidy up
            OnTestEnded(mScan.Clone());
            mAngFile.Close();
            mChannels.Xray.Controller.OperationMode(IpcContract.EOperationMode.Manual);
            mChannels.Xray.XRays.GenerationDemand(false);

            // Produce Log file
            OutputLogFile();



        }

        #endregion Circular Scan

        #region XCT Scan

        void XCTScan(string ProjectName)
        {
            // Project name
            mConfiguration.ProjectName = ProjectName;

            // Create Directory 
            CreateDirectory();

            // Clear text boxes
            DisplayLogClear();

            // Layout UI
            LayoutUI();

            // Run Circular Scan
            CircularScanRoutine();
        }

        #endregion XCT Scan

        #region Main routine
        //****************************************************************
        /// <summary>
        /// Main routine function
        /// </summary>
        private void WorkingThread()
        {
            // Change statuses
            mApplicationState = EApplicationState.Running;
            mStatus = EStatus.OK;
            // Layout UI Appropriately
            LayoutUI();

            // Run XCT Scan
            XCTScan(mProjectName);

            // Change statuses when process is finished
            mApplicationState = EApplicationState.Connected;

            //Reset UI
            LayoutUI();
        }

        /// <summary>
        /// Abort function
        /// </summary>
        private void UserAbort()
        {
            // If no existing problem, then set status to cancel
            if (mStatus == EStatus.OK)
            {
                DisplayLog("Process aborted by the user");
                // Set stop flags
                mStop = true;
                mStatus = EStatus.ExternalCancel;
                mChannels.Manipulator.Axes.Stop(); //stop movement on all axes
                mChannels.Xray.XRays.GenerationDemand(false); //turn x-rays off
                mApplicationState = EApplicationState.Connected;//stay connected to Inspect-X
                LayoutUI();
            }
        }

        #endregion Main routine



    }
}
