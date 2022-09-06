using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.RegularExpressions;

namespace NupkgExplorer.Business.Nupkg.Files
{
	public partial class AssemblyFileContent : IFileContent
	{
		public AttributeInfo[] AssemblyAttributes { get; }

		private readonly PEReader _peReader;
		private readonly MetadataReader _metadataReader;

		public AssemblyFileContent(Stream stream)
		{
			using (var memory = new MemoryStream())
			{
				stream.CopyTo(memory);
				memory.Seek(0, SeekOrigin.Begin);

				_peReader = new PEReader(memory);
				_metadataReader = _peReader.GetMetadataReader();

				AssemblyAttributes = GetAssemblyAttributes();
			}
		}

		private AttributeInfo[] GetAssemblyAttributes()
		{
			return _metadataReader.CustomAttributes
				.Select(_metadataReader.GetCustomAttribute)
				.Where(x => x.Constructor.Kind == HandleKind.MemberReference && x.Parent.Kind == HandleKind.AssemblyDefinition)
				.Select(customAttribute =>
				{
					var constructorRef = _metadataReader.GetMemberReference((MemberReferenceHandle)customAttribute.Constructor);
					var attributeTypeRefHandle = (TypeReferenceHandle)constructorRef.Parent;
					var typeProvider = new AttributeTypeProvider();
					var attributeTypeName = typeProvider.GetTypeFromReference(_metadataReader, attributeTypeRefHandle, 0);

					try
					{
						var customAttributeValues = customAttribute.DecodeValue(typeProvider);

						return (attributeTypeName, customAttributeValues);
					}
					catch (UnknownTypeException)
					{
						return default((string Name, CustomAttributeValue<string> Args)?);
					}
				})
				.Where(x => x.HasValue && x.Value.Name.StartsWith("System.Reflection.Assembly"))
				.Select(x => new AttributeInfo(
					Regex.Replace(x.Value.Name, @"^System\.Reflection\.Assembly(\w+)Attribute", "$1"),
					x.Value.Args.FixedArguments.FirstOrDefault().Value.ToString()
				))
				.ToArray();
		}
	}

	public partial class AssemblyFileContent
	{
		public class AttributeInfo
		{
			public string Attribute { get; }

			public string Value { get; }

			public AttributeInfo(string attribute, string value)
			{
				Attribute = attribute;
				Value = value;
			}
		}

		private class AttributeTypeProvider : ICustomAttributeTypeProvider<string>
		{
			private static readonly Dictionary<PrimitiveTypeCode, Type> PrimitiveTypeMappings =
				new Dictionary<PrimitiveTypeCode, Type>
				{
					{ PrimitiveTypeCode.Void, typeof(void) },
					{ PrimitiveTypeCode.Object, typeof(object) },
					{ PrimitiveTypeCode.Boolean, typeof(bool) },
					{ PrimitiveTypeCode.Char, typeof(char) },
					{ PrimitiveTypeCode.String, typeof(string) },
					{ PrimitiveTypeCode.TypedReference, typeof(TypedReference) },
					{ PrimitiveTypeCode.IntPtr, typeof(IntPtr) },
					{ PrimitiveTypeCode.UIntPtr, typeof(UIntPtr) },
					{ PrimitiveTypeCode.Single, typeof(float) },
					{ PrimitiveTypeCode.Double, typeof(double) },
					{ PrimitiveTypeCode.Byte, typeof(byte) },
					{ PrimitiveTypeCode.SByte, typeof(sbyte) },
					{ PrimitiveTypeCode.Int16, typeof(short) },
					{ PrimitiveTypeCode.UInt16, typeof(ushort) },
					{ PrimitiveTypeCode.Int32, typeof(int) },
					{ PrimitiveTypeCode.UInt32, typeof(uint) },
					{ PrimitiveTypeCode.Int64, typeof(long) },
					{ PrimitiveTypeCode.UInt64, typeof(ulong) }
				};

			public string GetPrimitiveType(PrimitiveTypeCode typeCode)
			{
				if (PrimitiveTypeMappings.TryGetValue(typeCode, out var type))
				{
					return type.FullName;
				}

				throw new ArgumentOutOfRangeException(nameof(typeCode), typeCode, @"Unexpected type code.");
			}

			public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
			{
				var definition = reader.GetTypeDefinition(handle);

				var name = definition.Namespace.IsNil
					? reader.GetString(definition.Name)
					: reader.GetString(definition.Namespace) + "." + reader.GetString(definition.Name);

				if (IsNested(definition.Attributes))
				{
					var declaringTypeHandle = definition.GetDeclaringType();
					return GetTypeFromDefinition(reader, declaringTypeHandle, 0) + "+" + name;
				}

				return name;
			}


			private static bool IsNested(TypeAttributes flags)
			{
				const TypeAttributes nestedMask = TypeAttributes.NestedFamily | TypeAttributes.NestedPublic;

				return (flags & nestedMask) != 0;
			}

			public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
			{
				var reference = reader.GetTypeReference(handle);

				var name = reference.Namespace.IsNil
					? reader.GetString(reference.Name)
					: reader.GetString(reference.Namespace) + "." + reader.GetString(reference.Name);

				Handle scope = reference.ResolutionScope;
				switch (scope.Kind)
				{
					case HandleKind.TypeReference: return GetTypeFromReference(reader, (TypeReferenceHandle)scope, 0) + "+" + name;

					// If type refers other module or assembly, don't append them to result.
					// Usually we don't have those assemblies, so we'll be unable to resolve the exact type.
					default: return name;
				};
			}

			public string GetSZArrayType(string elementType)
			{
				return elementType + "[]";
			}

			public string GetSystemType()
			{
				return typeof(Type).FullName;
			}

			public bool IsSystemType(string type)
			{
				return Type.GetType(type, false) == typeof(Type);
			}

			public string GetTypeFromSerializedName(string name)
			{
				return name;
			}

			public PrimitiveTypeCode GetUnderlyingEnumType(string type)
			{
				var runtimeType = Type.GetType(type, false);

				if (runtimeType != null)
				{
					var underlyingType = runtimeType.GetEnumUnderlyingType();

					foreach (var primitiveTypeMapping in PrimitiveTypeMappings)
					{
						if (primitiveTypeMapping.Value == underlyingType)
						{
							return primitiveTypeMapping.Key;
						}
					}
				}

				throw new UnknownTypeException($"Type '{type}' is of unknown TypeCode");
			}
		}

		private class UnknownTypeException : InvalidOperationException
		{
			public UnknownTypeException(string message) : base(message)
			{
			}

			public UnknownTypeException(string message, Exception innerException) : base(message, innerException)
			{
			}

			public UnknownTypeException()
			{
			}
		}
	}
}
