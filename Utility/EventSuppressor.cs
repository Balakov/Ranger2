
namespace Ranger2.Utility
{
    public class EventHandlerSuppressor
    {
        private int m_shouldDisableEventHandlersRefCount = 0;
        public bool ShouldEnableEventHandlers => m_shouldDisableEventHandlersRefCount == 0;

        public virtual void DisableEventHandlers() => m_shouldDisableEventHandlersRefCount++;
        public virtual void EnableEventHandlers() => m_shouldDisableEventHandlersRefCount--;
    }
}
