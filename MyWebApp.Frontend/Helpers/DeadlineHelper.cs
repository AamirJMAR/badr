namespace MyWebApp.Frontend.Helpers
{
    public static class DeadlineHelper
    {
        public static DateTime DefaultDeadline => DateTime.Today.AddDays(7);

        public static DateTime ParseDeadline(string? value)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                string.Equals(value.Trim(), "TBD", StringComparison.OrdinalIgnoreCase))
            {
                return DefaultDeadline;
            }

            return DateTime.TryParse(value, out var date) ? date.Date : DefaultDeadline;
        }

        public static DateTime Normalize(DateTime? value) =>
            value?.Date ?? DefaultDeadline;
    }
}
