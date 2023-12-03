namespace ChargePlan.Domain.Solver.GoalSeeking;

internal interface IGoalSeeker
{
    IEnumerable<(double DeltaToGoal, TModel Model)> Iterations<TModel>(
        double goal,
        double startModelValue,
        Func<double, TModel> createModel,
        Func<TModel, double> executeModel);
}