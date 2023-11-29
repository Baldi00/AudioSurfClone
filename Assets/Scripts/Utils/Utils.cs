using System.IO;
using UnityEngine;

public class Utils
{

    /// <summary>
    /// Returns the name of the folder or file (without extension) at the given path
    /// </summary>
    /// <param name="path">The path to the item whose name you want to retrieve</param>
    /// <returns>The name of the folder or file (without extension) at the given path</returns>
    public static string GetNameFromPath(string path)
    {
        if (path.EndsWith(Path.DirectorySeparatorChar))
            path = path[..^1];

        if (File.GetAttributes(path).HasFlag(FileAttributes.Directory))
            return path[(path.LastIndexOf(Path.DirectorySeparatorChar) + 1)..];
        else
            return Path.GetFileNameWithoutExtension(path);
    }

    /// <summary>
    /// Checks if the given path correspond to an audio file
    /// </summary>
    /// <param name="path">The path of the element to check</param>
    /// <returns>True if the given path correspond to an audio file (i.e. is MP3), false otherwise</returns>
    public static bool IsAudioFile(string path)
    {
        return path.EndsWith(".mp3");
    }

    public static void SetCursorVisibility(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Confined;
    }
}
