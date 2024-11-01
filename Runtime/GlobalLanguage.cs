using SeweralIdeas.Utils;

namespace SeweralIdeas.Localization
{
    /// <summary>
    /// Holds a reference to a global runtime language.
    /// </summary>
    public static class GlobalLanguage
    {
        public static readonly Observable<LanguageData> Language = new();
    }
}
