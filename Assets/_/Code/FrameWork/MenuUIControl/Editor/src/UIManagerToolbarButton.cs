using UnityEditor.Toolbars;

namespace MenuUiControl.Editor
{
    public class UIManagerToolbarButton
    {
        [MainToolbarElement("UIManagerToolbar", defaultDockPosition = MainToolbarDockPosition.Left)]
        public static MainToolbarButton ShowWindow()
        {
            MainToolbarContent content = new MainToolbarContent(
                "UI Manager",
                "Ouvrir le UI Manager");
            return new MainToolbarButton(content, Open);
        }

        private static void Open()
        {
            UIManagerWindowTool.Open();
        }
    }
}
