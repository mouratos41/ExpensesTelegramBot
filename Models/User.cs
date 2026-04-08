namespace ExpenseTrackerApi.Models;

public class User
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public string? PasswordHash { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    public long TelegramChatId { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsApproved { get; set; }
}