namespace MyWebApp.Frontend.Constants
{
    public static class CategoryConstants
    {
        public static readonly string[] FixedCategories =
        {
            "Choice 1", "Choice 2", "Choice 3", "Choice 4",
            "Choice 5", "Choice 6", "Others"
        };

        public const string DefaultCategory = "Others";

        public static string ResolveName(string? suggestedName)
        {
            if (!string.IsNullOrWhiteSpace(suggestedName) &&
                FixedCategories.Contains(suggestedName.Trim(), StringComparer.Ordinal))
            {
                return suggestedName.Trim();
            }

            return DefaultCategory;
        }
    }
}
