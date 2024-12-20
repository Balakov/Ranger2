using System.Collections.Generic;
using System.Linq;
using static Ranger2.DirectoryContentsControl;

namespace Ranger2
{
    public enum DuplicateContentDirection
    {
        Left,
        Right
    }

    public delegate void OnSwitchPanelFocusDelegate(DirectoryContentsControl activePanel);

    public interface IPanelLayout
    {
        void SetActivePanelCount(int panelCount);
        void SwitchFocus(DirectoryContentsControl activePanel);
        void SwitchFocus(DirectoryContentsControl.ViewModel activeViewModel);
        void DuplicateCurrentContent(DuplicateContentDirection direction);
        void SwitchCurrentContent();
        DirectoryContentsControl CurrentPanel { get; }
        event OnSwitchPanelFocusDelegate OnSwitchPanelFocus;
    }

    public class PanelLayout : IPanelLayout
    {
        private List<DirectoryContentsControl> m_filePanels;
        private DrivesTree m_drivesPanel;

        public DirectoryContentsControl CurrentPanel => m_filePanels.FirstOrDefault(x => x.IsCurrentPanel);

        public event OnSwitchPanelFocusDelegate OnSwitchPanelFocus;

        public PanelLayout(DrivesTree drivesPanel, IEnumerable<DirectoryContentsControl> panels)
        {
            m_drivesPanel = drivesPanel;
            m_filePanels = panels.ToList();
        }

        public void SetActivePanelCount(int panelCount)
        {
            App.UserSettings.ActivePanelCount = panelCount;
            DirectoryContentsControl lastVisiblePanel = null;
            DirectoryContentsControl panelToSetActive = null;

            m_drivesPanel.LinkedColumnDefinition.Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star);

            for (int i=0; i < m_filePanels.Count; i++)
            {
                if (i < panelCount)
                {
                    m_filePanels[i].LinkedColumnDefinition.Width = new System.Windows.GridLength(panelCount == 1 ? 3 : 2, System.Windows.GridUnitType.Star);
                    m_filePanels[i].LinkedColumnDefinition.MinWidth = 250;
                    m_filePanels[i].SetPanelVisibility(true);
                    lastVisiblePanel = m_filePanels[i];
                }
                else
                {
                    if (m_filePanels[i].IsCurrentPanel)
                    {
                        panelToSetActive = lastVisiblePanel;
                    }
                    m_filePanels[i].SetPanelVisibility(false);
                    m_filePanels[i].LinkedColumnDefinition.MinWidth = 0;
                    m_filePanels[i].LinkedColumnDefinition.Width = new System.Windows.GridLength(0, System.Windows.GridUnitType.Star);
                }
            }

            if (panelToSetActive != null)
            {
                SwitchFocus(panelToSetActive);
            }
        }

        public void SwitchFocus(DirectoryContentsControl.ViewModel activeViewModel)
        {
            foreach (var panel in m_filePanels)
            {
                if (panel.DataContext == activeViewModel)
                {
                    SwitchFocus(panel);
                    break;
                }
            }
        }

        public void SwitchFocus(DirectoryContentsControl activePanel)
        {
            if (activePanel.IsCurrentPanel)
                return;

            foreach (var panel in m_filePanels)
            {
                bool isNewActivePanel = (panel == activePanel);

                panel.OnSwitchFocus(isNewActivePanel);

                if (isNewActivePanel)
                {
                    OnSwitchPanelFocus?.Invoke(panel);

                    if (panel.DataContext is DirectoryContentsControl.ViewModel viewModel)
                    {
                        viewModel.FocusOwner.GrabFocus();
                    }
                }
            }
        }

        public void DuplicateCurrentContent(DuplicateContentDirection direction)
        {
            for (int i=0; i < m_filePanels.Count; i++)
            {
                if (m_filePanels[i].IsCurrentPanel)
                {
                    if (direction == DuplicateContentDirection.Left && (i - 1) >= 0)
                    {
                        m_filePanels[i-1].SetContentFromPanel(m_filePanels[i]);
                    }
                    else if (direction == DuplicateContentDirection.Right && (i + 1) < m_filePanels.Count)
                    {
                        m_filePanels[i+1].SetContentFromPanel(m_filePanels[i]);
                    }

                    break;
                }
            }
        }

        public void SwitchCurrentContent()
        {
            // Switch the first two panels
            if (m_filePanels.Count >= 2)
            {
                string panel1Path = m_filePanels[1].CurrentPath;
                DirectoryListingType panel1ListingType = m_filePanels[1].ListingType;
                string panel2Path = m_filePanels[0].CurrentPath;
                DirectoryListingType panel2ListingType = m_filePanels[0].ListingType;
                
                m_filePanels[0].SetContentFromPath(panel1Path, panel1ListingType);
                m_filePanels[1].SetContentFromPath(panel2Path, panel2ListingType);
            }
        }
    }
}
