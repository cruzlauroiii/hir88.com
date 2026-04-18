#pragma warning disable SA1400, SA1649, SA1402, SA1502, SA1128, SA1501, SA1119, SA1503, SA1513, SA1413, PT005, PT006, MA0016, S2386, S3887
public static class Srv
{
    public const string Host = "http://+";
    public const string WwwRoot = "wwwroot";
    public const string WsPath = "/ws";
    public const string DbFile = "hir88.db.json";
    public const int PortRangeStart = 18800;
    public const int PortRangeEnd = 18899;
    public const string IndexFile = "index.html";
    public const int Http404 = 404;
    public const int Http500 = 500;
    public const int WsBufferKb = 64;
}

public static class Mime
{
    public static readonly Dictionary<string, string> Types = new(StringComparer.OrdinalIgnoreCase)
    {
        [".html"] = "text/html; charset=utf-8",
        [".css"] = "text/css; charset=utf-8",
        [".js"] = "application/javascript; charset=utf-8",
        [".json"] = "application/json; charset=utf-8",
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".svg"] = "image/svg+xml",
        [".ico"] = "image/x-icon",
        [".woff2"] = "font/woff2",
    };
}

public static class WsMsg
{
    public const string TypeSync = "sync";
    public const string TypePut = "put";
    public const string TypeGet = "get";
    public const string TypeDelete = "delete";
    public const string TypeField = "type";
    public const string StoreField = "store";
    public const string KeyField = "key";
    public const string DataField = "data";
    public const string ItemsField = "items";
}
