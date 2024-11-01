using UnityEditor;

namespace SeweralIdeas.Localization.Editor
{
    public class LocalizationEditorWindow : EditorWindow
    {
        [MenuItem("Window/Localization Manager")]
        public static void Show()
        {
            var window = CreateWindow<LocalizationEditorWindow>("Localization Manager");
            ((EditorWindow)window).Show();
        }
    }
}
