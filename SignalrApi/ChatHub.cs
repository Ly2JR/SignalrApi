using Microsoft.AspNetCore.SignalR;
using SignalrApi.Consts;

namespace SignalrApi
{
    public class ChatHub:Hub<IChatHub>
    {
        private readonly ILogger<ChatHub> _logger;
        private readonly ChatManager _chatManager;
        public ChatHub(ILogger<ChatHub> logger, ChatManager chatManager)
        {
            _logger = logger;
            _chatManager = chatManager;
        }

        public override async Task OnConnectedAsync()
        {
            var ctx = Context.GetHttpContext();
            if (ctx != null)
            {
                if (!ctx.Request.Query.TryGetValue(Contracts.FLAG_NAME, out var oName))
                {
                    _logger.LogWarning($"非法用户连接到服务中心");
                    Context.Abort();
                    return;
                }
                var userId = oName.FirstOrDefault();
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning($"非法用户连接到服务中心");
                    Context.Abort();
                }
                var type = "";
                if (ctx.Request.Query.TryGetValue(Contracts.FLAG_TYPE, out var oType))
                {
                    type = oType.FirstOrDefault();
                }
                _logger.LogInformation($"用户ID[{userId}],ID[{Context.ConnectionId}],用户类型:{type}连接到服务中心");
                _chatManager.AddOrUpdate(Context.ConnectionId, userId!, string.IsNullOrEmpty(type) ? 0u : Convert.ToUInt32(type));
            }
            await base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var ctx = Context.GetHttpContext();
            if (ctx != null)
            {
                if (ctx.Request.Query.TryGetValue(Contracts.FLAG_NAME, out var oName))
                {
                    _logger.LogInformation($"用户[{oName.FirstOrDefault()}],ID[{Context.ConnectionId}]断开连接");
                }
                _chatManager.Remove(Context.ConnectionId);
            }
            return base.OnDisconnectedAsync(exception);
        }

        public async Task OnlySendToClient(string clientId, string message)
        {
            var ctx = Context.GetHttpContext();
            if (ctx != null)
            {
                if (!ctx.Request.Query.TryGetValue(Contracts.FLAG_NAME, out var oName))
                {
                    _logger.LogInformation($"非法用户向客户端发送消息：{message}");
                }
                else
                {
                    var currentClientUser = oName.FirstOrDefault();
                    var host = _chatManager.Get(clientId);
                    if (host is null)
                    {
                        _logger.LogInformation($"客户端用户[{currentClientUser}]向客户端[{clientId}]发送消息：{message},但客户端[{clientId}]不在线");
                        return;
                    }
                    await Clients.Client(host.ConnectionId!).GetHubMessage(currentClientUser, message);
                    _logger.LogInformation($"客户端用户[{currentClientUser}]向成功客户端[{clientId}]发送消息：{message}");
                }
            }
        }

        public async Task<string> SendToClientWithResponse(string clientId, string message)
        {
            var ctx = Context.GetHttpContext();
            if (ctx != null)
            {
                if (!ctx.Request.Query.TryGetValue(Contracts.FLAG_NAME, out var oName))
                {
                    _logger.LogInformation($"非法用户向客户端发送消息：{message}");
                    await Clients.Caller.GetHubMessage(Contracts.UNKNOWN, "非法用户");
                }
                else
                {
                    var currentClientUser = oName.FirstOrDefault();
                    var host = _chatManager.Get(clientId);
                    if (host is null)
                    {
                        _logger.LogInformation($"客户端用户[{currentClientUser}]向客户端[{clientId}]发送消息：{message},但客户端[{clientId}]不在线");
                        return $"客户端[{clientId}]不在线";
                    }
                    await Clients.Client(host.ConnectionId!).GetHubMessage(currentClientUser, message);
                    _logger.LogInformation($"客户端用户[{currentClientUser}]向成功客户端[{clientId}]发送消息：{message}");
                    return $"成功发送到客户端[{clientId}]";
                }
            }
            return "非法用户";
        }

        public async Task<string> SendToClientAndReceive(string clientId, string message)
        {
            var ctx = Context.GetHttpContext();
            if (ctx != null)
            {
                if (!ctx.Request.Query.TryGetValue(Contracts.FLAG_NAME, out var oName))
                {
                    _logger.LogInformation($"非法用户向客户端发送消息：{message}");
                    return "非法用户";
                }
                else
                {
                    var cancelTokenSource = new CancellationTokenSource();
                    cancelTokenSource.CancelAfter(3000);

                    var currentClientUser = oName.FirstOrDefault();
                    var host = _chatManager.Get(clientId);
                    if (host is null)
                    {
                        _logger.LogInformation($"客户端用户[{currentClientUser}]向客户端[{clientId}]发送消息：{message},但客户端[{clientId}]不在线");
                        return $"客户端[{clientId}]不在线";
                    }
                    await Clients.Client(host.ConnectionId!).GetHubMessage(currentClientUser, message);
                    _logger.LogInformation($"客户端用户[{currentClientUser}]向成功客户端[{clientId}]发送消息：{message}");

                    var msg = await Clients.Caller.InvokeClientMessage(cancelTokenSource.Token);
                    return msg;
                }
            }
            return "非法用户";
        }
    }
}
