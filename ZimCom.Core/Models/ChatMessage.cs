namespace ZimCom.Core.Models;
public class ChatMessage {
    public ChatMessage(User user, string message) {
        User = user;
        Message = message;
        DateTime = DateTime.Now;
    }

    public User User { get; set; }
    public string Message { get; set; }
    public DateTime DateTime { get; set; }
}
