using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace OpenTok.Net.iOS.PackageTests;

/// <summary>
/// Reads the public API out of the packed binding assembly using metadata only.
/// </summary>
/// <remarks>
/// The assembly targets *-ios and references Microsoft.iOS, so it cannot be loaded into this test
/// process; the metadata reader lets these checks run on a plain desktop runner with no simulator.
/// </remarks>
public sealed class AssemblyApi : IDisposable
{
    private readonly PEReader _peReader;
    private readonly MetadataReader _metadata;

    public AssemblyApi(Stream assembly)
    {
        _peReader = new PEReader(assembly);
        _metadata = _peReader.GetMetadataReader();
    }

    /// <summary>Namespace-qualified names of every public top-level type.</summary>
    public IReadOnlyList<string> PublicTypes => _metadata.TypeDefinitions
        .Select(_metadata.GetTypeDefinition)
        .Where(type => (type.Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.Public)
        .Select(FullNameOf)
        .ToList();

    /// <summary>
    /// The members of an enum, in declaration order, as name/value pairs.
    /// </summary>
    /// <remarks>
    /// Values as well as names, because both have been wrong here in different ways: the members of
    /// OTVideoViewScaleBehavior once carried the right values under unusable names ("t" and "ll",
    /// an Objective Sharpie prefix-stripping artifact), while OTPublisherVideoEventReason was
    /// missing members outright for values the SDK does send.
    /// </remarks>
    public IReadOnlyList<(string Name, long Value)> EnumMembersOf(string typeFullName)
    {
        var type = FindType(typeFullName);
        var members = new List<(string, long)>();

        foreach (var handle in type.GetFields())
        {
            var field = _metadata.GetFieldDefinition(handle);
            var name = _metadata.GetString(field.Name);

            // The backing field every enum carries; not a member.
            if (name == "value__")
            {
                continue;
            }

            var constant = field.GetDefaultValue();
            if (constant.IsNil)
            {
                continue;
            }

            var value = _metadata.GetConstant(constant);
            var reader = _metadata.GetBlobReader(value.Value);

            // The underlying type is the header's: NS_ENUM(int32_t, ...) binds as int, and
            // NS_ENUM(NSInteger, ...) as [Native] long. Both are read here so a check does not have
            // to know which, and so a change of underlying type does not silently skip a member.
            members.Add((name, value.TypeCode switch
            {
                ConstantTypeCode.Int32 => reader.ReadInt32(),
                ConstantTypeCode.Int64 => reader.ReadInt64(),
                _ => throw new InvalidOperationException(
                    $"'{typeFullName}.{name}' has unexpected constant type {value.TypeCode}."),
            }));
        }

        return members;
    }

    /// <summary>Whether a type declares a public parameterless constructor.</summary>
    /// <remarks>
    /// A signature blob starting with 0x20 (HASTHIS) followed by a zero parameter count is a
    /// parameterless instance constructor. Read that way rather than through a signature decoder
    /// because that is the whole question being asked.
    /// </remarks>
    public bool HasPublicDefaultConstructor(string typeFullName)
    {
        var type = FindType(typeFullName);

        foreach (var handle in type.GetMethods())
        {
            var method = _metadata.GetMethodDefinition(handle);

            if (_metadata.GetString(method.Name) != ".ctor" ||
                (method.Attributes & MethodAttributes.MemberAccessMask) != MethodAttributes.Public)
            {
                continue;
            }

            var signature = _metadata.GetBlobReader(method.Signature);
            signature.ReadSignatureHeader();

            if (signature.ReadCompressedInteger() == 0)
            {
                return true;
            }
        }

        return false;
    }

    private TypeDefinition FindType(string typeFullName)
    {
        foreach (var handle in _metadata.TypeDefinitions)
        {
            var type = _metadata.GetTypeDefinition(handle);
            if (FullNameOf(type) == typeFullName)
            {
                return type;
            }
        }

        throw new InvalidOperationException($"Type '{typeFullName}' is not defined in this assembly.");
    }

    private string FullNameOf(TypeDefinition type)
    {
        var name = _metadata.GetString(type.Name);
        var ns = _metadata.GetString(type.Namespace);
        return string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
    }

    public void Dispose() => _peReader.Dispose();
}
