
using chat.Models;
using chat.Services;
using Microsoft.AspNetCore.SignalR;

namespace chat.Hubs;

public interface IChatClient
{
    public Task ReceiveMessage(string userName, string message);
    public Task ReceiveMessageHistory(List<ChatMessage> messageHistory);
    public Task UsersInRoom(List<string> users);
}

public class ChatHub(IChatMessageService messageService, ILogger<ChatHub> logger)
    : Hub<IChatClient>
{
    private static readonly Dictionary<string, UserConnection> Users = new Dictionary<string, UserConnection>();

    public async Task JoinChat(UserConnection connection)
    {
        try
        {
            
            Users[Context.ConnectionId] = connection;

           
            await Groups.AddToGroupAsync(Context.ConnectionId, connection.ChatRoom);
            
            
            var messageHistory = await messageService.GetChatHistoryAsync(connection.ChatRoom);
            
            
            await Clients.Caller.ReceiveMessageHistory(messageHistory);
            
           
            var joinMessage = new ChatMessage("Система", $"{connection.UserName} присоединился к чату");
            
            
            await messageService.AddMessageAsync(connection.ChatRoom, joinMessage);
            
            
            await Clients.Group(connection.ChatRoom)
                .ReceiveMessage(joinMessage.UserName, joinMessage.Message);
                
           
            await SendUsersInRoom(connection.ChatRoom);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in JoinChat");
        }
    }

    public async Task SendMessage(string message)
    {
        try
        {
            if (Users.TryGetValue(Context.ConnectionId, out var connection))
            {
                var chatMessage = new ChatMessage(connection.UserName, message);
                await messageService.AddMessageAsync(connection.ChatRoom, chatMessage);
                await Clients.Group(connection.ChatRoom)
                    .ReceiveMessage(chatMessage.UserName, chatMessage.Message);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при отправке сообщений");
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            if (Users.TryGetValue(Context.ConnectionId, out var connection))
            {               
                var leaveMessage = new ChatMessage("Система", $"{connection.UserName} покинул чат");
                await messageService.AddMessageAsync(connection.ChatRoom, leaveMessage);                
                await Clients.Group(connection.ChatRoom)
                    .ReceiveMessage(leaveMessage.UserName, leaveMessage.Message);                
                Users.Remove(Context.ConnectionId);              
                await SendUsersInRoom(connection.ChatRoom);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка отключения от чата");
        }        
        await base.OnDisconnectedAsync(exception);
    }    
    
    private async Task SendUsersInRoom(string roomName)
    {
        try
        {           
            var usersInRoom = Users
                .Where(u => u.Value.ChatRoom == roomName)
                .Select(u => u.Value.UserName)
                .ToList();                
           
            await Clients.Group(roomName).UsersInRoom(usersInRoom);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка отправки сообщения");
        }
    }
}