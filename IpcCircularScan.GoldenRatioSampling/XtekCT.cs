using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using IpcContractClientInterface;
using System.Diagnostics;

using AppLog = IpcUtil.Logging;

namespace IpcCircGoldenRatioScan
{
    class XtekCT
    {
        #region Form variables

        /// <summary>Collection of all IPC channels, this object always exists.</summary>
        private IpcContractClientInterface.Channels mChannels = null;

        /// <summary>Current configuration.</summary>
        public Configuration mConfiguration = null;

        #endregion IPC variables

        #region XtekCT file variables
        // List of constants and to be assigned variables for XtekCT file
        public string Name = "";
        const string InputSeparator = "_";
        const string OutputSeparator = "_";
        const string InputFolderName = "";
        public string OutputFolderName = "";
        public int VoxelsX = 0;
        public int VoxelsY = 0;
        public int VoxelsZ = 0;
        public double VoxelSizeX = 0;
        public double VoxelSizeY = 0;
        public double VoxelSizeZ = 0;
        const int OffsetX = 0;
        const int OffsetY = 0;
        const int OffsetZ = 0;
        public double SrcToObject = 0;
        public double SrcToDetector = 0;
        public double MaskRadius = 1;
        public int DetectorPixelsX = 0;
        public int DetectorPixelsY = 0;
        public double DetectorPixelSizeX = 0;
        public double DetectorPixelSizeY = 0;
        const double DetectorOffsetX = 0.0;
        const double DetectorOffsetY = 0;
        const double CentreOfRotationTop = 0.0;
        const double CentreOfRotationBottom = 0.0;
        const double WhiteLevel = 60000.0;
        const double Scattering = 0.0;
        const double CoefX4 = 0;
        const double CoefX3 = 0;
        const double CoefX2 = 0;
        const double CoefX1 = 1;
        const double CoefX0 = 0;
        const double Scale = 1;
        const double RegionStartX = 0;
        const double RegionStartY = 0;
        public int RegionPixelsX = 2000;
        public int RegionPixelsY = 2000;
        public int Projections = 1800;
        public decimal InitialAngle = 0;
        public decimal AngularStep = 0;
        public int FilterType = 0;
        public double CutOffFrequency = 2.4999999627471;
        const double Exponent = 1.0;
        const double Normalisation = 1.0;
        public int InterpolationType = 0;
        const int MedianFilterKernelSize = 1;
        public double Scaling = 1000.0;
        const string OutputUnits = @"/m";
        const string Units = "mm";
        public int AutomaticCentreOfRotation = 0;
        public int OutputType = 1;
        const int ImportConversion = 0;
        const int AutoScalingType = 0;
        const double ScalingMinimum = 0;
        const double ScalingMaximum = 1;
        const double LowPercentile = 0.200000002980232;
        const double HighPercentile = 99.8000030517578;
        public double XraykV = 0;
        public double XrayuA = 0;
        #endregion XtekCT File variables

        #region Additional Calculation Variables
        
        private double GeometricMagnification = 1.0;
        private int Binning = 0;
        
        #endregion Additional Calculation Variables


        /// <summary>
        /// Constructor taking Channels and Configuration
        /// </summary>
        /// <param name="aChannels"></param>
        /// <param name="aConfiguration"></param>
        public XtekCT(IpcContractClientInterface.Channels aChannels, Configuration aConfiguration)
        {
            mChannels = aChannels;
            mConfiguration = aConfiguration;
        }

        /// <summary>
        /// Function for setting values of parameters needed in XTekCT File
        /// </summary>
        private void SetValues()
        {
            SrcToObject = (double)mChannels.Manipulator.Axis.Position(IpcContract.Manipulator.EAxisName.Magnification)
                + (double)mChannels.ImageProcessing.Geometry.DistanceSourceToMagnificationZero();
            SrcToDetector = mChannels.ImageProcessing.Geometry.DistanceSourceToDetector();

            GeometricMagnification = SrcToDetector / SrcToObject;
            Binning = Convert.ToInt32(Math.Pow(2.0, (double)mChannels.ImageProcessing.DetectorParameters.Binning()));

            Name = mConfiguration.ProjectName;
            OutputFolderName = mConfiguration.ProjectName;
            VoxelsX = mChannels.ImageProcessing.Detector.Width();
            VoxelsY = mChannels.ImageProcessing.Detector.Width();
            VoxelsZ = mChannels.ImageProcessing.Detector.Height();

            DetectorPixelsX = mChannels.ImageProcessing.Detector.Width();
            DetectorPixelsY = mChannels.ImageProcessing.Detector.Height();

            DetectorPixelSizeX = mChannels.ImageProcessing.Detector.Resolution() / 1000;
            DetectorPixelSizeY = mChannels.ImageProcessing.Detector.Resolution() / 1000;

            VoxelSizeX = DetectorPixelSizeX / GeometricMagnification;
            VoxelSizeY = DetectorPixelSizeX / GeometricMagnification;
            VoxelSizeZ = DetectorPixelSizeY / GeometricMagnification;

            MaskRadius = 0.5 * DetectorPixelsX * DetectorPixelSizeX / GeometricMagnification;

            RegionPixelsX = DetectorPixelsX;
            RegionPixelsY = DetectorPixelsY;
            Projections = mConfiguration.mNoProjectionsAcquired;
            InitialAngle = mConfiguration.StartPosition;
            AngularStep = mConfiguration.PositionalIncrement;
            FilterType = 0;
            CutOffFrequency = 0.5 / DetectorPixelSizeX;

            InterpolationType = 0;

            Scaling = 1000.0;

            AutomaticCentreOfRotation = 0;
            OutputType = 1;

            XraykV = mChannels.Xray.XRays.KilovoltsDemand();
            XrayuA = mChannels.Xray.XRays.MicroampsDemand();
        }

        /// <summary>
        /// Function to Write parameters to XtekCT file
        /// </summary>
        private void WriteXtekCTFile()
        {
            // File name
            string xtekctfilename = mConfiguration.ProjectDirectory + @"\" + mConfiguration.ProjectName + @".xtekct";
            // Create XTekCT file
            System.IO.StreamWriter mXtekCTFile = new System.IO.StreamWriter(xtekctfilename, false);

            // Write output
            // Write each line, line by line

            mXtekCTFile.Write
            (
             "[XTekCT]" + "\n" +
             "Name=" + Name + "\n" +
             "InputSeparator=" + InputSeparator.ToString() + "\n" +
             "OutputSeparator=" + OutputSeparator + "\n" +
             "InputFolderName=" + InputFolderName + "\n" +
             "OutputFolderName=" + OutputFolderName + "\n" +
             "VoxelsX=" + VoxelsX.ToString() + "\n" +
             "VoxelsY=" + VoxelsY.ToString() + "\n" +
             "VoxelsZ=" + VoxelsZ.ToString() + "\n" +
             "VoxelSizeX=" + VoxelSizeX.ToString() + "\n" +
             "VoxelSizeY=" + VoxelSizeY.ToString() + "\n" +
             "VoxelSizeZ=" + VoxelSizeZ.ToString() + "\n" +
             "OffsetX=" + OffsetX.ToString() + "\n" +
             "OffsetY=" + OffsetY.ToString() + "\n" +
             "OffsetZ=" + OffsetZ.ToString() + "\n" +
            "SrcToObject=" + SrcToObject.ToString() + "\n" +
            "SrcToDetector=" + SrcToDetector.ToString() + "\n" +
            "MaskRadius=" + MaskRadius.ToString() + "\n" +
            "DetectorPixelsX=" + DetectorPixelsX.ToString() + "\n" +
            "DetectorPixelsY=" + DetectorPixelsY.ToString() + "\n" +
            "DetectorPixelSizeX=" + DetectorPixelSizeX.ToString() + "\n" +
            "DetectorPixelSizeY=" + DetectorPixelSizeY.ToString() + "\n" +
            "DetectorOffsetX=" + DetectorOffsetX.ToString() + "\n" +
            "DetectorOffsetY=" + DetectorOffsetY.ToString() + "\n" +
            "CentreOfRotationTop=" + CentreOfRotationTop.ToString() + "\n" +
            "CentreOfRotationBottom=" + CentreOfRotationBottom.ToString() + "\n" +
            "WhiteLevel=" + WhiteLevel.ToString() + "\n" +
            "Scattering=" + Scattering.ToString() + "\n" +
            "CoefX4=" + CoefX4.ToString() + "\n" +
            "CoefX3=" + CoefX3.ToString() + "\n" +
            "CoefX2=" + CoefX2.ToString() + "\n" +
            "CoefX1=" + CoefX1.ToString() + "\n" +
            "CoefX0=" + CoefX0.ToString() + "\n" +
            "Scale=" + Scale.ToString() + "\n" +
            "RegionStartX=" + RegionStartX.ToString() + "\n" +
            "RegionStartY=" + RegionStartY.ToString() + "\n" +
            "RegionPixelsX=" + RegionPixelsX.ToString() + "\n" +
            "RegionPixelsY=" + RegionPixelsY.ToString() + "\n" +
            "Projections=" + Projections.ToString() + "\n" +
            "InitialAngle=" + InitialAngle.ToString() + "\n" +
            "AngularStep=" + AngularStep.ToString() + "\n" +
            "FilterType=" + FilterType.ToString() + "\n" +
            "CutOffFrequency=" + CutOffFrequency.ToString() + "\n" +
            "Exponent=" + Exponent.ToString() + "\n" +
            "Normalisation=" + Normalisation.ToString() + "\n" +
            "InterpolationType=" + InterpolationType.ToString() + "\n" +
            "Scaling=" + Scaling.ToString() + "\n" +
            "OutputUnits=" + OutputUnits.ToString() + "\n" +
            "Units=" + Units.ToString() + "\n" +
            "AutomaticCentreOfRotation=" + AutomaticCentreOfRotation.ToString() + "\n" +
            "OutputType=" + OutputType.ToString() + "\n" +
            "ImportConversion=" + ImportConversion.ToString() + "\n" +
            "AutoScalingType=" + AutoScalingType.ToString() + "\n" +
            "LowPercentile=" + LowPercentile.ToString() + "\n" +
            "HighPercentile=" + HighPercentile.ToString() + "\n" +
            "[Xrays]" + "\n" +
            "XraykV=" + XraykV.ToString() + "\n" +
            "XrayuA=" + XrayuA.ToString()
           );

            // Close file once done. 
            mXtekCTFile.Close();
        }

        /// <summary>
        /// Public function for creating XtekCT files.
        /// Sets appropriate values and prints to file. 
        /// </summary>
        public void CreateXtekCTFile()
        {
            try
            {
                // Set values
                SetValues();

                // Create file
                WriteXtekCTFile();

                // Test file
                WriteXtekCTFile_Test();
            }
            catch (Exception ex)
            {
                AppLog.LogException(ex);
            }
        }

        private void WriteXtekCTFile_Test()
        {
            // File name
            string xtekctfilename = mConfiguration.ProjectDirectory + @"\" + mConfiguration.ProjectName + @".xtekct.test";
            // Create Test XTekCT file
            System.IO.StreamWriter mXtekCTFile = new System.IO.StreamWriter(xtekctfilename, false);

            // Write output
            // Write each line, line by line

            mXtekCTFile.Write
            (
             "[XTekCT-Test]" + "\n" +
             "Name=" + Name + "\n" +
            "SrcToObject="+ SrcToObject.ToString() + "\n" +
            "SrcToDetector=" + SrcToDetector.ToString() + "\n" +

            "GeometricMagnification=" + GeometricMagnification.ToString() + "\n" +
            "Binning=" + Binning.ToString() + "\n" + 
            "mChannels.ImageProcessing.Detector.Width()="+mChannels.ImageProcessing.Detector.Width().ToString() + "\n" +
            "mChannels.ImageProcessing.Detector.Height()="+mChannels.ImageProcessing.Detector.Height().ToString() + "\n" +
            "mChannels.ImageProcessing.Detector.Resolution()="+mChannels.ImageProcessing.Detector.Resolution().ToString() + "\n" +
            "DistanceSourceToMagnificationZero()="+mChannels.ImageProcessing.Geometry.DistanceSourceToMagnificationZero().ToString()+ "\n" +
            "DistanceSourceToDetector()="+mChannels.ImageProcessing.Geometry.DistanceSourceToDetector().ToString() + "\n"
           );

            // Close file once done. 
            mXtekCTFile.Close();
        }

    }
}
