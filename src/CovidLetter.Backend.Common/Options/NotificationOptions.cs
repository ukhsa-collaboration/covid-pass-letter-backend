namespace CovidLetter.Backend.Common.Options;

using CovidLetter.Backend.Common.Application;
using CovidLetter.Backend.Common.Application.Notifications;

public class NotificationOptions
{
    public const string SectionName = "Notifications";

    public string ApiKey { get; set; } = null!;

    public IEnumerable<NotificationTemplateConfiguration> Configuration { get; set; } = null!;
}

public class NotificationTemplateConfiguration
{
    public NotificationReasonCode? Code { get; set; }

    public string Name { get; set; } = null!;

    public NotificationTemplates Templates { get; set; } = new();
}

public class NotificationTemplates : Dictionary<ContactMethodType, string>
{
}
