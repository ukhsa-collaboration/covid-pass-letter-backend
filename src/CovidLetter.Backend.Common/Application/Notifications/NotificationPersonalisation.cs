namespace CovidLetter.Backend.Common.Application.Notifications;

public class NotificationPersonalisation : Dictionary<string, object>
{
    public void AddRange(NotificationPersonalisation other)
    {
        ArgumentNullException.ThrowIfNull(other);

        foreach (var (key, value) in other)
        {
            if (!this.ContainsKey(key))
            {
                this.Add(key, value);
            }
        }
    }
}
