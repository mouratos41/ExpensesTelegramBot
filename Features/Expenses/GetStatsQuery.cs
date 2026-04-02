using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using ExpenseTrackerApi.Data;

namespace ExpenseTrackerApi.Features.Expenses;

public record GetStatsQuery(long ChatId, int UserId, DateTime Start, DateTime End, string PeriodLabel) : IRequest;

public class GetStatsHandler(AppDbContext context, ITelegramBotClient botClient)
    : IRequestHandler<GetStatsQuery>
{
    public async Task Handle(GetStatsQuery request, CancellationToken ct)
    {
        var stats = context.Expenses
            .Where(e => e.UserId == request.UserId && e.Date >= request.Start && e.Date < request.End)
            .GroupBy(e => e.Category)
            .Select(g => new { Cat = g.Key, Total = g.Sum(x => x.Amount) })
            .OrderByDescending(x => x.Total)
            .ToList();

        if (!stats.Any())
        {
            await botClient.SendMessage(request.ChatId, "📭 Δεν υπάρχουν έξοδα για αυτή την περίοδο.", cancellationToken: ct);
            return;
        }

        var report = $"📊 *Στατιστικά {request.PeriodLabel}*\n" +
                     string.Join("\n", stats.Select(s => $"🔹 {s.Cat}: {s.Total:N2}€")) +
                     $"\n\n💰 *Σύνολο: {stats.Sum(x => x.Total):N2}€*";

        await botClient.SendMessage(request.ChatId, report, parseMode: ParseMode.Markdown, cancellationToken: ct);
    }
}
