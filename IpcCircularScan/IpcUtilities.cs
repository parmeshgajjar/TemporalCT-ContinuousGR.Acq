using System;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.ComponentModel;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;

public class IpcUtilities
{
	//*****************************************************************************
	/// <summary>
	/// Load the XML file into the object of the type specified
	/// </summary>
	/// <typeparam name="T">Type of object to create</typeparam>
	/// <param name="filePath">XML file path</param>
	/// <returns>Object of specified type or null on error</returns>
	public static T LoadXmlFile<T>(string filePath) where T : class
	{
		TextReader textReader = null;
		T obj = default(T);
		try
		{
			if (File.Exists(filePath))
			{
				// Notes:
				// Throwing a binding error is NORMAL behaviour! for FileNotFoundException!
				// Basically, your XML is incorrect, but I cannot see what you have done wrong!
				// See: http://stackoverflow.com/questions/1127431/xmlserializer-giving-filenotfoundexception-at-constructor
				// http://msdn.microsoft.com/en-us/library/aa302290.aspx
				// http://msdn.microsoft.com/en-us/library/h5e30exc.aspx
				// http://msdn.microsoft.com/en-us/library/system.diagnostics.debuggernonusercodeattribute.aspx

				// Exceptions>Managed Debugging Assistants>BindingFailure,  Thrown -> uncheck this to stop warning!
				XmlSerializer xserDocumentSerializer = new XmlSerializer(typeof(T));
				if (xserDocumentSerializer != null)
				{
					textReader = new StreamReader(filePath);
					obj = xserDocumentSerializer.Deserialize(textReader) as T;
				}
			}
		}
		catch  // Just catch
		{
		}
		finally
		{
			//Make sure to close the file even if an exception is raised...
			if (textReader != null)
				textReader.Close();
		}
		return obj;
	}

	//*****************************************************************************
	/// <summary>
	/// Save the object to the specified XML file
	/// </summary>
	/// <param name="obj">Object to serialise</param>
	/// <param name="filePath">Path of file to create</param>
	/// <returns>true if file written, false otherwise</returns>
	public static bool SaveXmlFile(object obj, string filePath)
	{
		bool success = false;
		TextWriter textWriter = null;

		if (obj != null && filePath != null && filePath.Length > 0)
		{
			try
			{
				// Create serialiser object using the type name of the Object to serialize.
				Type T = obj.GetType();
				XmlSerializer xmlSerializer = new XmlSerializer(obj.GetType());
				textWriter = new StreamWriter(filePath);
				xmlSerializer.Serialize(textWriter, obj);
				success = true;
			}
			catch (System.IO.IOException)	// Ignore this one.
			{
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
			finally
			{
				// Make sure to close the file even if an exception is raised...
				if (textWriter != null)
					textWriter.Close();
			}
		}
		return success;
	}

	//*****************************************************************************
	/// <summary>
	/// Serialises the specified object into an XML string
	/// </summary>
	/// <typeparam name="T">Type of object</typeparam>
	/// <param name="xmlObject">Object to serialise</param>
	/// <returns>Object serialised as an XML string</returns>
	public static string SeraliseObjectToXML<T>(T xmlObject)
	{
		StringBuilder str = new StringBuilder();
		XmlWriter xmw = null;
		try
		{
			XmlSerializer xms = new XmlSerializer(xmlObject.GetType());
			xmw = XmlWriter.Create(str, new XmlWriterSettings());
			xms.Serialize(xmw, xmlObject);
		}
		catch
		{
		}
		finally
		{
			if (xmw != null)
				xmw.Close();
		}
		return str.ToString();
	}


	//*****************************************************************************
	/// <summary>
	/// Deserialise the XML data to create a new object
	/// </summary>
	/// <typeparam name="T">Type of object to create</typeparam>
	/// <param name="xmlData">Serialised XML data</param>
	/// <returns>Object of specified type or null on error</returns>
	public static T DeserialiseXML<T>(string xmlData) where T : new()
	{
		T obj = default(T);
		TextReader tr = null;
		try
		{
			if (!string.IsNullOrEmpty(xmlData))
			{
				tr = new StringReader(xmlData);
				obj = new T();
				XmlSerializer xms = new XmlSerializer(obj.GetType());
				obj = (T)xms.Deserialize(tr);
			}
		}
		catch
		{
		}
		finally
		{
			if (tr != null)
				tr.Close();
			if (obj == null)
				obj = default(T);
		}
		return obj;
	}

	//*****************************************************************************
	/// <summary>
	/// Creates a deep copy clone of the specified object. Must be able to be serialised.
	/// </summary>
	/// <typeparam name="T">Type of object</typeparam>
	/// <param name="obj">Object to copy</param>
	/// <returns>Deep copy clone of object</returns>
	public static T DeepCloneObject<T>(T obj) where T : new()
	{
		string serialisedData = IpcUtilities.SeraliseObjectToXML<T>(obj);
		return IpcUtilities.DeserialiseXML<T>(serialisedData);
	}

	//*****************************************************************************
	/// <summary>
	/// Compares two objects at a simple level. Does not compare contents of arrays, lists and structures
	/// </summary>
	/// <typeparam name="T">Type of objects to compare</typeparam>
	/// <param name="object1">First object</param>
	/// <param name="object2">Second object</param>
	/// <returns>0 if identical at simple level, 1 if different</returns>
	public static int SimpleCompare<T>(T object1, T object2)
	{
		int same = 1;

		if (object1 == null && object2 == null)
			same = 0;
		else if (object1 != null && object2 != null)
		{
			FieldInfo[] info = typeof(T).GetFields();
			// For each public field in the objects compare items
			foreach (FieldInfo fi in info)
			{
				if (fi.IsPublic && !fi.IsStatic)
				{
					switch (fi.FieldType.FullName)
					{
					case "System.String":
						string string1 = fi.GetValue(object1).ToString();
						string string2 = fi.GetValue(object2).ToString();
						same = String.Compare(string1, string2);
						break;
					default:
						object value1 = fi.GetValue(object1);
						object value2 = fi.GetValue(object2);
						if (value1 != null && value2 != null)
							same = String.Compare(value1.ToString(), value2.ToString());
						else if (value1 == null && value2 == null)
							same = 0;
						else
							same = 1;
						break;
					}
					if (same != 0)
						break;
				}
			}
		}
		return same;
	}

	// ******************************************************************************
	/// <summary>
	/// Checks if the two lists are the same
	/// </summary>
	/// <typeparam name="T">Generic object type</typeparam>
	/// <param name="left">left</param>
	/// <param name="right">right</param>
	/// <returns>0 for same 1 if different</returns>
	public static int ListCompare<T>(List<T> left, List<T> right)
	{
		int same = 1;
		if (left == null && right == null)							// Check if both null
			same = 0;
		else if (left != null && right != null)						// If both non-null, check contents
		{
			if (left.Count == right.Count)							// Check both the same length
			{
				Dictionary<T, int> dict = new Dictionary<T, int>();

				// Count the members in the left
				foreach (T member in left)
				{
					if (dict.ContainsKey(member))
						dict[member]++;
					else
						dict[member] = 1;							// Add first entry
				}

				// Now check the member in the right
				foreach (T member in right)
				{
					if (dict.ContainsKey(member))
						dict[member]--;
					else
						return 1;
				}

				same = 0;
				foreach (KeyValuePair<T, int> kvp in dict)
				{
					if (kvp.Value != 0)
					{
						same = 1;
						break;
					}
				}
			}
		}
		else
			same = 1;												// One is null, and the other is not

		return same;
	}

	// ******************************************************************************
	/// <summary>
	/// Gets the hash code for the object at a simple level
	/// </summary>
	/// <typeparam name="T">Type of object</typeparam>
	/// <param name="obj">Object</param>
	/// <returns>Hash code</returns>
	public static int SimpleGetHashCode<T>(T obj)
	{
		int hash = 0;

		FieldInfo[] info = typeof(T).GetFields();
		// For each public field in the objects factor in the hash code
		foreach (FieldInfo fi in info)
		{
			if (fi.IsPublic && !fi.IsStatic)
			{
				// Ignore lists - included later
				if (fi.FieldType.Name.Contains("List"))
					continue;
				switch (fi.FieldType.FullName)
				{
				case "System.String":
					string string1 = fi.GetValue(obj).ToString();
					hash ^= string1.GetHashCode();
					break;
				default:
					object value1 = fi.GetValue(obj);
					hash ^= value1.GetHashCode();
					break;
				}
			}
		}
		return hash;
	}

	// ******************************************************************************
	/// <summary>
	/// Gets a hash code for the list
	/// </summary>
	/// <typeparam name="T">Type of objects in list</typeparam>
	/// <param name="list">List</param>
	/// <returns>Hash code</returns>
	public static int ListGetHashCode<T>(List<T> list)
	{
		int hash = 0;

		foreach (T member in list)
			hash ^= member.GetHashCode();

		return hash;
	}
}
