using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace RuntimeNullables.Tasks;

/// <summary>
/// The <c>&lt;RuntimeNullables&gt;</c> element of Fody's weaver configuration, which configured RuntimeNullables when it was a Fody add-in.
/// </summary>
internal sealed class LegacyFodyConfiguration
{
    /// <summary>
    /// The name of the element.
    /// </summary>
    public const string ElementName = "RuntimeNullables";

    private readonly XElement _element;

    private LegacyFodyConfiguration(XElement element, string? file)
    {
        _element = element;
        File = file;
    }

    /// <summary>
    /// Gets the file the element is in, or <see langword="null"/> if it is in the <c>WeaverConfiguration</c> property.
    /// </summary>
    public string? File { get; }

    /// <summary>
    /// Gets the position of the element in <see cref="File"/>, or (0, 0) if it isn't in a file.
    /// </summary>
    public (int Line, int Column) Position => GetPosition(_element);

    /// <summary>
    /// Gets an attribute of the element, or <see langword="null"/> if it isn't set.
    /// </summary>
    public XAttribute? GetAttribute(string name) => _element.Attribute(name);

    /// <summary>
    /// Gets the position of an element or attribute in <see cref="File"/>, or (0, 0) if it isn't in a file.
    /// </summary>
    public static (int Line, int Column) GetPosition(XObject node)
    {
        var lineInfo = (IXmlLineInfo)node;
        return lineInfo.HasLineInfo() ? (lineInfo.LineNumber, lineInfo.LinePosition) : (0, 0);
    }

    /// <summary>
    /// Parses a boolean attribute value the way the Fody add-in did (<see cref="XmlConvert.ToBoolean(string)"/>): <c>true</c>, <c>false</c>,
    /// <c>1</c> or <c>0</c>, case-sensitive, with surrounding whitespace allowed.
    /// </summary>
    public static bool TryParseBoolean(string value, out bool result)
    {
        try
        {
            result = XmlConvert.ToBoolean(value);
            return true;
        }
        catch (FormatException)
        {
            result = false;
            return false;
        }
    }

    /// <summary>
    /// Finds the element that applies the way Fody does: the configuration files are read in order, then the <c>WeaverConfiguration</c> property, and
    /// the last element found is used as a whole.
    /// </summary>
    /// <param name="files">The configuration files, in the order Fody reads them: the solution's <c>FodyWeavers.xml</c>, then the project's. Files
    /// that don't exist (anymore) are skipped.</param>
    /// <param name="weaverConfiguration">The value of the <c>WeaverConfiguration</c> property.</param>
    /// <param name="onReadError">Called with the file (or <see langword="null"/> for the property) and the exception when configuration can't be
    /// read. That configuration is then skipped.</param>
    public static LegacyFodyConfiguration? Find(IEnumerable<string> files, string? weaverConfiguration, Action<string?, Exception> onReadError)
    {
        LegacyFodyConfiguration? configuration = null;

        foreach (string file in files)
        {
            if (TryRead(() => LoadFile(file), file, onReadError) is { } fileConfiguration)
                configuration = fileConfiguration;
        }

        // Unlike Fody, which fails on a property that is only whitespace, treat it as not set.
        if (!string.IsNullOrWhiteSpace(weaverConfiguration) &&
            TryRead(() => XDocument.Parse(weaverConfiguration), file: null, onReadError) is { } propertyConfiguration)
        {
            configuration = propertyConfiguration;
        }

        return configuration;
    }

    /// <summary>
    /// Loads a file the way Fody does: as UTF-8 unless it has a byte order mark (ignoring the encoding in the XML declaration), and allowing other
    /// processes to write it at the same time.
    /// </summary>
    private static XDocument? LoadFile(string file)
    {
        FileStream stream;

        try
        {
            stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            // Deleted since the build found it, e.g. while Visual Studio reuses an earlier evaluation of the project.
            return null;
        }

        using var reader = new StreamReader(stream);
        return XDocument.Load(reader, LoadOptions.SetLineInfo);
    }

    private static LegacyFodyConfiguration? TryRead(Func<XDocument?> load, string? file, Action<string?, Exception> onReadError)
    {
        XDocument? document;

        try
        {
            document = load();
        }
        catch (Exception ex) when (ex is XmlException or IOException or UnauthorizedAccessException)
        {
            onReadError(file, ex);
            return null;
        }

        // Like Fody, only look at the root's children, match the name case-insensitively ignoring namespaces, and use the last match.
        var element = document?.Root?.Elements().LastOrDefault(e => string.Equals(e.Name.LocalName, ElementName, StringComparison.OrdinalIgnoreCase));
        return element != null ? new LegacyFodyConfiguration(element, file) : null;
    }
}
