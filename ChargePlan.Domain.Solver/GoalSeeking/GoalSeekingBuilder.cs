using System.Runtime.CompilerServices;

namespace ChargePlan.Domain.Solver.GoalSeeking;

public record GoalSeekingBuilder(
)
{
    public GoalSeekingBuilderContinuation<TModel> Seek<TModel>(StatefulSeekerParameters<TModel> parameters)
        => new(() => new BinaryDivisionSeeker().Iterations(parameters));
}

public record GoalSeekingBuilderContinuation<TModel1>(
    Func<IEnumerable<(double DeltaToGoal, TModel1 Model)>> Seek1
)
{
    public IEnumerable<(double DeltaToGoal, TModel1 Model)> Run()
        => Seek1();

    public GoalSeekingBuilderContinuation<TModel1, TModel2> ForEachSeek<TModel2>(
            Func<TModel1, StatefulSeekerParameters<TModel2>> model2paramsFromModel1,
            Func<IEnumerable<(double DeltaToGoal, TModel2 Model)>, IEnumerable<(double DeltaToGoal, TModel2 Model)>> limiter)
        => new GoalSeekingBuilderContinuation<TModel1, TModel2>(
            Seek1,
            (model1Value) => new BinaryDivisionSeeker().Iterations(model2paramsFromModel1(model1Value)),
            limiter
        );
}

public record GoalSeekingBuilderContinuation<TModel1, TModel2>(
    Func<IEnumerable<(double DeltaToGoal, TModel1 Model)>> Seek1,
    Func<TModel1, IEnumerable<(double DeltaToGoal, TModel2 Model)>> Seek2,
    Func<IEnumerable<(double DeltaToGoal, TModel2 Model)>, IEnumerable<(double DeltaToGoal, TModel2 Model)>> Seek2Limiter
)
{
    public IEnumerable<(
            (double DeltaToGoal, TModel1 Model) Model1,
            (double DeltaToGoal, TModel2 Model) Model2
            )> Run()
    {
        // var s1Run = Seek1();

        // var s2Run = s1Run.SelectMany(seek1Result => {
        //     var s2Run = Seek2(seek1Result.Model);
        //     return s2Run.Select(seek2Result => (Model1: seek1Result, Model2: seek2Result));
        // });

        var results = Seek1()
            .SelectMany(seek1Result => Seek2Limiter(Seek2(seek1Result.Model))
            .Select(seek2Result => (Model1: seek1Result, Model2: seek2Result)));

        return results;
    }

    public GoalSeekingBuilderContinuation<TModel1, TModel2, TModel3> ForEachSeek<TModel3>(
            Func<TModel2, StatefulSeekerParameters<TModel3>> model3paramsFromModel2,
            Func<IEnumerable<(double DeltaToGoal, TModel3 Model)>, IEnumerable<(double DeltaToGoal, TModel3 Model)>> limiter)
        => new GoalSeekingBuilderContinuation<TModel1, TModel2, TModel3>(
            Seek1,
            Seek2,
            (model2Value) => new BinaryDivisionSeeker().Iterations(model3paramsFromModel2(model2Value)),
            Seek2Limiter,
            limiter
        );
}

public record GoalSeekingBuilderContinuation<TModel1, TModel2, TModel3>(
    Func<IEnumerable<(double DeltaToGoal, TModel1 Model)>> Seek1,
    Func<TModel1, IEnumerable<(double DeltaToGoal, TModel2 Model)>> Seek2,
    Func<TModel2, IEnumerable<(double DeltaToGoal, TModel3 Model)>> Seek3,
    Func<IEnumerable<(double DeltaToGoal, TModel2 Model)>, IEnumerable<(double DeltaToGoal, TModel2 Model)>> Seek2Limiter,
    Func<IEnumerable<(double DeltaToGoal, TModel3 Model)>, IEnumerable<(double DeltaToGoal, TModel3 Model)>> Seek3Limiter
)
{
    public IEnumerable<(
            (double DeltaToGoal, TModel1 Model) Model1,
            (double DeltaToGoal, TModel2 Model) Model2,
            (double DeltaToGoal, TModel3 Model) Model3
            )> Run()
    {
        var results = Seek1()
            .SelectMany(seek1Result => Seek2(seek1Result.Model)
            .SelectMany(seek2Result => Seek3(seek2Result.Model)
            .Select(seek3Result => (Model1: seek1Result, Model2: seek2Result, Model3: seek3Result))));

        return results;
    }
}