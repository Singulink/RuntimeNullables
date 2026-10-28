using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using RuntimeNullables.Weaver.Contexts;
using RuntimeNullables.Weaver.Extensions;

namespace RuntimeNullables.Weaver;

public partial class ModuleWeaver
{
    private static bool InjectParameterChecks(MethodContext methodContext, ref ReturnBlockInfo? returnBlockInfo, ArgumentCopies argumentCopies)
    {
        var method = methodContext.Method;
        var weavingContext = methodContext.WeavingContext;
        var moduleReferences = weavingContext.ModuleReferences;
        bool returnsBool = method.ReturnType.WithoutModifiers().FullName == weavingContext.BclReferences.BoolType.FullName;
        var outputChecks = new List<OutputCheck>();
        bool modified = false;

        foreach (var parameter in method.Parameters.Reverse())
        {
            var parameterType = parameter.ParameterType.WithoutModifiers();

            if (!parameterType.IsReferenceType())
                continue;

            // Preconditions:

            bool inputChecked = false;

            // IsOut is also set for by-value parameters with [Out] (e.g. arrays that the method fills), which still have inputs.

            if (!(parameterType.IsByReference && parameter.IsOut))
            {
                bool? allowsNull = AllowsNull(parameter);

                if (allowsNull == false || (allowsNull == null && !methodContext.IsParameterNullable(parameter)))
                {
                    InjectParameterInputCheck(parameter, method, moduleReferences);
                    inputChecked = true;
                    modified = true;
                }
            }

            // Postconditions, about the out/ref parameter's value or the caller's argument when the method returns:

            bool isByReference = parameterType.IsByReference;

            if (!weavingContext.CheckOutputs || (isByReference && parameter.IsIn))
                continue;

            // An argument that was checked going in can't be null coming out. The code of an async method or iterator hasn't all run when it returns.

            if (!isByReference && (inputChecked || method.IsAsyncOrIterator()))
                continue;

            GetPostconditions(parameter, isByReference, out bool notNullWhenTrue, out bool notNullWhenFalse);

            if (notNullWhenTrue && notNullWhenFalse)
                outputChecks.Add(CreateCheck(parameter, isByReference, whenResult: null));
            else if (notNullWhenTrue || notNullWhenFalse)
                outputChecks.Add(CreateCheck(parameter, isByReference, whenResult: notNullWhenTrue));

            // Callers ignore [NotNullIfNotNull] on by-value parameters, so it only applies to out/ref parameters.

            if (isByReference && !(notNullWhenTrue && notNullWhenFalse))
            {
                foreach (var source in GetNotNullIfNotNullSources(method, parameter))
                    outputChecks.Add(OutputCheck.ForParameter(parameter, whenArgumentNotNull: argumentCopies.GetOrAdd(source)));
            }
        }

        foreach (var check in outputChecks)
        {
            InjectOutputCheck(method, returnBlockInfo ??= new ReturnBlockInfo(method), moduleReferences, check);
            modified = true;
        }

        return modified;

        bool? AllowsNull(ParameterDefinition parameter)
        {
            const string messageFormat = "Skipping null checks due to conflicting preconditions on parameter '{0}' on method '{1}'.";

            bool allowsNull = parameter.HasAllowNullAttribute();
            bool disallowsNull = parameter.HasDisallowNullAttribute();

            return weavingContext.CombineExclusiveAnnotations(allowsNull, disallowsNull, messageFormat, parameter, method);
        }

        // Gets whether the value can't be null when the method returns true and when it returns false (both the same unless it returns bool). This
        // follows how the compiler reads the attributes for callers.

        void GetPostconditions(ParameterDefinition parameter, bool isByReference, out bool whenTrue, out bool whenFalse)
        {
            const string messageFormat = "Skipping null checks due to conflicting postconditions on parameter '{0}' on method '{1}'.";

            bool hasMaybeNull = parameter.HasMaybeNullAttribute();
            bool hasNotNull = parameter.HasNotNullAttribute();

            // The type says whether an out/ref parameter's value can be null, unless [MaybeNull] or [NotNull] says otherwise. A by-value argument
            // only has the postconditions its attributes give it.

            bool? maybeNull = weavingContext.CombineExclusiveAnnotations(hasMaybeNull, hasNotNull, messageFormat, parameter, method);
            bool notNull = maybeNull == false || (maybeNull == null && isByReference && !methodContext.IsParameterNullable(parameter));

            whenTrue = notNull;
            whenFalse = notNull;

            // [MaybeNullWhen] and [NotNullWhen] only count for methods that return bool, and not next to [MaybeNull] or [NotNull] respectively.

            if (!returnsBool)
                return;

            bool? maybeNullWhen = hasMaybeNull ? null : parameter.GetMaybeNullWhenValue(weavingContext);
            bool? notNullWhen = hasNotNull ? null : parameter.GetNotNullWhenValue(weavingContext);

            if (maybeNullWhen != null && maybeNullWhen == notNullWhen)
            {
                string result = maybeNullWhen.Value ? "true" : "false";
                weavingContext.WriteWarning(
                    $"Skipping the null check for a '{result}' result due to conflicting postconditions on parameter '{parameter}' on method '{method}'.");
            }

            whenTrue = GetPostcondition(true, whenTrue);
            whenFalse = GetPostcondition(false, whenFalse);

            bool GetPostcondition(bool result, bool notNull)
            {
                bool notNullWhenResult = notNullWhen == result;
                bool maybeNullWhenResult = maybeNullWhen == result;

                // For an out/ref parameter, the conditional attributes override the others for their result. For an argument, the result can't
                // be null if any attribute says so and none says it may be null. Either way, "may be null" wins a conflict.

                if (isByReference)
                    return notNullWhenResult || maybeNullWhenResult ? notNullWhenResult && !maybeNullWhenResult : notNull;

                return (notNull || notNullWhenResult) && maybeNull != true && !maybeNullWhenResult;
            }
        }

        OutputCheck CreateCheck(ParameterDefinition parameter, bool isByReference, bool? whenResult)
        {
            return isByReference ?
                OutputCheck.ForParameter(parameter, whenResult) :
                OutputCheck.ForArgument(parameter, argumentCopies.GetOrAdd(parameter), whenResult);
        }
    }

    private static void InjectParameterInputCheck(ParameterDefinition parameter, MethodDefinition method, ModuleReferences moduleReferences)
    {
        var instructions = method.Body.Instructions;
        var instruction0 = instructions[0];
        int index = 0;

        instructions.Insert(index++, Instruction.Create(OpCodes.Ldarg, parameter));
        ILHelpers.InsertGetValueRef(ref index, instructions, parameter.ParameterType);
        ILHelpers.InsertThrowHelperCallIfValueRefIsNull(
            ref index, instructions, moduleReferences.ThrowArgumentNullMethod, parameter.Name, instruction0);
    }
}
