using HearthMirror.Enums;

namespace HearthMirror
{
	internal class ServerOptions
	{
		public static ServerOptionFlagInfo GetServerOptionFlagInfo(int flag)
		{
			var value = flag * 2;
			var index = value / 64;
			var offset = value % 64;
			return new ServerOptionFlagInfo(Flags[index], 1uL << offset, 1uL << offset + 1);
		}

		public class ServerOptionFlagInfo
		{
			public ServerOption ServerOption { get; }
			public ulong Flag { get; }
			public ulong Exists { get; }

			public ServerOptionFlagInfo(ServerOption serverOption, ulong flag, ulong exists)
			{
				ServerOption = serverOption;
				Flag = flag;
				Exists = exists;
			}	
		}

		private static readonly ServerOption[] Flags = {
			ServerOption.FLAGS1,
			ServerOption.FLAGS2,
			ServerOption.FLAGS3,
			ServerOption.FLAGS4,
			ServerOption.FLAGS5,
			ServerOption.FLAGS6,
			ServerOption.FLAGS7,
			ServerOption.FLAGS8,
			ServerOption.FLAGS9,
			ServerOption.FLAGS10
		};	
	}
}