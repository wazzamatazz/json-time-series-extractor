using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

using Json.Pointer;

namespace Jaahas.Json {

    /// <summary>
    /// Options for <see cref="TimeSeriesExtractor"/>.
    /// </summary>
    public class TimeSeriesExtractorOptions : IValidatableObject {

        /// <summary>
        /// Specifies a JSON Pointer that the <see cref="TimeSeriesExtractor"/> should start 
        /// processing data from.
        /// </summary>
        /// <remarks>
        ///   If <see cref="StartAt"/> is <see langword="null"/> start from the root of the JSON 
        ///   document.
        /// </remarks>
        public JsonPointerLiteral? StartAt { get; set; }

        /// <summary>
        /// The template to use when generating keys for extracted values.
        /// </summary>
        /// <remarks>
        /// 
        /// <para>
        ///   If <see cref="Template"/> is <see langword="null"/> or white space, <see cref="TimeSeriesExtractorConstants.DefaultTemplate"/> 
        ///   will be used.
        /// </para>
        ///   
        /// <para>
        ///   Templates can contain placholders, in the format <c>{property_name}</c>, where 
        ///   <c>property_name</c> is the name of a property on the JSON object that is being 
        ///   processed. The placeholder for the JSON Pointer path of the property being processed 
        ///   (without the leading <c>/</c>) is <c>{$prop}</c>.
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
        public string Template { get; set; } = TimeSeriesExtractorConstants.DefaultTemplate;

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
        /// Specifies if the <see cref="TimeSeriesExtractor"/> will emit a value for a JSON 
        /// property even if it cannot find replacement values for all placeholders in the 
        /// <see cref="Template"/>.
        /// </summary>
        public bool AllowUnresolvedTemplateReplacements { get; set; } = true;

        /// <summary>
        /// The JSON Pointer to the property that defines the timestamp for the samples 
        /// extracted from the JSON.
        /// </summary>
        /// <remarks>
        /// 
        /// <para>
        ///   If <see cref="TimestampProperty"/> is <see langword="null"/>, the configured 
        ///   timestamp property can not be found in the JSON document, or the property does not 
        ///   represent a <see cref="DateTimeOffset"/> value, <see cref="GetDefaultTimestamp"/> 
        ///   will be used the retrieve the sample time.
        /// </para>
        /// 
        /// <para>
        ///   By default, timestamps can be specified as a string value that can be directly parsed 
        ///   to <see cref="DateTimeOffset"/>, or as a number value that represents the number of 
        ///   milliseconds since 1 January 1970 UTC. The default parsing rules can be overridden 
        ///   by specifying a value for the <see cref="TimestampParser"/> property.
        /// </para>
        /// 
        /// </remarks>
        /// <seealso cref="TimestampParser"/>
        public JsonPointerLiteral? TimestampProperty { get; set; } = JsonPointer.Parse(TimeSeriesExtractorConstants.DefaultTimestampProperty);

        /// <summary>
        /// A delegate that overrides the default timestamp parser.
        /// </summary>
        /// <remarks>
        /// 
        /// <para>
        ///   By default, timestamps can be specified as a string value that can be directly parsed 
        ///   to <see cref="DateTimeOffset"/>, or as a number value that represents the number of 
        ///   milliseconds since 1 January 1970 UTC.
        /// </para>
        /// 
        /// <para>
        ///   Specify a value for this property if you need to customise timestamp parsing e.g. if 
        ///   the timestamp is specified as the number of whole seconds since 1 Januaty 1970.
        /// </para>
        /// 
        /// </remarks>
        public Func<JsonElement, DateTimeOffset?>? TimestampParser { get; set; }

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
        /// When both <see cref="AllowNestedTimestamps"/> and <see cref="Recursive"/> are <see langword="true"/>, 
        /// the <see cref="TimestampProperty"/> will be resolved at every level of the JSON document 
        /// hierarchy instead of being resolved once against the root element only.
        /// </summary>
        /// <remarks>
        ///   
        /// <para>
        ///   This option is useful when processing JSON documents that contain multiple samples, 
        ///   with each sample specifying its own timestamp.
        /// </para>
        /// 
        /// <para>
        ///   For example, consider the following JSON:
        /// </para>
        /// 
        /// <code lang="JSON">
        /// {
        ///   "data": {
        ///     "device-1": {
        ///       "time": "2023-12-01T00:00:00Z",
        ///       "temperature": 21.7
        ///     },
        ///     "device-2": {
        ///       "time": "2023-12-01T00:30:00Z",
        ///       "temperature": 22.1
        ///     }
        ///   }
        /// }
        /// </code>
        /// 
        /// <para>
        ///   By setting <see cref="AllowNestedTimestamps"/> and <see cref="Recursive"/> to <see langword="true"/> 
        ///   and using <c>/time</c> as the <see cref="TimestampProperty"/>, each <c>temperature</c> 
        ///   sample will be assigned the timestamp from its sibling <c>time</c> property.
        /// </para>
        /// 
        /// </remarks>
        public bool AllowNestedTimestamps { get; set; }

        /// <summary>
        /// A delegate that is used to determine if a JSON element should be processed by the time 
        /// series extractor.
        /// </summary>
        public JsonPointerMatchDelegate? CanProcessElement { get; set; } 

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
        ///   separator of <c>/</c>, values for the following keys under the <c>measurements</c> 
        ///   property will be emitted:
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
        ///       When a template includes a reference to another property (e.g. <c>deviceId</c> in 
        ///       <c>{deviceId}/{$prop}</c>), all instances of that property from the root object 
        ///       to the current element will be used in the replacement, using the <see cref="PathSeparator"/> 
        ///       to join them together. For example, if a parent object has a <c>deviceId</c> of 
        ///       <c>1</c> and a child object has a <c>deviceId</c> of <c>A</c>, the <c>{deviceId}</c> 
        ///       placeholder in the key template will be replaced with <c>1/A</c> when it is applied 
        ///       to a property on the child object.
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <description>
        ///       The <c>{$prop}</c> placeholder is replaced with the names of every property that 
        ///       was visited in order to arrive at the current property from the root object (e.g. 
        ///       <c>measurements/acceleration/X</c>). If you only require the local, unqualified 
        ///       property name in your generated keys, you can use the <c>{$prop-local}</c> placeholder 
        ///       instead.
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
        public int MaxDepth { get; set; } = TimeSeriesExtractorConstants.DefaultMaxDepth;

        /// <summary>
        /// When <see cref="Recursive"/> is <see langword="true"/>, <see cref="PathSeparator"/> is 
        /// used to separate hierarchy levels when generating sample keys for nested objects and arrays.
        /// </summary>
        [Required]
        public string PathSeparator { get; set; } = TimeSeriesExtractorConstants.DefaultPathSeparator;

        /// <summary>
        /// When <see cref="Recursive"/> is <see langword="true"/>, setting <see cref="IncludeArrayIndexesInSampleKeys"/> 
        /// to <see langword="false"/> will omit array indexes from the keys generated for extracted 
        /// samples.
        /// </summary>
        /// <remarks>
        /// 
        /// <para>
        ///   This property is <see langword="true"/> by default. It can be useful to set it to
        ///   <see langword="false"/> when a JSON document contains multiple samples for the same 
        ///   device or instrument, with each sample defining its own timestamp.
        /// </para>
        /// 
        /// <para>
        ///   For example, consider the following JSON:
        /// </para>
        /// 
        /// <code lang="JSON">
        /// {
        ///   "data": {
        ///     "device-1": [
        ///       {
        ///         "time": "2023-12-01T00:00:00Z",
        ///         "temperature": 21.7
        ///       },
        ///       {
        ///         "time": "2023-12-01T00:30:00Z",
        ///         "temperature": 22.1
        ///       }
        ///     ]
        ///   }
        /// }
        /// </code>
        /// 
        /// <para>
        ///   When <see cref="Recursive"/> and <see cref="AllowNestedTimestamps"/> are <see langword="true"/>, 
        ///   and <see cref="IncludeArrayIndexesInSampleKeys"/> is <see langword="false"/>, multiple 
        ///   samples are emitted using <c>data/device-1/temperature</c> as their sample key. If 
        ///   <see cref="IncludeArrayIndexesInSampleKeys"/> was <see langword="true"/>, the sample 
        ///   keys would be <c>data/device-1/0/temperature</c> and <c>data/device-1/1/temperature</c>.
        /// </para>
        /// 
        /// <para>
        ///   Use the <see cref="AllowNestedTimestamps"/> property to allow sample timestamps to 
        ///   be defined in nested objects.
        /// </para>
        /// 
        /// </remarks>
        /// <seealso cref="AllowNestedTimestamps"/>
        public bool IncludeArrayIndexesInSampleKeys { get; set; } = true;


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

            AllowNestedTimestamps = existing.AllowNestedTimestamps;
            AllowUnresolvedTemplateReplacements = existing.AllowUnresolvedTemplateReplacements;
            CanProcessElement = existing.CanProcessElement;
            GetDefaultTimestamp = existing.GetDefaultTimestamp;
            GetTemplateReplacement = existing.GetTemplateReplacement;
            IncludeArrayIndexesInSampleKeys = existing.IncludeArrayIndexesInSampleKeys;
            MaxDepth = existing.MaxDepth;
            PathSeparator = existing.PathSeparator;
            Recursive = existing.Recursive;
            StartAt = existing.StartAt;
            Template = existing.Template;
            TimestampParser = existing.TimestampParser;
            TimestampProperty = existing.TimestampProperty;
        }


        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
            if (string.IsNullOrWhiteSpace(Template)) {
                yield return new ValidationResult($"The template cannot be null or white space.", new[] { nameof(Template) });
            }
        }

    }
}
