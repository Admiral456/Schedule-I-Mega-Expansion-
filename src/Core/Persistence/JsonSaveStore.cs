using System.Text.Json;

namespace ScheduleIMegaExpansion.Core.Persistence;

/// <summary>
/// Atomic file store for mod-owned data. Pass a mod-specific path; never point it at the vanilla save.
/// The previous valid file is copied to .bak before the new file replaces it.
/// </summary>
public sealed class JsonSaveStore<TDocument> where TDocument : class
{
    private readonly string _path;
    private readonly long _maximumBytes;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly object _gate = new();

    public JsonSaveStore(string path, long maximumBytes = 8 * 1024 * 1024)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("A mod-owned save path is required.", nameof(path));
        if (maximumBytes < 1)
            throw new ArgumentOutOfRangeException(nameof(maximumBytes));

        _path = Path.GetFullPath(path);
        _maximumBytes = maximumBytes;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = false,
            WriteIndented = true
        };
    }

    public void Write(TDocument document)
    {
        if (document is null)
            throw new ArgumentNullException(nameof(document));

        lock (_gate)
        {
            string? directory = Path.GetDirectoryName(_path);
            if (string.IsNullOrEmpty(directory))
                throw new InvalidOperationException("Save path must have a parent directory.");

            Directory.CreateDirectory(directory);
            string temporaryPath = _path + ".tmp";
            string backupPath = _path + ".bak";

            try
            {
                using (var stream = new FileStream(
                    temporaryPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 16 * 1024,
                    options: FileOptions.WriteThrough))
                {
                    JsonSerializer.Serialize(stream, document, _jsonOptions);
                    stream.Flush(flushToDisk: true);
                    if (stream.Length > _maximumBytes)
                        throw new InvalidDataException("Serialized save exceeds the configured size limit.");
                }

                if (File.Exists(_path))
                    File.Copy(_path, backupPath, overwrite: true);

                File.Move(temporaryPath, _path, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }
    }

    public TDocument? Read()
    {
        lock (_gate) return ReadPath(_path);
    }

    public TDocument? ReadBackup()
    {
        lock (_gate) return ReadPath(_path + ".bak");
    }

    private TDocument? ReadPath(string path)
    {
        if (!File.Exists(path))
            return null;

        var info = new FileInfo(path);
        if (info.Length > _maximumBytes)
            throw new InvalidDataException("Save file exceeds the configured size limit.");

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return JsonSerializer.Deserialize<TDocument>(stream, _jsonOptions);
    }
}
