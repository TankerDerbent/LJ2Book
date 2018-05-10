using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Reflection;
using System.Xml.Linq;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Specialized;

namespace SimplesNet
{
	class CustomSettingsProvider : SettingsProvider
	{
		const string CONFIG = "configuration";
		const string CONFIGNODE = "configSections";
		const string GROUPNODE = "sectionGroup";
		const string USER_SETTINGS = "userSettings";

		private string PROPERTIES_SETTING;
		private string _UserConfigPath;
		private string UserConfigPath
		{
			get
			{
				return _UserConfigPath;
			}

		}

		private System.Xml.XmlDocument xmlDoc = null;

		/// <summary>
		/// Loads the file into memory.
		/// </summary>
		public CustomSettingsProvider(string propertiesClassName, string userConfigPath)
		{
			PROPERTIES_SETTING = propertiesClassName;
			_UserConfigPath = userConfigPath;
		}

		/// <summary>
		/// Override.
		/// </summary>
		public override void Initialize(string name, System.Collections.Specialized.NameValueCollection config)
		{
			base.Initialize(ApplicationName, config);
		}

		/// <summary>
		/// Override.
		/// </summary>
		public override string ApplicationName
		{
			get
			{
				//return System.Reflection.Assembly.GetExecutingAssembly().ManifestModule.Name;
				return (System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);
			}
			set
			{
				//do nothing
			}
		}

		/// <summary>
		/// Must override this, this is the bit that matches up the designer properties to the dictionary values
		/// </summary>
		/// <param name="context"></param>
		/// <param name="collection"></param>
		/// <returns></returns>
		public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext context, SettingsPropertyCollection collection)
		{
			// Create a collection of values to return
			SettingsPropertyValueCollection retValues = new SettingsPropertyValueCollection();

			// Create a temporary SettingsPropertyValue to reuse
			SettingsPropertyValue setVal;

			// Loop through the list of settings that the application has requested and add them
			// to our collection of return values.
			foreach (SettingsProperty sProp in collection)
			{
				setVal = new SettingsPropertyValue(sProp);
				setVal.IsDirty = false;
				setVal.SerializedValue = GetSetting(sProp);
				retValues.Add(setVal);
			}
			return retValues;
		}

		/// <summary>
		/// Must override this, this is the bit that does the saving to file.  Called when Settings.Save() is called
		/// </summary>
		/// <param name="context"></param>
		/// <param name="collection"></param>
		public override void SetPropertyValues(SettingsContext context, SettingsPropertyValueCollection collection)
		{
			// Set the values in XML
			foreach (SettingsPropertyValue spVal in collection)
			{
				SetSetting(spVal);
			}

			// Write the XML file to disk
			try
			{
				XMLConfig.Save(UserConfigPath);
			}
			catch (Exception ex)
			{
				// Create an informational message for the user if we cannot save the settings.
				// Enable whichever applies to your application type.

				// Uncomment the following line to enable a MessageBox for forms-based apps
				System.Windows.Forms.MessageBox.Show(ex.Message, "Error writting configuration file to disk", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);

				// Uncomment the following line to enable a console message for console-based apps
				//Console.WriteLine("Error writing configuration file to disk: " + ex.Message);
			}
		}

		private XmlDocument XMLConfig
		{
			get
			{
				// Check if we already have accessed the XML config file. If the xmlDoc object is empty, we have not.
				if (xmlDoc == null)
				{
					xmlDoc = new XmlDocument();

					// If we have not loaded the config, try reading the file from disk.
					try
					{
						xmlDoc.Load(UserConfigPath);
					}

					// If the file does not exist on disk, catch the exception then create the XML template for the file.
					catch (Exception)
					{
						// XML Declaration
						// <?xml version="1.0" encoding="utf-8"?>
						XmlDeclaration dec = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", null);
						xmlDoc.AppendChild(dec);

						// Create root node and append to the document
						// <configuration>
						XmlElement rootNode = xmlDoc.CreateElement(CONFIG);
						xmlDoc.AppendChild(rootNode);

						// Create Configuration Sections node and add as the first node under the root
						// <configSections>
						XmlElement configNode = xmlDoc.CreateElement(CONFIGNODE);
						xmlDoc.DocumentElement.PrependChild(configNode);

						// Create the user settings section group declaration and append to the config node above
						// <sectionGroup name="userSettings"...>
						XmlElement groupNode = xmlDoc.CreateElement(GROUPNODE);
						groupNode.SetAttribute("name", USER_SETTINGS);
						groupNode.SetAttribute("type", "System.Configuration.UserSettingsGroup");
						configNode.AppendChild(groupNode);

						// Create the Application section declaration and append to the groupNode above
						// <section name="AppName.Properties.Settings"...>
						XmlElement newSection = xmlDoc.CreateElement("section");
						newSection.SetAttribute("name", PROPERTIES_SETTING);
						newSection.SetAttribute("type", "System.Configuration.ClientSettingsSection");
						groupNode.AppendChild(newSection);

						// Create the userSettings node and append to the root node
						// <userSettings>
						XmlElement userNode = xmlDoc.CreateElement(USER_SETTINGS);
						xmlDoc.DocumentElement.AppendChild(userNode);

						// Create the Application settings node and append to the userNode above
						// <AppName.Properties.Settings>
						XmlElement appNode = xmlDoc.CreateElement(PROPERTIES_SETTING);
						userNode.AppendChild(appNode);
					}
				}
				return xmlDoc;
			}
		}

		// Retrieve values from the configuration file, or if the setting does not exist in the file, 
		// retrieve the value from the application's default configuration
		private object GetSetting(SettingsProperty setProp)
		{
			try
			{
				// Search for the specific settings node we are looking for in the configuration file.
				// If it exists, return the InnerText or InnerXML of its first child node, depending on the setting type.

				// If the setting is serialized as a string, return the text stored in the config
				var obj = XMLConfig.SelectSingleNode("//setting[@name='" + setProp.Name + "']");
				if (setProp.SerializeAs.ToString() == "String" && obj != null)
				{
					return obj.FirstChild.InnerText;
				}

				// If the setting is stored as XML, deserialize it and return the proper object.  This only supports
				// StringCollections at the moment - I will likely add other types as I use them in applications.
				else if (obj != null)
				{
					string settingType = setProp.PropertyType.ToString();
					string xmlData = XMLConfig.SelectSingleNode("//setting[@name='" + setProp.Name + "']").FirstChild.InnerText;
					XmlSerializer xs = new XmlSerializer(typeof(string[]));
					string[] data = (string[])xs.Deserialize(new XmlTextReader(xmlData, XmlNodeType.Element, null));

					switch (settingType)
					{
						case "System.Collections.Specialized.StringCollection":
							StringCollection sc = new StringCollection();
							sc.AddRange(data);
							return sc;
						default:
							return "";
					}
				}
			}
			catch (Exception){}

			// Check to see if a default value is defined by the application.
			// If so, return that value, using the same rules for settings stored as Strings and XML as above
			if ((setProp.DefaultValue != null))
			{
				if (setProp.SerializeAs.ToString() == "String")
				{
					return setProp.DefaultValue.ToString();
				}
				else
				{
					string settingType = setProp.PropertyType.ToString();
					string xmlData = setProp.DefaultValue.ToString();
					XmlSerializer xs = new XmlSerializer(typeof(string[]));
					string[] data = (string[])xs.Deserialize(new XmlTextReader(xmlData, XmlNodeType.Element, null));

					switch (settingType)
					{
						case "System.Collections.Specialized.StringCollection":
							StringCollection sc = new StringCollection();
							sc.AddRange(data);
							return sc;

						default: return "";
					}
				}
			}
			else
			{
				return "";
			}
		}

		private void SetSetting(SettingsPropertyValue setProp)
		{
			// Define the XML path under which we want to write our settings if they do not already exist
			XmlNode SettingNode = null;

			try
			{
				// Search for the specific settings node we want to update.
				// If it exists, return its first child node, (the <value>data here</value> node)
				SettingNode = XMLConfig.SelectSingleNode("//setting[@name='" + setProp.Name + "']").FirstChild;
			}
			catch (Exception)
			{
				SettingNode = null;
			}

			// If we have a pointer to an actual XML node, update the value stored there
			if ((SettingNode != null))
			{
				SettingNode.InnerText = setProp.SerializedValue.ToString();
			}
			else
			{
				// If the value did not already exist in this settings file, create a new entry for this setting

				// Search for the application settings node (<Appname.Properties.Settings>) and store it.
				XmlNode tmpNode = XMLConfig.SelectSingleNode("//" + PROPERTIES_SETTING);

				// Create a new settings node and assign its name as well as how it will be serialized
				XmlElement newSetting = xmlDoc.CreateElement("setting");
				newSetting.SetAttribute("name", setProp.Name);

				if (setProp.Property.SerializeAs.ToString() == "String")
				{
					newSetting.SetAttribute("serializeAs", "String");
				}
				else
				{
					newSetting.SetAttribute("serializeAs", "Xml");
				}

				// Append this node to the application settings node (<Appname.Properties.Settings>)
				tmpNode.AppendChild(newSetting);

				// Create an element under our named settings node, and assign it the value we are trying to save
				XmlElement valueElement = xmlDoc.CreateElement("value");
				valueElement.InnerText = setProp.SerializedValue.ToString();
				

				//Append this new element under the setting node we created above
				newSetting.AppendChild(valueElement);
			}
		}
	}
}