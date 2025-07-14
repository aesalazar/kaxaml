using System;

namespace Kaxaml.Controls;

public class GrayscaleParameters
{
    #region Fields

    private double _redDistribution = 0.30;
    private double _greenDistribution = 0.59;
    private double _blueDistribution = 0.11;
    private double _compression = 0.8;
    private double _washout = -0.05;

    #endregion Fields

    #region Properties

    public double RedDistribution
    {
        get => _redDistribution;
        set => _redDistribution = Math.Max(0, Math.Min(1, value));
    }

    public double GreenDistribution
    {
        get => _greenDistribution;
        set => _greenDistribution = Math.Max(0, Math.Min(1, value));
    }

    public double BlueDistribution
    {
        get => _blueDistribution;
        set => _blueDistribution = Math.Max(0, Math.Min(1, value));
    }

    public double Compression
    {
        get => _compression;
        set => _compression = Math.Max(0, Math.Min(1, value));
    }

    public double Washout
    {
        get => _washout;
        set => _washout = Math.Max(0, Math.Min(1, value));
    }

    #endregion Properties
}