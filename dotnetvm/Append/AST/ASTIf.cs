using Append.Parsing;
using Append.Types;

namespace Append.AST
{
    internal class ASTIf(ASTNode Condition, ASTNode YesBody) : ASTNode
    {
        internal override TypeId KnownType => TypeId.None;
        internal override TypeId ReturnType => YesBody.ReturnType;

        internal override void ReplaceSubNodes(Func<ASTNode, ASTNode, ASTNode> replaceFunction)
        {
            Condition = replaceFunction(this, Condition);
            YesBody = replaceFunction(this, YesBody);
        }
        internal override void ReplaceSubNode(ASTNode oldNode, ASTNode newNode)
        {
            if (oldNode == Condition)
                Condition = newNode;
            else if (oldNode == YesBody)
                YesBody = newNode;
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
