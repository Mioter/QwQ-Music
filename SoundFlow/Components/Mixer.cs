using System;
using System.Collections.Generic;
using SoundFlow.Abstracts;

namespace SoundFlow.Components;

/// <summary>
///     Represents an audio mixer that combines and processes audio from multiple SoundComponents.
/// </summary>
public sealed class Mixer : SoundComponent
{
    private readonly List<SoundComponent> _components = [];
    private readonly object _lock = new();

    /// <summary>
    ///     Gets the master mixer, representing the final output of the audio graph.
    /// </summary>
    public static Mixer Master { get; } = new();

    /// <inheritdoc />
    public override string Name { get; set; } = "Mixer";

    /// <summary>
    ///     Adds a sound component to the mixer.
    /// </summary>
    /// <param name="component">The sound component to add.</param>
    /// <exception cref="ArgumentException">
    ///     Thrown if the component is the mixer itself or if adding the component would create
    ///     a cycle in the graph.
    /// </exception>
    public void AddComponent(SoundComponent component)
    {
        if (component == this) throw new ArgumentException("Cannot add a mixer to itself.", nameof(component));

        // Check for cycles
        if (WouldCreateCycle(component))
            throw new ArgumentException("Adding this component would create a cycle in the audio graph.",
                nameof(component));

        lock (_lock)
        {
            if (_components.Contains(component)) return;
            _components.Add(component);
            component.Parent = this;
        }
    }

    /// <summary>
    ///     Checks if adding a component to this mixer would create a cycle in the audio graph.
    /// </summary>
    /// <param name="component">The component to check.</param>
    /// <returns>True if adding the component would create a cycle, false otherwise.</returns>
    private bool WouldCreateCycle(SoundComponent component)
    {
        var current = Parent;
        while (current != null)
        {
            if (current == component)
                return true;

            current = current.Parent;
        }

        return false;
    }

    /// <summary>
    ///     Removes a sound component from the mixer.
    /// </summary>
    /// <param name="component">The sound component to remove.</param>
    public void RemoveComponent(SoundComponent component)
    {
        lock (_lock)
        {
            if (_components.Remove(component))
                component.Parent = null;
        }
    }

    /// <inheritdoc />
    protected override void GenerateAudio(Span<float> buffer)
    {
        if (!Enabled || Mute) return;

        lock (_lock)
        {
            foreach (var component in _components)
                if (component is { Enabled: true, Mute: false })
                    component.Process(buffer);
        }
    }
}