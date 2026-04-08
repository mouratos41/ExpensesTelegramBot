using MediatR;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using ExpenseTrackerApi.Data;

namespace ExpenseTrackerApi.Features.Telegram;

public record RegisterTelegramUserCommand(long ChatId, string? Username, string? FirstName) : IRequest;

public class RegisterTelegramUserHandler(ITelegramBotClient botClient, AppDbContext db)
    : IRequestHandler<RegisterTelegramUserCommand>
{
    public async Task Handle(RegisterTelegramUserCommand request, CancellationToken ct)
    {
        var telegramUsername = request.Username ?? request.FirstName ?? request.ChatId.ToString();

        if (await db.Users.AnyAsync(u => u.Username == telegramUsername, ct))
            telegramUsername = $"{telegramUsername}_{request.ChatId}";

        var newUser = new Models.User
        {
            Username = telegramUsername,
            TelegramChatId = request.ChatId,
            IsApproved = false,
            IsAdmin = false
        };
        db.Users.Add(newUser);
        await db.SaveChangesAsync(ct);

        try
        {
            await botClient.SendMessage(request.ChatId,
                "⏳ Η εγγραφή σου καταχωρήθηκε! Αναμένεις έγκριση από τον διαχειριστή.",
                cancellationToken: ct);
        }
        catch { /* fake or unreachable chatId during testing */ }

        var admins = await db.Users.Where(u => u.IsAdmin && u.TelegramChatId != 0).ToListAsync(ct);
        foreach (var admin in admins)
        {
            try
            {
                await botClient.SendMessage(
                    admin.TelegramChatId,
                    $"🔔 *Νέος χρήστης ζητά πρόσβαση*\n\n" +
                    $"👤 Username: `{telegramUsername}`\n" +
                    $"🆔 Chat ID: `{request.ChatId}`",
                    parseMode: ParseMode.Markdown,
                    replyMarkup: new InlineKeyboardMarkup([
                        [
                            InlineKeyboardButton.WithCallbackData("✅ Αποδοχή", $"approve_{newUser.Id}"),
                            InlineKeyboardButton.WithCallbackData("❌ Απόρριψη", $"reject_{newUser.Id}")
                        ]
                    ]),
                    cancellationToken: ct);
            }
            catch { /* admin chatId unreachable */ }
        }
    }
}
