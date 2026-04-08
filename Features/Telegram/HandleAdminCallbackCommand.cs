using MediatR;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ExpenseTrackerApi.Data;

namespace ExpenseTrackerApi.Features.Telegram;

public record HandleAdminCallbackCommand(CallbackQuery Callback) : IRequest;

public class HandleAdminCallbackHandler(ITelegramBotClient botClient, AppDbContext db)
    : IRequestHandler<HandleAdminCallbackCommand>
{
    public async Task Handle(HandleAdminCallbackCommand request, CancellationToken ct)
    {
        var callback = request.Callback;
        var chatId = callback.Message!.Chat.Id;
        var data = callback.Data!;

        try { await botClient.AnswerCallbackQuery(callback.Id, cancellationToken: ct); } catch { }
        try { await botClient.DeleteMessage(chatId, callback.Message.MessageId, ct); } catch { }

        var parts = data.Split('_');
        if (!int.TryParse(parts[1], out var targetUserId)) return;

        var targetUser = await db.Users.FindAsync([targetUserId], ct);
        if (targetUser == null) return;

        if (data.StartsWith("approve_"))
        {
            // Atomic update
            var affected = await db.Users
                .Where(u => u.Id == targetUserId && !u.IsApproved)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.IsApproved, true), ct);

            if (affected == 0) return; // ήδη εγκρίθηκε από άλλο request

            await botClient.SendMessage(chatId,
                $"✅ Ο χρήστης *{targetUser.Username}* εγκρίθηκε.",
                parseMode: ParseMode.Markdown, cancellationToken: ct);

            if (targetUser.TelegramChatId != 0)
                try { await botClient.SendMessage(targetUser.TelegramChatId, "✅ Ο λογαριασμός σου εγκρίθηκε! Πάτα /start για να ξεκινήσεις.", cancellationToken: ct); } catch { }
        }
        else if (data.StartsWith("reject_"))
        {
            var affected = await db.Users
                .Where(u => u.Id == targetUserId)
                .ExecuteDeleteAsync(ct);

            if (affected == 0) return; // ήδη διαγράφηκε από άλλο request

            await botClient.SendMessage(chatId,
                $"❌ Ο χρήστης *{targetUser.Username}* απορρίφθηκε.",
                parseMode: ParseMode.Markdown, cancellationToken: ct);

            if (targetUser.TelegramChatId != 0)
                try { await botClient.SendMessage(targetUser.TelegramChatId, "❌ Η αίτησή σου απορρίφθηκε.", cancellationToken: ct); } catch { }
        }
    }
}
