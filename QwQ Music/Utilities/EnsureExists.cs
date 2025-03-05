using System.IO;

namespace QwQ_Music.Utilities;

public static class EnsureExists
{
    public static string Path(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        return path;
    }

    public static string File(string file)
    {
        if (!System.IO.File.Exists(file))
            System.IO.File.Create(file).Close();
        return file;
    }
}
