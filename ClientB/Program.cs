using Microsoft.AspNetCore.SignalR.Client;

HubConnection connection = null;
string temp = string.Empty;
Console.Clear();
var suc=await ConnectAsync();
if (!suc) return;
ConsoleKeyInfo pressKey;

Console.CancelKeyPress += Console_CancelKeyPress;

void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
{
    e.Cancel = true;
}

do
{
    Console.WriteLine("按Q键退出程序，或CTRL+C中断操作");
    pressKey = Console.ReadKey(true);

    Console.Write("向客户端A发送消息:");
    var msg = Console.ReadLine();
    if (connection != null)
    {
        //await connection.SendAsync("OnlySendToClient", "clientA", msg);
        var clientA = await connection.InvokeAsync<string>("SendToClientAndReceive", "clientA", msg);
        Console.WriteLine(clientA);
    }
} while (pressKey.Key!=ConsoleKey.Q);
await CloseConnectionAsync();

async Task<bool> ConnectAsync()
{
    try
    {
        connection = new HubConnectionBuilder()
        .WithUrl("http://localhost:5214/chathub?user=clientB&type=1")
        .WithAutomaticReconnect()
        .Build();
        connection.On<string, string>("GetHubMessage", (userId, msg) =>
        {
            Console.WriteLine($"来自客户端B[{userId}]消息:{msg}");
        });
        connection.On("InvokeClientMessage", () =>
        {
            return "客户端B已收到";
        });
        await connection.StartAsync();
        Console.WriteLine("客户端[clientB]成功连接到chathub");
        return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"启动异常:{ex.Message}");
    }
    return false;
}

async Task CloseConnectionAsync()
{
    if (connection != null)
    {
        if (connection.State != HubConnectionState.Disconnected)
        {
            await connection.StopAsync();
        }
        await connection.DisposeAsync();
    };
    Console.WriteLine("离开chathub");
}

