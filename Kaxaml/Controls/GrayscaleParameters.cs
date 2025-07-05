using System;

namespace Kaxaml.Controls
{
    public class GrayscaleParameters
    {
        #region Fields


        private double _RedDistribution = 0.30;
        private double _GreenDistribution = 0.59;
        private double _BlueDistribution = 0.11;
        private double _Compression = 0.8;
        private double _Washout = -0.05;

        #endregion Fields

        #region Properties


        public double RedDistribution
        { get => _RedDistribution; set => _RedDistribution = Math.Max(0, Math.Min(1, value));
        }

        public double GreenDistribution
        { get => _GreenDistribution; set => _GreenDistribution = Math.Max(0, Math.Min(1, value));
        }

        public double BlueDistribution
        { get => _BlueDistribution; set => _BlueDistribution = Math.Max(0, Math.Min(1, value));
        }

        public double Compression
        { get => _Compression; set => _Compression = Math.Max(0, Math.Min(1, value));
        }

        public double Washout
        { get => _Washout; set => _Washout = Math.Max(0, Math.Min(1, value));
        }


        #endregion Properties
    }
}
