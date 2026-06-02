using MediatR;
namespace InstantBot.Application.Commands.AskMentor;

public record AskMentorCommand(long TelegramUserId, string Question) : IRequest<AskMentorResult>;
public record AskMentorResult(string Response, bool LimitReached, int QuestionsLeft);
