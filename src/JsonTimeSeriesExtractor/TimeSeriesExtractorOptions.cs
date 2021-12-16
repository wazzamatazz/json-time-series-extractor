using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Jaahas.Json {

    /// <summary>
    /// Options for <see cref="TimeSeriesExtractor"/>.
    /// </summary>
    public class TimeSeriesExtractorOptions {

        /// <summary>
        /// Default <see cref="Template"/> value.
        /// </summary>
        public const string DefaultTemplate = "{$prop}";

        /// <summary>
        /// Default <see cref="PathSeparator"/> value.
        /// </summary>
        public const string DefaultPathSeparator = "/";

        /// <summary>
        /// Default <see cref="MaxDepth"/> value.
        /// </summary>
        public const int DefaultMaxDepth = 5;

        /// <summary>
        /// The template to use when generating keys for extracted values.
        /// </summary>
        /// <remarks>
        /// 
        /// <para>
        ///   If <see cref="Template"/> is <see langword="null"/> or white space, <see cref="DefaultTemplate"/> 
        ///   will be used.
        /// </para>
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
        ///   Use the <see cref="IncludeProperty"/> delegate to ignore JSON properties that are 
        ///   not required or are used only for metadata purposes, and the <see cref="GetTemplateReplacement"/> 
        ///   delegate to define default replacement values for placeholders that are not found in 
        ///   the JSON object.
        /// </para>
        /// 
        /// <para>
        ///   Important: When <see cref="Recursive"/> mode is enabled, placeholders behave differently. 
        ///   See the documentation for the <see cref="Recursive"/> property for more information.
        /// </para>
        /// 
        /// </remarks>
        public string Template { get; set; } = DefaultTemplate;

        /// <summary>
        /// A delegate that accepts a placeholder name referenced in the <see cref="Template"/> and
        /// returns the default replacement for that placeholder.
        /// </summary>
        /// <remarks>
        ///   The default replacement for a given placeholder is only used if a replacement cannot 
        ///   be identified from the JSON that is being parsed.
        /// </remarks>
        public Func<string, string?>? GetTemplateReplacement { get; set; }

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
        ///   If no timestamp property can be found, <see cref="GetDefaultTimestamp"/> will be 
        ///   used as the sample time.
        /// </para>
        /// 
        /// </remarks>
        public Func<string, bool>? IsTimestampProperty { get; set; }

        /// <summary>
        /// A delegate that will retrieve the default sample timestamp to use if a timestamp 
        /// property cannot be identified on a JSON object.
        /// </summary>
        /// <remarks>
        ///   Specify <see langword="null"/> to use <see cref="DateTimeOffset.UtcNow"/> at the 
        ///   moment that the JSON is parsed as the default sample timestamp.
        /// </remarks>
        public Func<DateTimeOffset>? GetDefaultTimestamp { get; set; }

        /// <summary>
        /// A delegate that is used to determine if a sample should be emitted for a given 
        /// property name.
        /// </summary>
        /// <remarks>
        /// 
        /// <para>
        ///   The parameter passed to the delegate is an array of <see cref="KeyValuePair{String, JsonElement}"/> 
        ///   that represents the JSON property names and elements that have been visited to reach 
        ///   the property that is currently being processed, with the entry for the current 
        ///   property first on the list, and the entry for the property on the root object last. 
        ///   The entry for the root object in the document will always have a <see langword="null"/> 
        ///   <see cref="KeyValuePair{String, JsonElement}.Key"/> value.
        /// </para>
        /// 
        /// <para>
        ///   When <see cref="IncludeProperty"/> is <see langword="null"/>, the default behaviour 
        ///   is to emit a sample for every property except for the property identified as the 
        ///   timestamp property.
        /// </para>
        /// 
        /// </remarks>
        public Func<KeyValuePair<string?, JsonElement>[], bool>? IncludeProperty { get; set; } = null;

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
        ///   Given a key template of <c>devices/{deviceId}/{$prop}</c>, <see cref="IncludeProperty"/> 
        ///   configured to skip <c>deviceId</c>, recursive processing enabled, and a path 
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
        /// <para>
        ///   Note that, when running in recursive mode, template placeholder replacements behave 
        ///   differently:
        /// </para>
        /// 
        /// <list type="bullet">
        ///   <item>
        ///     <description>
        ///       When a template includes a reference to another property (e.g. <c>deviceId</c> in <c>{deviceId}/{$prop}</c>), 
        ///       all instances of that property from the root object to the current element will 
        ///       be used in the replacement, using the <see cref="PathSeparator"/> to join them 
        ///       together. For example, if a parent object has a <c>deviceId</c> of <c>1</c> and 
        ///       a child object has a <c>deviceId</c> of <c>A</c>, the <c>{deviceId}</c> placeholder 
        ///       in the key template will be replaced with <c>1/A</c> when it is applied to a 
        ///       property on the child object.
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <description>
        ///       The <c>{$prop}</c> placeholder is replaced with the names of every property that 
        ///       was visited in order to arrive at the current property from the root object (e.g. 
        ///       <c>measurements/acceleration/X</c>). If you only require the local, unqualified 
        ///       property name in your generated keys, you can use the <c>{$prop-local}</c> instead.
        ///     </description>
        ///   </item>
        /// </list>
        /// 
        /// </remarks>
        public bool Recursive { get; set; }

        /// <summary>
        /// The maximum allowed recursion depth when <see cref="Recursive"/> is <see langword="true"/>.
        /// </summary>
        /// <remarks>
        /// 
        /// <para>
        ///   When the recursion depth limit is reached and the current JSON element is an array or 
        ///   an object, a sample will be emitted that contains the serialized JSON element as its 
        ///   value instead of recursing into the object or array.
        /// </para>
        /// 
        /// <para>
        ///   A <see cref="MaxDepth"/> of less than one specifies that there is no recursion limit.
        /// </para>
        /// 
        /// </remarks>
        public int MaxDepth { get; set; } = DefaultMaxDepth;

        /// <summary>
        /// When <see cref="Recursive"/> is <see langword="true"/>, <see cref="PathSeparator"/> is 
        /// used to separate hierarchy levels when processing nested objects and arrays.
        /// </summary>
        public string PathSeparator { get; set; } = DefaultPathSeparator;


        /// <summary>
        /// Creates a new <see cref="TimeSeriesExtractorOptions"/> instance.
        /// </summary>
        public TimeSeriesExtractorOptions() { }


        /// <summary>
        /// Creates a new <see cref="TimeSeriesExtractorOptions"/> instance from an existing 
        /// instance.
        /// </summary>
        /// <param name="existing">
        ///   The existing instance.
        /// </param>
        public TimeSeriesExtractorOptions(TimeSeriesExtractorOptions? existing) {
            if (existing == null) {
                return;
            }

            GetDefaultTimestamp = existing.GetDefaultTimestamp;
            GetTemplateReplacement = existing.GetTemplateReplacement;
            IncludeProperty = existing.IncludeProperty;
            IsTimestampProperty = existing.IsTimestampProperty;
            MaxDepth = existing.MaxDepth;
            PathSeparator = existing.PathSeparator;
            Recursive = existing.Recursive;
            Template = existing.Template;
        }

    }
}
