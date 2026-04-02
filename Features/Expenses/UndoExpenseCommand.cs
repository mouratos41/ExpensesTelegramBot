using MediatR;
using Telegram.Bot;
using ExpenseTrackerApi.Data;

namespace ExpenseTrackerApi.Features.Expenses;

public record UndoExpenseCommand(long ChatId, int UserId) : IRequest;

public class UndoExpenseHandler(AppDbContext context, ITelegramBotClient botClient)
    : IRequestHandler<UndoExpenseCommand>
{
    public async Task Handle(UndoExpenseCommand request, CancellationToken ct)
    {
        var lastExpense = context.Expenses
            .Where(e => e.UserId == request.UserId)
            .OrderByDescending(e => e.Date)
            .FirstOrDefault();

        if (lastExpense == null)
        {
            await botClient.SendMessage(request.ChatId, "📭 Δεν βρέθηκε κάποιο έξοδο για να διαγραφεί.", cancellationToken: ct);
            return;
        }

        context.Expenses.Remove(lastExpense);
        await context.SaveChangesAsync(ct);
        await botClient.SendMessage(request.ChatId, $"🗑️ Διαγράφηκε το τελευταίο έξοδο: {lastExpense.Amount:N2}€ ({lastExpense.Category}).", cancellationToken: ct);
    }
}
