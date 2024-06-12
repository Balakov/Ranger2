using System;
using System.Windows.Input;

namespace Ranger2
{
    // Searching by keypress
    public class KeySearch
    {
        public interface IVisualSearchProvider
        {
            bool SelectClosestFile(string searchString, SearchLimitType limitType);
        }


        public enum SearchLimitType
        {
            None,
            Files,
            Directories
        }

        private IVisualSearchProvider m_visualSearchProvider;
        private string m_searchBuffer = string.Empty;

        private readonly TimeSpan m_searchTimerClearDelay = TimeSpan.FromMilliseconds(1500);
        private System.Windows.Threading.DispatcherTimer m_clearSearchTimer = new();
        
        private SearchLimitType m_searchLimit = SearchLimitType.None;
        public void SetSearchLimitType(SearchLimitType limitType) => m_searchLimit = limitType;

        public KeySearch(IVisualSearchProvider visualSearchProvider)
        {
            m_visualSearchProvider = visualSearchProvider;
            
            m_clearSearchTimer.Interval = m_searchTimerClearDelay;
            m_clearSearchTimer.Tick += (s, e) =>
            {
                m_searchBuffer = string.Empty;
                m_clearSearchTimer.Stop();
            };
        }

        public void OnKeypress(Key key)
        {
            if (KeyboardUtilities.IsAlphaNumericKey(key))
            {
                m_searchBuffer += KeyboardUtilities.AlphaNumericKeyKeyToString(key);

                m_visualSearchProvider.SelectClosestFile(m_searchBuffer, m_searchLimit);

                m_clearSearchTimer.Stop();
                m_clearSearchTimer.Start();
            }
        }
    }
}
