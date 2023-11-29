namespace SignalrApi.Models
{
    public class User
    {
        public User() { }

        public User(string? userId, uint userType, string? intime)
        {
            this.UserId = userId;
            this.UserType = userType;
            this.InTime = intime;
        }

        public string? UserId { get; set; }

        public string? ConnectionId { get; set; }

        public uint UserType { get; set; }

        public string? InTime { get; set; }
    }
}
