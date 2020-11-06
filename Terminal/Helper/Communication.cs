using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Globalization;

namespace MQPanel.Helper
{
  public class Communication
  {
    public static Byte[] ConvertTo<T>(T data) where T : struct
    {
      // Serialize

      var size = Marshal.SizeOf(typeof(T));
      var result = new Byte[size];
      var memory = Marshal.AllocHGlobal(size);

      Marshal.StructureToPtr(data, memory, true);
      Marshal.Copy(memory, result, 0, size);
      Marshal.FreeHGlobal(memory);

      return result;
    }

    public static T ConvertFrom<T>(byte[] data) where T : struct
    {
      var size = Marshal.SizeOf(typeof(T));
      var memory = Marshal.AllocHGlobal(size);

      Marshal.Copy(data, 0, memory, size);
      T result = (T)Marshal.PtrToStructure(memory, typeof(T));
      Marshal.FreeHGlobal(memory);

      return result;
    }

    public static NumberFormatInfo GetCultureFormat()
    {
      NumberFormatInfo provider = new NumberFormatInfo();
      provider.NumberGroupSeparator = string.Empty;
      provider.NumberDecimalSeparator = ".";
      return provider;
    }

    public static double D(object data)
    {
      double result = 0;

      try
      {
        result = Convert.ToDouble(data, GetCultureFormat());
      }
      catch (Exception)
      {
        result = 0;
      }

      return result;
    }

    public static ulong U(object data)
    {
      ulong result = 0;

      try
      {
        result = Convert.ToUInt64(data);
      }
      catch (Exception)
      {
        result = 0;
      }

      return result;
    }

    public static int I(object data)
    {
      int result = 0;

      try
      {
        result = Convert.ToInt32(data);
      }
      catch (Exception)
      {
        result = 0;
      }

      return result;
    }

    public static string S(object data)
    {
      string result = string.Empty;

      try
      {
        result = Convert.ToString(data, GetCultureFormat());
      }
      catch (Exception)
      {
        result = string.Empty;
      }

      return result;
    }
  }
}
