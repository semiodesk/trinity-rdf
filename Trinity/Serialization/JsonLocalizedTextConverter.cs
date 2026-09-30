// LICENSE:
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
// AUTHORS:
//
//  Moritz Eberl <moritz@semiodesk.com>
//
// Copyright (c) Semiodesk GmbH 2026

using System;
using Newtonsoft.Json;

namespace Semiodesk.Trinity.Serialization
{
    /// <summary>
    /// Writes a localized-text container as an object carrying both its tagged values and its
    /// untagged one, and refuses to read one back.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Both containers implement <see cref="System.Collections.Generic.IEnumerable{T}"/> of
    /// <see cref="LangString"/>, so Newtonsoft serialized them as a bare array of tagged values and
    /// silently dropped the untagged <c>Invariant</c> — a value the container does hold and that no
    /// consumer of the JSON could tell was missing.
    /// </para>
    /// <para>
    /// Reading is <b>refused rather than approximated</b>, where reading is attempted at all.
    /// Populating a container would have to decide whether to merge with or replace what the resource
    /// already holds, and that decision belongs with the redesign in issue #51, not with a converter;
    /// without the refusal the attempt failed anyway, with an opaque "unable to find a constructor to
    /// use for type LangString".
    /// </para>
    /// <para>
    /// <b>The refusal only fires for a container property that declares a setter</b> — the shape
    /// TRIN009 warns about. For the recommended get-only shape Newtonsoft skips the property outright
    /// and never consults a converter, so such a container is silently left as it was: a JSON edit to
    /// it is ignored. That is the limitation to know about, and it is deliberately preferred to the
    /// alternative that was tried — clearing containers before deserialization, which made an
    /// unedited round trip followed by <c>Commit()</c> delete every stored value of the property.
    /// </para>
    /// </remarks>
    public class JsonLocalizedTextConverter : JsonConverter
    {
        /// <summary>
        /// Indicates whether this converter handles the given type.
        /// </summary>
        /// <param name="objectType">The type to convert.</param>
        /// <returns><c>true</c> for a localized-text container.</returns>
        public override bool CanConvert(Type objectType)
        {
            return typeof(ILocalizedText).IsAssignableFrom(objectType);
        }

        /// <summary>
        /// Writes the container's tagged values and its untagged value.
        /// </summary>
        /// <param name="writer">The JSON writer.</param>
        /// <param name="value">The container.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();

                return;
            }

            writer.WriteStartObject();
            writer.WritePropertyName("values");
            writer.WriteStartArray();

            foreach (LangString literal in (ILocalizedText)value)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("value");
                writer.WriteValue(literal.Value);
                writer.WritePropertyName("language");
                writer.WriteValue(literal.Language);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            switch (value)
            {
                case LocalizedString single when single.Invariant != null:
                    writer.WritePropertyName("invariant");
                    writer.WriteValue(single.Invariant);
                    break;

                case LocalizedStringCollection many when many.HasInvariant:
                    writer.WritePropertyName("invariant");
                    writer.WriteStartArray();

                    foreach (string plain in many.Invariant)
                    {
                        writer.WriteValue(plain);
                    }

                    writer.WriteEndArray();
                    break;
            }

            writer.WriteEndObject();
        }

        /// <summary>
        /// Always throws: a localized-text container cannot be deserialized.
        /// </summary>
        /// <param name="reader">The JSON reader.</param>
        /// <param name="objectType">The target type.</param>
        /// <param name="existingValue">The existing value, if any.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>Never returns.</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotSupportedException(
                $"A '{objectType.Name}' cannot be deserialized from JSON. The container is a mutable " +
                "view owned by the mapping and is declared get-only, so there is nothing to assign, " +
                "and merging into the existing instance would silently combine the JSON with whatever " +
                "the resource already holds. Read the resource from its model and edit the container " +
                "in place instead.");
        }
    }
}
