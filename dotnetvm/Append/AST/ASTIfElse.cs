using Append.Parsing;
using Append.Types;

namespace Append.AST
{
    internal class ASTIfElse(ASTNode Condition, ASTNode YesBody, ASTNode NoBody) : ASTNode
    {
        public ASTNode YesBody { get; set; } = YesBody;
        public ASTNode NoBody { get; set; } = NoBody;

        internal override TypeId KnownType
        {
            get
            {
                var yesType = YesBody.KnownType;
                var noType = YesBody.KnownType;
                if (yesType != noType)
                {
                    if (yesType == TypeId.None)
                        return noType;
                    else
                        return yesType;
                }
                else
                    return yesType;
            }
        }

        internal override TypeId ReturnType {
            get
            {
                var yesType = YesBody.ReturnType;
                if (yesType != TypeId.None)
                    return yesType;
                return NoBody.ReturnType;
            }
        }

        internal override void ReplaceSubNodes(Func<ASTNode, ASTNode, ASTNode> replaceFunction)
        {
            Condition = replaceFunction(this, Condition);
            YesBody = replaceFunction(this, YesBody);
            NoBody = replaceFunction(this, NoBody);
        }
        internal override void ReplaceSubNode(ASTNode oldNode, ASTNode newNode)
        {
            if (oldNode == Condition)
                Condition = newNode;
            else if (oldNode == YesBody)
                YesBody = newNode;
            else if (oldNode == NoBody)
                NoBody = newNode;
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
                        step++;
                        return (ASTSignal.Enter, NoBody);
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
            return Operations.Brackets(
                $"{Condition.ToString(myPriority)} ? {YesBody.ToString(myPriority)} else {NoBody.ToString(myPriority)}", 
                myPriority, surroundingPriority);
        }
    }
}
