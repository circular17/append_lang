using Append.Parsing;
using Append.Types;

namespace Append.AST
{
    internal class ASTIf(ASTNode Condition, ASTNode YesBody) : ASTNode
    {
        internal override TypeId KnownType => TypeId.None;
        internal override TypeId ReturnType => YesBody.ReturnType;

        internal override int SubNodeCount => 2;
        internal override ASTNode GetSubNode(int index)
        {
            if (index < 0 || index >= SubNodeCount)
                throw new IndexOutOfRangeException(nameof(index));
            return index == 0 ? Condition : YesBody;
        }
        internal override void SetSubNode(int index, ASTNode node)
        {
            if (index < 0 || index >= SubNodeCount)
                throw new IndexOutOfRangeException(nameof(index));
            if (index == 0)
                Condition = node;
            else
                YesBody = node;
        }

        internal override (ASTSignal, ASTNode?) Step(VMThread context, ref int step)
        {
            switch (step)
            {
                case 0:
                    step++;
                    return (ASTSignal.Enter, Condition);

                case 1:
#if DEBUG
                    if (context.Value.TypeId != Types.TypeId.Bool)
                        throw new Exceptions.InvalidValueTypeException();
#endif
                    if (context.Value.Data.Bool)
                    {
                        step++;
                        return (ASTSignal.Enter, YesBody);
                    }
                    else
                    {
                        context.Value = Value.Void;
                        return (ASTSignal.Done, null);
                    }

                default:
#if DEBUG
                    if (step != 2)
                        throw new Exceptions.InvalidStepException();
#endif
                    return (ASTSignal.Done, null);
            }
        }

        internal override string ToString(int surroundingPriority)
        {
            var myPriority = Operations.Priorty("?");
            return Operations.Brackets($"{Condition.ToString(myPriority)} ? {YesBody.ToString(myPriority)}", 
                myPriority, surroundingPriority);
        }
            
    }
}
