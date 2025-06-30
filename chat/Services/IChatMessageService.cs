using chat.Models;

namespace chat.Services;

public interface IChatMessageService
{
    Task<List<ChatMessage>> GetChatHistoryAsync(string chatRoom);
    Task AddMessageAsync(string chatRoom, ChatMessage message);
}