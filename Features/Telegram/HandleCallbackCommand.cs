using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types;
using ExpenseTrackerApi.Features.Expenses;

namespace ExpenseTrackerApi.Features.Telegram;

public record HandleCallbackCommand(CallbackQuery Callback, int UserId) : IRequest;

public class HandleCallbackHandler(IMediator mediator, ITelegramBotClient botClient)
    : IRequestHandler<HandleCallbackCommand>
{
    public async Task Handle(HandleCallbackCommand request, CancellationToken ct)
    {
        var callback = request.Callback;
        var chatId = callback.Message!.Chat.Id;

        try { await botClient.AnswerCallbackQuery(callback.Id, cancellationToken: ct); } catch { /* fake id during testing */ }

        if (!callback.Data!.StartsWith("month_")) return;

        // Delete the month picker message so it doesn't clutter the chat
        try { await botClient.DeleteMessage(chatId, callback.Message.MessageId, ct); } catch { }

        var month = int.Parse(callback.Data.Split('_')[1]);
        var start = new DateTime(DateTime.UtcNow.Year, month, 1);
        await mediator.Send(new GetStatsQuery(chatId, request.UserId, start, start.AddMonths(1), start.ToString("MMMM yyyy")), ct);
    }
}
