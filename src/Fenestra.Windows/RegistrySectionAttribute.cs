namespace Fenestra.Windows;

/// <summary>
/// Marks a class or struct so that its instances are stored as registry subkeys
/// instead of single values when used as properties in a section object.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class RegistrySectionAttribute : Attribute
{
}
