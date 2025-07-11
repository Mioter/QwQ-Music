﻿using System.Numerics;
using SoundFlow.Interfaces;

namespace SoundFlow.Visualization;

/// <summary>
/// Visualizes audio data as a waveform.
/// </summary>
public class WaveformVisualizer : IVisualizer
{
    /// <inheritdoc />
    public string Name => "Waveform Visualizer";

    /// <summary>
    /// Gets the waveform data.
    /// </summary>
    public List<float> Waveform { get; } = [];

    /// <summary>
    /// Gets or sets the color of the waveform.
    /// </summary>
    public Color WaveformColor
    {
        get => _waveformColor;
        set
        {
            _waveformColor = value;
            VisualizationUpdated?.Invoke(this, EventArgs.Empty);
        }
    }

    private Color _waveformColor = new(0, 1, 0); // Default green

    /// <summary>
    /// Gets the size of the waveform visualization in pixels (X - width, Y - height).
    /// </summary>
    public static Vector2 Size => new(800, 200);

    /// <inheritdoc/>
    public void ProcessOnAudioData(Span<float> audioData)
    {
        Waveform.Clear();
        Waveform.AddRange(audioData.ToArray());
        VisualizationUpdated?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public void Render(IVisualizationContext context)
    {
        context.Clear();

        if (Waveform.Count < 2)
        {
            return;
        }

        float midY = Size.Y / 2;
        float xStep = Size.X / (Waveform.Count - 1);

        for (int i = 0; i < Waveform.Count - 1; i++)
        {
            float x1 = i * xStep;
            float y1 = midY + Waveform[i] * midY;
            float x2 = (i + 1) * xStep;
            float y2 = midY + Waveform[i + 1] * midY;

            context.DrawLine(x1, y1, x2, y2, _waveformColor);
        }
    }

    /// <inheritdoc />
    public event EventHandler? VisualizationUpdated;

    /// <inheritdoc />
    public void Dispose() { }
}
