using Microsoft.AspNetCore.SignalR.Client;

HubConnection connection=null;
string temp=string.Empty;
Console.Clear();
await ConnectAsync();
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

    Console.Write("向客户端B发送消息:");
    var msg = Console.ReadLine();
    if (connection != null)
    {
        //await connection.SendAsync("OnlySendToClient", "clientB", msg);
        var clientB =await connection.InvokeAsync<string>("SendToClientAndReceive", "clientB", msg);
        Console.WriteLine(clientB);
    }
} while (pressKey.Key != ConsoleKey.Q);
await CloseConnectionAsync();

async Task ConnectAsync()
{
    try
    {
        connection = new HubConnectionBuilder()
        .WithUrl("http://localhost:5214/chathub?user=clientA&type=0")
        .WithAutomaticReconnect()
        .Build();
        connection.On<string, string>("GetHubMessage", (userId, msg) =>
        {
            Console.WriteLine($"来自客户端B[{userId}]消息:{msg}");
        });
        connection.On("InvokeClientMessage", () =>
        {
            return "客户端A已收到"; 
        });
        await connection.StartAsync();
        Console.WriteLine("客户端[clientA]成功连接到chathub");
    }
    catch (Exception ex)
    {
        throw new Exception(ex.Message);
    }
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

