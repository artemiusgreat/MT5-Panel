using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MQPanel.Helper
{
  static class Constants
  {
    public const int AVERAGE_CHAR = 250;
    public const int SMALL_CHAR = 50;
  }

  public enum ENUM_POSITION_TYPE
  {
    POSITION_TYPE_BUY,
    POSITION_TYPE_SELL
  };

  public enum ENUM_TRADE_REQUEST_ACTIONS
  {
    TRADE_ACTION_DEAL,
    TRADE_ACTION_PENDING,
    TRADE_ACTION_SLTP,
    TRADE_ACTION_MODIFY,
    TRADE_ACTION_REMOVE
  };

  public enum ENUM_ORDER_TYPE
  {
    ORDER_TYPE_BUY,
    ORDER_TYPE_SELL,
    ORDER_TYPE_BUY_LIMIT,
    ORDER_TYPE_SELL_LIMIT,
    ORDER_TYPE_BUY_STOP,
    ORDER_TYPE_SELL_STOP,
    ORDER_TYPE_BUY_STOP_LIMIT,
    ORDER_TYPE_SELL_STOP_LIMIT
  };

  public enum ENUM_ORDER_TYPE_FILLING
  {
    ORDER_FILLING_FOK,
    ORDER_FILLING_IOC,
    ORDER_FILLING_RETURN
  };

  public enum ENUM_ORDER_TYPE_TIME
  {
    ORDER_TIME_GTC,
    ORDER_TIME_DAY,
    ORDER_TIME_SPECIFIED,
    ORDER_TIME_SPECIFIED_DAY
  };

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct PriceData
  {
    public double StopLevel;
    public double Open;
    public double High;
    public double Low;
    public double Close;
    public double Ask;
    public double Bid;
    public double Spread;
    public double Capacity;
    public long Time;
  };

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct OrderData
  {
    public double SL;
    public double TP;
    public double Price;
    public double Volume;
    public double StopLevel;
    public ulong Magic;
    public ulong Ticket;
    public ulong Deviation;
    public ulong Expiration;
    public ulong OrderStatus;
    public ulong Type;
    public ulong TypeTime;
    public ulong Action;
    public ulong TypeFilling;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.SMALL_CHAR)]
    public char[] Currency;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.AVERAGE_CHAR)]
    public char[] Comments;
  };

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct OperationData
  {
    public ulong RequestId;
    public ulong ReturnCode;
    public ulong Deal;
    public ulong Order;
    public ulong DealStatus;
    public double Volume;
    public double Price;
    public double Bid;
    public double Ask;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.AVERAGE_CHAR)]
    public char[] Message;
  };

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct CountData
  {
    public long OrdersCount;
    public long PositionsCount;
  };

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct PositionData
  {
    public double Volume;
    public double Opening;
    public double SL;
    public double TP;
    public double Price;
    public double Profit;
    public ulong Id;
    public long Time;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.SMALL_CHAR)]
    public char[] Type;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.SMALL_CHAR)]
    public char[] Currency;
  };
}
