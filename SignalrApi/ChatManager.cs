using Microsoft.AspNetCore.Identity;
using SignalrApi.Models;
using System.Collections.Concurrent;

namespace SignalrApi
{
    public class ChatManager
    {
        private readonly ConcurrentDictionary<string, User> _chatRoom;
        private readonly ILogger<ChatManager> _logger;
        private const string TimeFormat = "HH:mm:ss";
        public ChatManager(ILogger<ChatManager> logger, ConcurrentDictionary<string, User> chatRoom)
        {
            _logger = logger;
            _chatRoom = chatRoom;
        }

        public User? this[string connectionId]
        { get { return _chatRoom[connectionId]; } }

        public User? Get(string userId)
        {
            return _chatRoom.Values.FirstOrDefault(u => u.UserId == userId);
        }


        public List<User> GetAll()
        {
            var onlineUsers = _chatRoom.Values.OrderBy(o => o.InTime);
            return onlineUsers.ToList();
        }


        public void AddOrUpdate(string connectionId, string userId, uint type = 0)
        {
            _chatRoom.AddOrUpdate(connectionId, new User()
            {
                UserId = userId,
                ConnectionId = connectionId,
                UserType = type,
                InTime = DateTime.Now.ToString(TimeFormat),
            }, (k, o) =>
            {
                o.UserId = userId;
                o.ConnectionId = connectionId;
                o.UserType = type;
                o.InTime = DateTime.Now.ToString(TimeFormat);
                return new User() { ConnectionId = connectionId, UserType = type };
            });
            _logger.LogInformation($"房间管理:用户ID:{userId},用户类型:{type}进入房间");
        }

        public void Remove(string connectionId)
        {
            if (_chatRoom.TryRemove(connectionId, out var v))
            {
                _logger.LogInformation($"房间管理:移除用户ID:{v.UserId},用户类型:{v.UserType},进入时间:{v.InTime},离开时间:{DateTime.Now.ToString(TimeFormat)}");
            }
            else
            {
                _logger.LogInformation($"房间管理:移除用户失败");
            }
        }
    }
}
