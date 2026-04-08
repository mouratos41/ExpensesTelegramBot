using ExpenseTrackerApi.Models;

namespace ExpenseTrackerApi.Extensions;

public static class HttpContextExtensions
{
    private static readonly object TelegramUserKey = new();

    public static void SetTelegramUser(this HttpContext ctx, User user)
        => ctx.Items[TelegramUserKey] = user;

    public static User GetTelegramUser(this HttpContext ctx)
        => (User)ctx.Items[TelegramUserKey]!;
}
