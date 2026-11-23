using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;

namespace TinyHand.Windows;

internal sealed class EmbeddedPluginPackage
{
    private readonly byte[] bytes;
    internal Version Version { get; }
    internal Version MinimumSoftware { get; }
    internal string Hash { get; }
    internal EmbeddedPluginPackage(byte[] bytes, string metadata)
    {
        using var document = JsonDocument.Parse(metadata);
        var root = document.RootElement;
        if (root.GetProperty("uuid").GetString() != StreamDeckDetector.PluginId
            || !PluginVersion.TryParse(root.GetProperty("version").GetString(), out var version)
            || !System.Version.TryParse(root.GetProperty("minimumSoftware").GetString(), out var minimum))
            throw new InvalidDataException("Invalid embedded plugin metadata.");
        Hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        if (Hash != root.GetProperty("sha256").GetString()) throw new InvalidDataException("Embedded package hash mismatch.");
        using var stream = new MemoryStream(bytes, false);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        var candidates = archive.Entries.Where(e => e.FullName == "manifest.json" || e.FullName.EndsWith("/manifest.json", StringComparison.Ordinal)).ToArray();
        if (candidates.Length != 1 || candidates[0].Length > 65536) throw new InvalidDataException("Invalid package manifest.");
        using var manifestStream = candidates[0].Open();
        using var manifest = JsonDocument.Parse(manifestStream);
        if (manifest.RootElement.GetProperty("UUID").GetString() != StreamDeckDetector.PluginId
            || manifest.RootElement.GetProperty("Version").GetString() != version.ToString(4)
            || manifest.RootElement.GetProperty("Software").GetProperty("MinimumVersion").GetString() != minimum.ToString())
            throw new InvalidDataException("Packaged manifest differs from embedded metadata.");
        this.bytes = (byte[])bytes.Clone();
        Version = version;
        MinimumSoftware = minimum;
    }

    internal static EmbeddedPluginPackage? Load()
    {
        var assembly = typeof(EmbeddedPluginPackage).Assembly;
        using var package = assembly.GetManifestResourceStream("TinyHand.StreamDeck.Package");
        using var metadata = assembly.GetManifestResourceStream("TinyHand.StreamDeck.Metadata");
        if (package == null && metadata == null) return null;
        if (package == null || metadata == null) throw new InvalidDataException("Incomplete embedded plugin resources.");
        using var bytes = new MemoryStream(); package.CopyTo(bytes);
        using var reader = new StreamReader(metadata);
        return new(bytes.ToArray(), reader.ReadToEnd());
    }

    internal string Extract(string root)
    {
        string directory = Path.Combine(Path.GetFullPath(root), Version.ToString(4));
        RejectReparse(directory);
        Directory.CreateDirectory(directory);
        RejectReparse(directory);
        string path = Path.Combine(directory, StreamDeckDetector.PluginId + ".streamDeckPlugin");
        RejectReparse(path);
        if (File.Exists(path))
        {
            if (Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant() != Hash)
                throw new InvalidDataException("Existing package was modified.");
            return path;
        }
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllBytes(temporary, bytes);
            RejectReparse(directory);
            File.Move(temporary, path);
            RejectReparse(path);
            if (Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant() != Hash)
                throw new InvalidDataException("Extracted package hash mismatch.");
            return path;
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    private static void RejectReparse(string path)
    {
        for (string? current = Path.GetFullPath(path); current != null; current = Path.GetDirectoryName(current))
            if ((Directory.Exists(current) || File.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Plugin extraction refuses reparse targets.");
    }
}
