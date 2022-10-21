namespace CovidLetter.Backend.Common.Application.Barcodes
{
    public record ValidationResult(bool Success, string? FailureReason)
    {
        private static readonly ValidationResult SuccessfulResult = new(true, null);

        public static ValidationResult Successful() => SuccessfulResult;

        public static ValidationResult Failed(string reason)
        {
            return new ValidationResult(false, reason);
        }
    }
}
