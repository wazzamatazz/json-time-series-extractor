using System;
using System.ComponentModel;
using System.Globalization;

using Json.Pointer;

namespace Jaahas.Json {

    /// <summary>
    /// <see cref="JsonPointerMatch"/> describes a rule for matching JSON Pointer paths.
    /// </summary>
    [TypeConverter(typeof(JsonPointerMatchTypeConverter))]
    public sealed class JsonPointerMatch {

        /// <summary>
        /// The JSON Pointer for the rule, if a valid JSON Pointer was specified when creating the 
        /// <see cref="JsonPointerMatch"/> instance.
        /// </summary>
        /// <remarks>
        ///   <see cref="Pointer"/> will be <see langword="null"/> when <see cref="RawValue"/> is 
        ///   a pattern matching expression that is not a syntactically valid JSON Pointer.  
        /// </remarks>
        public JsonPointer? Pointer { get; }

        /// <summary>
        /// The raw value of the JSON Pointer or pattern match expression.
        /// </summary>
        public string RawValue { get; } = default!;

        /// <summary>
        /// Specifies if <see cref="RawValue"/> contains a single-character pattern match wildcard.
        /// </summary>
        private readonly bool _containsSingleCharacterPatternWildcard;

        /// <summary>
        /// Specifies if <see cref="RawValue"/> contains a multi-character pattern match wildcard.
        /// </summary>
        private readonly bool _containsMultiCharacterPatternWildcard;

        /// <summary>
        /// Specifies if <see cref="Pointer"/> contains any segments that are single-level MQTT 
        /// wildcards.
        /// </summary>
        private readonly bool _containsSingleLevelMqttWildcardSegment;

        /// <summary>
        /// Specifies if <see cref="Pointer"/> contains a final segments that is a multi-level 
        /// MQTT wildcard.
        /// </summary>
        private readonly bool _containsMultiLevelMqttWildcardSegment;


        /// <summary>
        /// Specifies if the <see cref="JsonPointerMatch"/> instance represents a pattern match or 
        /// MQTT-style wildcard match rule.
        /// </summary>
        public bool IsWildcardMatchRule => IsPatternWildcardMatchRule || IsMqttWildcardMatchRule;

        /// <summary>
        /// Specifies if the <see cref="JsonPointerMatch"/> instance represents a pattern match rule.
        /// </summary>
        public bool IsPatternWildcardMatchRule => _containsSingleCharacterPatternWildcard || _containsMultiCharacterPatternWildcard;

        /// <summary>
        /// Specifies if the <see cref="JsonPointerMatch"/> instance represents an MQTT-style 
        /// wildcard match rule.
        /// </summary>
        public bool IsMqttWildcardMatchRule => _containsSingleLevelMqttWildcardSegment || _containsMultiLevelMqttWildcardSegment;

        
        /// <summary>
        /// Creates a new <see cref="JsonPointerMatch"/> instance.
        /// </summary>
        /// <param name="pointer">
        ///   The JSON Pointer.
        /// </param>
        /// <param name="throwOnNull">
        ///   When <see langword="true"/>, an exception will be thrown if <paramref name="pointer"/> 
        ///   is <see langword="null"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="pointer"/> is <see langword="null"/> and <paramref name="throwOnNull"/> 
        ///   is <see langword="true"/>.
        /// </exception>
        private JsonPointerMatch(JsonPointer? pointer, bool throwOnNull) {
            if (pointer == null && throwOnNull) {
                throw new ArgumentNullException(nameof(pointer));
            }

            Pointer = pointer;
            
            if (Pointer != null) {
                RawValue = Pointer.ToString();

                foreach (var segment in Pointer.Segments) {
                    if (segment.Value.Equals(TimeSeriesExtractor.SingleLevelMqttWildcard, StringComparison.Ordinal)) {
                        _containsSingleLevelMqttWildcardSegment = true;
                    }
                    else if (segment.Value.Equals(TimeSeriesExtractor.MultiLevelMqttWildcard, StringComparison.Ordinal) && segment.Equals(Pointer.Segments[Pointer.Segments.Length - 1])) {
                        _containsMultiLevelMqttWildcardSegment = true;
                    }
                    else if (segment.Value.Contains(TimeSeriesExtractor.SingleCharacterWildcard)) {
                        _containsSingleCharacterPatternWildcard = true;
                    }
                    else if (segment.Value.Contains(TimeSeriesExtractor.MultiCharacterWildcard)) {
                        _containsMultiCharacterPatternWildcard = true;
                    }
                }
            }
        }


        /// <summary>
        /// Creates a new <see cref="JsonPointerMatch"/> instance using the specified JSON 
        /// Pointer.
        /// </summary>
        /// <param name="pointer">
        ///   The JSON Pointer.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="pointer"/> is <see langword="null"/>.
        /// </exception>
        public JsonPointerMatch(JsonPointer pointer) : this(pointer, true) { }


        /// <summary>
        /// Creates a new <see cref="JsonPointerMatch"/> instance using the specified pointer 
        /// literal or pattern wildcard expression.
        /// </summary>
        /// <param name="pointer">
        ///   The JSON Pointer literal or pattern wildcard expression.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="pointer"/> is not a valid JSON Pointer or 
        ///   pattern wildcard expression.
        /// </exception>
        public JsonPointerMatch(string pointer) : this(pointer != null && JsonPointer.TryParse(pointer, out var p) ? p : null, false) {
            if (Pointer == null && pointer != null) {
                RawValue = pointer;

                if (pointer.Contains(TimeSeriesExtractor.SingleCharacterWildcard)) {
                    _containsSingleCharacterPatternWildcard = true;
                }
                if (pointer.Contains(TimeSeriesExtractor.MultiCharacterWildcard)) {
                    _containsMultiCharacterPatternWildcard = true;
                }
            }
            
            // If we haven't been given a valid pointer, we'll throw an exception unless the
            // pointer string is a pattern expression
            if (Pointer == null && !IsPatternWildcardMatchRule) {
                throw new ArgumentException(string.Format(Resources.Error_NotAValidJsonPointerOrPatternExpression, pointer), nameof(pointer));
            }
        }


        /// <summary>
        /// Tries to create a new <see cref="JsonPointerMatch"/> from a string.
        /// </summary>
        /// <param name="pointer">
        ///   The JSON Pointer string.
        /// </param>
        /// <param name="result">
        ///   The resulting <see cref="JsonPointerMatch"/> instance, or <see langword="null"/> 
        ///   if parsing is unsuccessful.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the string was successfully parsed; otherwise, 
        ///   <see langword="false"/>.
        /// </returns>
        public static bool TryParse(
            string? pointer,
#if NETCOREAPP
            [System.Diagnostics.CodeAnalysis.NotNullWhen(true)]
#endif
            out JsonPointerMatch? result
        ) {
            if (pointer == null) {
                result = null;
                return false;
            }

            try {
                result = new JsonPointerMatch(pointer);
                return true;
            }
            catch {
                result = null;
                return false;
            }
        }


        /// <summary>
        /// Creates a new <see cref="JsonPointerMatch"/> from a string.
        /// </summary>
        /// <param name="pointer">
        ///   The JSON Pointer string.
        /// </param>
        /// <returns>
        ///   The resulting <see cref="JsonPointerMatch"/> instance.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="pointer"/> is not a valid JSON Pointer or 
        ///   pattern wildcard expression.
        /// </exception>
        public static JsonPointerLiteral Parse(string pointer) => new JsonPointerLiteral(pointer);


        /// <inheritdoc />
        public override string ToString() => RawValue ?? Pointer!.ToString();


        /// <summary>
        /// Converts a <see cref="JsonPointer"/> to a <see cref="JsonPointerMatch"/>.
        /// </summary>
        /// <param name="pointer">
        ///   The JSON Pointer.
        /// </param>
        public static implicit operator JsonPointerMatch(JsonPointer pointer) => pointer == null ? null! : new JsonPointerMatch(pointer);


        /// <summary>
        /// Converts a <see cref="string"/> to a <see cref="JsonPointerMatch"/>.
        /// </summary>
        /// <param name="pointer">
        ///   The JSON Pointer literal.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="pointer"/> is not a valid JSON Pointer or a pattern match wildcard 
        ///   expression.
        /// </exception>
        public static implicit operator JsonPointerMatch(string pointer) => pointer == null ? null! : new JsonPointerMatch(pointer);


        /// <summary>
        /// <see cref="TypeConverter"/> for <see cref="JsonPointerMatch"/>.
        /// </summary>
        internal sealed class JsonPointerMatchTypeConverter : TypeConverter {

            /// <summary>
            /// The standard values used by this converter.
            /// </summary>
            /// <remarks>
            ///   This field is initialized using a so-called poor man's lazy in <see cref="GetStandardValues(ITypeDescriptorContext)"/>.
            /// </remarks>
            private static StandardValuesCollection? _standardValues;


            /// <inheritdoc />
            public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) {
                return sourceType == typeof(string) || sourceType == typeof(JsonPointer) || sourceType == typeof(JsonPointerMatch) || base.CanConvertFrom(context, sourceType);
            }


            /// <inheritdoc />
            public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) {
                return destinationType == typeof(string) || destinationType == typeof(JsonPointerMatch) || base.CanConvertTo(context, destinationType);
            }


            /// <inheritdoc />
            public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) {
                if (value is string s) {
                    return new JsonPointerMatch(s);
                }
                else if (value is JsonPointer pointer) {
                    return new JsonPointerMatch(pointer);
                }
                else if (value is JsonPointerMatch match) {
                    return match;
                }

                throw GetConvertFromException(value);
            }


            /// <inheritdoc />
            public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType) {
                if (destinationType == typeof(string)) {
                    return value?.ToString()!;
                }
                else if (destinationType == typeof(JsonPointerMatch)) {
                    return value;
                }

                throw GetConvertToException(value, destinationType);
            }


            /// <inheritdoc/>
            public override bool GetStandardValuesSupported(ITypeDescriptorContext? context) {
                return true;
            }


            /// <inheritdoc/>
            public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context) {
                return false;
            }


            /// <inheritdoc/>
            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context) {
                return _standardValues ??= new StandardValuesCollection(new[] { JsonPointer.Empty });
            }


            /// <inheritdoc/>
            public override bool IsValid(ITypeDescriptorContext? context, object? value) {
                if (value is string s) {
                    return TryParse(s, out _);
                }
                return value is JsonPointerMatch || value is JsonPointer;
            }

        }

    }
}
