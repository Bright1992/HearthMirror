using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace HearthMirror
{
	internal class ProcessView
	{
		private readonly IntPtr _procHandle;
		private readonly long _moduleBase;
		private readonly byte[] _module;
		private readonly byte[] _buffer = new byte[16];
		private int _exportOffset;
		public bool Valid { get; private set; }

		public ProcessView(Process proc)
		{
			_procHandle = proc.Handle;
			_moduleBase = proc.MainModule.BaseAddress.ToInt64();
			_module = new byte[proc.MainModule.ModuleMemorySize];
			Valid = ReadBytes(_module, 0, _module.Length, _moduleBase) && LoadPeHeader();
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
			var result = ReadProcessMemory(_procHandle, (IntPtr)(unchecked((int)addr)), buffPtr, size, out bytesRead) && (int)bytesRead == size;
			buffHandle.Free();
			return result;
		}

		public uint ReadUint(long addr)
		{
			ReadBytes(_buffer, 0, 4, addr);
			return BitConverter.ToUInt32(_buffer, 0);
		}

		public int ReadInt(long addr)
		{
			ReadBytes(_buffer, 0, 4, addr);
			return BitConverter.ToInt32(_buffer, 0);
		}

		public byte ReadByte(long addr)
		{
			ReadBytes(_buffer, 0, 1, addr);
			return _buffer[0];
		}

		public sbyte ReadSByte(long addr) => unchecked((sbyte)ReadByte(addr));

		public short ReadShort(long addr)
		{
			ReadBytes(_buffer, 0, 2, addr);
			return BitConverter.ToInt16(_buffer, 0);
		}

		public ushort ReadUshort(long addr)
		{
			ReadBytes(_buffer, 0, 2, addr);
			return BitConverter.ToUInt16(_buffer, 0);
		}

		public bool ReadBool(long addr) => ReadByte(addr) != 0;

		public float ReadFloat(long addr)
		{
			ReadBytes(_buffer, 0, 4, addr);
			return BitConverter.ToSingle(_buffer, 0);
		}

		public double ReadDouble(long addr)
		{
			ReadBytes(_buffer, 0, 8, addr);
			return BitConverter.ToDouble(_buffer, 0);
		}

		public long ReadLong(long addr)
		{
			ReadBytes(_buffer, 0, 8, addr);
			return BitConverter.ToInt64(_buffer, 0);
		}

		public ulong ReadUlong(long addr)
		{
			ReadBytes(_buffer, 0, 8, addr);
			return BitConverter.ToUInt64(_buffer, 0);
		}

		public string ReadCString(long addr)
		{
			// Do this in blocks of 16 to minimize reads.
			// This is equally as safe as byte-at-a-time, and could be done
			// in larger blocks, but 16 is a good choice for most strings.
			var buffer = new byte[0x100];
			var ascii = Encoding.ASCII;
			var ofs = 0;
			if(0 != (addr & 15))
			{
				var adj = (int)(16 - (addr & 15));
				ReadBytes(buffer, ofs, adj, addr);
				for(var i = 0; i < adj; i++)
				{
					if(buffer[i] == 0)
						return ascii.GetString(buffer, 0, i);
				}
				ofs += adj;
				addr += adj;
			}
			while(true)
			{
				ReadBytes(buffer, ofs, 16, addr);
				for(var i = 0; i < 16; i++)
				{
					if(buffer[ofs + i] == 0)
						return ascii.GetString(buffer, 0, ofs + i);
				}
				ofs += 0x10;
				addr += 0x10;
			}
		}

		public long GetExport(string name)
		{
			var nFunctions = BitConverter.ToInt32(_module, _exportOffset + (int)Offsets.ImageExportDirectory_NumberOfFunctions);
			var ofsFunctions = BitConverter.ToInt32(_module, _exportOffset + (int)Offsets.ImageExportDirectory_AddressOfFunctions);
			var ofsNames = BitConverter.ToInt32(_module, _exportOffset + (int)Offsets.ImageExportDirectory_AddressOfNames);
			for(var i = 0; i < nFunctions; i++)
			{
				var nameRva = BitConverter.ToInt32(_module, ofsNames + 4 * i);
				var fName = GetCString(_module, nameRva);
				if(fName == name)
					return _moduleBase + BitConverter.ToInt32(_module, ofsFunctions + 4 * i);
			}
			return 0;
		}

		static string GetCString(byte[] buf, int ofs)
		{
			int i = ofs;
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