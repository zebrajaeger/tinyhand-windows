using System;
using System.IO;
using System.IO.Pipes;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using TinyHand.Windows;

internal static class ControlRegression
{
    private static void Pump(Task task)
    {
        var frame = new DispatcherFrame();
        task.ContinueWith(_ => Application.Current.Dispatcher.BeginInvoke(() => frame.Continue = false), TaskScheduler.Default);
        Dispatcher.PushFrame(frame);
        task.GetAwaiter().GetResult();
    }

    internal static void Run(Action<bool, string> check)
    {
        string directory = Path.Combine(Path.GetTempPath(), "TinyHand-control-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            using var controller = new PointerController(Path.Combine(directory, "settings.json"));
            var commands = new LocalCommands(controller);
            CommandResult Command(string name, string args = "{}") => commands.ExecuteAsync(name, JsonDocument.Parse(args).RootElement).GetAwaiter().GetResult();
            check(Command("pointer.setActive", "{\"active\":false}").Ok && !controller.Active, "Explicit pointer-off is idempotent through commands");
            check(Command("pointer.toggle").Ok && controller.Active && Command("pointer.toggle").Ok && !controller.Active, "Pointer toggle executes once per direct command");
            check(Command("pointer.setSize", "{\"height\":220}").Ok && controller.PointerHeight == 220, "Fixed size command uses supported preset");
            controller.SetPointerHeight(300);
            check(Command("pointer.cycleSize").Ok && controller.PointerHeight == 340, "Command size cycle advances a legacy custom height");
            check(!Command("pointer.setSize", "{\"height\":999}").Ok && controller.PointerHeight == 340, "Invalid command size causes no mutation");
            check(Command("pointer.setSize", "{\"height\":\"220\"}").Error == "invalidArgument", "Wrong size type is rejected as invalid argument, not an execution failure");
            check(Command("annotation.setColor", "{\"color\":\"#abcdef\"}").Ok && controller.SelectedColor == "#ABCDEF", "External color validates and normalizes");
            check(!Command("annotation.setColor", "{\"color\":\"red\"}").Ok && controller.SelectedColor == "#ABCDEF", "Invalid command color causes no mutation");
            check(Command("annotation.beginText").Ok && Command("annotation.beginText").Error == "busy", "External text arms once and repeats report busy");
            check(Command("annotation.clear").Ok && controller.TextPhase == "idle", "External clear cancels pending text");
            check(Command("execute", "{\"file\":\"bad.exe\"}").Error == "unsupportedCommand", "Process execution is not on control allowlist");
            check(Command("sound.setEnabled", "{\"enabled\":false,\"extra\":true}").Error == "invalidArgument", "Extra command fields rejected before mutation");
            check(Command("sound.setEnabled", "{\"enabled\":false}").Ok && !controller.State.SoundEnabled, "Explicit sound-off command preserves disabled audio policy");
            check(Command("sound.toggle").Ok && controller.State.SoundEnabled && Command("sound.toggle").Ok && !controller.State.SoundEnabled,
                "Sound toggle updates authoritative state without enabling pointer");
            string blocked = Path.Combine(directory, "blocked");
            File.WriteAllText(blocked, "not a directory");
            using (var unwritable = new PointerController(Path.Combine(blocked, "settings.json")))
            {
                var warning = new LocalCommands(unwritable).ExecuteAsync("annotation.setColor", JsonDocument.Parse("{\"color\":\"#123456\"}").RootElement).GetAwaiter().GetResult();
                check(warning.Ok && warning.Warning == "persistenceFailed" && unwritable.SelectedColor == "#123456",
                    "Persistence failure reports warning while retaining session-only change");
            }
            string pipe = "TinyHand.Test." + Guid.NewGuid().ToString("N");
            using var host = new LocalControlHost(controller, Application.Current.Dispatcher, pipe);
            Pump(Task.Run(async () =>
            {
                using var client = new NamedPipeClientStream(".", pipe, PipeDirection.InOut, PipeOptions.Asynchronous);
                using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await client.ConnectAsync(deadline.Token);
                using var reader = new StreamReader(client, Encoding.UTF8, false, 1024, true);
                using var writer = new StreamWriter(client, new UTF8Encoding(false), 1024, true) { AutoFlush = true };
                await writer.WriteLineAsync("{\"type\":\"hello\",\"version\":1}");
                using var hello = JsonDocument.Parse((await reader.ReadLineAsync(deadline.Token))!);
                check(hello.RootElement.GetProperty("type").GetString() == "hello", "Real named pipe handshake returns authoritative snapshot");
                await writer.WriteLineAsync("{\"type\":\"command\",\"id\":\"1\",\"command\":\"annotation.setColor\",\"arguments\":{\"color\":\"#112233\"}}");
                bool eventReceived = false;
                while (true)
                {
                    using var response = JsonDocument.Parse((await reader.ReadLineAsync(deadline.Token))!);
                    if (response.RootElement.GetProperty("type").GetString() == "state") { eventReceived = true; continue; }
                    check(response.RootElement.GetProperty("ok").GetBoolean() && eventReceived, "Command mutation produces state event and correlated success");
                    break;
                }
                await writer.WriteLineAsync("{\"type\":\"command\",\"id\":\"1\",\"command\":\"pointer.toggle\",\"arguments\":{}}");
                using var duplicate = JsonDocument.Parse((await reader.ReadLineAsync(deadline.Token))!);
                check(duplicate.RootElement.GetProperty("error").GetString() == "invalidArgument", "Duplicate request ID rejected without executing toggle");
            }));
            check(!controller.Active && controller.SelectedColor == "#112233", "Pipe commands share authoritative controller state");
            foreach (string malformed in new[] { "[]", "{\"type\":1}", "{\"type\":\"hello\",\"type\":\"hello\",\"version\":1}", new string('x', 16386) })
            {
                Pump(Task.Run(async () =>
                {
                    using var client = new NamedPipeClientStream(".", pipe, PipeDirection.InOut, PipeOptions.Asynchronous);
                    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    await client.ConnectAsync(deadline.Token);
                    await client.WriteAsync(Encoding.UTF8.GetBytes(malformed + "\n"), deadline.Token);
                    try
                    {
                        var one = new byte[1];
                        check(await client.ReadAsync(one, deadline.Token) == 0, "Malformed/duplicate/oversized handshake is disconnected");
                    }
                    catch (IOException) { check(true, "Malformed handshake closes pipe"); }
                }));
            }
            Pump(Task.Run(async () =>
            {
                using var client = new NamedPipeClientStream(".", pipe, PipeDirection.InOut, PipeOptions.Asynchronous);
                using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await client.ConnectAsync(deadline.Token);
                using var writer = new StreamWriter(client, new UTF8Encoding(false), 1024, true) { AutoFlush = true };
                using var reader = new StreamReader(client, Encoding.UTF8, false, 1024, true);
                await writer.WriteLineAsync("{\"type\":\"hello\",\"version\":99}");
                using var response = JsonDocument.Parse((await reader.ReadLineAsync(deadline.Token))!);
                check(response.RootElement.GetProperty("type").GetString() == "incompatible", "Unsupported protocol receives explicit incompatibility response");
            }));
            Pump(Task.Run(async () =>
            {
                using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var held = new List<NamedPipeClientStream>();
                try
                {
                    for (int i = 0; i < 4; i++)
                    {
                        var client = new NamedPipeClientStream(".", pipe, PipeDirection.InOut, PipeOptions.Asynchronous);
                        held.Add(client);
                        await client.ConnectAsync(deadline.Token);
                        await client.WriteAsync(Encoding.UTF8.GetBytes("{\"type\":\"hello\",\"version\":1}\n"), deadline.Token);
                        using var reader = new StreamReader(client, Encoding.UTF8, false, 1024, true);
                        check((await reader.ReadLineAsync(deadline.Token))?.Contains("\"hello\"") == true, "Four bounded clients can negotiate normally");
                    }
                    using var excess = new NamedPipeClientStream(".", pipe, PipeDirection.InOut, PipeOptions.Asynchronous);
                    await excess.ConnectAsync(deadline.Token);
                    try { check(await excess.ReadAsync(new byte[1], deadline.Token) == 0, "Fifth client is rejected without blocking existing clients"); }
                    catch (IOException) { check(true, "Excess client is disconnected"); }
                }
                finally { foreach (var client in held) client.Dispose(); }
            }));
            Pump(Task.Run(async () =>
            {
                using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                using var stalled = new NamedPipeClientStream(".", pipe, PipeDirection.InOut, PipeOptions.Asynchronous);
                await stalled.ConnectAsync(deadline.Token);
                // No hello: the server must release this connection after the handshake deadline.
                check(await stalled.ReadAsync(new byte[1], deadline.Token) == 0, "Stalled handshake is disconnected by bounded deadline");
            }));
            Pump(Task.Run(async () =>
            {
                using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                using var flooded = new NamedPipeClientStream(".", pipe, PipeDirection.InOut, PipeOptions.Asynchronous);
                await flooded.ConnectAsync(deadline.Token);
                await flooded.WriteAsync(Encoding.UTF8.GetBytes("{\"type\":\"hello\",\"version\":1}\n"), deadline.Token);
                using var reader = new StreamReader(flooded, Encoding.UTF8, false, 1024, true);
                await reader.ReadLineAsync(deadline.Token);
                var burst = new StringBuilder();
                for (int i = 0; i < 257; i++) burst.Append("{\"type\":\"getState\",\"id\":\"").Append(i).Append("\"}\n");
                try { await flooded.WriteAsync(Encoding.UTF8.GetBytes(burst.ToString()), deadline.Token); }
                catch (IOException) { }
                // Drain only after submitting the burst: either the bounded outbound queue or ID limit closes it.
                try { while (await reader.ReadLineAsync(deadline.Token) != null) { } }
                catch (IOException) { }
                check(true, "Request-ID/outbound flood terminates without an unbounded client backlog");
            }));
            string? node = Environment.GetEnvironmentVariable("TINYHAND_NODE_INTEROP");
            if (!string.IsNullOrEmpty(node))
            {
                using var process = new Process { StartInfo = new ProcessStartInfo("node") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true } };
                process.StartInfo.ArgumentList.Add(node);
                process.StartInfo.ArgumentList.Add(pipe);
                process.Start();
                Pump(process.WaitForExitAsync());
                check(process.ExitCode == 0 && controller.SelectedColor == "#778899", "Actual Node client interoperates with .NET named-pipe handshake, events and command result: " + process.StandardError.ReadToEnd());
            }
        }
        finally
        {
            foreach (var file in Directory.EnumerateFiles(directory)) File.Delete(file);
            Directory.Delete(directory);
        }
    }
}
