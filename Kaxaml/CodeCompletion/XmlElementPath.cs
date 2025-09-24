namespace Kaxaml.CodeCompletion;

/// <summary>
/// Represents the path to a xml element starting from the root of the
/// document.
/// </summary>
public class XmlElementPath
{
    #region Properties

    /// <summary>
    /// Gets the elements specifying the path.
    /// </summary>
    /// <remarks>The order of the elements determines the path.</remarks>
    public QualifiedNameCollection Elements { get; } = new();

    #endregion Properties

    #region Public Methods

    /// <summary>
    /// Compacts the path so it only contains the elements that are from 
    /// the namespace of the last element in the path. 
    /// </summary>
    /// <remarks>This method is used when we need to know the path for a
    /// particular namespace and do not care about the complete path.
    /// </remarks>
    public void Compact()
    {
        if (Elements.Count > 0)
        {
            var lastName = Elements[Elements.Count - 1];
            var index = FindNonMatchingParentElement(lastName.Namespace);
            if (index != -1) RemoveParentElements(index);
        }
    }

    #endregion Public Methods

    #region Overridden Methods

    /// <summary>
    /// An xml element path is considered to be equal if 
    /// each path item has the same name and namespace.
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is not XmlElementPath rhs) return false;
        if (this == rhs) return true;

        if (Elements.Count == rhs.Elements.Count)
        {
            for (var i = 0; i < Elements.Count; ++i)
                if (!Elements[i].Equals(rhs.Elements[i]))
                    return false;

            return true;
        }

        return false;
    }

    public override int GetHashCode() => Elements.GetHashCode();

    #endregion Overridden Methods

    #region Private Methods

    /// <summary>
    /// Finds the first parent that does belong in the specified
    /// namespace.
    /// </summary>
    private int FindNonMatchingParentElement(string namespaceUri)
    {
        var index = -1;

        if (Elements.Count > 1)
            // Start the check from the the last but one item.
            for (var i = Elements.Count - 2; i >= 0; --i)
            {
                var name = Elements[i];
                if (name.Namespace != namespaceUri)
                {
                    index = i;
                    break;
                }
            }

        return index;
    }

    /// <summary>
    /// Removes elements up to and including the specified index.
    /// </summary>
    private void RemoveParentElements(int index)
    {
        while (index >= 0)
        {
            --index;
            Elements.RemoveFirst();
        }
    }

    #endregion Private Methods
}