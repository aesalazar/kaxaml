using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;

using ICSharpCode.TextEditor.Gui.CompletionWindow;

namespace Kaxaml.CodeCompletion
{
    /// <summary>
    /// Holds the completion (intellisense) data for an xml schema.
    /// </summary>
    /// <remarks>
    /// The XmlSchema class throws an exception if we attempt to load 
    /// the xhtml1-strict.xsd schema.  It does not like the fact that
    /// this schema redefines the xml namespace, even though this is
    /// allowed by the w3.org specification.
    /// </remarks>
    public class XmlSchemaCompletionData
    {

        /// <summary>
        /// Stores attributes that have been prohibited whilst the code
        /// generates the attribute completion data.
        /// </summary>
        private readonly XmlSchemaObjectCollection _prohibitedAttributes = new();

        public XmlSchemaCompletionData()
        {
        }

        /// <summary>
        /// Creates completion data from the schema passed in 
        /// via the reader object.
        /// </summary>
        public XmlSchemaCompletionData(TextReader reader)
        {
            ReadSchema(string.Empty, reader);
        }

        /// <summary>
        /// Creates completion data from the schema passed in 
        /// via the reader object.
        /// </summary>
        public XmlSchemaCompletionData(XmlTextReader reader)
        {
            reader.XmlResolver = null;
            ReadSchema(reader);
        }

        /// <summary>
        /// Creates the completion data from the specified schema file.
        /// </summary>
        public XmlSchemaCompletionData(string fileName)
            : this(string.Empty, fileName)
        {
        }

        /// <summary>
        /// Creates the completion data from the specified schema file and uses
        /// the specified baseUri to resolve any referenced schemas.
        /// </summary>
        public XmlSchemaCompletionData(string baseUri, string fileName)
        {
            using (var reader = new StreamReader(fileName, true))
            {
                ReadSchema(baseUri, reader);
                FileName = fileName;
            }
        }

        /// <summary>
        /// Gets the schema.
        /// </summary>
        public XmlSchema? Schema { get; private set; }

        /// <summary>
        /// Read only schemas are those that are installed with 
        /// SharpDevelop.
        /// </summary>
        public bool ReadOnly { get; set; } = false;

        /// <summary>
        /// Gets or sets the schema's file name.
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Gets the namespace URI for the schema.
        /// </summary>
        public string NamespaceUri { get; private set; } = string.Empty;

        /// <summary>
        /// Converts the filename into a valid Uri.
        /// </summary>
        public static string GetUri(string fileName)
        {
            var uri = string.Empty;

            if (fileName is { Length: > 0 })
            {
                uri = string.Concat("file:///", fileName.Replace('\\', '/'));
            }

            return uri;
        }

        /// <summary>
        /// Gets the possible root elements for an xml document using this schema.
        /// </summary>
        public ICompletionData[] GetElementCompletionData()
        {
            return GetElementCompletionData(string.Empty);
        }

        /// <summary>
        /// Gets the possible root elements for an xml document using this schema.
        /// </summary>
        public ICompletionData[] GetElementCompletionData(string namespacePrefix)
        {
            var data = new XmlCompletionDataCollection();

            if (Schema is not null)
            {
                foreach (XmlSchemaElement element in Schema.Elements.Values)
                {
                    if (element.Name != null)
                    {
                        AddElement(data, element.Name, namespacePrefix, element.Annotation);
                    }
                    // Do not add reference element.
                }
            }

            return data.ToArray();
        }

        /// <summary>
        /// Gets the attribute completion data for the xml element that exists
        /// at the end of the specified path.
        /// </summary>
        public ICompletionData[] GetAttributeCompletionData(XmlElementPath path)
        {
            var data = new XmlCompletionDataCollection();

            // Locate matching element.
            var element = FindElement(path);

            // Get completion data.
            if (element != null)
            {
                _prohibitedAttributes.Clear();
                data = GetAttributeCompletionData(element);
            }

            return data.ToArray();
        }

        /// <summary>
        /// Gets the child element completion data for the xml element that exists
        /// at the end of the specified path.
        /// </summary>
        public ICompletionData[] GetChildElementCompletionData(XmlElementPath path)
        {
            var data = new XmlCompletionDataCollection();

            // Locate matching element.
            var element = FindElement(path);

            // Get completion data.
            if (element != null)
            {
                data = GetChildElementCompletionData(element, path.Elements.LastPrefix);
            }

            return data.ToArray();
        }

        /// <summary>
        /// Gets the autocomplete data for the specified attribute value.
        /// </summary>
        public ICompletionData[] GetAttributeValueCompletionData(XmlElementPath path, string name)
        {
            var data = new XmlCompletionDataCollection();

            // Locate matching element.
            var element = FindElement(path);

            // Get completion data.
            if (element != null)
            {
                data = GetAttributeValueCompletionData(element, name);
            }

            return data.ToArray();
        }

        /// <summary>
        /// Finds the element that exists at the specified path.
        /// </summary>
        /// <remarks>This method is not used when generating completion data,
        /// but is a useful method when locating an element so we can jump
        /// to its schema definition.</remarks>
        /// <returns><see langword="null"/> if no element can be found.</returns>
        public XmlSchemaElement? FindElement(XmlElementPath path)
        {
            XmlSchemaElement? element = null;
            for (var i = 0; i < path.Elements.Count; ++i)
            {
                var name = path.Elements[i];
                if (i == 0)
                {
                    // Look for root element.
                    element = FindElement(name);
                    if (element == null)
                    {
                        break;
                    }
                }
                else
                {
                    element = FindChildElement(element!, name);
                    if (element == null)
                    {
                        break;
                    }
                }
            }
            return element;
        }

        /// <summary>
        /// Finds an element in the schema.
        /// </summary>
        /// <remarks>
        /// Only looks at the elements that are defined in the 
        /// root of the schema so it will not find any elements
        /// that are defined inside any complex types.
        /// </remarks>
        public XmlSchemaElement? FindElement(QualifiedName name)
        {
            if (Schema is null) throw new Exception("Schema expected");
            foreach (XmlSchemaElement element in Schema.Elements.Values)
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                // TODO: Should look into an implicit cast
                if (name.Equals(element.QualifiedName))
                {
                    return element;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds the complex type with the specified name.
        /// </summary>
        public XmlSchemaComplexType? FindComplexType(QualifiedName name)
        {
            if (Schema is null) throw new Exception("Schema expected");
            var qualifiedName = new XmlQualifiedName(name.Name, name.Namespace);
            return FindNamedType(Schema, qualifiedName);
        }

        /// <summary>
        /// Finds the specified attribute name given the element.
        /// </summary>
        /// <remarks>This method is not used when generating completion data,
        /// but is a useful method when locating an attribute so we can jump
        /// to its schema definition.</remarks>
        /// <returns><see langword="null"/> if no attribute can be found.</returns>
        public XmlSchemaAttribute? FindAttribute(XmlSchemaElement element, string name)
        {
            XmlSchemaAttribute? attribute = null;
            var complexType = GetElementAsComplexType(element);
            if (complexType != null)
            {
                attribute = FindAttribute(complexType, name);
            }
            return attribute;
        }

        /// <summary>
        /// Finds the attribute group with the specified name.
        /// </summary>
        public XmlSchemaAttributeGroup? FindAttributeGroup(string name)
        {
            if (Schema is null) throw new Exception("Schema expected");
            return FindAttributeGroup(Schema, name);
        }

        /// <summary>
        /// Finds the simple type with the specified name.
        /// </summary>
        public XmlSchemaSimpleType? FindSimpleType(string name)
        {
            var qualifiedName = new XmlQualifiedName(name, NamespaceUri);
            return FindSimpleType(qualifiedName);
        }

        /// <summary>
        /// Finds the specified attribute in the schema. This method only checks
        /// the attributes defined in the root of the schema.
        /// </summary>
        public XmlSchemaAttribute? FindAttribute(string name)
        {
            if (Schema is null) throw new Exception("Schema expected");
            foreach (XmlSchemaAttribute attribute in Schema.Attributes.Values)
            {
                if (attribute.Name == name)
                {
                    return attribute;
                }
            }
            return null;
        }

        /// <summary>
        /// Finds the schema group with the specified name.
        /// </summary>
        public XmlSchemaGroup? FindGroup(string? name)
        {
            if (Schema is null) throw new Exception("Schema expected");
            if (name != null)
            {
                foreach (XmlSchemaObject schemaObject in Schema.Groups.Values)
                {
                    if (schemaObject is XmlSchemaGroup group)
                    {
                        if (group.Name == name)
                        {
                            return group;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Takes the name and creates a qualified name using the namespace of this
        /// schema.
        /// </summary>
        /// <remarks>If the name is of the form myprefix:mytype then the correct 
        /// namespace is determined from the prefix. If the name is not of this
        /// form then no prefix is added.</remarks>
        public QualifiedName CreateQualifiedName(string name)
        {
            var index = name.IndexOf(":", StringComparison.Ordinal);
            if (index >= 0)
            {
                if (Schema is null) throw new Exception("Schema expected");
                var prefix = name.Substring(0, index);
                name = name.Substring(index + 1);
                foreach (var xmlQualifiedName in Schema.Namespaces.ToArray())
                {
                    if (xmlQualifiedName.Name == prefix)
                    {
                        return new QualifiedName(name, xmlQualifiedName.Namespace, prefix);
                    }
                }
            }

            // Default behaviour just return the name with the namespace uri.
            return new QualifiedName(name, NamespaceUri);
        }

        /// <summary>
        /// Converts the element to a complex type if possible.
        /// </summary>
        public XmlSchemaComplexType? GetElementAsComplexType(XmlSchemaElement element)
        {
            if (Schema is null) throw new Exception("Schema expected");
            return (XmlSchemaComplexType?)element.SchemaType
                              ?? FindNamedType(Schema, element.SchemaTypeName);
        }

        /// <summary>
        /// Handler for schema validation errors.
        /// </summary>
        private void SchemaValidation(object? _, ValidationEventArgs __)
        {
            // Do nothing.
        }

        /// <summary>
        /// Loads the schema.
        /// </summary>
        private void ReadSchema(XmlReader reader)
        {
            try
            {
                Schema = XmlSchema.Read(reader, SchemaValidation);
                if (Schema is null) throw new Exception("Schema could not be read");

                var schemaSet = new XmlSchemaSet();
                schemaSet.ValidationEventHandler += SchemaValidation;
                schemaSet.Add(Schema);
                schemaSet.Compile();

                NamespaceUri = Schema.TargetNamespace ?? string.Empty;
            }
            finally
            {
                reader.Close();
            }
        }


        private void ReadSchema(string baseUri, TextReader reader)
        {
            using (var xmlReader = new XmlTextReader(baseUri, reader))
            {
                // Setting the resolver to null allows us to
                // load the xhtml1-strict.xsd without any exceptions if
                // the referenced dtds exist in the same folder as the .xsd
                // file.  If this is not set to null the dtd files are looked
                // for in the assembly's folder.
                xmlReader.XmlResolver = null;
                ReadSchema(xmlReader);
            }
        }

        /// <summary>
        /// Finds an element in the schema.
        /// </summary>
        /// <remarks>
        /// Only looks at the elements that are defined in the 
        /// root of the schema so it will not find any elements
        /// that are defined inside any complex types.
        /// </remarks>
        private XmlSchemaElement? FindElement(XmlQualifiedName name)
        {
            if (Schema is null) throw new Exception("Schema expected");
            XmlSchemaElement? matchedElement = null;
            foreach (XmlSchemaElement element in Schema.Elements.Values)
            {
                if (name.Equals(element.QualifiedName))
                {
                    matchedElement = element;
                    break;
                }
            }

            return matchedElement;
        }

        private XmlCompletionDataCollection GetChildElementCompletionData(XmlSchemaElement element, string prefix)
        {
            var data = new XmlCompletionDataCollection();

            var complexType = GetElementAsComplexType(element);

            if (complexType != null)
            {
                data = GetChildElementCompletionData(complexType, prefix);
            }

            return data;
        }

        private XmlCompletionDataCollection GetChildElementCompletionData(XmlSchemaComplexType complexType, string prefix)
        {
            var data = new XmlCompletionDataCollection();

            if (complexType.Particle is XmlSchemaSequence sequence)
            {
                data = GetChildElementCompletionData(sequence.Items, prefix);
            }
            else if (complexType.Particle is XmlSchemaChoice choice)
            {
                data = GetChildElementCompletionData(choice.Items, prefix);
            }
            else if (complexType.ContentModel is XmlSchemaComplexContent complexContent)
            {
                data = GetChildElementCompletionData(complexContent, prefix);
            }
            else if (complexType.Particle is XmlSchemaGroupRef groupRef)
            {
                data = GetChildElementCompletionData(groupRef, prefix);
            }
            else if (complexType.Particle is XmlSchemaAll all)
            {
                data = GetChildElementCompletionData(all.Items, prefix);
            }

            return data;
        }

        private XmlCompletionDataCollection GetChildElementCompletionData(XmlSchemaObjectCollection items, string prefix)
        {
            var data = new XmlCompletionDataCollection();

            foreach (var schemaObject in items)
            {
                if (schemaObject is XmlSchemaElement childElement)
                {
                    var name = childElement.Name;
                    if (name == null)
                    {
                        name = childElement.RefName.Name;
                        var element = FindElement(childElement.RefName);
                        if (element != null)
                        {
                            if (element.IsAbstract)
                            {
                                AddSubstitionGroupElements(data, element.QualifiedName, prefix);
                            }
                            else
                            {
                                AddElement(data, name, prefix, element.Annotation);
                            }
                        }
                        else
                        {
                            AddElement(data, name, prefix, childElement.Annotation);
                        }
                    }
                    else
                    {
                        AddElement(data, name, prefix, childElement.Annotation);
                    }
                }
                else if (schemaObject is XmlSchemaSequence childSequence)
                {
                    AddElements(data, GetChildElementCompletionData(childSequence.Items, prefix));
                }
                else if (schemaObject is XmlSchemaChoice childChoice)
                {
                    AddElements(data, GetChildElementCompletionData(childChoice.Items, prefix));
                }
                else if (schemaObject is XmlSchemaGroupRef groupRef)
                {
                    AddElements(data, GetChildElementCompletionData(groupRef, prefix));
                }
            }

            return data;
        }

        private XmlCompletionDataCollection GetChildElementCompletionData(XmlSchemaComplexContent complexContent, string prefix)
        {
            var data = new XmlCompletionDataCollection();

            if (complexContent.Content is XmlSchemaComplexContentExtension extension)
            {
                data = GetChildElementCompletionData(extension, prefix);
            }
            else
            {
                if (complexContent.Content is XmlSchemaComplexContentRestriction restriction)
                {
                    data = GetChildElementCompletionData(restriction, prefix);
                }
            }

            return data;
        }

        private XmlCompletionDataCollection GetChildElementCompletionData(XmlSchemaComplexContentExtension extension, string prefix)
        {
            if (Schema is null) throw new Exception("Schema expected");
            var data = new XmlCompletionDataCollection();

            var complexType = FindNamedType(Schema, extension.BaseTypeName);
            if (complexType != null)
            {
                data = GetChildElementCompletionData(complexType, prefix);
            }

            // Add any elements.
            if (extension.Particle != null)
            {
                if (extension.Particle is XmlSchemaSequence sequence)
                {
                    data.AddRange(GetChildElementCompletionData(sequence.Items, prefix));
                }
                else if (extension.Particle is XmlSchemaChoice choice)
                {
                    data.AddRange(GetChildElementCompletionData(choice.Items, prefix));
                }
                else if (extension.Particle is XmlSchemaGroupRef groupRef)
                {
                    data.AddRange(GetChildElementCompletionData(groupRef, prefix));
                }
            }

            return data;
        }

        private XmlCompletionDataCollection GetChildElementCompletionData(XmlSchemaGroupRef groupRef, string prefix)
        {
            var data = new XmlCompletionDataCollection();

            var group = FindGroup(groupRef.RefName.Name);
            if (group != null)
            {
                if (group.Particle is XmlSchemaSequence sequence)
                {
                    data = GetChildElementCompletionData(sequence.Items, prefix);
                }
                else if (group.Particle is XmlSchemaChoice choice)
                {
                    data = GetChildElementCompletionData(choice.Items, prefix);
                }
            }

            return data;
        }

        private XmlCompletionDataCollection GetChildElementCompletionData(XmlSchemaComplexContentRestriction restriction, string prefix)
        {
            var data = new XmlCompletionDataCollection();

            // Add any elements.
            if (restriction.Particle != null)
            {
                if (restriction.Particle is XmlSchemaSequence sequence)
                {
                    data = GetChildElementCompletionData(sequence.Items, prefix);
                }
                else if (restriction.Particle is XmlSchemaChoice choice)
                {
                    data = GetChildElementCompletionData(choice.Items, prefix);
                }
                else if (restriction.Particle is XmlSchemaGroupRef groupRef)
                {
                    data = GetChildElementCompletionData(groupRef, prefix);
                }
            }

            return data;
        }

        /// <summary>
        /// Adds an element completion data to the collection if it does not 
        /// already exist.
        /// </summary>
        private void AddElement(XmlCompletionDataCollection data, string name, string prefix, string documentation)
        {
            if (!data.Contains(name))
            {
                if (prefix.Length > 0)
                {
                    name = string.Concat(prefix, ":", name);
                }
                var completionData = new XmlCompletionData(name, documentation);
                data.Add(completionData);
            }
        }

        /// <summary>
        /// Adds an element completion data to the collection if it does not 
        /// already exist.
        /// </summary>
        private void AddElement(XmlCompletionDataCollection data, string name, string prefix, XmlSchemaAnnotation? annotation)
        {
            // Get any annotation documentation.
            var documentation = GetDocumentation(annotation);
            AddElement(data, name, prefix, documentation);
        }

        /// <summary>
        /// Adds elements to the collection if it does not already exist.
        /// </summary>
        private void AddElements(XmlCompletionDataCollection lhs, XmlCompletionDataCollection rhs)
        {
            foreach (var data in rhs)
            {
                if (!lhs.Contains(data))
                {
                    lhs.Add(data);
                }
            }
        }

        /// <summary>
        /// Gets the documentation from the annotation element.
        /// </summary>
        /// <remarks>
        /// All documentation elements are added.  All text nodes inside
        /// the documentation element are added.
        /// </remarks>
        private string GetDocumentation(XmlSchemaAnnotation? annotation)
        {
            var documentation = string.Empty;

            if (annotation != null)
            {
                var documentationBuilder = new StringBuilder();
                foreach (var schemaObject in annotation.Items)
                {
                    if (schemaObject is XmlSchemaDocumentation { Markup: not null } schemaDocumentation)
                    {
                        foreach (var node in schemaDocumentation.Markup)
                        {
                            if (node is XmlText { Data.Length: > 0 } textNode)
                            {
                                documentationBuilder.Append(textNode.Data);
                            }
                        }
                    }
                }

                documentation = documentationBuilder.ToString();
            }

            return documentation;
        }

        private XmlCompletionDataCollection GetAttributeCompletionData(XmlSchemaElement element)
        {
            var data = new XmlCompletionDataCollection();

            var complexType = GetElementAsComplexType(element);

            if (complexType != null)
            {
                data.AddRange(GetAttributeCompletionData(complexType));
            }

            return data;
        }

        private XmlCompletionDataCollection GetAttributeCompletionData(XmlSchemaComplexContentRestriction restriction)
        {
            if (Schema is null) throw new Exception("Schema expected");
            var data = new XmlCompletionDataCollection();
            data.AddRange(GetAttributeCompletionData(restriction.Attributes));

            var baseComplexType = FindNamedType(Schema, restriction.BaseTypeName);
            if (baseComplexType != null)
            {
                data.AddRange(GetAttributeCompletionData(baseComplexType));
            }

            return data;
        }

        private XmlCompletionDataCollection GetAttributeCompletionData(XmlSchemaComplexType complexType)
        {
            var data = new XmlCompletionDataCollection();

            data = GetAttributeCompletionData(complexType.Attributes);

            // Add any complex content attributes.
            if (complexType.ContentModel is XmlSchemaComplexContent complexContent)
            {
                if (complexContent.Content is XmlSchemaComplexContentExtension extension)
                {
                    data.AddRange(GetAttributeCompletionData(extension));
                }
                else if (complexContent.Content is XmlSchemaComplexContentRestriction restriction)
                {
                    data.AddRange(GetAttributeCompletionData(restriction));
                }
            }
            else
            {
                if (complexType.ContentModel is XmlSchemaSimpleContent simpleContent)
                {
                    data.AddRange(GetAttributeCompletionData(simpleContent));
                }
            }

            return data;
        }

        private XmlCompletionDataCollection GetAttributeCompletionData(XmlSchemaComplexContentExtension extension)
        {
            if (Schema is null) throw new Exception("Schema expected");
            var data = new XmlCompletionDataCollection();

            data.AddRange(GetAttributeCompletionData(extension.Attributes));
            var baseComplexType = FindNamedType(Schema, extension.BaseTypeName);
            if (baseComplexType != null)
            {
                data.AddRange(GetAttributeCompletionData(baseComplexType));
            }

            return data;
        }

        private XmlCompletionDataCollection GetAttributeCompletionData(XmlSchemaSimpleContent simpleContent)
        {
            var data = new XmlCompletionDataCollection();

            if (simpleContent.Content is XmlSchemaSimpleContentExtension extension)
            {
                data.AddRange(GetAttributeCompletionData(extension));
            }

            return data;
        }

        private XmlCompletionDataCollection GetAttributeCompletionData(XmlSchemaSimpleContentExtension extension)
        {
            var data = new XmlCompletionDataCollection();

            data.AddRange(GetAttributeCompletionData(extension.Attributes));

            return data;
        }

        private XmlCompletionDataCollection GetAttributeCompletionData(XmlSchemaObjectCollection attributes)
        {
            var data = new XmlCompletionDataCollection();

            foreach (var schemaObject in attributes)
            {
                if (schemaObject is XmlSchemaAttribute attribute)
                {
                    if (!IsProhibitedAttribute(attribute))
                    {
                        AddAttribute(data, attribute);
                    }
                    else
                    {
                        _prohibitedAttributes.Add(attribute);
                    }
                }
                else if (schemaObject is XmlSchemaAttributeGroupRef attributeGroupRef)
                {
                    data.AddRange(GetAttributeCompletionData(attributeGroupRef));
                }
            }
            return data;
        }

        /// <summary>
        /// Checks that the attribute is prohibited or has been flagged
        /// as prohibited previously. 
        /// </summary>
        private bool IsProhibitedAttribute(XmlSchemaAttribute attribute)
        {
            var prohibited = false;
            if (attribute.Use == XmlSchemaUse.Prohibited)
            {
                prohibited = true;
            }
            else
            {
                foreach (XmlSchemaAttribute prohibitedAttribute in _prohibitedAttributes)
                {
                    if (prohibitedAttribute.QualifiedName == attribute.QualifiedName)
                    {
                        prohibited = true;
                        break;
                    }
                }
            }

            return prohibited;
        }

        /// <summary>
        /// Adds an attribute to the completion data collection.
        /// </summary>
        /// <remarks>
        /// Note the special handling of xml:lang attributes.
        /// </remarks>
        private void AddAttribute(XmlCompletionDataCollection data, XmlSchemaAttribute attribute)
        {
            var name = attribute.Name;
            if (name == null)
            {
                if (attribute.RefName.Namespace == "http://www.w3.org/XML/1998/namespace")
                {
                    name = string.Concat("xml:", attribute.RefName.Name);
                }
            }

            if (name != null)
            {
                var documentation = GetDocumentation(attribute.Annotation);
                var completionData = new XmlCompletionData(name, documentation, XmlCompletionData.DataType.XmlAttribute);
                data.Add(completionData);
            }
        }

        /// <summary>
        /// Gets attribute completion data from a group ref.
        /// </summary>
        private XmlCompletionDataCollection GetAttributeCompletionData(XmlSchemaAttributeGroupRef groupRef)
        {
            if (Schema is null) throw new Exception("Schema expected");
            var data = new XmlCompletionDataCollection();
            var group = FindAttributeGroup(Schema, groupRef.RefName.Name);
            if (group != null)
            {
                data = GetAttributeCompletionData(group.Attributes);
            }

            return data;
        }

        private static XmlSchemaComplexType? FindNamedType(XmlSchema schema, XmlQualifiedName? name)
        {
            XmlSchemaComplexType? matchedComplexType = null;

            if (name != null)
            {
                foreach (var schemaObject in schema.Items)
                {
                    if (schemaObject is XmlSchemaComplexType complexType)
                    {
                        if (complexType.QualifiedName == name)
                        {
                            matchedComplexType = complexType;
                            break;
                        }
                    }
                }

                // Try included schemas.
                if (matchedComplexType == null)
                {
                    foreach (var external in schema.Includes)
                    {
                        if (external is XmlSchemaInclude { Schema: not null } include)
                        {
                            matchedComplexType = FindNamedType(include.Schema, name);
                        }
                    }
                }
            }

            return matchedComplexType;
        }

        /// <summary>
        /// Finds an element that matches the specified <paramref name="name"/>
        /// from the children of the given <paramref name="element"/>.
        /// </summary>
        private XmlSchemaElement? FindChildElement(XmlSchemaElement element, QualifiedName name)
        {
            XmlSchemaElement? matchedElement = null;

            var complexType = GetElementAsComplexType(element);
            if (complexType != null)
            {
                matchedElement = FindChildElement(complexType, name);
            }

            return matchedElement;
        }

        private XmlSchemaElement? FindChildElement(XmlSchemaComplexType complexType, QualifiedName name)
        {
            XmlSchemaElement? matchedElement = null;

            if (complexType.Particle is XmlSchemaSequence sequence)
            {
                matchedElement = FindElement(sequence.Items, name);
            }
            else if (complexType.Particle is XmlSchemaChoice choice)
            {
                matchedElement = FindElement(choice.Items, name);
            }
            else if (complexType.ContentModel is XmlSchemaComplexContent complexContent)
            {
                if (complexContent.Content is XmlSchemaComplexContentExtension extension)
                {
                    matchedElement = FindChildElement(extension, name);
                }
                else if (complexContent.Content is XmlSchemaComplexContentRestriction restriction)
                {
                    matchedElement = FindChildElement(restriction, name);
                }
            }
            else if (complexType.Particle is XmlSchemaGroupRef groupRef)
            {
                matchedElement = FindElement(groupRef, name);
            }
            else if (complexType.Particle is XmlSchemaAll all)
            {
                matchedElement = FindElement(all.Items, name);
            }

            return matchedElement;
        }

        /// <summary>
        /// Finds the named child element contained in the extension element.
        /// </summary>
        private XmlSchemaElement? FindChildElement(XmlSchemaComplexContentExtension extension, QualifiedName name)
        {
            if (Schema is null) throw new Exception("Schema expected");
            XmlSchemaElement? matchedElement = null;

            var complexType = FindNamedType(Schema, extension.BaseTypeName);
            if (complexType != null)
            {
                matchedElement = FindChildElement(complexType, name);

                if (matchedElement == null)
                {
                    if (extension.Particle is XmlSchemaSequence sequence)
                    {
                        matchedElement = FindElement(sequence.Items, name);
                    }
                    else if (extension.Particle is XmlSchemaChoice choice)
                    {
                        matchedElement = FindElement(choice.Items, name);
                    }
                    else if (extension.Particle is XmlSchemaGroupRef groupRef)
                    {
                        matchedElement = FindElement(groupRef, name);
                    }
                }
            }

            return matchedElement;
        }

        /// <summary>
        /// Finds the named child element contained in the restriction element.
        /// </summary>
        private XmlSchemaElement? FindChildElement(XmlSchemaComplexContentRestriction restriction, QualifiedName name)
        {
            XmlSchemaElement? matchedElement = null;

            if (restriction.Particle is XmlSchemaSequence sequence)
            {
                matchedElement = FindElement(sequence.Items, name);
            }
            else if (restriction.Particle is XmlSchemaGroupRef groupRef)
            {
                matchedElement = FindElement(groupRef, name);
            }

            return matchedElement;
        }

        /// <summary>
        /// Finds the element in the collection of schema objects.
        /// </summary>
        private XmlSchemaElement? FindElement(XmlSchemaObjectCollection items, QualifiedName name)
        {
            XmlSchemaElement? matchedElement = null;

            foreach (var schemaObject in items)
            {
                if (schemaObject is XmlSchemaElement element)
                {
                    if (element.Name != null)
                    {
                        if (name.Name == element.Name)
                        {
                            matchedElement = element;
                        }
                    }
                    else if (element.RefName != null)
                    {
                        if (name.Name == element.RefName.Name)
                        {
                            matchedElement = FindElement(element.RefName);
                        }
                        else
                        {
                            // Abstract element?
                            var abstractElement = FindElement(element.RefName);
                            if (abstractElement is { IsAbstract: true })
                            {
                                matchedElement = FindSubstitutionGroupElement(abstractElement.QualifiedName, name);
                            }
                        }
                    }
                }
                else if (schemaObject is XmlSchemaSequence sequence)
                {
                    matchedElement = FindElement(sequence.Items, name);
                }
                else if (schemaObject is XmlSchemaChoice choice)
                {
                    matchedElement = FindElement(choice.Items, name);
                }
                else if (schemaObject is XmlSchemaGroupRef groupRef)
                {
                    matchedElement = FindElement(groupRef, name);
                }

                // Did we find a match?
                if (matchedElement != null)
                {
                    break;
                }
            }

            return matchedElement;
        }

        private XmlSchemaElement? FindElement(XmlSchemaGroupRef groupRef, QualifiedName name)
        {
            XmlSchemaElement? matchedElement = null;

            var group = FindGroup(groupRef.RefName.Name);
            if (group != null)
            {
                if (group.Particle is XmlSchemaSequence sequence)
                {
                    matchedElement = FindElement(sequence.Items, name);
                }
                else if (group.Particle is XmlSchemaChoice choice)
                {
                    matchedElement = FindElement(choice.Items, name);
                }
            }

            return matchedElement;
        }

        private static XmlSchemaAttributeGroup? FindAttributeGroup(XmlSchema schema, string? name)
        {
            XmlSchemaAttributeGroup? matchedGroup = null;

            if (name != null)
            {
                foreach (var schemaObject in schema.Items)
                {
                    if (schemaObject is XmlSchemaAttributeGroup group)
                    {
                        if (group.Name == name)
                        {
                            matchedGroup = group;
                            break;
                        }
                    }
                }

                // Try included schemas.
                if (matchedGroup == null)
                {
                    foreach (var external in schema.Includes)
                    {
                        if (external is XmlSchemaInclude { Schema: not null } include)
                        {
                            matchedGroup = FindAttributeGroup(include.Schema, name);
                        }
                    }
                }
            }

            return matchedGroup;
        }

        private XmlCompletionDataCollection GetAttributeValueCompletionData(XmlSchemaElement element, string name)
        {
            var data = new XmlCompletionDataCollection();

            var complexType = GetElementAsComplexType(element);
            if (complexType != null)
            {
                var attribute = FindAttribute(complexType, name);
                if (attribute != null)
                {
                    data.AddRange(GetAttributeValueCompletionData(attribute));
                }
            }

            return data;
        }

        private XmlCompletionDataCollection GetAttributeValueCompletionData(XmlSchemaAttribute attribute)
        {
            var data = new XmlCompletionDataCollection();

            if (attribute.SchemaType != null)
            {
                if (attribute.SchemaType.Content is XmlSchemaSimpleTypeRestriction simpleTypeRestriction)
                {
                    data.AddRange(GetAttributeValueCompletionData(simpleTypeRestriction));
                }
            }
            else
            {
                var simpleType = attribute.AttributeSchemaType;
                if (simpleType != null)
                {
                    data.AddRange(simpleType.Name == "boolean"
                        ? GetBooleanAttributeValueCompletionData()
                        : GetAttributeValueCompletionData(simpleType));
                }
            }

            return data;
        }

        private XmlCompletionDataCollection GetAttributeValueCompletionData(XmlSchemaSimpleTypeRestriction simpleTypeRestriction)
        {
            var data = new XmlCompletionDataCollection();

            foreach (var schemaObject in simpleTypeRestriction.Facets)
            {
                if (schemaObject is XmlSchemaEnumerationFacet { Value: not null } enumFacet)
                {
                    AddAttributeValue(data, enumFacet.Value, enumFacet.Annotation);
                }
            }

            return data;
        }

        private XmlCompletionDataCollection GetAttributeValueCompletionData(XmlSchemaSimpleTypeUnion union)
        {
            var data = new XmlCompletionDataCollection();

            foreach (var schemaObject in union.BaseTypes)
            {
                if (schemaObject is XmlSchemaSimpleType simpleType)
                {
                    data.AddRange(GetAttributeValueCompletionData(simpleType));
                }
            }

            return data;
        }

        private XmlCompletionDataCollection GetAttributeValueCompletionData(XmlSchemaSimpleType simpleType)
        {
            var data = new XmlCompletionDataCollection();

            if (simpleType.Content is XmlSchemaSimpleTypeRestriction simpleTypeRestriction)
            {
                data.AddRange(GetAttributeValueCompletionData(simpleTypeRestriction));
            }
            else if (simpleType.Content is XmlSchemaSimpleTypeUnion union)
            {
                data.AddRange(GetAttributeValueCompletionData(union));
            }
            else if (simpleType.Content is XmlSchemaSimpleTypeList list)
            {
                data.AddRange(GetAttributeValueCompletionData(list));
            }

            return data;
        }

        private XmlCompletionDataCollection GetAttributeValueCompletionData(XmlSchemaSimpleTypeList list)
        {
            var data = new XmlCompletionDataCollection();

            if (list.ItemType != null)
            {
                data.AddRange(GetAttributeValueCompletionData(list.ItemType));
            }
            else if (list.ItemTypeName != null)
            {
                var simpleType = FindSimpleType(list.ItemTypeName);
                if (simpleType != null)
                {
                    data.AddRange(GetAttributeValueCompletionData(simpleType));
                }
            }

            return data;
        }

        /// <summary>
        /// Gets the set of attribute values for an xs:boolean type.
        /// </summary>
        private XmlCompletionDataCollection GetBooleanAttributeValueCompletionData()
        {
            var data = new XmlCompletionDataCollection();

            AddAttributeValue(data, "0");
            AddAttributeValue(data, "1");
            AddAttributeValue(data, "true");
            AddAttributeValue(data, "false");

            return data;
        }

        private XmlSchemaAttribute? FindAttribute(XmlSchemaComplexType complexType, string name)
        {
            XmlSchemaAttribute? matchedAttribute = null;

            matchedAttribute = FindAttribute(complexType.Attributes, name);

            if (matchedAttribute == null)
            {
                if (complexType.ContentModel is XmlSchemaComplexContent complexContent)
                {
                    matchedAttribute = FindAttribute(complexContent, name);
                }
            }

            return matchedAttribute;
        }

        private XmlSchemaAttribute? FindAttribute(XmlSchemaObjectCollection schemaObjects, string name)
        {
            XmlSchemaAttribute? matchedAttribute = null;

            foreach (var schemaObject in schemaObjects)
            {
                if (schemaObject is XmlSchemaAttribute attribute)
                {
                    if (attribute.Name == name)
                    {
                        matchedAttribute = attribute;
                        break;
                    }
                }
                else if (schemaObject is XmlSchemaAttributeGroupRef groupRef)
                {
                    matchedAttribute = FindAttribute(groupRef, name);
                    if (matchedAttribute != null)
                    {
                        break;
                    }
                }
            }

            return matchedAttribute;
        }

        private XmlSchemaAttribute? FindAttribute(XmlSchemaAttributeGroupRef groupRef, string name)
        {
            if (Schema is null) throw new Exception("Schema expected");
            XmlSchemaAttribute? matchedAttribute = null;

            var group = FindAttributeGroup(Schema, groupRef.RefName.Name);
            if (group != null)
            {
                matchedAttribute = FindAttribute(group.Attributes, name);
            }

            return matchedAttribute;
        }

        private XmlSchemaAttribute? FindAttribute(XmlSchemaComplexContent complexContent, string name)
        {
            XmlSchemaAttribute? matchedAttribute = null;

            if (complexContent.Content is XmlSchemaComplexContentExtension extension)
            {
                matchedAttribute = FindAttribute(extension, name);
            }
            else if (complexContent.Content is XmlSchemaComplexContentRestriction restriction)
            {
                matchedAttribute = FindAttribute(restriction, name);
            }

            return matchedAttribute;
        }

        private XmlSchemaAttribute? FindAttribute(XmlSchemaComplexContentExtension extension, string name)
        {
            return FindAttribute(extension.Attributes, name);
        }

        private XmlSchemaAttribute? FindAttribute(XmlSchemaComplexContentRestriction restriction, string name)
        {
            if (Schema is null) throw new Exception("Schema expected");
            var matchedAttribute = FindAttribute(restriction.Attributes, name);

            if (matchedAttribute == null)
            {
                var complexType = FindNamedType(Schema, restriction.BaseTypeName);
                if (complexType != null)
                {
                    matchedAttribute = FindAttribute(complexType, name);
                }
            }

            return matchedAttribute;
        }

        /// <summary>
        /// Adds an attribute value to the completion data collection.
        /// </summary>
        private void AddAttributeValue(XmlCompletionDataCollection data, string valueText)
        {
            var completionData = new XmlCompletionData(valueText, XmlCompletionData.DataType.XmlAttributeValue);
            data.Add(completionData);
        }

        /// <summary>
        /// Adds an attribute value to the completion data collection.
        /// </summary>
        private void AddAttributeValue(XmlCompletionDataCollection data, string valueText, XmlSchemaAnnotation? annotation)
        {
            var documentation = GetDocumentation(annotation);
            var completionData = new XmlCompletionData(valueText, documentation, XmlCompletionData.DataType.XmlAttributeValue);
            data.Add(completionData);
        }

        private XmlSchemaSimpleType? FindSimpleType(XmlQualifiedName name)
        {
            if (Schema is null) throw new Exception("Schema expected");
            XmlSchemaSimpleType? matchedSimpleType = null;

            foreach (XmlSchemaObject schemaObject in Schema.SchemaTypes.Values)
            {
                if (schemaObject is XmlSchemaSimpleType simpleType)
                {
                    if (simpleType.QualifiedName == name)
                    {
                        matchedSimpleType = simpleType;
                        break;
                    }
                }
            }

            return matchedSimpleType;
        }

        /// <summary>
        /// Adds any elements that have the specified substitution group.
        /// </summary>
        private void AddSubstitionGroupElements(XmlCompletionDataCollection data, XmlQualifiedName group, string prefix)
        {
            if (Schema is null) throw new Exception("Schema expected");
            foreach (XmlSchemaElement element in Schema.Elements.Values)
            {
                if (element.Name is not null && element.SubstitutionGroup == group)
                {
                    AddElement(data, element.Name, prefix, element.Annotation);
                }
            }
        }

        /// <summary>
        /// Looks for the substitution group element of the specified name.
        /// </summary>
        private XmlSchemaElement? FindSubstitutionGroupElement(XmlQualifiedName group, QualifiedName name)
        {
            if (Schema is null) throw new Exception("Schema expected");
            XmlSchemaElement? matchedElement = null;

            foreach (XmlSchemaElement element in Schema.Elements.Values)
            {
                if (element.SubstitutionGroup == group)
                {
                    if (element.Name != null)
                    {
                        if (element.Name == name.Name)
                        {
                            matchedElement = element;
                            break;
                        }
                    }
                }
            }

            return matchedElement;
        }
    }
}
