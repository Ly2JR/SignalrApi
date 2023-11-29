using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Serilog;
using SignalrApi;
using SignalrApi.Consts;
using SignalrApi.Models;
using System.Collections.Concurrent;
using System.Text.Json.Serialization;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.Async(f => f.File("Logs\\log-.txt",
    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}[{Level:u3}] {Message:lj}{NewLine}{Exception}",
    rollingInterval: RollingInterval.Day))
    .MinimumLevel.Information()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateSlimBuilder(args);
    builder.Host.UseSerilog();//»’÷æ
    builder.Services.AddSingleton<ChatManager>();
    builder.Services.AddSingleton<ConcurrentDictionary<string, User>>();
    builder.Services.AddSignalR();
    builder.Services.AddCors(option => option.AddPolicy("SignalR", builder =>
    {
        builder.AllowAnyMethod()
        .AllowAnyHeader()
        .SetIsOriginAllowed(origin => true)
        .AllowCredentials();
    }));
    builder.Services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
    });

    var app = builder.Build();
    app.MapHub<ChatHub>("/chathub", options =>
    {
        options.Transports = HttpTransportType.WebSockets |
        HttpTransportType.LongPolling;
    });
    app.UseCors("SignalR");
    app.UseSerilogRequestLogging();

    //API
    var chatApi = app.MapGroup("/chat");
    chatApi.MapGet("/", ([FromServices] ChatManager chatManager) =>Results.Ok(chatManager.GetAll()));

    chatApi.MapPost("/SendTo", async (PostMsg msg, [FromServices] IHubContext<ChatHub> ctx, [FromServices] ChatManager chatManager) =>
    {
        var host = chatManager.Get(msg.receiver);
        if (host is not null)
        {
            await ctx.Clients.Client(host.ConnectionId!).SendAsync(Contracts.SendClientMessage, msg.sender, msg.sendMsg);
            return Results.Ok("Ok");
        }
        return Results.Ok("Offline");
    });

    chatApi.MapPost("/SendAndReceive", async (PostMsg msg, [FromServices] IHubContext<ChatHub> ctx, [FromServices] ChatManager chatManager) =>
    {
        var host = chatManager.Get(msg.receiver);
        if (host is not null)
        {
            await ctx.Clients.Client(host.ConnectionId!).SendAsync(Contracts.SendClientMessage, msg.sender, msg.sendMsg);
            var cancelTokenSource = new CancellationTokenSource();
            cancelTokenSource.CancelAfter(3000);
            var ret = await ctx.Clients.Client(host.ConnectionId!).InvokeAsync<string>(Contracts.GetClientMessage, cancelTokenSource.Token);
            return Results.Ok(ret);
        }
        return Results.Ok("Offline");
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public record PostMsg(string receiver, string sender, string sendMsg);

[JsonSerializable(typeof(List<User>))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}