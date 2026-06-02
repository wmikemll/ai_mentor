using InstantBot.Application.Commands.RegisterUser;
using InstantBot.Application.Interfaces;
using InstantBot.Application.Services;
using InstantBot.Domain.Enums;
using InstantBot.TelegramBot.Keyboards;
using InstantBot.TelegramBot.StateMachine;
using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace InstantBot.TelegramBot.Handlers;

public class MessageHandler(
    ITelegramBotClient bot,
    ISender mediator,
    UserStateManager stateManager,
    IUserRepository userRepo,
    ICacheService cache)
{
    public async Task HandleAsync(Message msg, CancellationToken ct)
    {
        if (msg.From is null) return;
        var uid = msg.From.Id;
        var text = msg.Text ?? string.Empty;

        if (text.StartsWith("/start"))
        {
            await HandleStartAsync(msg, ct);
            return;
        }

        if (text == "/menu")
        {
            await stateManager.SetStateAsync(uid, UserBotState.MainMenu);
            await bot.SendMessage(uid, "Главное меню:", replyMarkup: MainMenuKeyboard.Get(), cancellationToken: ct);
            return;
        }

        var state = await stateManager.GetStateAsync(uid);
        switch (state)
        {
            case UserBotState.AwaitingName:
                await HandleAwaitingNameAsync(msg, ct);
                break;
            case UserBotState.AwaitingBirthDate:
                await HandleAwaitingBirthDateAsync(msg, ct);
                break;
            case UserBotState.AwaitingMentorQuestion:
                await HandleMentorQuestionAsync(msg, ct);
                break;
            case UserBotState.AwaitingPartnerName:
                await HandlePartnerNameAsync(msg, ct);
                break;
            case UserBotState.AwaitingPartnerBirthDate:
                await HandlePartnerBirthDateAsync(msg, ct);
                break;
            default:
                var user = await userRepo.GetByTelegramIdAsync(uid, ct);
                if (user is null)
                {
                    await HandleStartAsync(msg, ct);
                    return;
                }
                await bot.SendMessage(uid, "Используй меню ниже:", replyMarkup: MainMenuKeyboard.Get(), cancellationToken: ct);
                break;
        }
    }

    private async Task HandleStartAsync(Message msg, CancellationToken ct)
    {
        var uid = msg.From!.Id;
        var parts = (msg.Text ?? "").Split(' ');
        var refCode = parts.Length > 1 && parts[1].StartsWith("REF_") ? parts[1][4..] : null;

        var user = await userRepo.GetByTelegramIdAsync(uid, ct);
        if (user is not null)
        {
            await stateManager.SetStateAsync(uid, UserBotState.MainMenu);
            await bot.SendMessage(uid, $"С возвращением, {user.FirstName}! 👋",
                replyMarkup: MainMenuKeyboard.Get(), cancellationToken: ct);
            return;
        }

        if (refCode is not null)
            await stateManager.SetTempValueAsync(uid, "ref_code", refCode);

        await stateManager.SetStateAsync(uid, UserBotState.AwaitingName);
        await bot.SendMessage(uid,
            "Привет! Я твой личный ИИ-наставник. 🌟\n\nДавай познакомимся. Как тебя зовут?",
            cancellationToken: ct);
    }

    private async Task HandleAwaitingNameAsync(Message msg, CancellationToken ct)
    {
        var uid = msg.From!.Id;
        var name = msg.Text?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length < 2)
        {
            await bot.SendMessage(uid, "Введи своё имя (минимум 2 символа):", cancellationToken: ct);
            return;
        }
        await stateManager.SetTempValueAsync(uid, "name", name);
        await stateManager.SetStateAsync(uid, UserBotState.AwaitingBirthDate);
        await bot.SendMessage(uid,
            $"Приятно познакомиться, {name}! 😊\n\nВведи дату рождения в формате ДД.ММ.ГГГГ\nНапример: 15.05.1990",
            cancellationToken: ct);
    }

    private async Task HandleAwaitingBirthDateAsync(Message msg, CancellationToken ct)
    {
        var uid = msg.From!.Id;
        if (!DateOnly.TryParseExact(msg.Text?.Trim(), "dd.MM.yyyy",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var birthDate))
        {
            await bot.SendMessage(uid, "Неверный формат. Введи дату как ДД.ММ.ГГГГ, например: 15.05.1990",
                cancellationToken: ct);
            return;
        }

        var name    = await stateManager.GetTempValueAsync(uid, "name") ?? msg.From.FirstName;
        var refCode = await stateManager.GetTempValueAsync(uid, "ref_code");

        var user = await mediator.Send(
            new RegisterUserCommand(uid, name, msg.From.Username, birthDate, refCode), ct);

        await stateManager.SetStateAsync(uid, UserBotState.MainMenu);

        var profile = user.PersonalityProfile!;
        var archetypeName = GetArchetypeName(profile.ArchetypeType);
        var profileText = $"""
            ✨ Твой профиль готов, {user.FirstName}!

            🔢 Число жизненного пути: {profile.LifePathNumber}
            🌟 Архетип: {archetypeName}

            💪 Сильные стороны:
            {string.Join("\n", profile.Strengths.Select(s => $"• {s}"))}

            🎯 Зоны роста:
            {string.Join("\n", profile.Weaknesses.Select(w => $"• {w}"))}

            💡 Рекомендации:
            {string.Join("\n", profile.Recommendations.Select(r => $"• {r}"))}
            """;

        await bot.SendMessage(uid, profileText,
            replyMarkup: MainMenuKeyboard.Get(), cancellationToken: ct);
    }

    protected virtual async Task HandleMentorQuestionAsync(Message msg, CancellationToken ct)
    {
        var uid      = msg.From!.Id;
        var question = msg.Text?.Trim();
        if (string.IsNullOrEmpty(question)) return;

        await bot.SendChatAction(uid, ChatAction.Typing, cancellationToken: ct);

        var result = await mediator.Send(new Application.Commands.AskMentor.AskMentorCommand(uid, question), ct);

        if (result.LimitReached)
        {
            var shown = await cache.IsOfferShownAsync(uid, "limit_upsell");
            var text  = "Ты использовал все 3 бесплатных вопроса на сегодня. 🌙\n\nПереходи на Premium — наставник без ограничений!";
            if (!shown)
            {
                text += "\n\n🎁 Скидка 20% на Premium — только 24 часа!";
                await cache.MarkOfferShownAsync(uid, "limit_upsell", TimeSpan.FromHours(24));
            }
            await bot.SendMessage(uid, text,
                replyMarkup: Keyboards.SubscriptionKeyboard.GetUpsell(), cancellationToken: ct);
            return;
        }

        var response = result.QuestionsLeft < int.MaxValue && result.QuestionsLeft > 0
            ? $"{result.Response}\n\n_Осталось вопросов сегодня: {result.QuestionsLeft}_"
            : result.Response;

        await bot.SendMessage(uid, response,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: Keyboards.MainMenuKeyboard.Get(), cancellationToken: ct);
        await stateManager.SetStateAsync(uid, UserBotState.MainMenu);
    }
    protected virtual async Task HandlePartnerNameAsync(Message msg, CancellationToken ct)
    {
        var uid  = msg.From!.Id;
        var name = msg.Text?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length < 2)
        {
            await bot.SendMessage(uid, "Введи имя партнёра (минимум 2 символа):", cancellationToken: ct);
            return;
        }
        await stateManager.SetTempValueAsync(uid, "partner_name", name);
        await stateManager.SetStateAsync(uid, UserBotState.AwaitingPartnerBirthDate);
        await bot.SendMessage(uid,
            $"Отлично! Теперь введи дату рождения {name} в формате ДД.ММ.ГГГГ:", cancellationToken: ct);
    }

    protected virtual async Task HandlePartnerBirthDateAsync(Message msg, CancellationToken ct)
    {
        var uid = msg.From!.Id;
        if (!DateOnly.TryParseExact(msg.Text?.Trim(), "dd.MM.yyyy",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var partnerBirth))
        {
            await bot.SendMessage(uid, "Неверный формат. Введи как ДД.ММ.ГГГГ:", cancellationToken: ct);
            return;
        }

        var partnerName = await stateManager.GetTempValueAsync(uid, "partner_name") ?? "партнёр";
        await bot.SendChatAction(uid, Telegram.Bot.Types.Enums.ChatAction.Typing, cancellationToken: ct);

        var result = await mediator.Send(
            new Application.Commands.AnalyzeRelationship.AnalyzeRelationshipCommand(uid, partnerName, partnerBirth), ct);

        await stateManager.SetStateAsync(uid, UserBotState.MainMenu);

        var user = await userRepo.GetByTelegramIdAsync(uid, ct);
        await bot.SendMessage(uid,
            $"❤️ *Анализ пары: {user?.FirstName} & {partnerName}*\n\n{result}",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: Keyboards.ShareKeyboard.Get("Поделиться анализом"),
            cancellationToken: ct);

        // VIP upsell after relationship analysis for Premium users
        if (user?.SubscriptionTier == Domain.Enums.SubscriptionTier.Premium)
        {
            var shown = await cache.IsOfferShownAsync(uid, "vip_after_relationship");
            if (!shown)
            {
                await Task.Delay(2000, ct);
                await bot.SendMessage(uid,
                    "💡 Хочешь голосовые ответы наставника и расширенную аналитику? Доступно в VIP!",
                    replyMarkup: Keyboards.SubscriptionKeyboard.GetUpsell(), cancellationToken: ct);
                await cache.MarkOfferShownAsync(uid, "vip_after_relationship", TimeSpan.FromDays(7));
            }
        }
    }

    private static string GetArchetypeName(ArchetypeType type) => type switch
    {
        ArchetypeType.Leader     => "Лидер",     ArchetypeType.Diplomat   => "Дипломат",
        ArchetypeType.Creative   => "Творец",    ArchetypeType.Organizer  => "Организатор",
        ArchetypeType.Adventurer => "Искатель",  ArchetypeType.Nurturer   => "Опекун",
        ArchetypeType.Seeker     => "Мыслитель", ArchetypeType.Authority  => "Стратег",
        ArchetypeType.Sage       => "Мудрец",    ArchetypeType.Visionary  => "Провидец",
        ArchetypeType.Builder    => "Строитель", _                        => "Исследователь",
    };
}
