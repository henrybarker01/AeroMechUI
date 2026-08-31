using Microsoft.AspNetCore.Components;

namespace AeroMech.UI.Web.Components
{
    /// <summary>
    /// One action on a grid row. Every grid uses these so the same job wears the
    /// same icon everywhere: <see cref="Icons"/> holds the vocabulary.
    /// </summary>
    public partial class RowAction
    {
        /// <summary>Bootstrap Icons class for the glyph. Use a constant from <see cref="Icons"/>.</summary>
        [Parameter, EditorRequired] public string Icon { get; set; } = string.Empty;

        /// <summary>
        /// What the button does, phrased as an instruction - "Edit this client".
        /// Shown on hover and read out in place of the icon.
        /// </summary>
        [Parameter, EditorRequired] public string Label { get; set; } = string.Empty;

        [Parameter] public EventCallback OnClick { get; set; }

        /// <summary>Deletes and cancellations, which turn red on hover.</summary>
        [Parameter] public bool Danger { get; set; }

        [Parameter] public bool Disabled { get; set; }

        [Parameter] public string? Class { get; set; }
    }

    /// <summary>
    /// The icon each row action is drawn with. One job, one glyph - the grids used
    /// to disagree about which icon meant "delete", which is what this fixes.
    /// </summary>
    public static class Icons
    {
        public const string Edit = "bi-pencil";
        public const string Delete = "bi-trash";
        public const string Remove = "bi-x-lg";
        public const string Print = "bi-printer";
        public const string Open = "bi-box-arrow-up-right";
        public const string Convert = "bi-file-earmark-check";
        public const string Add = "bi-plus-lg";
    }
}
