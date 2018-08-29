using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using SRM = System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.Util;
using System.Reflection.Metadata.Ecma335;
using ICSharpCode.Decompiler.Metadata;

namespace ICSharpCode.Decompiler
{
	public static partial class SRMExtensions
	{

		public static TypeReferenceHandle GetDeclaringType(this TypeReference tr)
		{
			switch (tr.ResolutionScope.Kind) {
				case HandleKind.TypeReference:
					return (TypeReferenceHandle)tr.ResolutionScope;
				default:
					return default(TypeReferenceHandle);
			}
		}

		public static FullTypeName GetFullTypeName(this EntityHandle handle, MetadataReader reader)
		{
			if (handle.IsNil)
				throw new ArgumentNullException(nameof(handle));
			switch (handle.Kind) {
				case HandleKind.TypeDefinition:
					return ((TypeDefinitionHandle)handle).GetFullTypeName(reader);
				case HandleKind.TypeReference:
					return ((TypeReferenceHandle)handle).GetFullTypeName(reader);
				case HandleKind.TypeSpecification:
					return ((TypeSpecificationHandle)handle).GetFullTypeName(reader);
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

        /// <summary>
        /// Gets the type of the attribute.
        /// </summary>
        public static EntityHandle GetAttributeType(this SRM.CustomAttribute attribute, MetadataReader reader)
        {
            switch (attribute.Constructor.Kind)
            {
                case HandleKind.MethodDefinition:
                    var md = reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor);
                    return md.GetDeclaringType();
                case HandleKind.MemberReference:
                    var mr = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
                    return mr.Parent;
                default:
                    throw new BadImageFormatException("Unexpected token kind for attribute constructor: " + attribute.Constructor.Kind);
            }
        }

        public static FullTypeName GetFullTypeName(this TypeSpecificationHandle handle, MetadataReader reader)
		{
			if (handle.IsNil)
				throw new ArgumentNullException(nameof(handle));
			var ts = reader.GetTypeSpecification(handle);
			return ts.DecodeSignature(new Metadata.FullTypeNameSignatureDecoder(reader), default(Unit));
		}

		public static FullTypeName GetFullTypeName(this TypeReferenceHandle handle, MetadataReader reader)
		{
			if (handle.IsNil)
				throw new ArgumentNullException(nameof(handle));
			var tr = reader.GetTypeReference(handle);
			string name;
			try {
				name = reader.GetString(tr.Name);
			} catch (BadImageFormatException) {
				name = $"TR{reader.GetToken(handle):x8}";
			}
			name = SplitTypeParameterCountFromReflectionName(name, out var typeParameterCount);
			TypeReferenceHandle declaringTypeHandle;
			try {
				declaringTypeHandle = tr.GetDeclaringType();
			} catch (BadImageFormatException) {
				declaringTypeHandle = default(TypeReferenceHandle);
			}
			if (declaringTypeHandle.IsNil) {
				string ns;
				try {
					ns = tr.Namespace.IsNil ? "" : reader.GetString(tr.Namespace);
				} catch (BadImageFormatException) {
					ns = "";
				}
				return new FullTypeName(new TopLevelTypeName(ns, name, typeParameterCount));
			} else {
				return declaringTypeHandle.GetFullTypeName(reader).NestedType(name, typeParameterCount);
			}
		}

        public static string SplitTypeParameterCountFromReflectionName(string reflectionName, out int typeParameterCount)
        {
            int pos = reflectionName.LastIndexOf('`');
            if (pos < 0)
            {
                typeParameterCount = 0;
                return reflectionName;
            }
            else
            {
                string typeCount = reflectionName.Substring(pos + 1);
                if (int.TryParse(typeCount, out typeParameterCount))
                    return reflectionName.Substring(0, pos);
                else
                    return reflectionName;
            }
        }

        public static FullTypeName GetFullTypeName(this TypeDefinitionHandle handle, MetadataReader reader)
		{
			if (handle.IsNil)
				throw new ArgumentNullException(nameof(handle));
			return reader.GetTypeDefinition(handle).GetFullTypeName(reader);
		}

		public static FullTypeName GetFullTypeName(this TypeDefinition td, MetadataReader reader)
		{
			TypeDefinitionHandle declaringTypeHandle;
			string name = SplitTypeParameterCountFromReflectionName(reader.GetString(td.Name), out var typeParameterCount);
			if ((declaringTypeHandle = td.GetDeclaringType()).IsNil) {
				string @namespace = td.Namespace.IsNil ? "" : reader.GetString(td.Namespace);
				return new FullTypeName(new TopLevelTypeName(@namespace, name, typeParameterCount));
			} else {
				return declaringTypeHandle.GetFullTypeName(reader).NestedType(name, typeParameterCount);
			}
		}

		public static FullTypeName GetFullTypeName(this ExportedType type, MetadataReader metadata)
		{
			string ns = type.Namespace.IsNil ? "" : metadata.GetString(type.Namespace);
			string name = SplitTypeParameterCountFromReflectionName(metadata.GetString(type.Name), out int typeParameterCount);
			return new TopLevelTypeName(ns, name, typeParameterCount);
		}

	}
}
