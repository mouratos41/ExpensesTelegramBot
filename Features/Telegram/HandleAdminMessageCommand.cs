using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.EntityFrameworkCore;
using ExpenseTrackerApi.Data;

namespace ExpenseTrackerApi.Features.Telegram;

public record HandleAdminMessageCommand(Message Message, int UserId) : IRequest;

public class HandleAdminMessageHandler(ITelegramBotClient botClient, AppDbContext db)
    : IRequestHandler<HandleAdminMessageCommand>
{
    public async Task Handle(HandleAdminMessageCommand request, CancellationToken ct)
    {
        var chatId = request.Message.Chat.Id;
        var text = request.Message.Text!.Trim().ToLower();

        switch (text)
        {
            case "/pending":
                var pending = await db.Users.Where(u => !u.IsApproved && !u.IsAdmin).ToListAsync(ct);

                if (pending.Count == 0)
                {
                    await botClient.SendMessage(chatId, "✅ Δεν υπάρχουν εκκρεμείς χρήστες.", cancellationToken: ct);
                    return;
                }

                var lines = pending.Select(u => $"• `{u.Username}` (ID: {u.Id})");
                var buttons = pending.Select(u => new[]
                {
                    InlineKeyboardButton.WithCallbackData($"✅ {u.Username}", $"approve_{u.Id}"),
                    InlineKeyboardButton.WithCallbackData($"❌ {u.Username}", $"reject_{u.Id}")
                });

                await botClient.SendMessage(chatId,
                    $"⏳ *Εκκρεμείς χρήστες ({pending.Count}):*\n\n" + string.Join("\n", lines),
                    parseMode: ParseMode.Markdown,
                    replyMarkup: new InlineKeyboardMarkup(buttons),
                    cancellationToken: ct);
                break;
        }
    }
}
