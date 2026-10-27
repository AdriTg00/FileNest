using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;

namespace FileNest.Core;

/// <summary>Nueva version publicada en GitHub.</summary>
public sealed record UpdateInfo(string Tag, string InstallerUrl);

/// <summary>
/// Auto-actualizacion desde GitHub Releases:
/// consulta /releases/latest, compara con la version local y, si hay una mas
/// nueva, descarga el instalador y lo ejecuta.
/// </summary>
public static class Updater
{
    private static readonly HttpClient Client = CreateClient();

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"{AppInfo.Name}/{AppInfo.Version}");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }

    /// <summary>Ultima version publicada, o null si no hay conexion / no hay releases.</summary>
    public static async Task<UpdateInfo?> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            string url = $"https://api.github.com/repos/{AppInfo.Repo}/releases/latest";
            string json = await Client.GetStringAsync(url, cancellationToken).ConfigureAwait(false);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("tag_name", out var tagElement)) return null;
            string tag = tagElement.GetString() ?? "";
            if (tag.Length == 0) return null;

            if (!root.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
                return null;

            string installerUrl = "";
            foreach (var asset in assets.EnumerateArray())
            {
                if (!asset.TryGetProperty("browser_download_url", out var urlElement)) continue;
                string candidate = urlElement.GetString() ?? "";
                if (candidate.Contains("instalador", StringComparison.OrdinalIgnoreCase))
                {
                    installerUrl = candidate;
                    break;
                }
            }

            if (installerUrl.Length == 0) return null;
            return new UpdateInfo(tag, installerUrl);
        }
        catch
        {
            return null;                            // sin red, 404, JSON raro...
        }
    }

    /// <summary>True si <paramref name="tag"/> es mas nuevo que la version instalada.</summary>
    public static bool IsNewer(string tag) => CompareVersions(tag, AppInfo.Version) > 0;

    /// <summary>&gt;0 si 'a' es mas nueva que 'b'. Compara los numeros que haya en cada texto.</summary>
    public static int CompareVersions(string a, string b)
    {
        int[] va = Numbers(a);
        int[] vb = Numbers(b);
        int count = Math.Max(va.Length, vb.Length);

        for (int i = 0; i < count; ++i)
        {
            int x = i < va.Length ? va[i] : 0;
            int y = i < vb.Length ? vb[i] : 0;
            if (x != y) return x - y;
        }

        return 0;
    }

    private static int[] Numbers(string text)
    {
        var numbers = new List<int>();
        var current = new System.Text.StringBuilder();

        foreach (char c in text)
        {
            if (char.IsDigit(c))
            {
                current.Append(c);
            }
            else if (current.Length > 0)
            {
                numbers.Add(int.Parse(current.ToString()));
                current.Clear();
            }
        }

        if (current.Length > 0) numbers.Add(int.Parse(current.ToString()));
        return numbers.ToArray();
    }

    /// <summary>Descarga el instalador en la carpeta de datos. Devuelve su ruta o null.</summary>
    public static async Task<string?> DownloadAsync(UpdateInfo info, IProgress<double>? progress = null,
                                                    CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await Client.GetAsync(info.InstallerUrl, HttpCompletionOption.ResponseHeadersRead,
                                                       cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            long total = response.Content.Headers.ContentLength ?? -1;
            string destination = Path.Combine(Paths.DataDirectory, $"FileNest-{info.Tag}-instalador.exe");

            await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
            await using (var target = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None,
                                                     81920, useAsync: true))
            {
                var buffer = new byte[81920];
                long copied = 0;
                int read;

                while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                    copied += read;
                    if (total > 0) progress?.Report((double)copied / total);
                }
            }

            return destination;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Lanza el instalador descargado.</summary>
    public static bool RunInstaller(string installerPath)
    {
        try
        {
            Process.Start(new ProcessStartInfo(installerPath) { UseShellExecute = true });
            return true;
        }
        catch
        {
            return false;
        }
    }
}
