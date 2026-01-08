using Append.AST;
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
            var f = new ASTFunction("factorialNoReturn", _types,
                parameters: [("N", Types.TypeId.Int)],
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
            var result = CompileAndRun();
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

        private void TestFactorialFunctionWhileComparaisonWithReturn()
        {
            Reset();
            var f = new ASTFunction("factorialWhileComp", _types,
                parameters: [("N", Types.TypeId.Int)],
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
            var result = CompileAndRun();
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
            var f = new ASTFunction("factorialRec", _types,
                parameters: [("N", Types.TypeId.Int)],
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
            var result = CompileAndRun();
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

        private ASTFunction FactorialFunctionRecursiveAccumulationTailCall()
            => new("factorialTail", _types,
                parameters: [("N", Types.TypeId.Int), ("acc", Types.TypeId.Int)],
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
            var result = CompileAndRun();
            Debug.Assert(result.TypeId == Types.TypeId.Int && result.Data.Int == 720);
        }

        private ASTFunction FactorialFunctionRecursiveTailCall()
        {
            var accumulation = FactorialFunctionRecursiveAccumulationTailCall();
            return new("factorialProxy", _types,
                parameters: [("N", Types.TypeId.Int)],
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
            var result = CompileAndRun();
            Debug.Assert(result.TypeId == Types.TypeId.Bool && result.Data.Bool == true);
        }

        private ASTFunction AddOne()
            => new("addOne", _types,
                parameters: [("a", Types.TypeId.Int)],
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
                    "f", _types,
                    body: new ASTWriteVar("a", new ASTConst(Value.FromInt(42)))
                ),
                new ASTCall("f", []),
                new ASTReadVar("a")
            ]));
            var result = CompileAndRun();
            Debug.Assert(result.TypeId == Types.TypeId.Int && result.Data.Int == 42);
        }
    }
}
