// Copyright (c) 2018 Siegfried Pammer
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.Metadata
{
    [Serializable]
    public class PEFileNotSupportedException : Exception
    {
        public PEFileNotSupportedException() { }
        public PEFileNotSupportedException(string message) : base(message) { }
        public PEFileNotSupportedException(string message, Exception inner) : base(message, inner) { }
        //protected PEFileNotSupportedException(
        //  SerializationInfo info,
        //  StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// PEFile is the main class the decompiler uses to represent a metadata assembly/module.
    /// Every file on disk can be loaded into a standalone PEFile instance.
    /// 
    /// A PEFile can be combined with its referenced assemblies/modules to form a type system,
    /// in that case the <see cref="MetadataModule"/> class is used instead.
    /// </summary>
    /// <remarks>
    /// In addition to wrapping a <c>System.Reflection.Metadata.PEReader</c>, this class
    /// contains a few decompiled-specific caches to allow efficiently constructing a type
    /// system from multiple PEFiles. This allows the caches to be shared across multiple
    /// decompiled type systems.
    /// </remarks>
    public class PEFile : IDisposable
	{
		public string FileName { get; }
		public PEReader Reader { get; }
		public MetadataReader Metadata { get; }

		public PEFile(string fileName, PEStreamOptions options = PEStreamOptions.Default)
			: this(fileName, new PEReader(new FileStream(fileName, FileMode.Open, FileAccess.Read), options))
		{
		}

		public PEFile(string fileName, Stream stream, PEStreamOptions options = PEStreamOptions.Default)
			: this(fileName, new PEReader(stream, options))
		{
		}

		public PEFile(string fileName, PEReader reader)
		{
			this.FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
			this.Reader = reader ?? throw new ArgumentNullException(nameof(reader));
			if (!reader.HasMetadata)
				throw new PEFileNotSupportedException("PE file does not contain any managed metadata.");
			this.Metadata = reader.GetMetadataReader();
		}

		public bool IsAssembly => Metadata.IsAssembly;
		public string Name => GetName();
		public string FullName => IsAssembly ? Metadata.GetFullAssemblyName() : Name;


		string GetName()
		{
			var metadata = Metadata;
			if (metadata.IsAssembly)
				return metadata.GetString(metadata.GetAssemblyDefinition().Name);
			return metadata.GetString(metadata.GetModuleDefinition().Name);
		}

		public ImmutableArray<AssemblyReference> AssemblyReferences => Metadata.AssemblyReferences.Select(r => new AssemblyReference(this, r)).ToImmutableArray();

		public void Dispose()
		{
			Reader.Dispose();
		}

		Dictionary<TopLevelTypeName, TypeDefinitionHandle> typeLookup;

		/// <summary>
		/// Finds the top-level-type with the specified name.
		///// </summary>
		//public TypeDefinitionHandle GetTypeDefinition(TopLevelTypeName typeName)
		//{
		//	var lookup = LazyInit.VolatileRead(ref typeLookup);
		//	if (lookup == null) {
		//		lookup = new Dictionary<TopLevelTypeName, TypeDefinitionHandle>();
		//		foreach (var handle in Metadata.TypeDefinitions) {
		//			var td = Metadata.GetTypeDefinition(handle);
		//			if (!td.GetDeclaringType().IsNil) {
		//				continue; // nested type
		//			}
		//			var nsHandle = td.Namespace;
		//			string ns = nsHandle.IsNil ? string.Empty : Metadata.GetString(nsHandle);
		//			string name = ReflectionHelper.SplitTypeParameterCountFromReflectionName(Metadata.GetString(td.Name), out int typeParameterCount);
		//			lookup[new TopLevelTypeName(ns, name, typeParameterCount)] = handle;
		//		}
		//		lookup = LazyInit.GetOrSet(ref typeLookup, lookup);
		//	}
		//	if (lookup.TryGetValue(typeName, out var resultHandle))
		//		return resultHandle;
		//	else
		//		return default;
		//}

		Dictionary<FullTypeName, ExportedTypeHandle> typeForwarderLookup;

        /*
		/// <summary>
		/// Finds the type forwarder with the specified name.
		/// </summary>
		public ExportedTypeHandle GetTypeForwarder(FullTypeName typeName)
		{
			var lookup = LazyInit.VolatileRead(ref typeForwarderLookup);
			if (lookup == null) {
				lookup = new Dictionary<FullTypeName, ExportedTypeHandle>();
				foreach (var handle in Metadata.ExportedTypes) {
					var td = Metadata.GetExportedType(handle);
					lookup[td.GetFullTypeName(Metadata)] = handle;
				}
				lookup = LazyInit.GetOrSet(ref typeForwarderLookup, lookup);
			}
			if (lookup.TryGetValue(typeName, out var resultHandle))
				return resultHandle;
			else
				return default;
		}

		MethodSemanticsLookup methodSemanticsLookup;

		internal MethodSemanticsLookup MethodSemanticsLookup {
			get {
				var r = LazyInit.VolatileRead(ref methodSemanticsLookup);
				if (r != null)
					return r;
				else
					return LazyInit.GetOrSet(ref methodSemanticsLookup, new MethodSemanticsLookup(Metadata));
			}
		}

		public TypeSystem.IModuleReference WithOptions(TypeSystemOptions options)
		{
			return new PEFileWithOptions(this, options);
		}

		IModule TypeSystem.IModuleReference.Resolve(ITypeResolveContext context)
		{
			return new MetadataModule(context.Compilation, this, TypeSystemOptions.Default);
		}

		private class PEFileWithOptions : TypeSystem.IModuleReference
		{
			readonly PEFile peFile;
			readonly TypeSystemOptions options;

			public PEFileWithOptions(PEFile peFile, TypeSystemOptions options)
			{
				this.peFile = peFile;
				this.options = options;
			}

			IModule TypeSystem.IModuleReference.Resolve(ITypeResolveContext context)
			{
				return new MetadataModule(context.Compilation, peFile, options);
			}
		}
        */
	}
}
