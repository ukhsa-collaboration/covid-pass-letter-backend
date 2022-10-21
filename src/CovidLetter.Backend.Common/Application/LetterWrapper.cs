namespace CovidLetter.Backend.Common.Application;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

/// <summary>
/// Simple wrapper to store things metadata about the letter (eg. correlation ID), but isn't actually part of the letter.
/// </summary>
public record LetterWrapper<TRow>(
    Guid Id,
    string AppId,
    DateTime CreatedOn,
    [property: JsonConverter(typeof(StringEnumConverter))]
    FileType FileType,
    TRow Letter);
