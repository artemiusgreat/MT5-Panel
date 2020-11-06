using MQPanel.Helper;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MQPanel
{
  class CustomChannel : IDisposable
  {
    private bool _isClosing;
    private string _connectionAddress;
    private OrderData _order = new OrderData();

    public CustomChannel(string address)
    {
      _connectionAddress = address;

      Delegation.SetTradeDelegates += UpdateTrade;
      Delegation.SetChannelStatusDelegates += UpdateStatus;
    }

    public void Dispose()
    {
      Delegation.SetTradeDelegates -= UpdateTrade;
      Delegation.SetChannelStatusDelegates -= UpdateStatus;
    }

    public void StartServer()
    {
      Task.Factory.StartNew(() =>
      {
        var countSize = Marshal.SizeOf(typeof(CountData));
        var priceSize = Marshal.SizeOf(typeof(PriceData));
        var positionSize = Marshal.SizeOf(typeof(PositionData));
        var operationSize = Marshal.SizeOf(typeof(OperationData));
        var positionOrderSize = Marshal.SizeOf(typeof(PositionData));

        var inputCount = new byte[countSize];
        var inputPrice = new byte[priceSize];
        var inputPosition = new byte[positionSize];
        var inputOperation = new byte[operationSize];
        var inputPositionOrder = new byte[positionOrderSize];

        try
        {
          using (NamedPipeServerStream serverPipe = new NamedPipeServerStream(_connectionAddress, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
          {
            serverPipe.BeginWaitForConnection(a => { try { serverPipe.EndWaitForConnection(a); } catch (Exception) { _isClosing = true; } }, null);

            while (_isClosing == false)
            {
              if (serverPipe.IsConnected)
              {
                // Get price

                serverPipe.Read(inputPrice, 0, priceSize);
                var price = Communication.ConvertFrom<PriceData>(inputPrice);
                Delegation.SetPriceDelegates(price);

                // Send order

                serverPipe.Write(Communication.ConvertTo<OrderData>(_order), 0, Marshal.SizeOf(typeof(OrderData)));

                // Get trade result

                serverPipe.Read(inputOperation, 0, operationSize);
                var deal = Communication.ConvertFrom<OperationData>(inputOperation);

                if (_order.OrderStatus == 1)
                {
                  Delegation.SetDealDelegates(deal, _order);

                  _order = new OrderData();
                  _order.OrderStatus = 0;
                }

                // Get orders count

                serverPipe.Read(inputCount, 0, countSize);
                var posCounters = Communication.ConvertFrom<CountData>(inputCount);

                // Get order list

                var posOrders = new List<PositionData>();

                for (var i = 0; i < posCounters.OrdersCount; i++)
                {
                  serverPipe.Read(inputPositionOrder, 0, positionOrderSize);
                  posOrders.Add(Communication.ConvertFrom<PositionData>(inputPositionOrder));
                }

                Delegation.SetPositionOrderDelegates(posOrders);

                // Get position list

                var posPositions = new List<PositionData>();

                for (var i = 0; i < posCounters.PositionsCount; i++)
                {
                  serverPipe.Read(inputPosition, 0, positionSize);
                  posPositions.Add(Communication.ConvertFrom<PositionData>(inputPosition));
                }

                Delegation.SetPositionDelegates(posPositions);
              }
            }

            serverPipe.Disconnect();
          }
        }
        catch (Exception)
        {
          _isClosing = true;
        }

        Delegation.SetChannelCloseDelegates("No connection");
      });
    }

    public void StartClient()
    {
      Task.Factory.StartNew(() =>
      {
        using (NamedPipeClientStream clientPipe = new NamedPipeClientStream(".", _connectionAddress, PipeDirection.InOut, PipeOptions.Asynchronous))
        {
          clientPipe.Connect();
          clientPipe.Close();
        }
      });
    }

    public void UpdateTrade(OrderData order, string orderComment)
    {
      _order = order;
    }

    public void UpdateStatus(bool isClosing)
    {
      _isClosing = isClosing;
    }
  }
}