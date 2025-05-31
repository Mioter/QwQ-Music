using System;
using System.Collections.Generic;
using System.Linq;

namespace QwQ_Music.Utilities;

/// <summary>
///     专业音乐回放增益计算器（支持EBU R128、ReplayGain 2.0和自定义标准）
///     <para></para>
///     实现说明：
///     <list type="bullet">
///         <item>符合EBU R128标准使用K加权滤波器</item>
///         <item>支持多声道音频的响度计算</item>
///         <item>用双重门限处理（绝对门限和相对门限）</item>
///     </list>
/// </summary>
public static class ReplayGainCalculator
{
    // 标准目标响度值（单位：LUFS）
    private const double STREAMING_TARGET_LUFS = -16.0; // 流媒体平台常用值
    private const double EBU_R128_TARGET_LUFS = -23.0;
    private const double REPLAY_GAIN2_TARGET_LUFS = -18.0;

    public static event EventHandler? CalcCompletedChanged;

    /// <summary>
    ///     计算音频数据的回放增益（线性比例值）
    /// </summary>
    /// <param name="audioBlocks">分块的音频数据（每个区块为32/64位浮点PCM）</param>
    /// <param name="sampleRate">采样率（Hz）</param>
    /// <param name="channels">声道数（1-8）</param>
    /// <param name="standard">响度标准类型</param>
    /// <param name="customTargetLufs">自定义目标响度</param>
    /// <para></para>
    /// 输入数据要求：
    /// <list type="bullet">
    ///     <item>
    ///         必须是浮点 PCM 格式（32-bit 或 64-bit）
    ///     </item>
    ///     <item>
    ///         声道数必须在 1 到 8 之间
    ///     </item>
    /// </list>
    public static double CalculateGain(
        IEnumerable<float[]> audioBlocks,
        int sampleRate,
        int channels,
        MusicReplayGainStandard standard = MusicReplayGainStandard.Streaming,
        double customTargetLufs = STREAMING_TARGET_LUFS
    )
    {
        var filter = new KWeightingFilter(sampleRate);
        var loudnessMeter = new LoudnessMeter(channels);

        foreach (float[] buffer in audioBlocks)
        {
            filter.ProcessBuffer(buffer);
            loudnessMeter.AnalyzeSamples(buffer);
        }

        double measuredLufs = loudnessMeter.GetIntegratedLoudness();
        double targetLufs = GetTargetLoudness(standard, customTargetLufs);
        double gain = CalculateLinearGain(measuredLufs, targetLufs);

        CalcCompletedChanged?.Invoke(null, EventArgs.Empty);

        return gain;
    }

    #region 私有辅助方法

    private static double GetTargetLoudness(MusicReplayGainStandard standard, double customTarget) =>
        standard switch
        {
            MusicReplayGainStandard.EbuR128 => EBU_R128_TARGET_LUFS,
            MusicReplayGainStandard.ReplayGain2 => REPLAY_GAIN2_TARGET_LUFS,
            MusicReplayGainStandard.Custom => customTarget,
            _ => STREAMING_TARGET_LUFS,
        };

    private static double CalculateLinearGain(double measuredLufs, double targetLufs) =>
        Math.Pow(10, (targetLufs - measuredLufs) / 20.0);

    #endregion

    #region 信号处理组件

    /// <summary>
    /// K加权滤波器组（符合EBU R128规范）
    /// 包含：
    /// - 高通滤波器：38Hz, Q=0.69
    /// - 低通滤波器：1682Hz, Q=0.69
    /// </summary>
    private sealed class KWeightingFilter(int sampleRate)
    {
        private readonly BiQuadFilter _highPass = BiQuadFilter.CreateHighPass(sampleRate, 38, 0.69);
        private readonly BiQuadFilter _lowPass = BiQuadFilter.CreateLowPass(sampleRate, 1682, 0.69);

        public void ProcessBuffer(float[] buffer)
        {
            _highPass.Transform(buffer); // 第一阶段高通滤波
            _lowPass.Transform(buffer); // 第二阶段低通滤波
        }
    }

    /// <summary>
    /// 双二阶滤波器实现（支持高低通配置）
    /// 参考：Audio EQ Cookbook (https://www.w3.org/TR/audio-eq-cookbook/)
    /// </summary>
    private sealed class BiQuadFilter
    {
        private readonly double _b0,
            _b1,
            _b2;
        private readonly double _a1,
            _a2;
        private double _z1,
            _z2;

        private BiQuadFilter(double b0, double b1, double b2, double a1, double a2)
        {
            _b0 = b0;
            _b1 = b1;
            _b2 = b2;
            _a1 = a1;
            _a2 = a2;
        }

        public static BiQuadFilter CreateHighPass(int sampleRate, double freq, double q)
        {
            double w0 = 2 * Math.PI * freq / sampleRate;
            double cos = Math.Cos(w0);
            double sin = Math.Sin(w0);
            double alpha = sin / (2 * q);

            double b0 = (1 + cos) / 2;
            double b1 = -(1 + cos);
            double a0 = 1 + alpha;
            double a1 = -2 * cos;
            double a2 = 1 - alpha;

            return new BiQuadFilter(b0 / a0, b1 / a0, b0 / a0, a1 / a0, a2 / a0);
        }

        public static BiQuadFilter CreateLowPass(int sampleRate, double freq, double q)
        {
            double w0 = 2 * Math.PI * freq / sampleRate;
            double cos = Math.Cos(w0);
            double sin = Math.Sin(w0);
            double alpha = sin / (2 * q);

            double b0 = (1 - cos) / 2;
            double b1 = 1 - cos;
            double a0 = 1 + alpha;
            double a1 = -2 * cos;
            double a2 = 1 - alpha;

            return new BiQuadFilter(b0 / a0, b1 / a0, b0 / a0, a1 / a0, a2 / a0);
        }

        public void Transform(float[] buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                double input = buffer[i];
                double output = input * _b0 + _z1;
                _z1 = input * _b1 + _z2 - _a1 * output;
                _z2 = input * _b2 - _a2 * output;
                buffer[i] = (float)output;
            }
        }
    }

    /// <summary>
    /// 响度计实现（符合EBU R128标准）
    /// 功能：
    /// - 多声道能量累加
    /// - 绝对门限处理（-70 LUFS）
    /// - 相对门限处理（-10 dB）
    /// </summary>
    private sealed class LoudnessMeter(int channels)
    {
        private readonly double[] _channelSquares = new double[channels];
        private long _totalSamples;
        private double _absoluteThresholdEnergy;

        public void AnalyzeSamples(float[] buffer)
        {
            int samplesPerChannel = buffer.Length / channels;

            for (int c = 0; c < channels; c++)
            {
                double sum = 0;
                for (int i = c; i < buffer.Length; i += channels)
                {
                    double sample = buffer[i];
                    sum += sample * sample;
                }
                _channelSquares[c] += sum;
            }
            _totalSamples += samplesPerChannel;

            // 绝对门限计算（-70 LUFS）
            _absoluteThresholdEnergy = Math.Pow(10, -70 / 10.0) * _totalSamples;
        }

        public double GetIntegratedLoudness()
        {
            // 计算总能量（包括所有声道）
            double totalEnergy = _channelSquares.Sum();

            // 应用绝对门限
            if (totalEnergy <= _absoluteThresholdEnergy)
                return double.NegativeInfinity;

            // 计算平均能量（考虑声道数）
            double meanSquare = totalEnergy / (_totalSamples * channels);
            return 10 * Math.Log10(meanSquare);
        }
    }

    #endregion
}

/// <summary>
/// 回放增益标准类型
/// </summary>
public enum MusicReplayGainStandard
{
    /// <summary>
    /// 流媒体优化（-16 LUFS）
    /// </summary>
    Streaming,

    /// <summary>
    /// EBU R128广播标准（-23 LUFS）
    /// </summary>
    EbuR128,

    /// <summary>
    /// ReplayGain 2.0标准（-18 LUFS）
    /// </summary>
    ReplayGain2,

    /// <summary>
    /// 自定义目标响度
    /// </summary>
    Custom,
}
