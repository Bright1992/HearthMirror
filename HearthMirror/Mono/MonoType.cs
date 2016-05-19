namespace HearthMirror.Mono
{
	internal class MonoType
	{
		private readonly uint _pType;
		private readonly ProcessView _view;

		public MonoType(ProcessView view, uint pType)
		{
			_view = view;
			_pType = pType;
		}

		public uint Attrs => _view.ReadUint(_pType + Offsets.MonoType_attrs);

		public uint Data => _view.ReadUint(_pType);

		public bool IsStatic => 0 != (Attrs & 0x10);

		public bool IsPublic => 6 == (Attrs & 0x7);

		public bool IsLiteral => 0 != (Attrs & 0x40);

		public bool HasDefault => 0 != (Attrs & 0x8000);

		public bool HasFieldRva => 0 != (Attrs & 0x100);

		public bool ByRef => 0 != (Attrs & 0x40000000);

		public MonoTypeEnum Type => (MonoTypeEnum) (0xff & (Attrs >> 16));
	}
}