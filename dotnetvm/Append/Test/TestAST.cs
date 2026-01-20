using Append.AST;
using Append.Parsing;
using System.Diagnostics;

namespace Append.Test
{
    internal class TestAST : VMApplication
    {
        public void DoTest()
        {
            TestFactorialFunctionWhileTrueNoReturn();
            TestFactorialFunctionWhileComparaisonWithReturn();
            TestFactorialFunctionRecursive();
            TestFactorialFunctionRecursiveAccumulationTailCall();
            TestFactorialFunctionRecursiveTailCall();
            TestFloatToInt();
            TestGlobalVar();

            Console.WriteLine("TestAST ok");
        }

        private void TestFactorialFunctionWhileTrueNoReturn()
        {
            Reset();
            var f = new ASTFunction("factorialNoReturn",
                parameters: [("N", "int")],
                body: new ASTBlock([
                    new ASTDefineVar("result", "int", InitialValue: new ASTConst(Value.FromInt(1L))),
                    new ASTLoop(
                        new ASTConst(Value.FromBool(true)),
                        new ASTBlock([
                            new ASTWriteVar("result", new ASTCall("*", [new ASTReadVar("result"), new ASTReadVar("N")])),
                            new ASTWriteVar("N", new ASTCall("-", [new ASTReadVar("N"), new ASTConst(Value.FromInt(1L))])),
                            new ASTIf(
                                new ASTCall("<=", [new ASTReadVar("N"), new ASTConst(Value.FromInt(1L))]),
                                new ASTBreak())
                        ])),
                    new ASTReadVar("result")
                    ])
                );
            SetMain(new ASTBlock([
                f,
                new ASTCall(f.Name, [new ASTConst(Value.FromInt(6L))])
            ]));
            var result = ParseCompileAndRun();
            Debug.Assert(result.TypeId == Types.TypeId.Int && result.Data.Int == 720);
            Debug.Assert(ProgramToString() ==
@"func N: int 'factorialNoReturn' {
  var result: int = 1
  loop {
    result := result * N
    N := N - 1
    N <= 1 ? break
  }
  result
}
6 factorialNoReturn");
        }

        private Value ParseCompileAndRun()
        {
            var p = new Parser(ProgramToString());
            p.Parse();
            return CompileAndRun();
        }

        private void TestFactorialFunctionWhileComparaisonWithReturn()
        {
            Reset();
            var f = new ASTFunction("factorialWhileComp",
                parameters: [("N", "int")],
                body: new ASTBlock([
                    new ASTDefineVar("result", "int"),
                    new ASTWriteVar("result", new ASTConst(Value.FromInt(1L))),
                    new ASTLoop(
                        new ASTCall(">", [new ASTReadVar("N"), new ASTConst(Value.FromInt(1L))]),
                        new ASTBlock([
                            new ASTWriteVar("result", new ASTCall("*", [new ASTReadVar("result"), new ASTReadVar("N")])),
                            new ASTWriteVar("N", new ASTCall("-", [new ASTReadVar("N"), new ASTConst(Value.FromInt(1L))]))
                        ])),
                    new ASTReturn(new ASTReadVar("result"))
                    ])
                );
            SetMain(new ASTBlock([
                f,
                new ASTResolvedCall(f, [new ASTConst(Value.FromInt(6L))])
            ]));
            var result = ParseCompileAndRun();
            Debug.Assert(result.TypeId == Types.TypeId.Int && result.Data.Int == 720);
            Debug.Assert(ProgramToString() ==
@"func N: int 'factorialWhileComp' {
  var result: int
  result := 1
  N > 1 loop {
    result := result * N
    N := N - 1
  }
  return result
}
6 factorialWhileComp");
        }

        private void TestFactorialFunctionRecursive()
        {
            Reset();
            var f = new ASTFunction("factorialRec",
                parameters: [("N", "int")],
                body: new ASTIfElse(
                    new ASTCall("<=", [new ASTReadVar("N"), new ASTConst(Value.FromInt(1L))]),
                    new ASTConst(Value.FromInt(1L)),
                    new ASTCall("*",
                        [new ASTCall("factorialRec", [new ASTCall("-", [new ASTReadVar("N"), new ASTConst(Value.FromInt(1L))])]),
                        new ASTReadVar("N")]))
                );
            SetMain(
                new ASTBlock([f, new ASTResolvedCall(f, [new ASTConst(Value.FromInt(6L))])])
            );
            var result = ParseCompileAndRun();
            Debug.Assert(result.TypeId == Types.TypeId.Int && result.Data.Int == 720);
            Debug.Assert(ProgramToString() ==
@"func N: int 'factorialRec' => N <= 1 ? 1 else (N - 1) factorialRec * N
6 factorialRec");
        }

        private Value TestFactorialFunctionRecursiveAccumulationTailCall()
        {
            Value result;
            Reset();
            var f = FactorialFunctionRecursiveAccumulationTailCall();
            SetMain(new ASTMain([
                f,
                new ASTResolvedCall(f,
                [new ASTConst(Value.FromInt(6)), new ASTConst(Value.FromInt(1L))])
                ]));
            result = CompileAndRun();
            Debug.Assert(result.TypeId == Types.TypeId.Int && result.Data.Int == 720);
            Debug.Assert(ProgramToString() ==
@"func acc: int 'factorialTail' N: int
  => N <= 1 ? acc else (N - 1) factorialTail (acc * N)
6 factorialTail 1");
            return result;
        }

        private static ASTFunction FactorialFunctionRecursiveAccumulationTailCall()
            => new("factorialTail", 
                parameters: [("N", "int"), ("acc", "int")],
                body: new ASTIfElse(
                    new ASTCall("<=", [new ASTReadVar("N"), new ASTConst(Value.FromInt(1L))]),
                    new ASTReadVar("acc"),
                    new ASTCall("factorialTail", [
                        new ASTCall("-", [new ASTReadVar("N"), new ASTConst(Value.FromInt(1L))]),
                        new ASTCall("*", [new ASTReadVar("acc"), new ASTReadVar("N")])
                        ], TailCall: true))
                );

        private void TestFactorialFunctionRecursiveTailCall()
        {
            Reset();
            var f = FactorialFunctionRecursiveTailCall();
            SetMain(new ASTMain([
                f,
                new ASTResolvedCall(f, [new ASTConst(Value.FromInt(6L))])
            ]));
            var result = ParseCompileAndRun();
            Debug.Assert(result.TypeId == Types.TypeId.Int && result.Data.Int == 720);
        }

        private static ASTFunction FactorialFunctionRecursiveTailCall()
        {
            var accumulation = FactorialFunctionRecursiveAccumulationTailCall();
            return new("factorialProxy",
                parameters: [("N", "int")],
                body: new ASTBlock([
                    accumulation,
                    new ASTCall(accumulation.Name, [new ASTReadVar("N"), new ASTConst(Value.FromInt(1L))])
                    ])
                );
        }

        private void TestFloatToInt()
        {
            Reset();
            var addOneFunc = AddOne();
            var callNode = new ASTResolvedCall(addOneFunc, [new ASTConst(Value.FromInt(3L))]);
            var compNode = new ASTCall("<", [callNode, new ASTConst(Value.FromInt(8L))]);
            SetMain(new ASTMain([
                addOneFunc,
                compNode
                ]));
            var result = ParseCompileAndRun();
            Debug.Assert(result.TypeId == Types.TypeId.Bool && result.Data.Bool == true);
        }

        private static ASTFunction AddOne()
            => new("addOne", 
                parameters: [("a", "int")],
                body: new ASTReturn(
                        new ASTCall(
                            "+",
                            [new ASTCastFloatToInt(
                                new ASTConst(Value.FromFloat(1.2))),
                            new ASTReadVar("a")]))
                );

        private void TestGlobalVar()
        {
            Reset();
            SetMain(new ASTBlock([
                new ASTDefineVar("a", "int"),
                new ASTFunction(
                    "f",
                    body: new ASTWriteVar("a", new ASTConst(Value.FromInt(42)))
                ),
                new ASTCall("f", []),
                new ASTReadVar("a")
            ]));
            var result = ParseCompileAndRun();
            Debug.Assert(result.TypeId == Types.TypeId.Int && result.Data.Int == 42);
        }
    }
}
