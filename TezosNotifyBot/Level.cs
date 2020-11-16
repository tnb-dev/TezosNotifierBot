namespace TezosNotifyBot
{
	public struct Level
	{
		public const int CycleSize = 4096;
		public const int PeriodSize = 32768;

		public Level(int level)
		{
			Height = level;
		}

		public int CycleFirst => Height - (Height - 1) % CycleSize;

		public int CycleLast => ((Height - 1) / CycleSize + 1) * CycleSize;

		public int Cycle => (Height - 1) / CycleSize;

		public int CyclePosition => (Height - 1) % CycleSize;

		public int Height { get; }

		public int Period => (Height - 1) / PeriodSize;

		public static implicit operator Level(int level) => new Level(level);

		public static implicit operator int(Level level) => level.Height;
	}
}
