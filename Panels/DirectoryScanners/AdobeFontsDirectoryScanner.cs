using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Xml.Linq;

namespace Ranger2
{
    public class AdobeFontsDirectoryScanner : DirectoryScanner
    {
        private static string m_adobeLiveTypePath;
        private static string m_adobeEntitlementsPath;

        public AdobeFontsDirectoryScanner()
        {
            m_adobeLiveTypePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Adobe", "CoreSync", "plugins", "livetype");
            m_adobeEntitlementsPath = Path.Combine(m_adobeLiveTypePath, "c", "entitlements.xml");
        }

        public bool IsAdobeFontsDirectory(string directory)
        {
            return directory == m_adobeLiveTypePath;
        }

        public override void ScanDirectory(string directory, string pathToSelect, IEnumerable<string> allowedExtensions = null)
        {
            BackgroundWorker backgroundThread = new BackgroundWorker();

            backgroundThread.DoWork += (s, e) =>
            {
                List<ScanResult.Filename> fontFiles = new();

                if (File.Exists(m_adobeEntitlementsPath))
                {
                    XDocument doc = XDocument.Load(m_adobeEntitlementsPath);

                    var idToPath = new Dictionary<string, string>();

                    Dictionary<string, int> filesPerDirectory = new();

                    foreach (string file in Directory.GetFiles(m_adobeLiveTypePath, "*", SearchOption.AllDirectories))
                    {
                        var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                        var headerBuffer = new byte[4];
                        if (fs.Read(headerBuffer, 0, 4) != 0)
                        {
                            // Is it a font?
                            if (headerBuffer[0] == 'O' &&
                                headerBuffer[1] == 'T' &&
                                headerBuffer[2] == 'T' &&
                                headerBuffer[3] == 'O')
                            {
                                string id = Path.GetFileName(file);
                                idToPath.Add(id, file);


                                string dir = Path.GetDirectoryName(file);
                                if (!filesPerDirectory.ContainsKey(dir))
                                {
                                    filesPerDirectory[dir] = 0;
                                }
                                filesPerDirectory[dir]++;
                            }
                        }
                    }

                    foreach (XElement fontElement in doc.Root.Element("fonts").Elements("font"))
                    {
                        string fullName = fontElement.Element("properties").Element("fullName")?.Value;
                        string id = fontElement.Element("id")?.Value;

                        if (id != null && fullName != null)
                        {
                            if (idToPath.TryGetValue(id, out var path))
                            {
                                fontFiles.Add(new ScanResult.Filename(path, fullName + ".otf"));
                            }
                        }
                    }

                    fontFiles.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
                }

                e.Result = new ScanResult(directory, fontFiles, [], pathToSelect);
            };

            backgroundThread.RunWorkerCompleted += new RunWorkerCompletedEventHandler((s, e) =>
            {
                InvokeOnDirectoryScanComplete(e.Result as ScanResult);
            });

            backgroundThread.RunWorkerAsync();
        }
    }
}
