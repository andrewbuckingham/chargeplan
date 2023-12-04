using System.Diagnostics;

namespace ChargePlan.Domain.Solver.GoalSeeking;

/// <summary>
/// 
/// </summary>
/// <param name="MaxIterations">Absolute max iterations when binary seeking. A more convenient way is to use .Take on the result</param>
/// <param name="ModelValueLimiter">Function to limit the model value (optional)</param>
internal record BinaryDivisionSeeker(
    int MaxIterations = 64,
    Func<double, double>? ModelValueLimiter = null
) : IGoalSeeker
{
    public IEnumerable<(double DeltaToGoal, TModel Model)> Iterations<TModel>(
        double goal,
        double startValue,
        Func<double, TModel> createModel,
        Func<TModel, double> executeModel)
        => Iterations(
            goal,
            startValue,
            createModel,
            executeModel,
            (value, existing) => createModel(value));

    public IEnumerable<(double DeltaToGoal, TModel Model)> Iterations<TModel>(
        double goal,
        double startValue,
        Func<double, TModel> createInitialModel,
        Func<TModel, double> executeModel,
        Func<double, TModel, TModel> reviseModel
        )
    {
        // Model starts from this point.
        double paramValue = startValue;

        // Next time around the loop, we'll adjust the threshold up or down by half of the current value.
        double nextAdjustmentAbs = paramValue / 2.0f;
        double previousAdjustmentDirection = -1.0;
        double nextAdjustmentDirection = -1.0;
        
        // Which direction should we move in?
        double? previousDelta = null;

        // Create model for current param value.
        TModel model = createInitialModel(paramValue);

        // Iterate
        for (int option = 0; option < MaxIterations; option++)
        {           
            // Find out how close the current model is to the goal
            double yielded = executeModel(model);
            double delta = yielded - goal;
            var result = (delta, model);
            yield return result;

            // Next time around the loop, modify the charge power up/down depending if there was too little/much energy acquired.
            // Or, if the delta is getting worse, then that's another reason to flip the direction.
            if (previousDelta != null)
            {
                bool hasOvershotGoal = Math.Sign(delta) != Math.Sign(previousDelta.Value);
                bool hasGotFurtherFromGoal = Math.Abs(delta) > Math.Abs(previousDelta.Value);

                if (hasOvershotGoal || hasGotFurtherFromGoal)
                {
                    // Have crossed the threshold between under- and over-shooting. Change the direction, and also move to a finer model adjustment for next time.
                    // Or the delta is getting worse, and relationship between Value and Result is the other way around.
                    nextAdjustmentDirection *= -1.0;
                    nextAdjustmentAbs /= 2;
                }
            }

            // Prepare the next model parameter value.
            paramValue += nextAdjustmentAbs * nextAdjustmentDirection;

            if (ModelValueLimiter != null)
            {
                paramValue = ModelValueLimiter(paramValue);
            }

            previousAdjustmentDirection = nextAdjustmentDirection;
            previousDelta = delta;

            // Prepare the next model.
            model = reviseModel(paramValue, model);
        }
    }
}