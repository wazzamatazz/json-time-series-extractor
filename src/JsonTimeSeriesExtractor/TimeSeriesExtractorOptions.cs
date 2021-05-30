using System;
using System.Collections.Generic;

namespace Jaahas.Json {

    /// <summary>
    /// Options for <see cref="TimeSeriesExtractor"/>.
    /// </summary>
    public class TimeSeriesExtractorOptions {

        /// <summary>
        /// The template to use when generating keys for extracted values.
        /// </summary>
        /// <remarks>
        ///   
        /// <para>
        ///   Templates can contain placholders, in the format <c>{property_name}</c>, where 
        ///   <c>property_name</c> is the name of a property on the JSON object that is being 
        ///   processed. The placeholder for the current property name being processed is 
        ///   <c>{$prop}</c>.
        /// </para>
        /// 
        /// <para>
        ///   For example, consider the following JSON:
        /// </para>
        /// 
        /// <code lang="JSON">
        /// {
        ///   "deviceId": 1,
        ///   "temperature": 21.7,
        ///   "pressure": 1001.2
        /// }
        /// </code>
        /// 
        /// <para>
        ///   Given a <see cref="Template"/> value of <c>devices/{deviceId}/{$prop}</c>, the key 
        ///   generated for the <c>pressure</c> property will be <c>devices/1/pressure</c>.
        /// </para>
        /// 
        /// <para>
        ///   Use the <see cref="ExcludeProperties"/> property to ignore JSON properties that are 
        ///   not required or are used only for metadata purposes.
        /// </para>
        /// 
        /// </remarks>
        public string Template { get; set; } = "{$prop}";

        /// <summary>
        /// A dictionary of default <see cref="Template"/> replacements to use if a referenced 
        /// property is not present in the JSON object.
        /// </summary>
        /// <remarks>
        ///   Don't include the <c>{</c> and <c>}</c> in the dictionary keys (i.e. use <c>deviceId</c> 
        ///   and not <c>{deviceId}</c>).
        /// </remarks>
        public IDictionary<string, string>? TemplateReplacements;

        /// <summary>
        /// A delegate used to identify the property name that contains the timestamp to use for 
        /// the extracted samples.
        /// </summary>
        /// <remarks>
        /// 
        /// <para>
        ///   If no <see cref="IsTimestampProperty"/> delegate is specified, the extractor will 
        ///   perform a case-insensitive match against the following property names (in order):
        /// </para>
        /// 
        /// <list type="bullet">
        ///   <item>
        ///     <description><c>time</c></description>
        ///   </item>
        ///   <item>
        ///     <description><c>timestamp</c></description>
        ///   </item>
        /// </list>
        /// 
        /// <para>
        ///   If a match is found and the property value can be converted to a <see cref="DateTimeOffset"/>, 
        ///   the property will be used.
        /// </para>
        /// 
        /// <para>
        ///   If no timestamp property can be found, <see cref="NowTimestamp"/> will be 
        ///   used as the sample time.
        /// </para>
        /// 
        /// </remarks>
        public Func<string, bool>? IsTimestampProperty { get; set; }

        /// <summary>
        /// The default timestamp to use if a timestamp property cannot be identified on a JSON 
        /// object.
        /// </summary>
        /// <remarks>
        ///   Specify <see langword="null"/> to use <see cref="DateTimeOffset.UtcNow"/> as the 
        ///   default sample timestamp.
        /// </remarks>
        public DateTimeOffset? NowTimestamp { get; set; }

        /// <summary>
        /// A delegate that is used to determine if a sample should be emitted for a given 
        /// property name.
        /// </summary>
        /// <remarks>
        ///   When <see cref="IncludeProperty"/> is <see langword="null"/>, the default behaviour 
        ///   is to emit a sample for every property except for the property identified as the 
        ///   timestamp property.
        /// </remarks>
        public Func<string, bool>? IncludeProperty { get; set; } = null;

        /// <summary>
        /// When <see langword="true"/>, JSON properties that contain other objects or arrays will 
        /// be processed recursively, instead of treating the properties as string values.
        /// </summary>
        /// <remarks>
        /// 
        /// <para>
        ///   <see cref="PathSeparator"/> is used to separate hierarchy levels when recursively 
        ///   processing objects.
        /// </para>
        /// 
        /// <para>
        ///   Consider the following JSON:
        /// </para>
        /// 
        /// <code lang="JSON">
        /// {
        ///   "deviceId": 1,
        ///   "measurements": {
        ///     "temperature": 21.7,
        ///     "pressure": 1001.2
        ///   }
        /// }
        /// </code>
        /// 
        /// <para>
        ///   Given a key template of <c>devices/{deviceId}/{$prop}</c>, <see cref="ExcludeProperties"/> 
        ///   configured to include <c>deviceId</c>, recursive processing enabled, and a path 
        ///   separator of <c>/</c>, values for the following keys will be emitted:
        /// </para>
        /// 
        /// <list type="bullet">
        ///   <item>
        ///     <description><c>devices/1/measurements/temperature</c></description>
        ///   </item>
        ///   <item>
        ///     <description><c>devices/1/measurements/pressure</c></description>
        ///   </item>
        /// </list>
        /// 
        /// <para>
        ///   When processing an array rather than an object, the array index will be used as part 
        ///   of the key. For example, consider the following JSON:
        /// </para>
        /// 
        /// <code lang="JSON">
        /// {
        ///   "deviceId": 1,
        ///   "measurements": [
        ///     21.7,
        ///     1001.2
        ///   ]
        /// }
        /// </code>
        /// 
        /// <para>
        ///   Using the same options as the previous example, values for the following keys will 
        ///   be emitted:
        /// </para>
        /// 
        /// <list type="bullet">
        ///   <item>
        ///     <description><c>devices/1/measurements/0</c></description>
        ///   </item>
        ///   <item>
        ///     <description><c>devices/1/measurements/1</c></description>
        ///   </item>
        /// </list>
        /// 
        /// </remarks>
        public bool Recursive { get; set; }

        /// <summary>
        /// When <see cref="Recursive"/> is <see langword="true"/>, <see cref="PathSeparator"/> is 
        /// used to separate hierarchy levels when processing nested objects and arrays.
        /// </summary>
        public string PathSeparator { get; set; } = "/";

    }
}
