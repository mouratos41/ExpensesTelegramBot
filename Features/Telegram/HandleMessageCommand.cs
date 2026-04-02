using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ExpenseTrackerApi.Features.Expenses;

namespace ExpenseTrackerApi.Features.Telegram;

public record HandleMessageCommand(Message Message, int UserId) : IRequest;

public class HandleMessageHandler(IMediator mediator, ITelegramBotClient botClient)
    : IRequestHandler<HandleMessageCommand>
{
    public async Task Handle(HandleMessageCommand request, CancellationToken ct)
    {
        var chatId = request.Message.Chat.Id;
        var text = request.Message.Text!.Trim();

        var command = text.ToLower() switch
        {
            "📊 στατιστικά μήνα" => "/stats",
            "📅 στατιστικά έτους" => "/year",
            "↩️ αναίρεση τελευταίου" => "/undo",
            "❓ βοήθεια" => "/help",
            var t => t
        };

        switch (command)
        {
            case "/start":
                await botClient.SendMessage(chatId,
                    "👋 *Καλώς ήρθες στο Expense Tracker!*\n\n" +
                    "Για να καταχωρήσεις έξοδο γράψε:\n`Ποσό Κατηγορία` (π.χ. `10 Καφές`)\n\n" +
                    "Ή χρησιμοποίησε τα κουμπιά παρακάτω 👇",
                    parseMode: ParseMode.Markdown,
                    replyMarkup: TelegramKeyboards.Main,
                    cancellationToken: ct);
                break;

            case "/help":
                await botClient.SendMessage(chatId,
                    "📖 *Οδηγίες χρήσης*\n\n" +
                    "*Καταχώρηση εξόδου:*\n`Ποσό Κατηγορία`\nπ.χ. `15.50 Φαγητό`\n\n" +
                    "*Εντολές:*\n" +
                    "📊 `/stats` — Επιλογή μήνα\n" +
                    "📅 `/year` — Στατιστικά τρέχοντος έτους\n" +
                    "↩️ `/undo` — Διαγραφή τελευταίου εξόδου",
                    parseMode: ParseMode.Markdown,
                    replyMarkup: TelegramKeyboards.Main,
                    cancellationToken: ct);
                break;

            case "/undo":
                await mediator.Send(new UndoExpenseCommand(chatId, request.UserId), ct);
                break;

            case "/year":
                var yearStart = new DateTime(DateTime.UtcNow.Year, 1, 1);
                await mediator.Send(new GetStatsQuery(chatId, request.UserId, yearStart, yearStart.AddYears(1), "Έτους " + DateTime.UtcNow.Year), ct);
                break;

            case var s when s == "/stats" || s.StartsWith("/stats "):
                var parts = command.Split(' ', 2);
                if (parts.Length == 2 && int.TryParse(parts[1], out var month) && month >= 1 && month <= 12)
                {
                    var monthStart = new DateTime(DateTime.UtcNow.Year, month, 1);
                    await mediator.Send(new GetStatsQuery(chatId, request.UserId, monthStart, monthStart.AddMonths(1), monthStart.ToString("MMMM yyyy")), ct);
                }
                else
                {
                    await botClient.SendMessage(chatId, "📅 Επίλεξε μήνα:", replyMarkup: TelegramKeyboards.MonthPicker, cancellationToken: ct);
                }
                break;

            default:
                await mediator.Send(new AddExpenseCommand(chatId, request.UserId, text), ct);
                break;
        }
    }
}
