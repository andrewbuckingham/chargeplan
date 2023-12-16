namespace ChargePlan.Domain.Solver.GoalSeeking;

/// <summary>
/// Goal seek by adjusting a model to arrive at a goal.
/// This is achieved by having a model "parameter" which begins at startValue and is then automatically adjusted up/down each iteration based on distance to the goal.
/// This overload is for where the model is recreated each iteration with no prior knowledge.
/// </summary>
/// <typeparam name="TModel"></typeparam>
/// <param name="goal">Goal value to aim for</param>
/// <param name="startValue">Start value for the model parameter</param>
/// <param name="createModel">Function to create the model from the parameter</param>
/// <param name="executeModel">Execute the model and produce the score for the run</param>
/// <returns></returns>
public record StatelessSeekerParameters<TModel>(
    double goal,
    double startValue,
    Func<double, TModel> createModel,
    Func<TModel, double> executeModel
)
{
    public StatefulSeekerParameters<TModel> AsStateful() => new(
        goal,
        startValue,
        createModel,
        executeModel,
        (value, existing) => createModel(value));
}
