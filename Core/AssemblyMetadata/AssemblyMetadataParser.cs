using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace NuGetPe.AssemblyMetadata
{
    internal class AssemblyMetadataParser : IDisposable
    {
        private readonly PEReader _peReader;
        private readonly MetadataReader _metadataReader;

        public AssemblyMetadataParser(string fileName)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            _peReader = new PEReader(File.OpenRead(fileName));
            _metadataReader = _peReader.GetMetadataReader();
        }

        public AssemblyDebugData? GetDebugData()
        {
            var entry = _peReader.ReadDebugDirectory().Where(de => de.Type == DebugDirectoryEntryType.EmbeddedPortablePdb).ToList();
            if (entry.Count == 0) // no embedded ppdb
            {
                return null;
            }

            using var reader = new AssemblyDebugParser(_peReader.ReadEmbeddedPortablePdbDebugDirectoryData(entry[0]), PdbType.Embedded);
            return reader.GetDebugData();
        }

        public IEnumerable<AssemblyName> GetReferencedAssemblyNames()
        {
            foreach (var referenceHandle in _metadataReader.AssemblyReferences)
            {
                var assemblyReference = _metadataReader.GetAssemblyReference(referenceHandle);

                var assemblyName = new AssemblyName
                {
                    Name = _metadataReader.GetString(assemblyReference.Name),
                    Version = assemblyReference.Version,
                    CultureName = _metadataReader.GetString(assemblyReference.Culture)
                };

                if (!assemblyReference.PublicKeyOrToken.IsNil)
                {
                    var publicKeyOrToken = _metadataReader.GetBlobBytes(assemblyReference.PublicKeyOrToken);

                    // PublicKeyToken is of 8 bytes length.
                    if (publicKeyOrToken.Length == 8)
                    {
                        assemblyName.SetPublicKeyToken(publicKeyOrToken);
                    }
                    else
                    {
                        assemblyName.SetPublicKey(publicKeyOrToken);
                    }
                }

                yield return assemblyName;
            }
        }

        public IEnumerable<AttributeInfo> GetAssemblyAttributes()
        {
            foreach (var attributeHandle in _metadataReader.CustomAttributes)
            {
                var customAttribute = _metadataReader.GetCustomAttribute(attributeHandle);
                if (customAttribute.Parent.Kind != HandleKind.AssemblyDefinition)
                {
                    continue;
                }

                var constructorRef = _metadataReader.GetMemberReference((MemberReferenceHandle)customAttribute.Constructor);
                var attributeTypeRefHandle = (TypeReferenceHandle)constructorRef.Parent;

                var typeProvider = new AttributeTypeProvider();

                var attributeTypeName = typeProvider.GetTypeFromReference(_metadataReader, attributeTypeRefHandle, 0);

                AttributeInfo? attrInfo = null;
                try
                {
                    var customAttributeValues = customAttribute.DecodeValue(typeProvider);

                    attrInfo = new AttributeInfo(
                        attributeTypeName,
                        customAttributeValues.FixedArguments.ToArray(),
                        customAttributeValues.NamedArguments.ToArray());
                }
                catch (UnknownTypeException)
                {
                    // It could happen that we cannot resolve some argument type (e.g. for custom attributes).
                    // In this case simply omit the attribute and try to read other ones.
                }

                if (attrInfo != null)
                {
                    yield return attrInfo;
                }
            }
        }

        public class AttributeInfo
        {
            public string FullTypeName { get; }
            public CustomAttributeTypedArgument<string>[] FixedArguments { get; }
            public CustomAttributeNamedArgument<string>[] NamedArguments { get; }

            public AttributeInfo(
                string fullTypeName,
                CustomAttributeTypedArgument<string>[] fixedArguments,
                CustomAttributeNamedArgument<string>[] namedArguments)
            {
                FullTypeName = fullTypeName;
                FixedArguments = fixedArguments;
                NamedArguments = namedArguments;
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
                    case HandleKind.TypeReference:
                        return GetTypeFromReference(reader, (TypeReferenceHandle)scope, 0) + "+" + name;

                    // If type refers other module or assembly, don't append them to result.
                    // Usually we don't have those assemblies, so we'll be unable to resolve the exact type.
                    default:
                        return name;
                }
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

                throw new UnknownTypeException();
            }
        }

        public void Dispose()
        {
            _peReader.Dispose();
        }

        private class UnknownTypeException : InvalidOperationException
        {

        }
    }
}
