// See https://aka.ms/new-console-template for more information
using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Reflection;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using FastExpressionCompiler;

BenchmarkRunner.Run<ExpressionBenchmarks>();

public class ExpressionBenchmarks
{
    const int opcount = 10000;
    static int callcount = 0;

    MethodInfo cachedmethod;
    object[] cachedargs;

    Action compiledopcountloopexpression;
    Action fastcompiledloopexpression;

    public ExpressionBenchmarks()
    {

        cachedmethod = typeof(ExpressionBenchmarks).GetMethod("DoNothingUsefulFunction")!;
        cachedargs = new object[] { 1 };

        // expression loop varaible
        var i = Expression.Variable(typeof(int), "i");

        // outside loop label
        var looplabel = Expression.Label("looplabel");

        // do all the looping inside of the expression itself
        var loop =
            Expression.Loop(
                Expression.Block(
                    // break if i is 0
                    Expression.IfThen(Expression.Equal(i, Expression.Constant(0)), Expression.Break(looplabel)),
                    // decrement i
                    Expression.PostDecrementAssign(i),
                    Expression.Call(
                        typeof(ExpressionBenchmarks).GetMethod("DoNothingUsefulFunction")!,
                        Expression.Constant(1)
                    )
                ), // end block  
                looplabel
          );

        var codeblock = Expression.Block(
            new[] { i }, Expression.Assign(i, Expression.Constant(opcount)),
             loop);

        var opcountloopexpression = Expression.Lambda<Action>(codeblock);
        compiledopcountloopexpression = opcountloopexpression.Compile(false);

        fastcompiledloopexpression = opcountloopexpression.CompileFast();        
    }

    [Benchmark(OperationsPerInvoke = opcount)]
    public void DirectCall()
    {
        var before = callcount;
        DirectCallInvoke();
        if (before + opcount != callcount) throw new Exception("Didn't actually call");
    }

    private static void DirectCallInvoke()
    {
        for (int i = 0; i < opcount; ++i)
        {
            DoNothingUsefulFunction(1);
        }
    }

    [Benchmark(OperationsPerInvoke = opcount)]
    public void ReflectionCall()
    {
        var before = callcount;
        for (int i = 0; i < opcount; ++i)
        {
            cachedargs[0] = 1;
            cachedmethod.Invoke(null, cachedargs);
        }
        if (before + opcount != callcount) throw new Exception("Didn't actually call");
    }

    [Benchmark(OperationsPerInvoke = opcount)]
    public void CompiledLoopingExpression()
    {
        var before = callcount;
        compiledopcountloopexpression();
        if (before + opcount != callcount) throw new Exception("Didn't actually call");
    }

      [Benchmark(OperationsPerInvoke = opcount)]
    public void FastCompiledLoopingExpression()
    {
        var before = callcount;
        fastcompiledloopexpression();
        if (before + opcount != callcount) throw new Exception("Didn't actually call");
    }

    public static int DoNothingUsefulFunction(int input)
    {
        callcount++;
        return callcount;
    }
}


