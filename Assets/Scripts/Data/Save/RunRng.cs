using System;

namespace Crookedile.Data.Save
{
    /// <summary>
    /// The run's random stream: a xorshift128 generator behind the <see cref="System.Random"/>
    /// API, so every existing caller keeps working, but with a state that can be saved and
    /// restored exactly. Same seed and same draws give the same numbers, including across a
    /// save and reload.
    /// </summary>
    public class RunRng : Random
    {
        private uint _x,
            _y,
            _z,
            _w;

        /// <summary>A stream seeded from <paramref name="seed"/>.</summary>
        public RunRng(int seed)
        {
            // SplitMix64 spreads one int over the four state words; xorshift needs a non-zero state.
            ulong s = unchecked((ulong)seed * 0x9E3779B97F4A7C15UL + 0x632BE59BD9B4E019UL);
            _x = (uint)SplitMix(ref s);
            _y = (uint)SplitMix(ref s);
            _z = (uint)SplitMix(ref s);
            _w = (uint)SplitMix(ref s);
            if ((_x | _y | _z | _w) == 0)
                _w = 1;
        }

        /// <summary>A stream resumed from a saved <see cref="GetState"/>.</summary>
        public RunRng(uint[] state)
        {
            if (state == null || state.Length != 4)
                throw new ArgumentException("RunRng state is four uints.", nameof(state));
            _x = state[0];
            _y = state[1];
            _z = state[2];
            _w = state[3];
            if ((_x | _y | _z | _w) == 0)
                _w = 1;
        }

        /// <summary>The four state words; pass them to <see cref="RunRng(uint[])"/> to resume.</summary>
        public uint[] GetState() => new[] { _x, _y, _z, _w };

        private static ulong SplitMix(ref ulong s)
        {
            ulong z = unchecked(s += 0x9E3779B97F4A7C15UL);
            z = unchecked((z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL);
            z = unchecked((z ^ (z >> 27)) * 0x94D049BB133111EBUL);
            return z ^ (z >> 31);
        }

        private uint NextUInt()
        {
            uint t = _x ^ (_x << 11);
            _x = _y;
            _y = _z;
            _z = _w;
            _w = _w ^ (_w >> 19) ^ t ^ (t >> 8);
            return _w;
        }

        /// <summary>A double in [0, 1) with 53 bits of precision.</summary>
        protected override double Sample()
        {
            ulong bits = ((ulong)(NextUInt() >> 5) << 26) | (NextUInt() >> 6);
            return bits * (1.0 / 9007199254740992.0);
        }

        public override double NextDouble() => Sample();

        public override int Next() => (int)(NextUInt() >> 1) % int.MaxValue;

        public override int Next(int maxValue)
        {
            if (maxValue < 0)
                throw new ArgumentOutOfRangeException(nameof(maxValue));
            return (int)(Sample() * maxValue);
        }

        public override int Next(int minValue, int maxValue)
        {
            if (minValue > maxValue)
                throw new ArgumentOutOfRangeException(nameof(minValue));
            long range = (long)maxValue - minValue;
            return (int)(minValue + (long)(Sample() * range));
        }

        public override void NextBytes(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = (byte)NextUInt();
        }
    }
}
