using Telegram.Bot.Types.ReplyMarkups;

namespace ExpenseTrackerApi.Features.Telegram;

public static class TelegramKeyboards
{
    public static readonly ReplyKeyboardMarkup Main = new([
        ["📊 Στατιστικά Μήνα", "📅 Στατιστικά Έτους"],
        ["↩️ Αναίρεση τελευταίου", "❓ Βοήθεια"]
    ])
    { ResizeKeyboard = true };

    public static readonly InlineKeyboardMarkup MonthPicker = new([
        [
            InlineKeyboardButton.WithCallbackData("Ιαν", "month_1"),
            InlineKeyboardButton.WithCallbackData("Φεβ", "month_2"),
            InlineKeyboardButton.WithCallbackData("Μαρ", "month_3"),
            InlineKeyboardButton.WithCallbackData("Απρ", "month_4"),
        ],
        [
            InlineKeyboardButton.WithCallbackData("Μαΐ", "month_5"),
            InlineKeyboardButton.WithCallbackData("Ιουν", "month_6"),
            InlineKeyboardButton.WithCallbackData("Ιουλ", "month_7"),
            InlineKeyboardButton.WithCallbackData("Αυγ", "month_8"),
        ],
        [
            InlineKeyboardButton.WithCallbackData("Σεπ", "month_9"),
            InlineKeyboardButton.WithCallbackData("Οκτ", "month_10"),
            InlineKeyboardButton.WithCallbackData("Νοε", "month_11"),
            InlineKeyboardButton.WithCallbackData("Δεκ", "month_12"),
        ]
    ]);
}
