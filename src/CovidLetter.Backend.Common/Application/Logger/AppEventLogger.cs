// ReSharper disable ContextualLoggerProblem
// ReSharper disable TemplateIsNotCompileTimeConstantProblem
namespace CovidLetter.Backend.Common.Application.Logger;

using System.Text;
using CovidLetter.Backend.Common.Utilities;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

public class AppEventLogger<TCategoryName>
{
    private readonly ILogger<TCategoryName> log;
    private readonly IClock clock;

    public AppEventLogger(ILogger<TCategoryName> log, IClock clock)
    {
        this.log = log;
        this.clock = clock;
    }

    public void LogSuccessfulInboundRequestReceived(
        LetterRequest letterRequest,
        [StructuredMessageTemplate]string? message,
        params object?[] args)
    {
        if (letterRequest == default!)
        {
            return;
        }

        message ??= string.Empty;
        var builder = new StringBuilder(message.Trim().TrimEnd('.') + " - ");
        var argList = new List<object?>(args);

        builder.Append($"{nameof(LetterRequest.AccessibilityNeeds)}: {{{nameof(LetterRequest.AccessibilityNeeds)}}}, ");
        argList.Add(letterRequest.AccessibilityNeeds);

        builder.Append("Age: {Age}, ");
        argList.Add(letterRequest.GetAge(this.clock));

        builder.Append($"{nameof(LetterRequest.AlternateLanguage)}: {{{nameof(LetterRequest.AlternateLanguage)}}}, ");
        argList.Add(letterRequest.AlternateLanguage);

        builder.Append($"{nameof(LetterRequest.ContactMethod)}: {{{nameof(LetterRequest.ContactMethod)}}}, ");
        argList.Add(letterRequest.ContactMethod);

        builder.Append($"{nameof(LetterRequest.LetterType)}: {{{nameof(LetterRequest.LetterType)}}}, ");
        argList.Add(letterRequest.LetterType == default! ? string.Empty : string.Join(", ", letterRequest.LetterType));

        builder.Append("UniqueHash: {UniqueHash}, ");
        argList.Add(letterRequest.GetNhsDobHash());

        builder.Append($"{nameof(LetterRequest.Region)}: {{{nameof(LetterRequest.Region)}}}, ");
        argList.Add(letterRequest.Region);

        this.LogLetterInformationEvent(
            AppEventId.SuccessfulInboundRequestReceived,
            letterRequest,
            builder.ToString(),
            argList.ToArray());
    }

    public void LogLetterInformationEvent(
        EventId eventId,
        LetterRequest letterRequest,
        [StructuredMessageTemplate]string? message,
        params object?[] args)
    {
        if (letterRequest == default!)
        {
            return;
        }

        message ??= string.Empty;
        var builder = new StringBuilder(message.Trim().TrimEnd('.', ',') + (message.Contains('-') ? ", " : " - "));
        var argList = new List<object?>(args);

        builder.Append($"{nameof(LetterRequest.CorrelationId)}: {{{nameof(LetterRequest.CorrelationId)}}}");
        argList.Add(letterRequest.CorrelationId);

        this.log.LogInformation(eventId, builder.ToString(), argList.ToArray());
    }

    public void LogLetterErrorEvent(
        EventId eventId,
        Exception ex,
        LetterRequest letterRequest,
        [StructuredMessageTemplate]string? message,
        params object?[] args)
    {
        if (letterRequest == default!)
        {
            return;
        }

        message ??= string.Empty;
        var builder = new StringBuilder(message.Trim().TrimEnd('.', ',') + (message.Contains('-') ? ", " : " - "));
        var argList = new List<object?>(args);

        builder.Append($"{nameof(LetterRequest.CorrelationId)}: {{{nameof(LetterRequest.CorrelationId)}}}");
        argList.Add(letterRequest.CorrelationId);

        this.log.LogError(eventId, ex, builder.ToString(), argList.ToArray());
    }
}
