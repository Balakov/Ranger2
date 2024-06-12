
namespace Ranger2
{
    public delegate void OnDirectoryChangedDelegate(string path, string previousPath);

    public interface IDirectoryChangeRequest
    {
        void SetDirectory(string path);
        void SetDirectoryToParent();

        event OnDirectoryChangedDelegate OnDirectoryChanged;
    }
}
