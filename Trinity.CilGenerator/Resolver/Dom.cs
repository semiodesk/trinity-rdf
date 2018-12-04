﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.Metadata
{
	public enum TargetRuntime
	{
		Unknown,
		Net_1_0,
		Net_1_1,
		Net_2_0,
		Net_4_0
	}

	public enum ResourceType
	{
		Linked,
		Embedded,
		AssemblyLinked,
	}

	public struct Resource : IEquatable<Resource>
	{
		public PEFile Module { get; }
		public ManifestResourceHandle Handle { get; }
		public bool IsNil => Handle.IsNil;

		public Resource(PEFile module, ManifestResourceHandle handle) : this()
		{
			this.Module = module ?? throw new ArgumentNullException(nameof(module));
			this.Handle = handle;
		}

		ManifestResource This() => Module.Metadata.GetManifestResource(Handle);

		public bool Equals(Resource other)
		{
			return Module == other.Module && Handle == other.Handle;
		}

		public override bool Equals(object obj)
		{
			if (obj is Resource res)
				return Equals(res);
			return false;
		}

		public override int GetHashCode()
		{
			return unchecked(982451629 * Module.GetHashCode() + 982451653 * MetadataTokens.GetToken(Handle));
		}

		public static bool operator ==(Resource lhs, Resource rhs) => lhs.Equals(rhs);
		public static bool operator !=(Resource lhs, Resource rhs) => !lhs.Equals(rhs);

		public string Name => Module.Metadata.GetString(This().Name);

		public ManifestResourceAttributes Attributes => This().Attributes;
		public bool HasFlag(ManifestResourceAttributes flag) => (Attributes & flag) == flag;
		public ResourceType ResourceType => GetResourceType();

		ResourceType GetResourceType()
		{
			if (This().Implementation.IsNil)
				return ResourceType.Embedded;
			if (This().Implementation.Kind == HandleKind.AssemblyReference)
				return ResourceType.AssemblyLinked;
			return ResourceType.Linked;
		}


	}
    public struct Unit { }

    public sealed class FullTypeNameSignatureDecoder : ISignatureTypeProvider<FullTypeName, Unit>
	{
		readonly MetadataReader metadata;

		public FullTypeNameSignatureDecoder(MetadataReader metadata)
		{
			this.metadata = metadata;
		}

		public FullTypeName GetArrayType(FullTypeName elementType, ArrayShape shape)
		{
			return elementType;
		}

		public FullTypeName GetByReferenceType(FullTypeName elementType)
		{
			return elementType;
		}

		public FullTypeName GetFunctionPointerType(MethodSignature<FullTypeName> signature)
		{
			return default(FullTypeName);
		}

		public FullTypeName GetGenericInstantiation(FullTypeName genericType, ImmutableArray<FullTypeName> typeArguments)
		{
			return genericType;
		}

		public FullTypeName GetGenericMethodParameter(Unit genericContext, int index)
		{
			return default(FullTypeName);
        }

		public FullTypeName GetGenericTypeParameter(Unit genericContext, int index)
		{
			return default(FullTypeName);
        }

		public FullTypeName GetModifiedType(FullTypeName modifier, FullTypeName unmodifiedType, bool isRequired)
		{
			return unmodifiedType;
		}

		public FullTypeName GetPinnedType(FullTypeName elementType)
		{
			return elementType;
		}

		public FullTypeName GetPointerType(FullTypeName elementType)
		{
			return elementType;
		}

        public FullTypeName GetPrimitiveType(PrimitiveTypeCode typeCode)
        {
            return default(FullTypeName);
        }

        public FullTypeName GetSZArrayType(FullTypeName elementType)
		{
			return elementType;
		}

		public FullTypeName GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
		{
			return handle.GetFullTypeName(reader);
		}

		public FullTypeName GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
		{
			return handle.GetFullTypeName(reader);
		}

		public FullTypeName GetTypeFromSpecification(MetadataReader reader, Unit genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
		{
			return reader.GetTypeSpecification(handle).DecodeSignature(new FullTypeNameSignatureDecoder(metadata), default(Unit));
		}
	}

	public class GenericContext
	{
		readonly PEFile module;
		readonly TypeDefinitionHandle declaringType;
		readonly MethodDefinitionHandle method;

		public static readonly GenericContext Empty = new GenericContext();

		private GenericContext() { }

		public GenericContext(MethodDefinitionHandle method, PEFile module)
		{
			this.module = module;
			this.method = method;
			this.declaringType = module.Metadata.GetMethodDefinition(method).GetDeclaringType();
		}

		public GenericContext(TypeDefinitionHandle declaringType, PEFile module)
		{
			this.module = module;
			this.declaringType = declaringType;
		}

		public string GetGenericTypeParameterName(int index)
		{
			GenericParameterHandle genericParameter = GetGenericTypeParameterHandleOrNull(index);
			if (genericParameter.IsNil)
				return index.ToString();
			return module.Metadata.GetString(module.Metadata.GetGenericParameter(genericParameter).Name);
		}

		public string GetGenericMethodTypeParameterName(int index)
		{
			GenericParameterHandle genericParameter = GetGenericMethodTypeParameterHandleOrNull(index);
			if (genericParameter.IsNil)
				return index.ToString();
			return module.Metadata.GetString(module.Metadata.GetGenericParameter(genericParameter).Name);
		}

		public GenericParameterHandle GetGenericTypeParameterHandleOrNull(int index)
		{
			GenericParameterHandleCollection genericParameters;
			if (declaringType.IsNil || index < 0 || index >= (genericParameters = module.Metadata.GetTypeDefinition(declaringType).GetGenericParameters()).Count)
				return MetadataTokens.GenericParameterHandle(0);
			return genericParameters[index];
		}

		public GenericParameterHandle GetGenericMethodTypeParameterHandleOrNull(int index)
		{
			GenericParameterHandleCollection genericParameters;
			if (method.IsNil || index < 0 || index >= (genericParameters = module.Metadata.GetMethodDefinition(method).GetGenericParameters()).Count)
				return MetadataTokens.GenericParameterHandle(0);
			return genericParameters[index];
		}
	}
}
