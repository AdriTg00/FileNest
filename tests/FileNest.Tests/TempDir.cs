using System.Text;

namespace FileNest.Tests;

/// <summary>Carpeta temporal de prueba, borrada al terminar.</summary>
public sealed class TempDir : IDisposable
{
    public string Path { get; }

    public TempDir()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "filenest-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string File(string name, string content = "x")
    {
        string full = System.IO.Path.Combine(Path, name);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
        System.IO.File.WriteAllText(full, content, new UTF8Encoding(false));
        return full;
    }

    public string Dir(string name)
    {
        string full = System.IO.Path.Combine(Path, name);
        Directory.CreateDirectory(full);
        return full;
    }

    public void Dispose()
    {
        try
        {
            foreach (string file in Directory.EnumerateFiles(Path, "*", SearchOption.AllDirectories))
                System.IO.File.SetAttributes(file, FileAttributes.Normal);

            Directory.Delete(Path, recursive: true);
        }
        catch
        {
            // Windows a veces deja un handle abierto: no es grave en un test.
        }
    }
}
