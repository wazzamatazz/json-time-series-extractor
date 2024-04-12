using System.Collections.Generic;

namespace Jaahas.Json {

    /// <summary>
    /// Options when creating delegates for matching JSON Pointer paths.
    /// </summary>
    public class JsonPointerMatchDelegateOptions {

        /// <summary>
        /// The JSON pointers to match.
        /// </summary>
        /// <remarks>
        ///   If not <see langword="null"/>, only pointers in a JSON document that match an entry 
        ///   in this list will be processed. Otherwise, pointers will be processed unless they 
        ///   match an entry in <paramref name="PointersToExclude"/>.
        /// </remarks>
        public IEnumerable<JsonPointerMatch>? PointersToInclude { get; set; }

        /// <summary>
        /// The JSON pointers to exclude.
        /// </summary>
        /// <remarks>
        ///   If not <see langword="null"/>, pointers in a JSON document that match an entry in 
        ///   this list will always be excluded.
        /// </remarks>
        public IEnumerable<JsonPointerMatch>? PointersToExclude { get; set; }

        /// <summary>
        /// Specifies if entries in <see cref="PointersToInclude"/> and <see cref="PointersToExclude"/> 
        /// containing known wildcard characters should be treated as wildcard expressions instead 
        /// of literal paths.
        /// </summary>
        /// <remarks>
        /// 
        /// <para>
        ///   When wildcard patterns are enabled, pointer match rules can specify either pattern 
        ///   match wildcards (i.e. <c>?</c> for a single-character wildcard, and <c>*</c> for a 
        ///   multi-character wildcard), or MQTT-style wildcard characters (i.e. <c>+</c> for a 
        ///   single-level wildcard, and <c>#</c> for a multi-level wildcard).
        /// </para>
        /// 
        /// <para>
        ///   The two matching styles are mutually exclusive; if a pointer path contains single- 
        ///   or multi-character wildcard characters the path is assumed to be a pattern match, 
        ///   and MQTT-style wildcards are treated as literal characters. For example, 
        ///   <c>/foo/+/bar</c> is treated as an MQTT-style match, but <c>/foo/+/*</c> is treated 
        ///   as a regular pattern match.
        /// </para>
        /// 
        /// <para>
        ///   In an MQTT-style match expression, the multi-level wildcard character is only valid 
        ///   in the final segment of the pointer path. For example, <c>/foo/bar/#</c> is a valid 
        ///   MQTT match expression, but <c>/foo/#/bar</c> is not.
        /// </para>
        /// 
        /// <para>
        ///   In an MQTT-style match expression, you cannot specify both wildcard and non-wildcard 
        ///   characters in the same pointer segment. For example, <c>/foo/bar+/baz</c> is not a 
        ///   valid MQTT match expression and will be interpreted as a literal JSON Pointer path.
        /// </para>
        /// 
        /// </remarks>
        public bool AllowWildcardExpressions { get; set; }

        /// <summary>
        /// Specifies if pattern match expressions should use the <see cref="System.Text.RegularExpressions.RegexOptions.Compiled"/> 
        /// flag when creating their underlying regular expressions.
        /// </summary>
        /// <remarks>
        ///   Compiled regular expressions are enabled by default.
        /// </remarks>
        public bool UseCompiledRegularExpressions { get; set; } = true;

    }
}
