using Append.Parsing;
using Append.Types;
using System;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Append.AST
{
    internal class ASTCallByName(string FunctionName, ASTNode[] Parameters, bool TailCall = false) : ASTNode
    {
        public string FunctionName { get; } = FunctionName;
        public ASTNode[] Parameters { get; } = Parameters;
        public bool TailCall { get; } = TailCall;

        internal override TypeId KnownType => TypeId.None;
        public TypeId[] KnownParameterTypes =>
            [.. from p in Parameters select p.KnownType];

        internal override void ReplaceSubNodes(Func<ASTNode, ASTNode, ASTNode> replaceFunction)
        {
            for (int i = 0; i < Parameters.Length; i++)
                Parameters[i] = replaceFunction(this,Parameters[i]);
        }
        internal override void ReplaceSubNode(ASTNode oldNode, ASTNode newNode)
        {
            for (int i = 0; i < Parameters.Length; i++)
                if (oldNode == Parameters[i])
                    Parameters[i] = newNode;
        }
        internal override (ASTSignal, ASTNode?) Step(VMThread context, ref int step)
        {
            throw new Exceptions.UnresolvedCallException();
        }

        internal override string ToString(int surroundingPriority)
        {
            var myPriority = Operations.Priorty(FunctionName);
            string span;
            if (Parameters.Length == 0)
                span = $"() {FunctionName}";
            else if (Parameters.Length == 1)
                span = $"{Parameters[0].ToString(myPriority)} {FunctionName}";
            else
                span = $"{Parameters[0].ToString(myPriority)} {FunctionName} " 
                    + string.Join("; ", Parameters.Skip(1).Select(p => p.ToString(myPriority)));
            
            return Operations.Brackets(
                   span, myPriority, surroundingPriority);
        }
    }
}
