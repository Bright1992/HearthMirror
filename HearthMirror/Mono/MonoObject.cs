using System.Collections.Generic;
using System.Linq;

namespace HearthMirror.Mono
{
	public class MonoObject
	{
		private readonly ProcessView _view;
		private readonly uint _vtable;
		public uint PObject;

		public MonoObject(ProcessView view, uint pObject)
		{
			PObject = pObject;
			_view = view;
			_vtable = _view.ReadUint(PObject);
		}

		public MonoClass Class => new MonoClass(_view, _view.ReadUint(_vtable));

		public IEnumerable<KeyValuePair<string, object>> Fields
			=> Class.Fields.Where(x => !x.Type.IsStatic).Select(x => new KeyValuePair<string, object>(x.Name, x.GetValue(this)));

		public dynamic this[string key] => Fields.FirstOrDefault(x => x.Key == key).Value;
	}
}