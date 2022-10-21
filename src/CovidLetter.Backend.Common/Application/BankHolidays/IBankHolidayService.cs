namespace CovidLetter.Backend.Common.Application.BankHolidays;

public interface IBankHolidayService
{
    /// <summary>
    /// Returns <c>true</c> if today is a bank holiday, <c>false</c> if not.
    /// </summary>
    public bool IsBankHoliday();

    /// <summary>
    /// Returns <c>true</c> if date is a bank holiday, <c>false</c> if not.
    /// </summary>
    /// <param name="date">The <see cref="DateTime"/> to determine bank holiday status.</param>
    public bool IsBankHoliday(DateTime date);
}
