using System.Collections.Generic;
using AblazeForge.DirectiveNetcode.Messaging;

/// <summary>
/// Represents a generic message processing pipeline that executes a sequence of pipeline steps.
/// This class manages a collection of pipeline steps and executes them in order to process messages.
/// </summary>
/// <typeparam name="T">The type of pipeline steps this pipeline executes, which must implement <see cref="IPipelineStep{TMessageParams}"/>.</typeparam>
/// <typeparam name="TMessageParams">The type of message parameters processed by the pipeline steps.</typeparam>
public class MessagePipeline<T, TMessageParams> where T : IPipelineStep<TMessageParams>
{
    /// <summary>
    /// The list of pipeline steps to be executed in sequence.
    /// </summary>
    protected List<T> Steps = new();

    /// <summary>
    /// Adds multiple pipeline steps to the pipeline.
    /// </summary>
    /// <param name="steps">The pipeline steps to add.</param>
    public void AddSteps(params T[] steps)
    {
        Steps.AddRange(steps);
    }

    /// <summary>
    /// Adds a single pipeline step to the pipeline.
    /// </summary>
    /// <param name="step">The pipeline step to add.</param>
    public void AddStep(T step)
    {
        Steps.Add(step);
    }

    /// <summary>
    /// Executes all pipeline steps in sequence with the specified message parameters.
    /// Execution stops if any step returns a result other than <see cref="PipelineStepResult.Success"/>.
    /// </summary>
    /// <param name="messageParams">The message parameters to pass to each pipeline step.</param>
    /// <returns>A <see cref="PipelineResult"/> indicating the overall result of the pipeline execution.</returns>
    protected PipelineResult ExecuteSteps(TMessageParams messageParams)
    {
        PipelineStepResult pipelineStepResult = PipelineStepResult.Success;

        foreach (IPipelineStep<TMessageParams> step in Steps)
        {
            pipelineStepResult = step.Execute(messageParams);

            if (pipelineStepResult != PipelineStepResult.Success)
            {
                break;
            }
        }

        return GetPipelineResultFromStepResult(pipelineStepResult);
    }

    /// <summary>
    /// Converts a pipeline step result to a pipeline result.
    /// </summary>
    /// <param name="pipelineStepResult">The pipeline step result to convert.</param>
    /// <returns>The corresponding pipeline result.</returns>
    private PipelineResult GetPipelineResultFromStepResult(PipelineStepResult pipelineStepResult)
    {
        return pipelineStepResult switch
        {
            PipelineStepResult.Success => PipelineResult.Success,
            PipelineStepResult.Failure => PipelineResult.DiscardMessage,
            PipelineStepResult.DisconnectClient => PipelineResult.DisconnectClient,
            _ => PipelineResult.DiscardMessage
        };
    }
}
