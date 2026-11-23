using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace TinyHand.Windows;

internal sealed class LocalControlHost : IDisposable
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly PointerController controller;
    private readonly Dispatcher dispatcher;
    private readonly LocalCommands commands;
    private readonly CancellationTokenSource stop = new();
    private readonly ConcurrentDictionary<int, Client> clients = new();
    private readonly string instanceId = Guid.NewGuid().ToString("N");
    private long revision;
    private int nextClient;
    private readonly string pipeName;
    internal static string PipeName => $"TinyHand.Windows.Control.v1.{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(WindowsIdentity.GetCurrent().User!.Value))).ToLowerInvariant()}.{Process.GetCurrentProcess().SessionId}";
    internal string Name => pipeName;

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetNamedPipeClientProcessId(Microsoft.Win32.SafeHandles.SafePipeHandle pipe, out uint processId);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetNamedPipeClientSessionId(Microsoft.Win32.SafeHandles.SafePipeHandle pipe, out uint sessionId);
    [StructLayout(LayoutKind.Sequential)]
    private struct SecurityAttributes { internal int Length; internal IntPtr Descriptor; internal int Inherit; }
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ConvertStringSecurityDescriptorToSecurityDescriptor(string descriptor, uint revision, out IntPtr security, out uint size);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern Microsoft.Win32.SafeHandles.SafePipeHandle CreateNamedPipe(string name, uint openMode, uint pipeMode,
        uint instances, uint outputSize, uint inputSize, uint timeout, ref SecurityAttributes attributes);
    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr memory);

    private NamedPipeServerStream CreateServer()
    {
        string sid = WindowsIdentity.GetCurrent().User!.Value;
        if (!ConvertStringSecurityDescriptorToSecurityDescriptor($"D:P(A;;GA;;;{sid})", 1, out var descriptor, out _))
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        try
        {
            var attributes = new SecurityAttributes { Length = Marshal.SizeOf<SecurityAttributes>(), Descriptor = descriptor };
            var handle = CreateNamedPipe(@"\\.\pipe\" + pipeName, 0x40000003, 0x8, 5, 32768, 16384, 0, ref attributes);
            if (handle.IsInvalid) { int error = Marshal.GetLastWin32Error(); handle.Dispose(); throw new IOException("Pipe creation failed", new System.ComponentModel.Win32Exception(error)); }
            try { return new NamedPipeServerStream(PipeDirection.InOut, true, false, handle); }
            catch { handle.Dispose(); throw; }
        }
        finally { LocalFree(descriptor); }
    }

    internal LocalControlHost(PointerController controller, Dispatcher dispatcher, string? name = null)
    {
        this.controller = controller;
        this.dispatcher = dispatcher;
        commands = new(controller);
        pipeName = name ?? PipeName;
        controller.StateChanged += Changed;
        _ = ListenAsync();
    }

    private object Envelope(string type) => new { type, instanceId, revision, state = controller.State };
    private void Changed()
    {
        revision++;
        foreach (var client in clients.Values.Where(c => c.Ready)) client.Send(Envelope("state"));
    }

    private async Task ListenAsync()
    {
        while (!stop.IsCancellationRequested)
        {
            NamedPipeServerStream? pipe = null;
            try
            {
                pipe = CreateServer();
                await pipe.WaitForConnectionAsync(stop.Token);
                if (clients.Count >= 4 || !SameSession(pipe)) { pipe.Dispose(); continue; }
                int id = Interlocked.Increment(ref nextClient);
                var client = new Client(pipe);
                clients[id] = client;
                pipe = null;
                _ = ServeAsync(id, client);
            }
            catch (Exception e) when (e is IOException or OperationCanceledException or UnauthorizedAccessException)
            {
                pipe?.Dispose();
                if (!stop.IsCancellationRequested) await Task.Delay(200, stop.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            }
        }
    }

    private static bool SameSession(NamedPipeServerStream pipe)
    {
        // CurrentUserOnly checks the token; session verification also excludes another login.
        if (!GetNamedPipeClientSessionId(pipe.SafePipeHandle, out uint session) || session != Process.GetCurrentProcess().SessionId) return false;
        if (!GetNamedPipeClientProcessId(pipe.SafePipeHandle, out uint pid)) return false;
        try { return Process.GetProcessById((int)pid).SessionId == Process.GetCurrentProcess().SessionId; }
        catch (ArgumentException) { return false; }
    }

    private async Task ServeAsync(int id, Client client)
    {
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(stop.Token, client.Stop.Token);
        var writer = client.WriteAsync(lifetime.Token);
        try
        {
            using (var handshake = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token))
            {
                handshake.CancelAfter(TimeSpan.FromSeconds(1));
                using var hello = await ReadAsync(client.Pipe, handshake.Token);
                if (hello == null) return;
                var root = hello.RootElement;
                if (!Fields(root, "type", "version") || root.GetProperty("type").ValueKind != JsonValueKind.String || root.GetProperty("type").GetString() != "hello") return;
                if (root.GetProperty("version").ValueKind != JsonValueKind.Number || !root.GetProperty("version").TryGetInt32(out int version) || version != 1)
                {
                    client.Send(new { type = "incompatible", version = 1 });
                    client.Output.Writer.TryComplete();
                    await writer;
                    return;
                }
                await dispatcher.InvokeAsync(() =>
                {
                    client.Send(new { type = "hello", version = 1, instanceId, revision, state = controller.State,
                        capabilities = new[] { "pointer", "size", "sound", "color", "text", "clear", "capture" } });
                    client.Ready = true;
                });
            }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            while (!lifetime.IsCancellationRequested)
            {
                using var document = await ReadAsync(client.Pipe, lifetime.Token);
                if (document == null) break;
                var root = document.RootElement;
                if (!root.TryGetProperty("id", out var idValue) || idValue.ValueKind != JsonValueKind.String) break;
                string request = idValue.GetString()!;
                if (request.Length is < 1 or > 64 || request.Any(c => c > 127) || ids.Count >= 256) break;
                if (!ids.Add(request)) { await Reply("invalidArgument"); continue; }
                if (!root.TryGetProperty("type", out var type) || type.ValueKind != JsonValueKind.String) break;
                if (type.GetString() == "getState" && Fields(root, "type", "id")) { await Reply(null); continue; }
                if (type.GetString() != "command" || !Fields(root, "type", "id", "command", "arguments")
                    || root.GetProperty("command").ValueKind != JsonValueKind.String) { await Reply("invalidArgument"); continue; }
                string command = root.GetProperty("command").GetString()!;
                var arguments = root.GetProperty("arguments").Clone();
                try
                {
                    var operation = await dispatcher.InvokeAsync(() => commands.ExecuteAsync(command, arguments));
                    var result = await operation.WaitAsync(TimeSpan.FromSeconds(6), lifetime.Token);
                    await dispatcher.InvokeAsync(() => client.Send(new { type = "result", id = request, result.Ok, result.Error, result.Warning,
                        instanceId, revision, state = controller.State }));
                }
                catch (TimeoutException) { await Reply("timeout"); }
                catch (Exception e) when (e is not OperationCanceledException and not OutOfMemoryException) { await Reply("operationFailed"); }

                async Task Reply(string? error) => await dispatcher.InvokeAsync(() => client.Send(new
                    { type = "result", id = request, ok = error == null, error, instanceId, revision, state = controller.State }));
            }
        }
        catch (Exception e) when (e is IOException or JsonException or OperationCanceledException or InvalidOperationException or DecoderFallbackException) { }
        finally
        {
            clients.TryRemove(id, out _);
            client.Dispose();
            await writer.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
    }

    private static bool Fields(JsonElement root, params string[] fields) => root.ValueKind == JsonValueKind.Object
        && root.EnumerateObject().Select(p => p.Name).Order().SequenceEqual(fields.Order());

    private static async Task<JsonDocument?> ReadAsync(Stream pipe, CancellationToken token)
    {
        var bytes = new List<byte>();
        var one = new byte[1];
        using var frameDeadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        frameDeadline.CancelAfter(TimeSpan.FromSeconds(30));
        while (await pipe.ReadAsync(one, frameDeadline.Token) != 0)
        {
            if (one[0] == 10)
            {
                var document = JsonDocument.Parse(new UTF8Encoding(false, true).GetString(bytes.ToArray()), new JsonDocumentOptions { MaxDepth = 16 });
                if (document.RootElement.ValueKind != JsonValueKind.Object || !Unique(document.RootElement)) { document.Dispose(); throw new JsonException("Invalid object or duplicate fields."); }
                return document;
            }
            if (bytes.Count == 16384) throw new JsonException("Frame too large.");
            bytes.Add(one[0]);
        }
        return null;
    }

    private static bool Unique(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject()) if (!names.Add(property.Name) || !Unique(property.Value)) return false;
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var item in element.EnumerateArray()) if (!Unique(item)) return false;
        return true;
    }

    private sealed class Client : IDisposable
    {
        internal readonly NamedPipeServerStream Pipe;
        internal readonly CancellationTokenSource Stop = new();
        internal readonly Channel<byte[]> Output = Channel.CreateBounded<byte[]>(16);
        internal volatile bool Ready;
        private int queued;
        internal Client(NamedPipeServerStream pipe) => Pipe = pipe;
        internal void Send(object message)
        {
            var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, Json) + "\n");
            if (Interlocked.Add(ref queued, bytes.Length) > 32768 || !Output.Writer.TryWrite(bytes)) Dispose();
        }
        internal async Task WriteAsync(CancellationToken token)
        {
            try
            {
                await foreach (var bytes in Output.Reader.ReadAllAsync(token))
                {
                    await Pipe.WriteAsync(bytes, token).AsTask().WaitAsync(TimeSpan.FromSeconds(1), token);
                    Interlocked.Add(ref queued, -bytes.Length);
                }
            }
            catch (Exception e) when (e is IOException or OperationCanceledException or TimeoutException or ObjectDisposedException) { Dispose(); }
        }
        public void Dispose() { Stop.Cancel(); Output.Writer.TryComplete(); Pipe.Dispose(); }
    }

    public void Dispose()
    {
        controller.StateChanged -= Changed;
        stop.Cancel();
        foreach (var client in clients.Values) client.Dispose();
    }
}
