using System.IO;
using FlexBuffers;
using UnityEditor;

public static class ImportAsFlexBuffer
{
    [MenuItem("Tools/FlexBuffers/JSON as FlexBuffer...")]
    static void ImportJson()
    {
        string jsonPath = EditorUtility.OpenFilePanel("Select JSON file", "", "json");
        if (jsonPath.Length == 0)
        {
            return;
        }
        var bytes = JsonToFlexBufferConverter.ConvertFile(jsonPath);

        if (bytes == null)
        {
            return;
        }

        var fileName = Path.GetFileNameWithoutExtension(jsonPath);
        
        var flxPath = EditorUtility.SaveFilePanel(
            "Save as FlexBuffer",
            "",
            fileName + ".bytes",
            "bytes");

        if (flxPath.Length != 0)
        {
            File.WriteAllBytes(flxPath, bytes);
        }
    }
    
    [MenuItem("Tools/FlexBuffers/CSV as FlexBuffer...")]
    static void ImportCsv()
    {
        string csvPath = EditorUtility.OpenFilePanel("Select CSV file", "", "csv");
        if (csvPath.Length == 0)
        {
            return;
        }

        var csv = File.ReadAllText(csvPath);
        var bytes = CsvToFlexBufferConverter.Convert(csv, FlexBuffersPreferences.CsvSeparator);

        if (bytes == null)
        {
            return;
        }

        var fileName = Path.GetFileNameWithoutExtension(csvPath);
        
        var flxPath = EditorUtility.SaveFilePanel(
            "Save as FlexBuffer",
            "",
            fileName + ".bytes",
            "bytes");

        if (flxPath.Length != 0)
        {
            File.WriteAllBytes(flxPath, bytes);
        }
    }
    
    [MenuItem("Tools/FlexBuffers/XML as FlexBuffer...")]
    static void ImportXML()
    {
        string xmlPath = EditorUtility.OpenFilePanel("Select XML file", "", "xml");
        if (xmlPath.Length == 0)
        {
            return;
        }

        var xmlData = File.ReadAllText(xmlPath);
        var bytes = XmlToFlexBufferConverter.Convert(xmlData);

        if (bytes == null)
        {
            return;
        }

        var fileName = Path.GetFileNameWithoutExtension(xmlPath);
        
        var flxPath = EditorUtility.SaveFilePanel(
            "Save as FlexBuffer",
            "",
            fileName + ".bytes",
            "bytes");

        if (flxPath.Length != 0)
        {
            File.WriteAllBytes(flxPath, bytes);
        }
    }
}
