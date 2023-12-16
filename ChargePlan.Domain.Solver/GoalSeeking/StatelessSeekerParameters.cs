namespace ChargePlan.Domain.Solver.GoalSeeking;

/// <summary>
/// Goal seek by adjusting a model to arrive at a goal.
/// This is achieved by having a model "parameter" which begins at startValue and is then automatically adjusted up/down each iteration based on distance to the goal.
/// This overload provides for the model to be adjusted iteration to iteration.
/// </summary>
/// <typeparam name="TModel"></typeparam>
/// <param name="goal">Goal value to aim for</param>
/// <param name="startValue">Start value for the model parameter</param>
/// <param name="createInitialModel">Function to create the initial model from the parameter</param>
/// <param name="executeModel">Execute the model and produce the score for the run</param>
/// <param name="reviseModel">Revise a model based on the current value, using the new parameter</param>
/// <returns></returns>
public record StatefulSeekerParameters<TModel>(
    double goal,
    double startValue,
    Func<double, TModel> createInitialModel,
    Func<TModel, double> executeModel,
    Func<double, TModel, TModel> reviseModel
);
