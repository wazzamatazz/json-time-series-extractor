using System.Text.Json;

namespace Jaahas.Json {

    /// <summary>
    /// An entry in <see cref="TimeSeriesExtractorContext.ElementStack"/>.
    /// </summary>
    internal readonly struct ElementStackEntry {

        /// <summary>
        /// The property name or array index of the element in its parent element, or <see langword="null"/> 
        /// if the element is the root element.
        /// </summary>
        public string? Key { get; }

        /// <summary>
        /// The element.
        /// </summary>
        public JsonElement Element { get; }

        /// <summary>
        /// Specifies if the <see cref="Element"/> is an entry in an array.
        /// </summary>
        public bool IsArrayItem { get; }


        /// <summary>
        /// Creates a new <see cref="ElementStackEntry"/> instance.
        /// </summary>
        /// <param name="key">
        ///   The property name or array index of the element in its parent element, or <see langword="null"/> 
        ///   if the element is the root element.
        /// </param>
        /// <param name="element">
        ///   The element.
        /// </param>
        /// <param name="isArrayItem">
        ///   Specifies if the <paramref name="element"/> is an entry in an array.
        /// </param>
        public ElementStackEntry(string? key, JsonElement element, bool isArrayItem) {
            Key = key;
            Element = element;
            IsArrayItem = isArrayItem;
        }

    }
}
