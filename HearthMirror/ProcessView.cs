using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace HearthMirror
{
	internal class ProcessView
	{
		private const int PageSize = 4096;
		private const int PageCount = 1024;
		private readonly Cache _cache = new Cache(PageCount);
		private readonly byte[] _module;
		private readonly long _moduleBase;
		private readonly IntPtr _procHandle;
		private int _exportOffset;

		public ProcessView(Process proc)
		{
			_procHandle = proc.Handle;
			_moduleBase = proc.MainModule.BaseAddress.ToInt64();
			_module = new byte[proc.MainModule.ModuleMemorySize];
			Valid = ReadBytes(_module, 0, _module.Length, _moduleBase) && LoadPeHeader();
		}

		public bool Valid { get; private set; }

		internal void ClearCache() => _cache.Clear();

		private byte[] ReadBytes(int size, long addr, int offset = 0)
		{
			var start = (int)(addr/PageSize)*PageSize;
			var page = _cache.Get(start);
			if(page == null)
			{
				page = ReadPage(start, offset);
				_cache.Add(start, page);
			}
			var pageOffset = addr%PageSize;
			var buffer = new byte[size];
			var overflow = (int)pageOffset + size - PageSize;
			if(overflow > 0)
			{
				var read = size - overflow;
				var remaining = ReadBytes(overflow, addr + read);
				Array.Copy(page, pageOffset, buffer, 0, read);
				Array.Copy(remaining, 0, buffer, read, overflow);
			}
			else
				Array.Copy(page, pageOffset, buffer, 0, size);
			return buffer;
		}

		private byte[] ReadPage(long addr, int offset)
		{
			var buffer = new byte[PageSize];
			IntPtr bytesRead;
			var buffHandle = GCHandle.Alloc(buffer);
			var buffPtr = Marshal.UnsafeAddrOfPinnedArrayElement(buffer, offset);
			var result = ReadProcessMemory(_procHandle, (IntPtr)unchecked((int)addr), buffPtr, PageSize, out bytesRead) &&
						(int)bytesRead == PageSize;
			buffHandle.Free();
			return result ? buffer : new byte[PageSize];
		}

		public bool ReadBytes(byte[] buf, int offset, int size, long addr)
		{
			// TODO: optimize this function to use a simple cache that copies an entire page
			// to local memory at once to avoid calling ReadProcessMemory excessively.
			// Basic idea: 256*4kB pages -- 64 pages 4 ways, and (addr >> 12)&0xff selects
			// which cache slot to occupy (i.e. just use the address in the same way a CPU
			// cache would); data storage on the ProcessView object is:
			// - long[256] to store page occupancy in the
			// - byte[256 * 4096] page cache.
			// (Once this is done, the ReadCString and LoadPeHeader methods can be simplified)
			IntPtr bytesRead;
			var buffHandle = GCHandle.Alloc(buf);
			var buffPtr = Marshal.UnsafeAddrOfPinnedArrayElement(buf, offset);
			var result = ReadProcessMemory(_procHandle, (IntPtr)unchecked((int)addr), buffPtr, size, out bytesRead) &&
						(int)bytesRead == size;
			buffHandle.Free();
			return result;
		}

		public uint ReadUint(long addr) => BitConverter.ToUInt32(ReadBytes(4, addr), 0);

		public int ReadInt(long addr) => BitConverter.ToInt32(ReadBytes(4, addr), 0);

		public byte ReadByte(long addr) => ReadBytes(1, addr)[0];

		public sbyte ReadSByte(long addr) => unchecked((sbyte)ReadByte(addr));

		public short ReadShort(long addr) => BitConverter.ToInt16(ReadBytes(2, addr), 0);

		public ushort ReadUshort(long addr) => BitConverter.ToUInt16(ReadBytes(2, addr), 0);

		public bool ReadBool(long addr) => ReadByte(addr) != 0;

		public float ReadFloat(long addr) => BitConverter.ToSingle(ReadBytes(4, addr), 0);

		public double ReadDouble(long addr) => BitConverter.ToDouble(ReadBytes(8, addr), 0);

		public long ReadLong(long addr) => BitConverter.ToInt64(ReadBytes(8, addr), 0);

		public ulong ReadUlong(long addr) => BitConverter.ToUInt64(ReadBytes(8, addr), 0);

		public string ReadCString(long addr) => Encoding.ASCII.GetString(ReadBytes(100, addr).TakeWhile(x => x != 0).ToArray());

		public long GetExport(string name)
		{
			var nFunctions = BitConverter.ToInt32(_module, _exportOffset + (int)Offsets.ImageExportDirectory_NumberOfFunctions);
			var ofsFunctions = BitConverter.ToInt32(_module, _exportOffset + (int)Offsets.ImageExportDirectory_AddressOfFunctions);
			var ofsNames = BitConverter.ToInt32(_module, _exportOffset + (int)Offsets.ImageExportDirectory_AddressOfNames);
			for(var i = 0; i < nFunctions; i++)
			{
				var nameRva = BitConverter.ToInt32(_module, ofsNames + 4*i);
				var fName = GetCString(_module, nameRva);
				if(fName == name)
					return _moduleBase + BitConverter.ToInt32(_module, ofsFunctions + 4*i);
			}
			return 0;
		}

		private static string GetCString(byte[] buf, int ofs)
		{
			var i = ofs;
			while(0 != buf[i++]) ;
			return Encoding.ASCII.GetString(buf, ofs, i - ofs - 1);
		}

		private bool LoadPeHeader()
		{
			// IMAGE_DOS_HEADER.e_lfanew
			var e_lfanew = BitConverter.ToInt32(_module, (int)Offsets.ImageDosHeader_e_lfanew);

			// IMAGE_FILE_HEADER.Signature
			var sig = BitConverter.ToInt32(_module, e_lfanew + (int)Offsets.ImageNTHeaders_Signature);
			if(sig != 0x4550)
				return false;

			// IMAGE_FILE_HEADER.Machine -- check to make sure this is 32-bit
			// throw an exception in this case, since the error requires code changes
			// this will be 0x8664 for a 64-bit process.
			var machine = BitConverter.ToUInt16(_module, e_lfanew + (int)Offsets.ImageNTHeaders_Machine);
			if(machine != 0x14c)
				throw new InvalidOperationException("ProcessView expects a 32-bit process, but was given a 64-bit process");

			// DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT].VirtualAddress
			_exportOffset = BitConverter.ToInt32(_module, e_lfanew + (int)Offsets.ImageNTHeaders_ExportDirectoryAddress);

			return _exportOffset > 0 && _exportOffset < _module.Length;
		}

		[DllImport("kernel32", SetLastError = true)]
		private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, int nSize, out IntPtr lpNumberOfBytesRead);
	}
}