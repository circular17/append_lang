using Append.AST;
using System.Diagnostics;

namespace Append.Test
{
    internal class TestAST : VMApplication
    {
        public void DoTest()
        {
            Value result;

            Reset();
            result = RunProgram(new ASTCall(FactorialFunctionWhileTrueNoReturn(), [new ASTConst(Value.FromInt(6))]));
            Debug.Assert(result.TypeId == Types.TypeId.Int && result.Data.Int == 720);

            Reset();
            result = RunProgram(new ASTCall(FactorialFunctionWhileComparaisonWithReturn(), [new ASTConst(Value.FromInt(6))]));
            Debug.Assert(result.TypeId == Types.TypeId.Int && result.Data.Int == 720);

            Reset();
            result = RunProgram(new ASTCall(FactorialFunctionRecursive(), [new ASTConst(Value.FromInt(6))]));
            Debug.Assert(result.TypeId == Types.TypeId.Int && result.Data.Int == 720);

            Reset();
            result = RunProgram(new ASTCall(FactorialFunctionRecursiveAccumulationTailCall(), 
                [new ASTConst(Value.FromInt(6)), new ASTConst(Value.FromInt(1))]));
            Debug.Assert(result.TypeId == Types.TypeId.Int && result.Data.Int == 720);

            Reset();
            result = RunProgram(new ASTCall(FactorialFunctionRecursiveTailCall(), [new ASTConst(Value.FromInt(6))]));
            Debug.Assert(result.TypeId == Types.TypeId.Int && result.Data.Int == 720);

            Reset();
            var callNode = new ASTCall(AddOne(), [new ASTConst(Value.FromInt(3))]);
            var compNode = new ASTCallByName("<", [callNode, new ASTConst(Value.FromInt(8))]);
            result = RunProgram(compNode);
            Debug.Assert(result.TypeId == Types.TypeId.Bool && result.Data.Bool == true);
        }

        private ASTFunction FactorialFunctionWhileTrueNoReturn()
        {
            var f = new ASTFunction("factorialNoReturn", _types, parameters: [("N", Types.TypeId.Int)], locals: [("result", Types.TypeId.Int)]);
            _globalScope.AddFunction(f);
            var N = f.FindVariable("N")!;
            var Result = f.FindVariable("result")!;
            f.Body =
                new ASTVarDef([Value.FromInt(1L)],
                    new ASTBlock([
                        new ASTWhile(
                            new ASTConst(Value.FromBool(true)),
                            new ASTBlock([
                                new ASTWriteVar(Result, new ASTCallByName("*", [new ASTReadLocalVar(Result), new ASTReadLocalVar(N)])),
                                new ASTWriteVar(N, new ASTCallByName("-", [new ASTReadLocalVar(N), new ASTConst(Value.FromInt(1L))])),
                                new ASTIf(
                                    new ASTCallByName("<=", [new ASTReadLocalVar(N), new ASTConst(Value.FromInt(1L))]),
                                    new ASTBreak())
                            ])),
                        new ASTReadLocalVar(Result)
                        ]));
            return f;
        }
        
        private ASTFunction FactorialFunctionWhileComparaisonWithReturn()
        {
            var f = new ASTFunction("factorialWhileComp", _types, parameters: [("N", Types.TypeId.Int)], locals: [("result", Types.TypeId.Int)]);
            _globalScope.AddFunction(f);
            var N = f.FindVariable("N")!;
            var Result = f.FindVariable("result")!;
            f.Body = new ASTVarDef([Value.FromInt(1L)],
                new ASTBlock([
                    new ASTWhile(
                        new ASTCallByName(">", [new ASTReadLocalVar(N), new ASTConst(Value.FromInt(1L))]),
                        new ASTBlock([
                            new ASTWriteVar(Result, new ASTCallByName("*", [new ASTReadLocalVar(Result), new ASTReadLocalVar(N)])),
                            new ASTWriteVar(N, new ASTCallByName("-", [new ASTReadLocalVar(N), new ASTConst(Value.FromInt(1L))]))
                        ])),
                    new ASTReturn(new ASTReadLocalVar(Result))
                    ]));
            return f;
        }

        private ASTFunction FactorialFunctionRecursive()
        {
            var f = new ASTFunction("factorialRec", _types, parameters: [("N", Types.TypeId.Int)]);
            _globalScope.AddFunction(f);
            var N = f.FindVariable("N")!;
            f.Body = 
                new ASTIfElse(
                new ASTCallByName("<=", [new ASTReadLocalVar(N), new ASTConst(Value.FromInt(1L))]),
                new ASTConst(Value.FromInt(1L)),
                new ASTCallByName("*",
                    [new ASTCallByName(f.Name, [new ASTCallByName("-", [new ASTReadLocalVar(N), new ASTConst(Value.FromInt(1L))])]),
                    new ASTReadLocalVar(N)]));
            return f;
        }

        private ASTFunction FactorialFunctionRecursiveTailCall()
        {
            var accumulation = FactorialFunctionRecursiveAccumulationTailCall();
            var f = new ASTFunction("factorialProxy", _types, parameters: [("N", Types.TypeId.Int)]);
            _globalScope.AddFunction(f);
            var N = f.FindVariable("N")!;
            f.Body = new ASTCall(accumulation, [new ASTReadLocalVar(N), new ASTConst(Value.FromInt(1L))]);
            return f;
        }

        private ASTFunction FactorialFunctionRecursiveAccumulationTailCall()
        {
            var f = new ASTFunction("factorialTail", _types, parameters: [("N", Types.TypeId.Int), ("acc", Types.TypeId.Int)]);
            _globalScope.AddFunction(f);
            var N = f.FindVariable("N")!;
            var acc = f.FindVariable("acc")!;
            f.Body =
                new ASTIfElse(
                    new ASTCallByName("<=", [new ASTReadLocalVar(N), new ASTConst(Value.FromInt(1L))]),
                    new ASTReadLocalVar(acc),
                    new ASTCallByName(f.Name, [
                        new ASTCallByName("-", [new ASTReadLocalVar(N), new ASTConst(Value.FromInt(1L))]),
                        new ASTCallByName("*", [new ASTReadLocalVar(acc), new ASTReadLocalVar(N)])
                        ], TailCall: true)
                );
            return f;
        }

        private ASTFunction AddOne()
        {
            var f = new ASTFunction("addOne", _types, parameters: [("a", Types.TypeId.Int)]);
            _globalScope.AddFunction(f);
            var oneFloatNode = new ASTConst(Value.FromFloat(1.2));
            var leftNode = new ASTCastFloatToInt(oneFloatNode);
            var rightNode = new ASTReadLocalVar(f.FindVariable("a")!);
            var addNode = new ASTCallByName("+", [leftNode, rightNode]);
            f.Body = new ASTReturn(addNode);
            return f;
        }
    }
}
