namespace EcommerceApi.Services.Logging
{
    // Any logger (File or Database) must be able to do this one thing: log a message.
    public interface IApplicationLogger
    {
        Task Log(string message);
    }
}