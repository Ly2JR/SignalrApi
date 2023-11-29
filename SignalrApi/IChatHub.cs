namespace SignalrApi
{
    public interface IChatHub
    {
        Task GetHubMessage(string clientId, string message);

        Task<string> InvokeClientMessage(CancellationToken token=default);
    }
}
