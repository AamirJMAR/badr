namespace MyWebApp.Constants
{
    public static class CategoryConstants
    {
        public static readonly string[] FixedCategories =
        {
            "Choice 1", "Choice 2", "Choice 3", "Choice 4",
            "Choice 5", "Choice 6", "Others"
        };

        public const string DefaultCategory = "Others";

        public static bool IsValid(string? name) =>
            !string.IsNullOrWhiteSpace(name) &&
            FixedCategories.Contains(name.Trim(), StringComparer.Ordinal);

        public static string ResolveName(string? suggestedName)
        {
            if (IsValid(suggestedName))
                return suggestedName!.Trim();

            return DefaultCategory;
        }
    }
}
