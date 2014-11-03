using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace metaio.unitycommunication.util
{
	public static class Util
	{
		public static string MarshalToStringUTF8(this IntPtr ptr)
		{
			List<byte> data = new List<byte>();
			int offset = 0;
			while (true)
			{
				byte ch = Marshal.ReadByte(ptr, offset++);
				if (ch == 0)
				{
					break;
				}
				data.Add(ch);
			}
			return Encoding.UTF8.GetString(data.ToArray());
		}

		public static byte[] MarshalToZeroTerminatedUTF8Array(this string s)
		{
			// Enforce serialization with zero terminator
			if (!s.EndsWith("\0"))
			{
				s += "\0";
			}

			return Encoding.UTF8.GetBytes(s);
		}
	}
}
