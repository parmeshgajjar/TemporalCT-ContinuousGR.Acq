using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using IpcUtil;


namespace IpcCircGoldenRatioScan
{
	[Serializable]
	public class Configuration
	{
		/// <summary> The name of the file associated with this data </summary>
		public static string mFileName = "Configuration.xml";
		/// <summary> The path where the results will be saved </summary>
		public string mPath = "";
		/// <summary> The name of the directory where all results of this application will be saved  </summary>
		public string ResultsDirectory = "";
		/// <summary>This allows the user to prefix a user friendly name to a particular project. Otherwise only time stamp is used </summary>
		public string ProjectName = "[Untitled]";
		/// <summary> The name of the folder inside the ResultsDirectory where all the images will be saved  </summary>
		public string ProjectDirectory = "";
        /// <summary> The axis to be moved  </summary>
        public IpcContract.Manipulator.EAxisName Axis = IpcContract.Manipulator.EAxisName.Rotate;
		/// <summary> Flag to signal if it is a 360 degree movement </summary>
		public bool Degree360 = true;
		/// <summary> The position at which movement starts </summary>
		public decimal StartPosition = 0;
		/// <summary> The position at which the movement ends </summary>
		public decimal EndPosition = 360;
		/// <summary> The number of projection images required </summary>
		public Int32 NoOfProjections = 4;
        /// <summary> Positional Increment </summary>
        public decimal PositionalIncrement = (decimal) IpcCircularScanForm_goldenRatioSampling.gAng;
        /// <summary> Total displacement through which sample will be moved </summary>
        public decimal TotalDisplacement = 0;
		/// <summary> The number of frames taken for each projection </summary>
		public Int32 AverageFrames = 1;
        /// <summary> Current Accumulation </summary>
        public Int32 mAccumulation;
		/// <summary> Imaging binning mode </summary>
		public int mBinning;
		/// <summary> Available exposures </summary>
		public Int32[] Exposure;
		/// <summary> Current exposure </summary>
		public Int32 mExposure;
        
		public Configuration()
		{ }

		public Configuration(Configuration configuration)
		{
			ResultsDirectory = configuration.ResultsDirectory;
		}

		//*************************************************************************
		/// <summary> Creates a configuration object by reading from a file </summary>
		/// <param name="directory">Directory where the Configuration.xml file is located</param>
		public Configuration(string directory)
		{
			mPath = directory + @"\" + mFileName;
			if (!File.Exists(mPath))
			{
				Configuration c = new Configuration();
				Configuration.Save(mPath, c);
			}

			Configuration configuration = Configuration.Load(mPath);
			ResultsDirectory = configuration.ResultsDirectory;
		}

		//******************************************************************
		/// <summary> Saves the settings to a file </summary>
		/// <param name="fullPath"> the full path to the file </param>
		/// <param name="configuration"> the settings to save </param>
		/// <returns> true/false if succeeds/fails </returns>
		public static bool Save(string fullPath, Configuration configuration)
		{
			return IpcUtilities.SaveXmlFile(configuration, fullPath);
		}

		//*************************************************************************
		/// <summary> Load the configuration from file </summary>
		/// <param name="fullPath"> the full path to the xml file </param>
		/// <returns> guaranteed to return a configuration object </returns>
		public static Configuration Load(string fullPath)
		{
			Configuration configuration = null;
			try
			{
				configuration = IpcUtilities.LoadXmlFile<Configuration>(fullPath);
			}
			catch
			{
			}

			//make sure it always return a valid configuration
			if (configuration == null)
				configuration = new Configuration();
			return configuration;
		}
	}
}
