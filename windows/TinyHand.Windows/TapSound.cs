using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using System.Threading;

namespace TinyHand.Windows;

internal sealed class TapSound : IDisposable
{
    internal static readonly string[] Names = { "thock", "click", "pop", "wood", "boop", "quack" };
    private readonly Dictionary<string, byte[]> samples = new();
    private readonly List<IntPtr> buffers = new();
    private readonly HashSet<IntPtr> silentBuffers = new();
    private readonly object gate = new();
    private readonly byte[] silence = new byte[1764]; // 20 ms, signed PCM zero
    private Timer? pump;
    private bool keepAlive;
    private IntPtr device;
    private bool disposed;

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    private struct WaveFormat
    {
        internal ushort Format, Channels;
        internal uint SamplesPerSecond, BytesPerSecond;
        internal ushort BlockAlign, BitsPerSample, ExtraSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WaveHeader
    {
        internal IntPtr Data;
        internal uint Length, Recorded;
        internal UIntPtr User;
        internal uint Flags, Loops;
        internal IntPtr Next;
        internal UIntPtr Reserved;
    }

    [DllImport("winmm.dll")]
    private static extern uint waveOutOpen(out IntPtr handle, uint deviceId, ref WaveFormat format, IntPtr callback, IntPtr instance, uint flags);
    [DllImport("winmm.dll")]
    private static extern uint waveOutPrepareHeader(IntPtr handle, IntPtr header, uint size);
    [DllImport("winmm.dll")]
    private static extern uint waveOutWrite(IntPtr handle, IntPtr header, uint size);
    [DllImport("winmm.dll")]
    private static extern uint waveOutUnprepareHeader(IntPtr handle, IntPtr header, uint size);
    [DllImport("winmm.dll")]
    private static extern uint waveOutReset(IntPtr handle);
    [DllImport("winmm.dll")]
    private static extern uint waveOutClose(IntPtr handle);

    private static readonly uint HeaderSize = (uint)Marshal.SizeOf<WaveHeader>();

    private static void Check(uint result)
    {
        if (result != 0) throw new InvalidOperationException($"Windows audio error: {result}");
    }

    private byte[] LoadSamples(string name)
    {
        using var source = OpenSound(name);
        using var reader = new BinaryReader(source);
        if (reader.ReadUInt32() != 0x46464952) throw new IOException("Missing RIFF header");
        reader.ReadUInt32();
        if (reader.ReadUInt32() != 0x45564157) throw new IOException("Missing WAVE header");
        bool validFormat = false;
        byte[]? data = null;
        while (source.Position + 8 <= source.Length)
        {
            uint chunk = reader.ReadUInt32();
            uint size = reader.ReadUInt32();
            long end = source.Position + size;
            if (end > source.Length) throw new IOException("Truncated WAV chunk");
            if (chunk == 0x20746d66 && size >= 16)
            {
                validFormat = reader.ReadUInt16() == 1;
                validFormat &= reader.ReadUInt16() == 1;
                validFormat &= reader.ReadUInt32() == 44100;
                validFormat &= reader.ReadUInt32() == 88200;
                validFormat &= reader.ReadUInt16() == 2;
                validFormat &= reader.ReadUInt16() == 16;
            }
            else if (chunk == 0x61746164) data = reader.ReadBytes(checked((int)size));
            source.Position = end + (size & 1);
        }
        if (!validFormat || data == null || data.Length == 0 || data.Length % 2 != 0)
            throw new IOException("Expected mono 44.1 kHz, 16-bit PCM sound");
        return data;
    }

    // The UI thread and background pump share buffers under gate. Native memory
    // stays allocated until Windows marks it done; another click never resets it.
    internal int PendingCount
    {
        get
        {
            lock (gate)
            {
                ReapBuffers();
                return buffers.Count - silentBuffers.Count;
            }
        }
    }

    internal bool KeepingAlive { get { lock (gate) return keepAlive && silentBuffers.Count > 0; } }

    private void ReapBuffers()
    {
        for (int i = buffers.Count - 1; i >= 0; i--)
        {
            var header = Marshal.PtrToStructure<WaveHeader>(buffers[i]);
            if ((header.Flags & 1) == 0) continue; // WHDR_DONE
            Check(waveOutUnprepareHeader(device, buffers[i], HeaderSize));
            Marshal.FreeHGlobal(header.Data);
            Marshal.FreeHGlobal(buffers[i]);
            silentBuffers.Remove(buffers[i]);
            buffers.RemoveAt(i);
        }
    }

    internal void SetKeepAlive(bool enabled)
    {
        lock (gate)
        {
            if (disposed) return;
            keepAlive = enabled;
            if (enabled)
            {
                try { ServiceAudio(); }
                catch (InvalidOperationException e) { AudioLog.Write($"Audio keep-alive failed: {e}"); keepAlive = false; }
            }
            AudioLog.Write($"Audio keep-alive: {keepAlive}");
        }
    }

    private void OpenDevice()
    {
        if (device != IntPtr.Zero) return;
        var format = new WaveFormat { Format = 1, Channels = 1, SamplesPerSecond = 44100,
            BytesPerSecond = 88200, BlockAlign = 2, BitsPerSample = 16 };
        Check(waveOutOpen(out device, uint.MaxValue, ref format, IntPtr.Zero, IntPtr.Zero, 0));
        pump ??= new Timer(_ =>
        {
            lock (gate)
            {
                if (disposed) return;
                try { ServiceAudio(); }
                catch (InvalidOperationException e)
                {
                    keepAlive = false;
                    pump?.Change(Timeout.Infinite, Timeout.Infinite);
                    AudioLog.Write($"Audio pump failed: {e}");
                }
            }
        }, null, Timeout.Infinite, Timeout.Infinite);
        pump.Change(10, 10);
    }

    private void ServiceAudio()
    {
        ReapBuffers();
        if (keepAlive)
        {
            OpenDevice();
            // Keep at most 60 ms of silence queued. Clicks use this same stream,
            // so there is no idle gap and no separate keep-alive device/session.
            while (buffers.Count < 3) Queue(silence, true);
        }
        else if (device != IntPtr.Zero && buffers.Count == 0)
        {
            Check(waveOutClose(device));
            device = IntPtr.Zero;
            pump?.Change(Timeout.Infinite, Timeout.Infinite);
        }
    }

    private void Queue(byte[] data, bool silent)
    {
        IntPtr pcm = Marshal.AllocHGlobal(data.Length);
        IntPtr pointer = IntPtr.Zero;
        bool prepared = false;
        try
        {
            Marshal.Copy(data, 0, pcm, data.Length);
            pointer = Marshal.AllocHGlobal((int)HeaderSize);
            Marshal.StructureToPtr(new WaveHeader { Data = pcm, Length = (uint)data.Length }, pointer, false);
            Check(waveOutPrepareHeader(device, pointer, HeaderSize));
            prepared = true;
            Check(waveOutWrite(device, pointer, HeaderSize));
            buffers.Add(pointer);
            if (silent) silentBuffers.Add(pointer);
        }
        catch
        {
            if (prepared) Check(waveOutUnprepareHeader(device, pointer, HeaderSize));
            if (pointer != IntPtr.Zero) Marshal.FreeHGlobal(pointer);
            Marshal.FreeHGlobal(pcm);
            throw;
        }
    }

    internal static Stream OpenSound(string name) =>
        typeof(TapSound).Assembly.GetManifestResourceStream($"TinyHand.Sounds.tap_{name}.wav")
        ?? throw new IOException($"Embedded sound not found: {name}");

    internal static void ValidateResources()
    {
        foreach (string name in Names)
        {
            using var stream = OpenSound(name);
            using var player = new SoundPlayer(stream);
            player.Load();
            if (!player.IsLoadCompleted) throw new IOException($"Sound did not load: {name}");
        }
    }

    internal void Play(string name)
    {
        lock (gate)
        {
            if (disposed) return;
            try
            {
                AudioLog.Write($"Play requested: {name}");
                // Bound bursts rather than cutting off clips or delaying sound.
                if (PendingCount >= 4) return;
                if (!samples.TryGetValue(name, out var data))
                {
                    data = LoadSamples(name);
                    samples.Add(name, data);
                }
                OpenDevice();
                Queue(data, false);
                AudioLog.Write($"PCM queued: {data.Length} bytes (complete clips, no PlaySound restart)");
            }
            catch (Exception e) when (e is IOException or InvalidOperationException or System.TimeoutException)
            {
                AudioLog.Write($"Playback failed: {e}");
            }
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed = true;
            keepAlive = false;
            pump?.Dispose();
            if (device == IntPtr.Zero) return;
            Check(waveOutReset(device));
            ReapBuffers();
            Check(waveOutClose(device));
            device = IntPtr.Zero;
        }
    }
}
