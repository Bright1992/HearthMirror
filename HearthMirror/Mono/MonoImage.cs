using System.Collections.Generic;

namespace HearthMirror.Mono
{
	internal class MonoImage
	{
		private readonly Dictionary<string, MonoClass> _classes = new Dictionary<string, MonoClass>();
		private readonly uint _pImage;
		private readonly ProcessView _view;

		public MonoImage(ProcessView view, uint pImage)
		{
			_view = view;
			_pImage = pImage;
			LoadAllTypes();
		}

		public dynamic this[string key] => _classes[key];

		private void LoadAllTypes()
		{
			var ht = _pImage + Offsets.MonoImage_class_cache;
			var size = _view.ReadUint(ht + Offsets.MonoInternalHashTable_size);
			var table = _view.ReadUint(ht + Offsets.MonoInternalHashTable_table);
			for(uint i = 0; i < size; i++)
			{
				var pClass = _view.ReadUint(table + 4*i);
				while(pClass != 0)
				{
					var klass = new MonoClass(_view, pClass);
					_classes[klass.FullName] = klass;
					pClass = _view.ReadUint(pClass + Offsets.MonoClass_next_class_cache);
				}
			}
		}
	}
}