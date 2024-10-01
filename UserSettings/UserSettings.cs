using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using System.Xml.Serialization;

namespace Ranger2
{
    public class UserSettings
    {
        public class FilePanelSettings
        {
            public string Path { get; set; }
            public DirectoryContentsControl.DirectoryListingType? ListingType { get; set; }
        }

        public class BookmarkGroup
        {
            public int Group { get; set; }
            public string Name { get; set; }
        }

        public class BookmarkGroupEntry
        {
            public int Group { get; set; }
            public int SortOrder { get; set; }
        }
        
        public class Bookmark
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public bool OpenInExplorer { get; set; }
            public List<BookmarkGroupEntry> BookmarkGroups { get; set; } = new();

            public override string ToString() => Name;
        }

        //
        // Serialised settings
        //

        public bool DarkMode { get; set; }

        public string DefaultEditorPath { get; set; }

        public FilePanelSettings Panel1Settings { get; set; } = new();
        public FilePanelSettings Panel2Settings { get; set; } = new();
        public FilePanelSettings Panel3Settings { get; set; } = new();
        public int ActivePanelCount { get; set; } = 2;

        public int? WindowX { get; set; }
        public int? WindowY { get; set; }
        public int? WindowWidth { get; set; }
        public int? WindowHeight { get; set; }
        public bool? WindowMaximised { get; set; }
        
        public bool? ShowHiddenFiles { get; set; }
        public bool? ShowSystemFiles { get; set; }
        public bool? ShowDotFiles { get; set; }

        public int ActiveBookmarkGroup { get; set; }
        public List<Bookmark> Bookmarks { get; set; } = new();
        public List<BookmarkGroup> BookmarkGroups { get; set; } = new();
        
        public List<string> IgnoredDrives { get; set; } = new();

        public BitmapScalingMode? ImageViewerScalingMode { get; set; }

        //
        // Non-serialised
        //

        private bool m_saveDisabled;
        private string m_settingsPath;

        //
        // Methods
        //

        public string GetSettingsPath() 
        { 
            // This can't be a property otherwise it will be serialised.
            return m_settingsPath; 
        }

        public static UserSettings Load(string path)
        {
            // Don't nuke all of the settings if we messed something up and made the settings un-serialisable.
            // m_loadFailed will not be set if the file doesn't exist, just if the XML is un-loadable.
            bool loadFailed = false;

            try
            {
                using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var xmlSerialiser = new XmlSerializer(typeof(UserSettings));
                    var newUserSettings = (UserSettings)xmlSerialiser.Deserialize(fileStream);

                    newUserSettings.m_settingsPath = path;

                    return newUserSettings;
                }
            }
            catch (DirectoryNotFoundException)
            {
            }
            catch (FileNotFoundException)
            {
            }
            catch (System.Exception e)
            {
                Debug.Log($"Failed to load settings from {path} - {e.Message}");
                loadFailed = true;
            }

            var settings = new UserSettings()
            {
                m_settingsPath = path,
                m_saveDisabled = loadFailed
            };

            return settings;
        }

        public void Save()
        {
            if (!m_saveDisabled)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(m_settingsPath));

                    using (FileStream fileStream = new FileStream(m_settingsPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var xmlSerialiser = new XmlSerializer(typeof(UserSettings));
                        xmlSerialiser.Serialize(fileStream, this);
                    }
                }
                catch (System.Exception e)
                {
                    System.Console.WriteLine($"Failed to save settings to {m_settingsPath} - {e.Message}");
                }
            }
        }
    }
}
