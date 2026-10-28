using System;
using Mono.Cecil;

namespace RuntimeNullables.Weaver.Contexts;

internal class WeavingContext
{
    private readonly ModuleWeaver _weaver;

    private BclReferences? _bclReferences;
    private ModuleReferences? _moduleReferences;
    private PublicApi? _publicApi;

    public WeavingContext(ModuleWeaver weaver)
    {
        _weaver = weaver;
        ModuleContext = new ModuleContext(weaver.ModuleDefinition, this);

        bool isDebug = weaver.DefineConstants.Contains("DEBUG");

        CheckOutputs = weaver.CheckOutputs ?? isDebug;
        CheckNonPublic = weaver.CheckNonPublic ?? isDebug;
    }

    public ModuleContext ModuleContext { get; }

    public BclReferences BclReferences => _bclReferences ?? throw new InvalidOperationException("Weaving context not built.");

    public ModuleReferences ModuleReferences
    {
        get => _moduleReferences ?? throw new InvalidOperationException("Module references not set.");
        set => _moduleReferences = value;
    }

    public bool CheckOutputs { get; }

    public bool CheckNonPublic { get; }

    /// <summary>
    /// Gets the module's public API, which is only needed (and built) when <see cref="CheckNonPublic"/> is <see langword="false"/>.
    /// </summary>
    public PublicApi PublicApi => _publicApi ??= new PublicApi(ModuleContext.Module);

    public void WriteInfo(string message) => _weaver.WriteInfo(message);

    public void WriteWarning(string message) => _weaver.WriteWarning(message);

    public void WriteError(string message) => _weaver.WriteError(message);

    /// <summary>
    /// Writes a warning for each <c>[NullChecks(true)]</c> on the type and its members, which can't turn checks on since the type is never checked.
    /// </summary>
    public void WarnAboutNullChecksAttributes(TypeDefinition type, string reason)
    {
        WarnAboutNullChecksAttribute(type, $"'{type}'", reason);

        foreach (var method in type.Methods)
            WarnAboutNullChecksAttributes(method, reason);

        foreach (var property in type.Properties)
            WarnAboutNullChecksAttribute(property, $"'{property}'", reason);
    }

    /// <summary>
    /// Writes a warning for each <c>[NullChecks(true)]</c> on the method and its return value, which can't turn checks on since the method is never
    /// checked.
    /// </summary>
    public void WarnAboutNullChecksAttributes(MethodDefinition method, string reason)
    {
        WarnAboutNullChecksAttribute(method, $"'{method}'", reason);
        WarnAboutNullChecksAttribute(method.MethodReturnType, $"the return value of '{method}'", reason);
    }

    /// <summary>
    /// Writes a warning for each <c>[NullChecks(true)]</c> on the property and its accessors, which can't turn checks on since the property is never
    /// checked.
    /// </summary>
    public void WarnAboutNullChecksAttributes(PropertyDefinition property, string reason)
    {
        WarnAboutNullChecksAttribute(property, $"'{property}'", reason);

        foreach (var accessor in new[] { property.GetMethod, property.SetMethod })
        {
            if (accessor != null)
                WarnAboutNullChecksAttributes(accessor, reason);
        }
    }

    public void Build()
    {
        _bclReferences = new BclReferences(_weaver);
        ModuleContext.Build();
    }

    /// <summary>
    /// Combines the result of two exclusive annotation presence checks and writes a warning if they are both present.
    /// </summary>
    /// <returns>True if trueResult == true or both result are true, false if falseResult == true, otherwise null.</returns>
    public bool? CombineExclusiveAnnotations(bool trueResult, bool falseResult, string warningFormat, object warningArg0, object? warningArg1 = null)
    {
        if (trueResult && falseResult)
        {
            string message = string.Format(null, warningFormat, warningArg0, warningArg1);
            WriteWarning(message);
            return true;
        }

        if (trueResult)
            return true;

        if (falseResult)
            return false;

        return null;
    }

    private void WarnAboutNullChecksAttribute(ICustomAttributeProvider item, string itemName, string reason)
    {
        if (item.GetNullChecksAttributeValue(this) == true)
            WriteWarning($"[NullChecks(true)] on {itemName} has no effect: {reason}");
    }
}
