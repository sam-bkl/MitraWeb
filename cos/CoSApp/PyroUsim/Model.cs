
namespace CosApp.PyroUsim
{
    public sealed class PyroUsimSimSaleRequest
{
    public required string SimVendor { get; init; }
    public required string CircleId { get; init; }
    public required string Msisdn { get; init; }
    public required string Iccid { get; init; }
    public required string Brand { get; init; }
    public required bool International { get; init; }
    public required string SimType { get; init; }
    public required string ChannelName { get; init; }
    public required string MethodName { get; init; }
}

internal sealed class PyroUsimSimSaleWireRequest
{
    public string simVendor { get; init; } = default!;
    public string circleId { get; init; } = default!;
    public string msisdn { get; init; } = default!;
    public string iccid { get; init; } = default!;
    public string brand { get; init; } = default!;
    public int international { get; init; }
    public string simType { get; init; } = default!;
    public string channelName { get; init; } = default!;
    public string method_name { get; init; } = default!;
    public string transactionId { get; init; } = default!;
}

public sealed class PyroUsimSimSaleResponse
{
    public int StatusCode { get; init; }
    public string Status { get; init; } = default!;
    public string StatusDescription { get; init; } = default!;
    public PyroUsimSimSaleData? Data { get; init; }

    public bool IsSuccess => StatusCode == 200;
}

public sealed class PyroUsimSimSaleData
{
    public string TransactionId { get; init; } = default!;
    public string Imsi { get; init; } = default!;
    public string Msisdn { get; init; } = default!;
    public string Iccid { get; init; } = default!;
    public string Pin1 { get; init; } = default!;
    public string Puk1 { get; init; } = default!;
    public string Pin2 { get; init; } = default!;
    public string Puk2 { get; init; } = default!;
}


 }