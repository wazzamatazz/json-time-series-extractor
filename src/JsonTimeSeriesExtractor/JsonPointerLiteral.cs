using System;
using System.ComponentModel;
using System.Globalization;

using Json.Pointer;

namespace Jaahas.Json {

    /// <summary>
    /// Represents a JSON Pointer that is created from a <see cref="JsonPointer"/> instance or a 
    /// literal string.
    /// </summary>
    [TypeConverter(typeof(JsonPointerLiteralTypeConverter))]
    public sealed class JsonPointerLiteral {

        /// <summary>
        /// The JSON Pointer.
        /// </summary>
        public JsonPointer Pointer { get; }


        /// <summary>
        /// Creates a new <see cref="JsonPointerLiteral"/> instance using a JSON Pointer.
        /// </summary>
        /// <param name="pointer">
        ///   The JSON Pointer.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="pointer"/> is <see langword="null"/>.
        /// </exception>
        public JsonPointerLiteral(JsonPointer pointer) {
            Pointer = pointer ?? throw new ArgumentNullException(nameof(pointer));
        }


        /// <summary>
        /// Creates a new <see cref="JsonPointerLiteral"/> instance using a JSON Pointer literal.
        /// </summary>
        /// <param name="pointer">
        ///   The pointer literal.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="pointer"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="PointerParseException">
        ///   <paramref name="pointer"/> is not a valid JSON Pointer.
        /// </exception>
        public JsonPointerLiteral(string pointer) {
            if (pointer == null) {
                throw new ArgumentNullException(nameof(pointer));
            }

            Pointer = JsonPointer.Parse(pointer);
        }


        /// <summary>
        /// Tries to create a new <see cref="JsonPointerLiteral"/> from a string.
        /// </summary>
        /// <param name="pointer">
        ///   The JSON Pointer string.
        /// </param>
        /// <param name="result">
        ///   The resulting <see cref="JsonPointerLiteral"/> instance, or <see langword="null"/> 
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
            out JsonPointerLiteral? result
        ) {
            if (pointer == null) {
                result = null;
                return false;
            }

            if (JsonPointer.TryParse(pointer, out var jsonPointer)) {
                result = new JsonPointerLiteral(jsonPointer);
                return true;
            }

            result = null;
            return false;
        }


        /// <inheritdoc />
        public override int GetHashCode() => Pointer.GetHashCode();


        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is JsonPointerLiteral other
            ? Pointer.Equals(other.Pointer)
            : Pointer.Equals(obj);


        /// <inheritdoc />
        public override string ToString() => Pointer.ToString();


        /// <summary>
        /// Converts a <see cref="JsonPointerLiteral"/> to a <see cref="JsonPointer"/>.
        /// </summary>
        /// <param name="literal">
        ///   The JSON Pointer literal.
        /// </param>
        public static implicit operator JsonPointer(JsonPointerLiteral literal) => literal.Pointer;


        /// <summary>
        /// Converts a <see cref="JsonPointer"/> to a <see cref="JsonPointerLiteral"/>.
        /// </summary>
        /// <param name="pointer">
        ///   The JSON Pointer.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="pointer"/> is <see langword="null"/>.
        /// </exception>
        public static implicit operator JsonPointerLiteral(JsonPointer pointer) => new JsonPointerLiteral(pointer);


        /// <summary>
        /// Converts a <see cref="string"/> to a <see cref="JsonPointerLiteral"/>.
        /// </summary>
        /// <param name="pointer">
        ///   The JSON Pointer literal.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="pointer"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="PointerParseException">
        ///   <paramref name="pointer"/> is not a valid JSON Pointer.
        /// </exception>
        public static implicit operator JsonPointerLiteral(string pointer) => new JsonPointerLiteral(pointer);


        /// <summary>
        /// <see cref="TypeConverter"/> for <see cref="JsonPointerLiteral"/>.
        /// </summary>
        public sealed class JsonPointerLiteralTypeConverter : TypeConverter {

            /// <inheritdoc />
            public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) {
                return sourceType == typeof(string) || sourceType == typeof(JsonPointer) || sourceType == typeof(JsonPointerLiteral);
            }


            /// <inheritdoc />
            public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) {
                return destinationType == typeof(string) || destinationType == typeof(JsonPointer) || destinationType == typeof(JsonPointerLiteral);
            }


            /// <inheritdoc />
            public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) {
                if (value is string s) {
                    return new JsonPointerLiteral(s);
                }
                else if (value is JsonPointer pointer) {
                    return new JsonPointerLiteral(pointer);
                }
                else if (value is JsonPointerLiteral literal) {
                    return literal;
                }

                return base.ConvertFrom(context, culture, value);
            }


            /// <inheritdoc />
            public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType) {
                if (destinationType == typeof(string)) {
                    return value?.ToString()!;
                }
                else if (destinationType == typeof(JsonPointer) && value is JsonPointerLiteral pointerLiteral) {
                    return pointerLiteral.Pointer;
                }
                else if (destinationType == typeof(JsonPointerLiteral)) {
                    return value;
                }

                return base.ConvertTo(context, culture, value, destinationType);
            }

        }

    }
}
