using System.Collections.Generic;

namespace MQPanel.Helper
{
  class Delegation
  {
    public delegate void SetDealDelegateTemplate(OperationData price, OrderData order);
    public static SetDealDelegateTemplate SetDealDelegates;

    public delegate void SetTradeDelegateTemplate(OrderData trade, string comment);
    public static SetTradeDelegateTemplate SetTradeDelegates;

    public delegate void SetPriceDelegateTemplate(PriceData price);
    public static SetPriceDelegateTemplate SetPriceDelegates;

    public delegate void SetPositionOrderDelegateTemplate(List<PositionData> posOrders);
    public static SetPositionOrderDelegateTemplate SetPositionOrderDelegates;

    public delegate void SetPositionDelegateTemplate(List<PositionData> posPositions);
    public static SetPositionDelegateTemplate SetPositionDelegates;

    public delegate void SetChannelCloseDelegateTemplate(string message);
    public static SetChannelCloseDelegateTemplate SetChannelCloseDelegates;

    public delegate void SetChannelStatusDelegateTemplate(bool connect);
    public static SetChannelStatusDelegateTemplate SetChannelStatusDelegates;
  }
}
