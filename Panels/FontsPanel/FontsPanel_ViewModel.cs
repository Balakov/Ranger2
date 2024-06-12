﻿using HandyControl.Tools.Extension;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Data;

namespace Ranger2
{
    public partial class FontPanel
    {
        public class ViewModel : DirectoryContentsControl.ViewModel
        {
            public class FontViewModel : FileSystemObjectViewModel, FontPreviewCache.IFontPreviewLoadedNotification
            {
                private readonly PanelContext m_context;
                private readonly bool m_isAdobeFont;
                private SKElement m_skElement;

                public SKPaint Paint { get; private set; }

                public FontViewModel(string name,
                                     FileSystemObjectInfo info,
                                     PanelContext context,
                                     bool isAdobeFont,
                                     DirectoryContentsControl.ViewModel parentViewModel) : base(name, info, parentViewModel)
                {
                    m_context = context;
                    m_isAdobeFont = isAdobeFont;

                    m_context.IconCache.QueueIconLoad(info.Path, IconCache.IconType.File, this);
                    m_context.FontPreviewCache.QueueFontPreviewLoad(info.Path, this);
                }

                public override bool CanRename => !m_isAdobeFont;
                public override bool CanDelete => CanRename;

                public void RegisterSKElement(SKElement element) => m_skElement = element;

                public void SetPaintColourFromTheme() => Paint.Color = m_context.UserSettings.DarkMode ? SKColors.White : SKColors.Black;

                public void FontSKPaintLoaded(SKPaint paint)
                {
                    Paint = paint;
                    m_skElement?.InvalidateVisual();
                }

                public override void OnActivate()
                {
                }
            }

            // ViewModel

            private static string[] s_allowedExtensions = [".otf", ".ttf"];

            protected AdobeFontsDirectoryScanner m_adobeDirectorScanner = new AdobeFontsDirectoryScanner();

            public override DirectoryContentsControl.DirectoryListingType ListingType => DirectoryContentsControl.DirectoryListingType.Fonts;

            private CollectionViewSource m_filteredFiles;
            public ICollectionView FilteredFiles => m_filteredFiles?.View;

            private string m_fontFilterText;
            public string FontFilterText
            {
                get => m_fontFilterText;
                set
                {
                    if (OnPropertyChanged(ref m_fontFilterText, value))
                    {
                        m_filteredFiles.View.Refresh();
                    }
                }
            }

            public ViewModel(PanelContext context,
                             UserSettings.FilePanelSettings settings,
                             PathHistory pathHistory,
                             IDirectoryWatcher directoryWatcher) : base(context, settings, pathHistory, directoryWatcher)
            {
                m_filteredFiles = new CollectionViewSource();
                m_filteredFiles.Source = m_files;
                m_filteredFiles.SortDescriptions.Add(new SortDescription(nameof(FileSystemObjectViewModel.NameSortValue), ListSortDirection.Ascending));
                m_filteredFiles.Filter += (s, e) =>
                {
                    if (e.Item is FileSystemObjectViewModel fileViewModel)
                    {
                        e.Accepted = (string.IsNullOrEmpty(m_fontFilterText) || fileViewModel.Name.ToLower().Contains(m_fontFilterText.ToLower()));
                    }
                    else
                    {
                        e.Accepted = true;
                    }
                };


                m_directoryScanner.OnDirectoryScanComplete += OnDirectoryScanComplete;
                m_adobeDirectorScanner.OnDirectoryScanComplete += OnDirectoryScanComplete;
            }

            protected override void OnActivateItem(FileSystemObjectViewModel viewModel)
            {
                viewModel.OnActivate();
            }

            protected override void OnDirectoryChanged(string path)
            {
                m_files.Clear();
                m_isLoading = true;
                UpdateUIVisibility();

                if (m_adobeDirectorScanner.IsAdobeFontsDirectory(path))
                {
                    m_adobeDirectorScanner.ScanDirectory(path);
                }
                else
                {
                    m_directoryScanner.ScanDirectory(path, s_allowedExtensions);
                }
            }

            private void OnDirectoryScanComplete(DirectoryScanner.ScanResult scanResult)
            {
                if (!scanResult.MatchesPath(m_settings.Path))
                    return;

                bool isAdobeFontsDirectory = m_adobeDirectorScanner.IsAdobeFontsDirectory(m_settings.Path);

                if (!isAdobeFontsDirectory)
                {
                    m_directoryWatcher.EnableDirectoryWatcher(m_settings.Path);
                }

                try
                {
                    // Directories
                    m_files.AddRange(CreateDirectoryViewModels(scanResult.Directories));

                    // Files
                    {
                        foreach (var file in scanResult.Files)
                        {
                            AddItemInternal(file.Path, file.Name, isAdobeFontsDirectory);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.Log(e.Message);
                }

                m_isLoading = false;
                UpdateUIVisibility();
            }

            protected override void OnItemAdded(string path)
            {
                if (m_adobeDirectorScanner.IsAdobeFontsDirectory(m_settings.Path))
                {
                    // Don't bother watching changes to the adibe fonts directories as that's not actually
                    // where the fonts are stored.
                    return;
                }

                if (s_allowedExtensions.Contains(Path.GetExtension(path).ToLower()))
                {
                    AddItemInternal(path, alternateName: null, isAdobeFont: false);
                }
            }

            private void AddItemInternal(string path, string alternateName, bool isAdobeFont)
            {
                try
                {
                    var fi = new FileInfo(path);
                    FileAttributes fileAttribs = FileSystemObjectViewModel.Sentinels.InvalidAttributes;
                    long fileSize = FileSystemObjectViewModel.Sentinels.InvalidSize;
                    DateTime lastWriteTime = FileSystemObjectViewModel.Sentinels.InvalidDate;

                    try
                    {
                        fileAttribs = fi.Attributes;
                        fileSize = fi.Length;
                        lastWriteTime = fi.LastWriteTime;
                    }
                    catch { }

                    string leafName = alternateName ?? Path.GetFileName(path);

                    if (FileSystemObjectViewModel.FilePassesViewFilter(path, m_viewMask, out var info))
                    {
                        var fontViewModel = new FontViewModel(leafName, info, m_context, isAdobeFont, this);
                        m_files.Add(fontViewModel);
                    }
                }
                catch
                {
                    // Ignore files we don't have permission to read
                }
            }
        }
    }
}