using System;
using HearthMirror.Enums;
using static HearthMirror.Enums.MirrorStatus;

namespace HearthMirror
{
	public class Status
	{
		private Status(MirrorStatus status)
		{
			MirrorStatus = status;
		}

		private Status(Exception ex)
		{
			MirrorStatus = Error;
			Exception = ex;
		}

		public MirrorStatus MirrorStatus { get; }
		public Exception Exception { get; }

		public static Status GetStatus()
		{
			try
			{
				return new Mirror {ImageName = "Hearthstone"}.View == null ? new Status(ProcNotFound) : new Status(Ok);
			}
			catch(Exception e)
			{
				return new Status(e);
			}
		}
	}
}