using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ModScripts
{
    [MenuItem("Export/Build Mod Files")]
    static void BuildAllModFiles()
    {
        var settings = OuterWildsSettings.GetOrCreateSettings();
        var assetBundleDirectory = settings.m_AssetBundleOutputPath;
        Debug.Log("Asset Bundle Path: " + assetBundleDirectory);
        var modOutputRootDirectory = settings.m_ModOutputPath;
        modOutputRootDirectory = modOutputRootDirectory.Replace("%APPDATA%", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
        Debug.Log("Mod Output Path: " + modOutputRootDirectory);
        var rootDirectory = Path.Combine(Directory.GetCurrentDirectory(), "..");
        Debug.Log("Project Root Path: " + rootDirectory);
        var manifestPath = Path.Combine(rootDirectory, "manifest.json");
        Debug.Log("Manifest Path: " + manifestPath);
        var manifest = JsonUtility.FromJson<ManifestJson>(File.ReadAllText(manifestPath));
        var modOutputDirectory = Path.Combine(modOutputRootDirectory, manifest.uniqueName);

        Directory.Delete(modOutputDirectory, true);
        Directory.CreateDirectory(Path.Combine(modOutputDirectory));
        Directory.CreateDirectory(Path.Combine(modOutputDirectory, "assetbundles"));
        foreach (var path in Directory.EnumerateFiles(assetBundleDirectory))
        {
            var name = Path.GetFileName(path);
            Copy(Path.Combine(assetBundleDirectory, name), Path.Combine(modOutputDirectory, "assetbundles", name));
        }
        foreach (var path in Directory.EnumerateFiles(rootDirectory))
        {
            var name = Path.GetFileName(path);
            if (name.EndsWith(manifest.filename) || name == "default-config.json" || name == "manifest.json")
            {
                Copy(Path.Combine(rootDirectory, name), Path.Combine(modOutputDirectory, name));
            }
        }
        Directory.CreateDirectory(Path.Combine(modOutputDirectory, "planets"));
        foreach (var path in Directory.EnumerateFiles(Path.Combine(rootDirectory, "planets")))
        {
            var name = Path.GetFileName(path);
            Copy(Path.Combine(Path.Combine(rootDirectory, "planets"), name), Path.Combine(modOutputDirectory, "planets", name));
        }
        Directory.CreateDirectory(Path.Combine(modOutputDirectory, "systems"));
        foreach (var path in Directory.EnumerateFiles(Path.Combine(rootDirectory, "systems")))
        {
            var name = Path.GetFileName(path);
            Copy(Path.Combine(Path.Combine(rootDirectory, "systems"), name), Path.Combine(modOutputDirectory, "systems", name));
        }
        Directory.CreateDirectory(Path.Combine(modOutputDirectory, "translations"));
        foreach (var path in Directory.EnumerateFiles(Path.Combine(rootDirectory, "translations")))
        {
            var name = Path.GetFileName(path);
            Copy(Path.Combine(Path.Combine(rootDirectory, "translations"), name), Path.Combine(modOutputDirectory, "translations", name));
        }
    }

    static void Copy(string src, string dest)
    {
        Debug.Log("Copying " + src + " to " + dest);
        File.Copy(src, dest, true);
    }

    [System.Serializable]
    public class ManifestJson
    {
        public string filename;
        public string author;
        public string name;
        public string uniqueName;
        public string version;
        public string owmlVersion;
        public List<string> dependencies;
    }
}
