using System.Text.Json;

using Json.Pointer;

namespace Jaahas.Json {

    /// <summary>
    /// A delegate that can be used to test if <see cref="TimeSeriesExtractor"/> should process a 
    /// JSON element.
    /// </summary>
    /// <param name="context">
    ///   The time series extractor context.
    /// </param>
    /// <param name="pointer">
    ///   The JSON pointer to the element.
    /// </param>
    /// <param name="element">
    ///   The JSON element.
    /// </param>
    /// <returns>
    ///   <see langword="true"/> if the element should be processed; otherwise, <see langword="false"/>.
    /// </returns>
    public delegate bool JsonPointerMatchDelegate(TimeSeriesExtractorContext context, JsonPointer pointer, JsonElement element);

}
