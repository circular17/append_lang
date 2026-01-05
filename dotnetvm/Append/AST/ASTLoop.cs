using Append.Parsing;
using Append.Types;

namespace Append.AST
{
    internal class ASTLoop(ASTNode Condition, ASTNode Body) : ASTNode
    {
        internal override TypeId KnownType => TypeId.None;
        internal override TypeId ReturnType => Body.ReturnType;

        internal override void ReplaceSubNodes(Func<ASTNode, ASTNode, ASTNode> replaceFunction)
        {
            Condition = replaceFunction(this, Condition);
            Body = replaceFunction(this, Body);
        }

        internal override void ReplaceSubNode(ASTNode oldNode, ASTNode newNode)
        {
            if (oldNode == Condition)
                Condition = newNode;
            else if (oldNode == Body)
                Body = newNode;
        }

        internal override (ASTSignal, ASTNode?) Step(VMThread context, ref int step)
        {
            switch (step)
            {
                case 0:
                    context.PushUnwind(this, breakStep: 3, nextStep: 1);
                    step++;
                    return (ASTSignal.Continue, null);

                case 1:
                    step++;
                    return (ASTSignal.Enter, Condition);

                case 2:
#if DEBUG
                    if (context.Value.TypeId != Types.TypeId.Bool)
                        throw new Exceptions.InvalidValueTypeException();
#endif
                    if (context.Value.Data.Bool)
                    {
                        step--;
                        return (ASTSignal.Enter, Body);
                    }
                    else
                    {
                        step++;
                        return (ASTSignal.Continue, null);
                    }

                default:
#if DEBUG
                    if (step != 3)
                        throw new Exceptions.InvalidStepException();
#endif
                    context.PopUnwind(this);
                    return (ASTSignal.Done, null);

            }
        }

        internal override string ToString(int surroundingPriority)
        {
            string conditionStr;
            if (Condition is ASTConst c && c.Value.TypeId == TypeId.Bool
                && c.Value.Data.Bool == true)
                conditionStr = "";
            else
                conditionStr = $"{Condition} ";

            if (Body is not ASTBlock)
                return $"{conditionStr}loop {Body.ToString() ?? ""}";
            else
                return $"{conditionStr}loop {{{Environment.NewLine}{Formatting.Indent(Body.ToString() ?? "")}"
                    + $"{Environment.NewLine}}}";
        }
    }
}
