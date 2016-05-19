using System.Collections.Generic;
using System.Linq;

namespace HearthMirror.Mono
{
	internal class MonoStruct
	{
		private readonly ProcessView _view;
		public uint PStruct;

		public MonoStruct(ProcessView view, MonoClass mClass, uint pStruct)
		{
			_view = view;
			Class = mClass;
			PStruct = pStruct;
		}

		public MonoClass Class { get; }

		public IEnumerable<KeyValuePair<string, object>> Fields
			=> Class.Fields.Where(x => !x.Type.IsStatic)
				.Select(x => new KeyValuePair<string, object>(x.Name, x.GetValue(new MonoObject(_view, PStruct - 8))));

		public dynamic this[string key] => Fields.FirstOrDefault(x => x.Key == key).Value;
	}
}