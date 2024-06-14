
namespace Ranger2
{
    public delegate void OnDirectoryChangedDelegate(string path, string previousPath, string pathToSelect);

    public interface IDirectoryChangeRequest
    {
        void SetDirectory(string path, string pathToSelect);
        void SetDirectoryToParent();

        event OnDirectoryChangedDelegate OnDirectoryChanged;
    }
}
