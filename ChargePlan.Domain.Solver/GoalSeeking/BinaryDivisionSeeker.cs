namespace ChargePlan.Domain.Solver.GoalSeeking;

/// <summary>
/// 
/// </summary>
/// <param name="MaxIterations">Max iterations when binary seeking</param>
/// <param name="ModelValueLimiter">Function to limit the model value (optional)</param>
internal record BinaryDivisionSeeker(
    int MaxIterations = 8,
    Func<double, double>? ModelValueLimiter = null
) : IGoalSeeker
{
    public IEnumerable<(double DeltaToGoal, TModel Model)> Iterations<TModel>(
        double goal,
        double startModelValue,
        Func<double, TModel> createModel,
        Func<TModel, double> executeModel)
    {
        // Model starts from this point.
        double paramValue = startModelValue;

        // Next time around the loop, we'll adjust the threshold up or down by half of the current value.
        double nextAdjustmentAbs = paramValue / 2.0f;
        double previousAdjustmentDirection = -1.0;

        // Iterate
        for (int option = 0; option < MaxIterations; option++)
        {
            // Create model for current param value.
            TModel model = createModel(paramValue);

            // Find out how close the current model is to the goal
            double yielded = executeModel(model);
            var result = (yielded - goal, model);
            yield return result;

            // Next time around the loop, modify the charge power up/down depending if there was too little/much energy acquired.
            double nextAdjustmentDirection = yielded - goal > 0.0 ? -1.0f : 1.0f;

            // Use a finer model param adjustment next time, if we've crossed the threshold between under/overshooting.
            if (previousAdjustmentDirection != nextAdjustmentDirection)
            {
                nextAdjustmentAbs /= 2;
            }

            // Prepare the next model parameter value.
            paramValue += nextAdjustmentAbs * nextAdjustmentDirection;

            if (ModelValueLimiter != null)
            {
                paramValue = ModelValueLimiter(paramValue);
            }

            previousAdjustmentDirection = nextAdjustmentDirection;
        }
    }
}