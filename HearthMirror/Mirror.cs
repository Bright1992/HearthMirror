using System;
using System.Diagnostics;
using System.Linq;
using HearthMirror.Mono;

namespace HearthMirror
{
	public class Mirror
	{
		public string ImageName { get; set; }
		public bool Active => _process != null;

		Process _process;
		public Process Proc => _process ?? (_process = Process.GetProcessesByName(ImageName).FirstOrDefault());

		private ProcessView _view;

		public ProcessView View
		{
			get
			{
				if(Proc == null)
					return null;
				return _view ?? (_view = new ProcessView(Proc));
			}
		}

		internal void Clean()
		{
			_process = null;
			_view = null;
		}

		public dynamic Root
		{
			get
			{
				var view = View;
				var rootDomainFuncPtr = view.GetExport("mono_get_root_domain");
				var rootDomainFunc = view.ReadUint(rootDomainFuncPtr);
				var buffer = new byte[6];
				view.ReadBytes(buffer, 0, 6, rootDomainFunc);
				if(buffer[0] != 0xa1 || buffer[5] != 0xc3)
					return null;
				var pRootDomain = BitConverter.ToUInt32(buffer, 1);
				view.ReadBytes(buffer, 0, 4, pRootDomain);
				var rootDomain = BitConverter.ToUInt32(buffer, 0);
				view.ReadBytes(buffer, 0, buffer.Length, rootDomain);
				var next = view.ReadUint(rootDomain + Offsets.MonoDomain_domain_assemblies);
				uint pImage = 0;
				while(next != 0)
				{
					var data = view.ReadUint(next);
					next = view.ReadUint(next + 4);
					var name = view.ReadCString(view.ReadUint(data + Offsets.MonoAssembly_name));
					if(name == "Assembly-CSharp")
					{
						pImage = view.ReadUint(data + Offsets.MonoAssembly_image);
						break;
					}
				}
				return new MonoImage(view, pImage);
			}
		}
	}
}