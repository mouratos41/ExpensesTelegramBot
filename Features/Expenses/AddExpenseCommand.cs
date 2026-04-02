using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using ExpenseTrackerApi.Data;
using ExpenseTrackerApi.Models;

namespace ExpenseTrackerApi.Features.Expenses;

public record AddExpenseCommand(long ChatId, int UserId, string Text) : IRequest;

public class AddExpenseHandler(AppDbContext context, ITelegramBotClient botClient)
    : IRequestHandler<AddExpenseCommand>
{
    public async Task Handle(AddExpenseCommand request, CancellationToken ct)
    {
        var parts = request.Text.Split(' ', 2);
        if (parts.Length < 2 || !decimal.TryParse(parts[0], out var amount))
        {
            await botClient.SendMessage(request.ChatId, "❌ Γράψε: `Ποσό Κατηγορία` (π.χ. `10 Καφές`)", parseMode: ParseMode.Markdown, cancellationToken: ct);
            return;
        }

        context.Expenses.Add(new Expense
        {
            UserId = request.UserId,
            Amount = amount,
            Category = parts[1],
            Description = "Telegram Bot",
            Date = DateTime.UtcNow
        });

        await context.SaveChangesAsync(ct);
        await botClient.SendMessage(request.ChatId, $"✅ Καταχωρήθηκε {amount:N2}€ στο '{parts[1]}'.", cancellationToken: ct);
    }
}
