using System;
using System.Xml;

namespace Kaxaml.CodeCompletion;

/// <summary>
/// An <see cref="XmlQualifiedName"/> with the namespace prefix.
/// </summary>
/// <remarks>
/// The namespace prefix active for a namespace is 
/// needed when an element is inserted via autocompletion. This
/// class just adds this extra information alongside the 
/// <see cref="XmlQualifiedName"/>.
/// </remarks>
public class QualifiedName : IEquatable<QualifiedName>, IEquatable<XmlQualifiedName>
{
    #region Fields

    private readonly XmlQualifiedName _xmlQualifiedName;

    #endregion Fields

    #region Constructors

    public QualifiedName(string name, string namespaceUri, string prefix)
    {
        _xmlQualifiedName = new XmlQualifiedName(name, namespaceUri);
        Prefix = prefix;
    }

    public QualifiedName(string name, string namespaceUri)
        : this(name, namespaceUri, string.Empty)
    {
    }

    #endregion Constructors

    #region Properties

    /// <summary>
    /// Gets or sets the namespace of the qualified name.
    /// </summary>
    public string Namespace => _xmlQualifiedName.Namespace;

    /// <summary>
    /// Gets or sets the name of the element.
    /// </summary>
    public string Name => _xmlQualifiedName.Name;

    /// <summary>
    /// Gets or sets the namespace prefix used.
    /// </summary>
    public string Prefix { get; }

    #endregion Properties

    #region Equality Members

    public bool Equals(QualifiedName? other) =>
        other != null && _xmlQualifiedName.Equals(other._xmlQualifiedName);

    public bool Equals(XmlQualifiedName? other) =>
        other != null && _xmlQualifiedName.Equals(other);

    public override bool Equals(object? obj) =>
        obj is QualifiedName qn
            ? Equals(qn)
            : obj is XmlQualifiedName xqn && Equals(xqn);

    public override int GetHashCode() => _xmlQualifiedName.GetHashCode();

    public static bool operator ==(QualifiedName? lhs, QualifiedName? rhs) => Equals(lhs, rhs);

    public static bool operator !=(QualifiedName? lhs, QualifiedName? rhs) => !Equals(lhs, rhs);

    #endregion Equality Members
}