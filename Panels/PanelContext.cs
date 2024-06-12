namespace Ranger2
{
    public class PanelContext
    {
        public IDirectoryChangeRequest DirectoryChangeRequester { get; }
        public IconCache IconCache { get; }
        public ImageCache ImageCache { get; }
        public FontPreviewCache FontPreviewCache { get; }
        public UserSettings UserSettings { get; }
        public IPanelLayout PanelLayout { get; set; }

        public PanelContext(IDirectoryChangeRequest directoryChangeRequester,
                            IPanelLayout panelLayout,
                            IconCache iconCache,
                            ImageCache imageCache,
                            FontPreviewCache fontPreviewCache,
                            UserSettings userSettings)
        {
            DirectoryChangeRequester = directoryChangeRequester;
            PanelLayout = panelLayout;
            IconCache = iconCache;
            ImageCache = imageCache;
            FontPreviewCache = fontPreviewCache;
            UserSettings = userSettings;
        }
    }
}
