namespace RecipesAPI.API.Infrastructure;

public interface IMessageQueue
{
    Task SendMessage(SendMessageRequest request);
    Task<ReceiveMessagesResponse> ReceiveMessages(ReceiveMessagesRequest request);
    Task DeleteMessage(DeleteMessageRequest request);
}

public class DeleteMessageRequest
{
    public string Topic { get; set; } = "";
    public List<string> MessageIds { get; set; } = [];
}

public class ReceiveMessagesRequest
{
    public string Topic { get; set; } = "";
}

public class SendMessageRequest
{
    public string Topic { get; set; } = "";
    public string? MessageBody { get; set; }
}

public class ReceiveMessagesResponse
{
    public List<Message> Messages { get; set; } = [];
}

public class Message
{
    public string? MessageBody { get; set; }
    public MessageAttributes Attributes { get; set; } = new();
}

public class MessageAttributes
{
    public string Id { get; set; } = "";
}

public class SqliteMessageQueue
{

}