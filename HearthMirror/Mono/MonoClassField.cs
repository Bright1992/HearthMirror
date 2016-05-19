using System;
using System.Text;

namespace HearthMirror.Mono
{
	public class MonoClassField
	{
		private readonly uint _pField;
		private readonly ProcessView _view;

		public MonoClassField(ProcessView view, uint pField)
		{
			_view = view;
			_pField = pField;
		}

		public string Name => _view.ReadCString(_view.ReadUint(_pField + Offsets.MonoClassField_name));

		public int Offset => _view.ReadInt(_pField + Offsets.MonoClassField_offset);

		public MonoType Type => new MonoType(_view, _view.ReadUint(_pField + Offsets.MonoClassField_type));

		public MonoClass Parent => new MonoClass(_view, _view.ReadUint(_pField + Offsets.MonoClassField_parent));

		public object StaticValue => Type.IsStatic ? GetValue(null) : null;

		public object GetValue(MonoObject o)
		{
			var offset = Offset;
			var type = Type;
			var typeType = type.Type;
			bool isRef;
			switch(typeType)
			{
				case MonoTypeEnum.String:
				case MonoTypeEnum.Szarray:
					// Special case since it makes sense to treat these as values.
					isRef = false;
					break;
				case MonoTypeEnum.Object:
				case MonoTypeEnum.Class:
				case MonoTypeEnum.Array:
					isRef = true;
					break;
				case MonoTypeEnum.GenericInst:
					var genericClass = type.Data;
					var container = new MonoClass(_view, _view.ReadUint(genericClass));
					isRef = !container.IsValueType;
					break;
				default:
					isRef = type.ByRef;
					break;
			}
			if(type.IsStatic)
			{
				var data = _view.ReadUint(Parent.VTable + Offsets.MonoVTable_data);
				if(isRef)
				{
					var po = _view.ReadUint(data + offset);
					return po == 0 ? null : new MonoObject(_view, po);
				}
				if(typeType == MonoTypeEnum.ValueType)
				{
					var sClass = new MonoClass(_view, type.Data);
					if(sClass.IsEnum)
						return ReadValue(new MonoClass(_view, _view.ReadUint(type.Data)).ByvalArg.Type, data + offset);
					return new MonoStruct(_view, sClass, (uint) (data + offset));
				}
				return typeType == MonoTypeEnum.GenericInst ? null : ReadValue(typeType, data + offset);
			}
			if(isRef)
			{
				var po = _view.ReadUint(o.PObject + offset);
				return po == 0 ? null : new MonoObject(_view, po);
			}
			if(typeType == MonoTypeEnum.ValueType)
			{
				var sClass = new MonoClass(_view, type.Data);
				if(sClass.IsEnum)
					return ReadValue(new MonoClass(_view, _view.ReadUint(type.Data)).ByvalArg.Type, o.PObject + offset);
				return new MonoStruct(_view, sClass, (uint) (o.PObject + offset));
			}
			return typeType == MonoTypeEnum.GenericInst ? null : ReadValue(typeType, o.PObject + offset);
		}

		private object ReadValue(MonoTypeEnum type, long addr)
		{
			switch(type)
			{
				case MonoTypeEnum.Boolean:
					return _view.ReadBool(addr);
				case MonoTypeEnum.U1:
					return _view.ReadByte(addr);
				case MonoTypeEnum.I1:
					return _view.ReadSByte(addr);
				case MonoTypeEnum.I2:
					return _view.ReadShort(addr);
				case MonoTypeEnum.U2:
					return _view.ReadUshort(addr);
				case MonoTypeEnum.Char:
					return (char) _view.ReadUshort(addr);
				case MonoTypeEnum.I:
				case MonoTypeEnum.I4:
					return _view.ReadInt(addr);
				case MonoTypeEnum.U:
				case MonoTypeEnum.U4:
					return _view.ReadUint(addr);
				case MonoTypeEnum.I8:
					return _view.ReadLong(addr);
				case MonoTypeEnum.U8:
					return _view.ReadUlong(addr);
				case MonoTypeEnum.R4:
					return _view.ReadFloat(addr);
				case MonoTypeEnum.R8:
					return _view.ReadDouble(addr);
				case MonoTypeEnum.Szarray:
					addr = _view.ReadUint(addr); // deref object
					var vt = _view.ReadUint(addr);
					var pArrClass = _view.ReadUint(vt);
					var arrClass = new MonoClass(_view, pArrClass);
					var elClass = new MonoClass(_view, _view.ReadUint(pArrClass));
					var count = _view.ReadInt(addr + 12);
					var start = addr + 16;
					var result = new object[count];
					for(var i = 0; i < count; i++)
					{
						var ea = start + i* arrClass.Size;
						if(elClass.IsValueType)
						{
							if(elClass.ByvalArg.Type == MonoTypeEnum.ValueType)
								result[i] = new MonoStruct(_view, elClass, (uint) ea);
							else
								result[i] = ReadValue(elClass.ByvalArg.Type, ea);
						}
						else
						{
							var po = _view.ReadUint(ea);
							if(po == 0)
								result[i] = null;
							else
								result[i] = new MonoObject(_view, po);
						}
					}
					return result;
				case MonoTypeEnum.String:
					var pArr = _view.ReadUint(addr);
					if(pArr == 0)
						return null;
					var strlen = _view.ReadInt(pArr + 8);
					if(strlen == 0)
						return string.Empty;
					var buf = new byte[2*strlen];
					_view.ReadBytes(buf, 0, strlen*2, pArr + 12);
					return Encoding.Unicode.GetString(buf);
				default:
					throw new Exception($"{type} not implemented");
			}
		}
	}
}