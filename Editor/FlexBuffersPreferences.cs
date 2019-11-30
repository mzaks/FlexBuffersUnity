using UnityEditor;

namespace FlexBuffers
{
    public static class FlexBuffersPreferences
    {
        public static bool PrettyPrintedJson
        {
            get => EditorPrefs.GetBool("flex_buffer_pretty_printing", true);
            set => EditorPrefs.SetBool("flex_buffer_pretty_printing", value);
        }
        
        private static string _CsvSeparator
        {
            get => EditorPrefs.GetString("flex_buffer_csv_separator", ",");
            set => EditorPrefs.SetString("flex_buffer_csv_separator", value);
        }

        public static char CsvSeparator => _CsvSeparator.Length == 1 ? _CsvSeparator[0] : ',';

        [PreferenceItem("FlexBuffers")]
        private static void PreferencesItem()
        {
            
            PrettyPrintedJson = EditorGUILayout.Toggle("Export JSON pretty printed", PrettyPrintedJson);
            var separatorError = _CsvSeparator.Length != 1 ? "✘ ︎" : ""; 
            _CsvSeparator = EditorGUILayout.TextField($"{separatorError}CSV separator", _CsvSeparator);
        }
    }
}