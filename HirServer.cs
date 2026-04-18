#pragma warning disable SA1400, SA1649, SA1402, SA1502, SA1128, SA1501, SA1119, SA1503, SA1513, SA1413, S6608, PT005
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

var Port = Srv.PortRangeStart;
HttpListener? Listener = null;
while (Port <= Srv.PortRangeEnd)
{
    try
    {
        Listener = new HttpListener();
        Listener.Prefixes.Add(string.Concat(Srv.Host, ":", Port.ToString(System.Globalization.CultureInfo.InvariantCulture), "/"));
        Listener.Start();
        break;
    }
    catch { Port++; Listener = null; }
}
if (Listener == null) { Console.Error.WriteLine("Cannot bind port"); return; }

var WwwRoot = Path.Combine(AppContext.BaseDirectory, Srv.WwwRoot);
if (!Directory.Exists(WwwRoot)) WwwRoot = Path.Combine(Directory.GetCurrentDirectory(), Srv.WwwRoot);
var Db = new HirDatabase(Path.Combine(Directory.GetCurrentDirectory(), Srv.DbFile));

Console.WriteLine(string.Concat("hir88 running at http://localhost:", Port.ToString(System.Globalization.CultureInfo.InvariantCulture)));
Console.WriteLine(string.Concat("WebSocket at ws://localhost:", Port.ToString(System.Globalization.CultureInfo.InvariantCulture), Srv.WsPath));
Console.WriteLine(string.Concat("Serving files from ", WwwRoot));

while (true)
{
    var Context = await Listener.GetContextAsync();
    _ = Task.Run(() => HandleRequest(Context, WwwRoot, Db));
}

static async Task HandleRequest(HttpListenerContext Context, string WwwRoot, HirDatabase Db)
{
    try
    {
        if (Context.Request.IsWebSocketRequest)
        {
            var WsContext = await Context.AcceptWebSocketAsync(null);
            await HandleWebSocket(WsContext.WebSocket, Db);
            return;
        }
        Context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
        var Path = Context.Request.Url?.AbsolutePath?.TrimStart('/') ?? string.Empty;
        if (string.IsNullOrEmpty(Path)) Path = Srv.IndexFile;
        var FilePath = System.IO.Path.Combine(WwwRoot, Path.Replace('/', System.IO.Path.DirectorySeparatorChar));
        if (!File.Exists(FilePath)) { Context.Response.StatusCode = Srv.Http404; Context.Response.Close(); return; }
        var Ext = System.IO.Path.GetExtension(FilePath);
        Context.Response.ContentType = Mime.Types.TryGetValue(Ext, out var ContentType) ? ContentType : "application/octet-stream";
        var Bytes = await File.ReadAllBytesAsync(FilePath);
        Context.Response.ContentLength64 = Bytes.Length;
        await Context.Response.OutputStream.WriteAsync(Bytes);
        Context.Response.Close();
    }
    catch { try { Context.Response.StatusCode = Srv.Http500; Context.Response.Close(); } catch { /* closed */ } }
}

static async Task HandleWebSocket(WebSocket Ws, HirDatabase Db)
{
    var Buffer = new byte[1024 * Srv.WsBufferKb];
    var Builder = new StringBuilder();
    try
    {
        while (Ws.State == WebSocketState.Open)
        {
            var Result = await Ws.ReceiveAsync(Buffer, CancellationToken.None);
            if (Result.MessageType == WebSocketMessageType.Close) break;
            Builder.Append(Encoding.UTF8.GetString(Buffer, 0, Result.Count));
            if (!Result.EndOfMessage) continue;
            var Msg = JsonNode.Parse(Builder.ToString());
            Builder.Clear();
            if (Msg == null) continue;
            var Type = Msg[WsMsg.TypeField]?.ToString() ?? string.Empty;
            var Store = Msg[WsMsg.StoreField]?.ToString() ?? string.Empty;
            var Response = Type switch
            {
                WsMsg.TypePut => HandlePut(Db, Store, Msg),
                WsMsg.TypeGet => HandleGet(Db, Store, Msg),
                WsMsg.TypeDelete => HandleDelete(Db, Store, Msg),
                WsMsg.TypeSync => HandleSync(Db, Store),
                _ => new JsonObject { [WsMsg.TypeField] = "error" },
            };
            var ResponseBytes = Encoding.UTF8.GetBytes(Response.ToJsonString());
            await Ws.SendAsync(ResponseBytes, WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
    catch { /* disconnected */ }
}

static JsonObject HandlePut(HirDatabase Db, string Store, JsonNode Msg)
{
    var Key = Msg[WsMsg.KeyField]?.ToString() ?? Guid.NewGuid().ToString("N");
    var Data = Msg[WsMsg.DataField];
    if (Data != null) Db.Put(Store, Key, Data.ToJsonString());
    return new JsonObject { [WsMsg.TypeField] = "ok", [WsMsg.KeyField] = Key };
}

static JsonObject HandleGet(HirDatabase Db, string Store, JsonNode Msg)
{
    var Key = Msg[WsMsg.KeyField]?.ToString() ?? string.Empty;
    var Data = Db.Get(Store, Key);
    var Result = new JsonObject { [WsMsg.TypeField] = "result", [WsMsg.KeyField] = Key };
    if (Data != null) Result[WsMsg.DataField] = JsonNode.Parse(Data);
    return Result;
}

static JsonObject HandleDelete(HirDatabase Db, string Store, JsonNode Msg)
{
    var Key = Msg[WsMsg.KeyField]?.ToString() ?? string.Empty;
    Db.Delete(Store, Key);
    return new JsonObject { [WsMsg.TypeField] = "ok" };
}

static JsonObject HandleSync(HirDatabase Db, string Store)
{
    var Items = Db.GetAll(Store);
    var Arr = new JsonArray();
    foreach (var (Key, Value) in Items) { var Obj = JsonNode.Parse(Value); if (Obj != null) { Obj[WsMsg.KeyField] = Key; Arr.Add(Obj); } }
    return new JsonObject { [WsMsg.TypeField] = WsMsg.TypeSync, [WsMsg.StoreField] = Store, [WsMsg.ItemsField] = Arr };
}

public class HirDatabase
{
    private readonly string FilePath;
    private readonly Dictionary<string, Dictionary<string, string>> Data = new(StringComparer.Ordinal);
    private readonly Lock Lock = new();

    public HirDatabase(string FilePath)
    {
        this.FilePath = FilePath;
        if (File.Exists(FilePath))
        {
            var Json = File.ReadAllText(FilePath);
            Data = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(Json) ?? new(StringComparer.Ordinal);
        }
    }

    private void Save() { lock (Lock) { File.WriteAllText(FilePath, JsonSerializer.Serialize(Data)); } }

    private Dictionary<string, string> GetStore(string Store)
    {
        if (!Data.ContainsKey(Store)) Data[Store] = new(StringComparer.Ordinal);
        return Data[Store];
    }

    public void Put(string Store, string Key, string Value) { lock (Lock) { GetStore(Store)[Key] = Value; Save(); } }
    public string? Get(string Store, string Key) { lock (Lock) { return GetStore(Store).TryGetValue(Key, out var V) ? V : null; } }
    public void Delete(string Store, string Key) { lock (Lock) { GetStore(Store).Remove(Key); Save(); } }
    public IList<(string Key, string Value)> GetAll(string Store) { lock (Lock) { return GetStore(Store).Select(Kv => (Kv.Key, Kv.Value)).ToList(); } }
}
