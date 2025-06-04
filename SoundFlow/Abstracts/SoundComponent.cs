using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using SoundFlow.Components;

namespace SoundFlow.Abstracts;

/// <summary>
///     Base class for audio processing components.
/// </summary>
public abstract class SoundComponent
{
    private static readonly ArrayPool<float> BufferPool = ArrayPool<float>.Shared;

    // Connection state
    private readonly List<SoundComponent> _inputs = [];
    private readonly List<SoundComponent> _outputs = [];
    private readonly object _connectionsLock = new();

    // Processing state
    private readonly List<SoundModifier> _modifiers = [];
    private readonly List<AudioAnalyzer> _analyzers = [];
    private float _pan = 0.5f;
    private bool _solo;
    private float _volume = 1f;
    private Vector2 _volumePanFactors;
    private Vector2 _previousVolumePanFactors;
    private readonly Lock _stateLock = new();

    /// <summary>
    ///     Name of the component
    /// </summary>
    public virtual string Name { get; set; } = "Component";

    /// <summary>
    ///     Parent mixer of the component
    /// </summary>
    public Mixer? Parent { get; set; } = Mixer.Master;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SoundComponent" /> class.
    /// </summary>
    protected SoundComponent()
    {
        UpdateVolumePanFactors();
        _previousVolumePanFactors = _volumePanFactors;
    }

    /// <summary>
    ///     Input connections
    /// </summary>
    public IReadOnlyList<SoundComponent> Inputs
    {
        get
        {
            lock (_connectionsLock)
                return new List<SoundComponent>(_inputs);
        }
    }

    /// <summary>
    ///     Output connections
    /// </summary>
    public IReadOnlyList<SoundComponent> Outputs
    {
        get
        {
            lock (_connectionsLock)
                return new List<SoundComponent>(_outputs);
        }
    }

    /// <summary>
    ///     Volume of the component
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the volume is negative.</exception>
    public virtual float Volume
    {
        get => _volume;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            lock (_stateLock)
            {
                _volume = value;
                UpdateVolumePanFactors();
            }
        }
    }

    /// <summary>
    ///     Pan of the component
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the pan is outside the range [0, 1].</exception>
    public virtual float Pan
    {
        get => _pan;
        set
        {
            if (value is < 0f or > 1f)
                throw new ArgumentOutOfRangeException(nameof(value), "Pan must be between 0.0 and 1.0.");
            lock (_stateLock)
            {
                _pan = value;
                UpdateVolumePanFactors();
            }
        }
    }

    /// <summary>
    ///     Whether the component is enabled
    /// </summary>
    public virtual bool Enabled { get; set; } = true;

    /// <summary>
    ///     Whether the component is soloed
    /// </summary>
    public virtual bool Solo
    {
        get => _solo;
        set
        {
            lock (_stateLock)
            {
                _solo = value;
                if (_solo)
                    AudioEngine.Instance.SoloComponent(this);
                else
                    AudioEngine.Instance.UnsoloComponent(this);
            }
        }
    }

    /// <summary>
    ///     Whether the component is muted
    /// </summary>
    public virtual bool Mute { get; set; }

    /// <summary>
    ///     Modifiers applied to the component
    /// </summary>
    public IReadOnlyList<SoundModifier> Modifiers
    {
        get
        {
            lock (_stateLock)
                return new List<SoundModifier>(_modifiers);
        }
    }

    /// <summary>
    ///     Analyzers applied to the component
    /// </summary>
    public IReadOnlyList<AudioAnalyzer> Analyzers
    {
        get
        {
            lock (_stateLock)
                return new List<AudioAnalyzer>(_analyzers);
        }
    }

    private void UpdateVolumePanFactors()
    {
        _previousVolumePanFactors = _volumePanFactors;
        float panValue = Math.Clamp(_pan, 0f, 1f);
        _volumePanFactors = new Vector2(_volume * MathF.Sqrt(1f - panValue), _volume * MathF.Sqrt(panValue));
    }

    /// <summary>
    ///     Connects this component to another component.
    /// </summary>
    /// <param name="input">The component to connect to.</param>
    /// <exception cref="InvalidOperationException">Thrown if the connection would create a cycle.</exception>
    public void ConnectInput(SoundComponent input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input == this)
            throw new InvalidOperationException("Cannot connect to self");

        SoundComponent first,
            second;
        if (GetHashCode() < input.GetHashCode())
        {
            first = this;
            second = input;
        }
        else
        {
            first = input;
            second = this;
        }

        lock (first._connectionsLock)
            lock (second._connectionsLock)
            {
                if (_inputs.Contains(input))
                    return;

                if (IsReachable(input, this))
                    throw new InvalidOperationException("Connection would create a cycle");

                _inputs.Add(input);
                input._outputs.Add(this);
            }
    }

    /// <summary>
    ///     Disconnects this component from another component.
    /// </summary>
    /// <param name="input">The component to disconnect from.</param>
    public void DisconnectInput(SoundComponent input)
    {
        lock (_connectionsLock)
        {
            if (!_inputs.Remove(input))
                return;

            lock (input._connectionsLock)
                input._outputs.Remove(this);
        }
    }

    private static bool IsReachable(SoundComponent start, SoundComponent target)
    {
        var visited = new HashSet<SoundComponent>();
        var queue = new Queue<SoundComponent>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == target)
                return true;
            if (!visited.Add(current))
                continue;

            List<SoundComponent> currentOutputs;
            lock (current._connectionsLock)
                currentOutputs = [.. current._outputs];

            foreach (var output in currentOutputs)
                queue.Enqueue(output);
        }

        return false;
    }

    /// <summary>
    ///     Adds a modifier to the component.
    /// </summary>
    /// <param name="modifier">The modifier to add.</param>
    public SoundComponent AddModifier(SoundModifier modifier)
    {
        lock (_stateLock)
        {
            if (!_modifiers.Contains(modifier))
                _modifiers.Add(modifier);
        }

        return this;
    }

    /// <summary>
    ///     Removes a modifier from the component.
    /// </summary>
    /// <param name="modifier">The modifier to remove.</param>
    public SoundComponent RemoveModifier(SoundModifier modifier)
    {
        lock (_stateLock)
            _modifiers.Remove(modifier);

        return this;
    }

    /// <summary>
    ///     Adds an analyzer to the component.
    /// </summary>
    /// <param name="analyzer">The analyzer to add.</param>
    public SoundComponent AddAnalyzer(AudioAnalyzer analyzer)
    {
        lock (_stateLock)
        {
            if (!_analyzers.Contains(analyzer))
                _analyzers.Add(analyzer);
        }

        return this;
    }

    /// <summary>
    ///     Removes an analyzer from the component.
    /// </summary>
    /// <param name="analyzer">The analyzer to remove.</param>
    public SoundComponent RemoveAnalyzer(AudioAnalyzer analyzer)
    {
        lock (_stateLock)
            _analyzers.Remove(analyzer);

        return this;
    }

    internal void Process(Span<float> outputBuffer)
    {
        if (!Enabled || Mute)
            return;

        float[]? rentedBuffer = null;
        try
        {
            rentedBuffer = BufferPool.Rent(outputBuffer.Length);
            var workingBuffer = rentedBuffer.AsSpan(0, outputBuffer.Length);
            workingBuffer.Clear();

            SoundComponent[] currentInputs;
            lock (_connectionsLock)
            {
                currentInputs = _inputs.Count == 0 ? [] : _inputs.ToArray();
            }

            foreach (var input in currentInputs)
                input.Process(workingBuffer);

            GenerateAudio(workingBuffer);

            SoundModifier[] currentModifiers;
            AudioAnalyzer[] currentAnalyzers;
            Vector2 currentVolumePan;

            lock (_stateLock)
            {
                currentModifiers = _modifiers.Count == 0 ? [] : _modifiers.ToArray();
                currentAnalyzers = _analyzers.Count == 0 ? [] : _analyzers.ToArray();

                currentVolumePan = Vector2.Lerp(
                    _previousVolumePanFactors,
                    _volumePanFactors,
                    Math.Clamp(128f / workingBuffer.Length, 0, 1)
                );
            }

            foreach (var modifier in currentModifiers)
                if (modifier.Enabled)
                    modifier.Process(workingBuffer);

            ApplyVolumeAndPanning(workingBuffer, currentVolumePan);

            MixBuffers(workingBuffer, outputBuffer);

            foreach (var analyzer in currentAnalyzers)
                analyzer.Process(workingBuffer);
        }
        finally
        {
            if (rentedBuffer != null)
                BufferPool.Return(rentedBuffer);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void MixBuffers(ReadOnlySpan<float> source, Span<float> destination)
    {
        if (source.Length != destination.Length)
            throw new ArgumentException("Source and destination buffers must have the same length.");

        // SIMD-accelerated mixing
        int count = 0;
        int simdLength = source.Length - source.Length % Vector<float>.Count;

        // Ensure there's enough data for SIMD operations
        if (simdLength > 0 && Vector<float>.Count <= source.Length && Vector<float>.Count <= destination.Length)
        {
            while (count < simdLength)
            {
                var vs = new Vector<float>(source.Slice(count, Vector<float>.Count));
                var vd = new Vector<float>(destination.Slice(count, Vector<float>.Count));
                (vd + vs).CopyTo(destination.Slice(count, Vector<float>.Count));
                count += Vector<float>.Count;
            }
        }

        // Scalar remainder
        while (count < source.Length)
        {
            destination[count] += source[count];
            count++;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ApplyVolumeAndPanning(Span<float> buffer, Vector2 volumePan)
    {
        switch (AudioEngine.Channels)
        {
            case 1:
                ApplyMonoVolume(buffer, volumePan.X + volumePan.Y);
                break;
            case 2:
                ApplyStereoVolume(buffer, volumePan);
                break;
            default:
                ApplyMultiChannelVolume(buffer, AudioEngine.Channels, volumePan);
                break;
        }
    }

    private static void ApplyMonoVolume(Span<float> buffer, float volume)
    {
        if (Math.Abs(volume - 1f) < 1e-6f)
            return;

        if (Vector.IsHardwareAccelerated && buffer.Length >= Vector<float>.Count)
        {
            var vecVolume = new Vector<float>(volume);
            int count = 0;
            for (; count <= buffer.Length - Vector<float>.Count; count += Vector<float>.Count)
            {
                var vec = new Vector<float>(buffer.Slice(count, Vector<float>.Count));
                (vec * vecVolume).CopyTo(buffer.Slice(count, Vector<float>.Count));
            }

            for (; count < buffer.Length; count++)
                buffer[count] *= volume;
        }
        else
        {
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] *= volume;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ApplyStereoVolume(Span<float> buffer, Vector2 volume)
    {
        // Early exit for unity volume (both channels at 1.0)
        if (Math.Abs(volume.X - 1f) < 1e-7f && Math.Abs(volume.Y - 1f) < 1e-7f)
            return;

        float volX = volume.X;
        float volY = volume.Y;

        int i = 0;

        if (Vector.IsHardwareAccelerated && buffer.Length >= Vector<float>.Count)
        {
            int vectorSize = Vector<float>.Count;

            if (vectorSize % 2 == 0)
            {
                Span<float> gainFactorsSpan = stackalloc float[vectorSize];
                for (int k = 0; k < vectorSize; k += 2)
                {
                    gainFactorsSpan[k] = volX;
                    gainFactorsSpan[k + 1] = volY;
                }
                var simdGainFactors = new Vector<float>(gainFactorsSpan);

                for (; i <= buffer.Length - vectorSize; i += vectorSize)
                {
                    var audioSimd = new Vector<float>(buffer.Slice(i, vectorSize));
                    (audioSimd * simdGainFactors).CopyTo(buffer.Slice(i, vectorSize));
                }
            }
        }

        // Scalar processing for the remainder or if SIMD is not applicable/enabled
        for (; i <= buffer.Length - 2; i += 2)
        {
            buffer[i] *= volX;
            buffer[i + 1] *= volY;
        }

        // Handle the last odd element if the buffer length is odd
        if (i < buffer.Length)
        {
            buffer[i] *= (volX + volY) * 0.5f;
        }
    }

    private static void ApplyMultiChannelVolume(Span<float> buffer, int channels, Vector2 volumePan)
    {
        if (channels < 2)
            return;

        float[] weights = new float[channels];
        weights[0] = volumePan.X;
        weights[1] = volumePan.Y;
        float avg = (volumePan.X + volumePan.Y) * 0.5f;

        for (int i = 2; i < channels; i++)
            weights[i] = avg;

        for (int i = 0; i < buffer.Length; i++)
            buffer[i] *= weights[i % channels];
    }

    /// <summary>
    ///     Generates audio data for the component.
    /// </summary>
    /// <param name="buffer">The buffer to write audio data to.</param>
    protected abstract void GenerateAudio(Span<float> buffer);
}
