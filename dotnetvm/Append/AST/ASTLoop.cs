using Append.Parsing;
using Append.Types;

namespace Append.AST
{
    internal class ASTLoop(ASTNode Condition, ASTNode Body) : ASTNode
    {
        internal override TypeId KnownType => TypeId.None;
        internal override TypeId ReturnType => Body.ReturnType;

        internal override int SubNodeCount => 2;
        internal override ASTNode GetSubNode(int index)
        {
            if (index < 0 || index >= SubNodeCount)
                throw new IndexOutOfRangeException(nameof(index));
            return index == 0 ? Condition : Body;
        }
        internal override void SetSubNode(int index, ASTNode node)
        {
            if (index < 0 || index >= SubNodeCount)
                throw new IndexOutOfRangeException(nameof(index));
            if (index == 0)
                Condition = node;
            else
                Body = node;
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
