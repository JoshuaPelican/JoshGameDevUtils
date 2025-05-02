using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;

/// <summary>
/// A static class for processing numerical values through a series of operations.
/// Supports generic numeric types and event-driven processing.
/// </summary>
public static class ProcessManager
{
    /// <summary>
    /// Event invoked to apply processing logic to a value.
    /// </summary>
    public static UnityEvent<ProcessorData> ProcessEvent = new UnityEvent<ProcessorData>();

    /// <summary>
    /// Processes a base value using event-driven logic and returns the result.
    /// </summary>
    /// <typeparam name="T">The numeric type of the value to process (must be a struct and IConvertible).</typeparam>
    /// <param name="baseValue">The initial value to process.</param>
    /// <param name="target">The object associated with the processing (e.g., the entity being processed).</param>
    /// <param name="processType">The type of process to apply.</param>
    /// <returns>A ProcessResult containing the processed value and the list of applied processes.</returns>
    public static ProcessResult<T> Process<T>(T baseValue, object target, ProcessType processType)
        where T : struct, IConvertible
    {
        float floatValue = Convert.ToSingle(baseValue);

        var processor = new ValueProcessor(floatValue);
        var data = new ProcessorData(target, processType, processor);
        ProcessEvent.Invoke(data);

        processor.GetFinalValue();
        T processedValue = (T)Convert.ChangeType(processor.FinalValue, typeof(T));

        return new ProcessResult<T>(processedValue, processor.Processes);
    }
}

/// <summary>
/// Data structure containing information about a processing operation.
/// </summary>
public struct ProcessorData
{
    /// <summary>
    /// The object associated with the processing (e.g., the entity being processed).
    /// </summary>
    public object Target;

    /// <summary>
    /// The type of process to apply.
    /// </summary>
    public ProcessType ProcessType;

    /// <summary>
    /// The processor handling the value modifications.
    /// </summary>
    public ValueProcessor Processor;

    /// <summary>
    /// Initializes a new instance of the ProcessorData struct.
    /// </summary>
    /// <param name="target">The object associated with the processing.</param>
    /// <param name="processType">The type of process to apply.</param>
    /// <param name="processor">The processor handling the value modifications.</param>
    public ProcessorData(object target, ProcessType processType, ValueProcessor processor)
    {
        Target = target;
        ProcessType = processType;
        Processor = processor;
    }
}

/// <summary>
/// Enumeration of process types that can be applied to a value.
/// </summary>
public enum ProcessType
{
    GenericProcess1,
    GenericProcess2,
    GenericProcess3,
    // Add more as needed for specific use cases
}

/// <summary>
/// A class for processing a float value through a series of operations.
/// </summary>
public class ValueProcessor
{
    /// <summary>
    /// Gets the initial value before processing.
    /// </summary>
    public float BaseValue { get; }

    /// <summary>
    /// Gets the list of processes applied to the value.
    /// </summary>
    public List<Process> Processes;

    /// <summary>
    /// Gets the final processed value after applying all processes.
    /// </summary>
    public float FinalValue { get; private set; }

    /// <summary>
    /// Initializes a new instance of the ValueProcessor class.
    /// </summary>
    /// <param name="baseValue">The initial value to process.</param>
    public ValueProcessor(float baseValue)
    {
        BaseValue = baseValue;
        Processes = new List<Process>();
    }

    /// <summary>
    /// Adds a process to the list of operations to apply.
    /// </summary>
    /// <param name="source">The source object initiating the process.</param>
    /// <param name="operation">The operation to apply.</param>
    /// <param name="value">The value to use in the operation.</param>
    public void AddProcess(object source, Operation operation, float value)
    {
        AddProcess(new Process(source, operation, value));
    }

    /// <summary>
    /// Adds a process to the list of operations to apply.
    /// </summary>
    /// <param name="process">The process to add.</param>
    public void AddProcess(Process process)
    {
        Processes.Add(process);
    }

    /// <summary>
    /// Calculates the final value by applying all processes in order.
    /// </summary>
    public void GetFinalValue()
    {
        float finalValue = BaseValue;
        foreach (var process in Processes.OrderBy(x => x.Operation))
        {
            finalValue = ApplyProcess(finalValue, process);
        }
        FinalValue = finalValue;
    }

    private float ApplyProcess(float value, Process process)
    {
        return process.Operation switch
        {
            Operation.Set => process.Value,
            Operation.Multiply => value * process.Value,
            Operation.Divide => value / process.Value,
            Operation.Add => value + process.Value,
            Operation.Subtract => value - process.Value,
            _ => value,
        };
    }
}

/// <summary>
/// Represents a single processing operation.
/// </summary>
public readonly struct Process
{
    /// <summary>
    /// The source object initiating the process.
    /// </summary>
    public readonly object Source;

    /// <summary>
    /// The operation to apply.
    /// </summary>
    public readonly Operation Operation;

    /// <summary>
    /// The value to use in the operation.
    /// </summary>
    public readonly float Value;

    /// <summary>
    /// Initializes a new instance of the Process struct.
    /// </summary>
    /// <param name="source">The source object initiating the process.</param>
    /// <param name="operation">The operation to apply.</param>
    /// <param name="value">The value to use in the operation.</param>
    public Process(object source, Operation operation, float value)
    {
        Source = source;
        Operation = operation;
        Value = value;
    }
}

/// <summary>
/// Represents the result of a processing operation.
/// </summary>
/// <typeparam name="T">The type of the processed value.</typeparam>
public readonly struct ProcessResult<T>
{
    /// <summary>
    /// Gets the processed value.
    /// </summary>
    public readonly T ProcessedValue { get; }

    /// <summary>
    /// Gets a value indicating whether the value was changed by any processes.
    /// </summary>
    public readonly bool WasChanged => Processes.Count > 0; // Fixed logic: true if processes were applied

    /// <summary>
    /// Gets the list of processes applied to the value.
    /// </summary>
    public readonly List<Process> Processes { get; }

    /// <summary>
    /// Initializes a new instance of the ProcessResult struct.
    /// </summary>
    /// <param name="processedValue">The processed value.</param>
    /// <param name="processes">The list of processes applied.</param>
    public ProcessResult(T processedValue, List<Process> processes)
    {
        ProcessedValue = processedValue;
        Processes = processes;
    }
}

/// <summary>
/// Enumeration of operations that can be applied to a value.
/// </summary>
public enum Operation
{
    Set,
    Multiply,
    Divide,
    Add,
    Subtract
}