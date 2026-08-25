namespace AeroMech.UI.Web.Pages.ListTemplate
{
    /// <summary>
    /// Sorting helpers shared by <see cref="ListTemplate{TItem}"/> and <see cref="SortHeader{TItem}"/>.
    /// </summary>
    internal static class GridSort
    {
        /// <summary>
        /// Compares the boxed values produced by a column selector. Columns hand back plain
        /// <see cref="object"/> so a single grid can mix strings, numbers, dates and enums.
        /// </summary>
        public static IComparer<object?> ValueComparer { get; } = new BoxedValueComparer();

        /// <summary>The sort order of a grid as it is written to local storage.</summary>
        public sealed record Preference(string Column, bool Descending);

        private sealed class BoxedValueComparer : IComparer<object?>
        {
            public int Compare(object? x, object? y)
            {
                if (ReferenceEquals(x, y)) return 0;

                // Empty cells sort together, ahead of everything else.
                if (x is null) return -1;
                if (y is null) return 1;

                if (x is string left && y is string right)
                    return string.Compare(left, right, StringComparison.CurrentCultureIgnoreCase);

                if (x.GetType() == y.GetType() && x is IComparable comparable)
                    return comparable.CompareTo(y);

                // Mismatched types are not something a column should produce, but fall back to
                // text rather than throwing in the middle of a render.
                return string.Compare(x.ToString(), y.ToString(), StringComparison.CurrentCultureIgnoreCase);
            }
        }
    }
}
