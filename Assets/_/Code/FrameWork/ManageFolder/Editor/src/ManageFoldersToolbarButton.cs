using UnityEditor.Toolbars;

namespace ManageFolders.Editor
{
    public class ManageFoldersToolbarButton
    {
        [MainToolbarElement("ManageFoldersToolbar", defaultDockPosition = MainToolbarDockPosition.Left)]
        public static MainToolbarButton ShowButton()
        {
            MainToolbarContent content = new MainToolbarContent(
                "Manage Folders", "Ouvrir Manage Folders");
            return new MainToolbarButton(content, PopulateProjectFolders.ShowWindow);
        }
    }
}