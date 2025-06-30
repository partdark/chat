namespace chat.Models;

public class ChatMessage(string userName, string message)
{
    public string UserName { get; set; } = userName;
    public string Message { get; set; } = message;

}