using System.Windows.Input;

namespace Ranger2
{
    // Searching by keypress
    public class KeySearch
    {
        public interface IVisualSearchProvider
        {
            bool SelectClosestFile(string search, int searchIndex);
        }

        private IVisualSearchProvider m_visualSearchProvider;
        private int m_searchIndex = 0;
        private Key m_lastKey = Key.None;

        public KeySearch(IVisualSearchProvider visualSearchProvider)
        {
            m_visualSearchProvider = visualSearchProvider;
        }

        public void OnKeypress(Key key)
        {
            if (KeyboardUtilities.IsAlphaNumericKey(key))
            {
                m_searchIndex = (key == m_lastKey) ? m_searchIndex + 1 : 0;
                m_lastKey = key;
                m_visualSearchProvider.SelectClosestFile(KeyboardUtilities.AlphaNumericKeyKeyToString(m_lastKey), m_searchIndex);
            }
        }
    }
}
