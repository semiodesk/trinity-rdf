using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using SRM = System.Reflection.Metadata;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.Metadata
{
	public static class MetadataExtensions
	{
		static HashAlgorithm GetHashAlgorithm(this MetadataReader reader)
		{
			switch (reader.GetAssemblyDefinition().HashAlgorithm) {
				case AssemblyHashAlgorithm.None:
					// only for multi-module assemblies?
					return SHA1.Create();
				case AssemblyHashAlgorithm.MD5:
					return MD5.Create();
				case AssemblyHashAlgorithm.Sha1:
					return SHA1.Create();
				case AssemblyHashAlgorithm.Sha256:
					return SHA256.Create();
				case AssemblyHashAlgorithm.Sha384:
					return SHA384.Create();
				case AssemblyHashAlgorithm.Sha512:
					return SHA512.Create();
				default:
					return SHA1.Create(); // default?
			}
		}

		static string CalculatePublicKeyToken(BlobHandle blob, MetadataReader reader)
		{
			// Calculate public key token:
			// 1. hash the public key using the appropriate algorithm.
			byte[] publicKeyTokenBytes = reader.GetHashAlgorithm().ComputeHash(reader.GetBlobBytes(blob));
			// 2. take the last 8 bytes
			// 3. according to Cecil we need to reverse them, other sources did not mention this.
			return publicKeyTokenBytes.TakeLast(8).Reverse().ToHexString(8);
		}

		public static string GetFullAssemblyName(this MetadataReader reader)
		{
			if (!reader.IsAssembly)
				return string.Empty;
			var asm = reader.GetAssemblyDefinition();
			string publicKey = "null";
			if (!asm.PublicKey.IsNil) {
				// AssemblyFlags.PublicKey does not apply to assembly definitions
				publicKey = CalculatePublicKeyToken(asm.PublicKey, reader);
			}
			return $"{reader.GetString(asm.Name)}, " +
				$"Version={asm.Version}, " +
				$"Culture={(asm.Culture.IsNil ? "neutral" : reader.GetString(asm.Culture))}, " +
				$"PublicKeyToken={publicKey}";
		}

		public static string GetFullAssemblyName(this SRM.AssemblyReference reference, MetadataReader reader)
		{
			string publicKey = "null";
			if (!reference.PublicKeyOrToken.IsNil) {
				if ((reference.Flags & AssemblyFlags.PublicKey) != 0) {
					publicKey = CalculatePublicKeyToken(reference.PublicKeyOrToken, reader);
				} else {
					publicKey = reader.GetBlobBytes(reference.PublicKeyOrToken).ToHexString(8);
				}
			}
			string properties = "";
			if ((reference.Flags & AssemblyFlags.Retargetable) != 0)
				properties = ", Retargetable=true";
			return $"{reader.GetString(reference.Name)}, " +
				$"Version={reference.Version}, " +
				$"Culture={(reference.Culture.IsNil ? "neutral" : reader.GetString(reference.Culture))}, " +
				$"PublicKeyToken={publicKey}{properties}";
		}

		static string ToHexString(this IEnumerable<byte> bytes, int estimatedLength)
		{
			StringBuilder sb = new StringBuilder(estimatedLength * 2);
			foreach (var b in bytes)
				sb.AppendFormat("{0:x2}", b);
			return sb.ToString();
		}

		public static IEnumerable<TypeDefinitionHandle> GetTopLevelTypeDefinitions(this MetadataReader reader)
		{
			foreach (var handle in reader.TypeDefinitions) {
				var td = reader.GetTypeDefinition(handle);
				if (td.GetDeclaringType().IsNil)
					yield return handle;
			}
		}

	}
}
