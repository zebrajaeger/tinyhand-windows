using System;
using System.Runtime.InteropServices;
using System.Threading;
using TinyHand.Windows;

internal static class AudioRegression
{
    [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PlaySound(string? sound, IntPtr module, uint flags);

    internal static int Run()
    {
        using var audio = new TapSound();
        audio.Play("quack");
        if (audio.PendingCount != 1) throw new Exception("First quack was not queued.");
        // The legacy player occupies the process-wide PlaySound slot. Another
        // click therefore replaces that sound instead of preserving its tail.
        bool slotFree = PlaySound(null, IntPtr.Zero, 0x10); // SND_NOSTOP
        if (!slotFree)
        {
            Console.Error.WriteLine("FAIL: tap playback occupies the interruptible PlaySound slot.");
            return 1;
        }
        audio.Play("quack");
        if (audio.PendingCount != 2) throw new Exception("Second click replaced the first clip.");
        for (int i = 0; i < 10; i++) audio.Play("quack");
        if (audio.PendingCount != 4) throw new Exception("Click burst queue is not bounded.");
        Thread.Sleep(1000);
        if (audio.PendingCount != 0) throw new Exception("Queued clips did not complete.");
        foreach (string name in TapSound.Names)
        {
            audio.Play(name);
            if (audio.PendingCount != 1) throw new Exception("Sound was not queued: " + name);
            Thread.Sleep(400);
            if (audio.PendingCount != 0) throw new Exception("Sound did not complete: " + name);
        }
        audio.SetKeepAlive(true);
        if (!audio.KeepingAlive) throw new Exception("Silent keep-alive did not start.");
        for (int i = 0; i < 3; i++)
        {
            Thread.Sleep(3000);
            if (!audio.KeepingAlive || audio.PendingCount != 0)
                throw new Exception("Audio stream went idle during a three-second click pause.");
            audio.Play("quack");
            if (audio.PendingCount != 1) throw new Exception("First click after pause was not queued.");
            Thread.Sleep(400);
            if (audio.PendingCount != 0) throw new Exception("First clip after pause did not complete.");
        }
        audio.SetKeepAlive(false);
        Thread.Sleep(200);
        if (audio.KeepingAlive || audio.PendingCount != 0) throw new Exception("Keep-alive did not stop.");
        audio.SetKeepAlive(true);
        audio.Play("quack");
        audio.Dispose();
        audio.Dispose();
        audio.Play("quack");
        if (audio.PendingCount != 0) throw new Exception("Disposal left active sound buffers.");
        if (audio.KeepingAlive) throw new Exception("Disposal left silent keep-alive active.");
        Console.WriteLine("PASS: complete clips, bounded bursts, six sounds, three-second pauses, keep-alive restart and cleanup.");
        return 0;
    }
}
