using MQPanel.Helper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MQPanel.Control
{
  public partial class Tester : Form, IDisposable
  {
    private bool _isClean = false;
    private CustomChannel _channel;
    private PriceData _price = new PriceData();
    private OperationData _operation = new OperationData();

    public void Protect()
    {
      if (_isClean == false)
      {
        //Process.GetCurrentProcess().Kill();
      }
    }

    public void Setup(string message)
    {
      if (string.IsNullOrEmpty(message) == false)
      {
        MessageBox.Show(message);
      }

      Process.GetCurrentProcess().Kill();

      _isClean = true;
    }

    public Tester()
    {
      InitializeComponent();

      var OrderTypes = new Dictionary<ENUM_ORDER_TYPE, string>
      {
        { ENUM_ORDER_TYPE.ORDER_TYPE_BUY_STOP, "BUY STOP" },
        { ENUM_ORDER_TYPE.ORDER_TYPE_SELL_STOP, "SELL STOP" },
        { ENUM_ORDER_TYPE.ORDER_TYPE_BUY_LIMIT, "BUY LIMIT" },
        { ENUM_ORDER_TYPE.ORDER_TYPE_SELL_LIMIT, "SELL LIMIT" }
      };

      LimitComboOrderType.ValueMember = "Key";
      LimitComboOrderType.DisplayMember = "Value";
      LimitComboOrderType.DataSource = OrderTypes.Select(x => new { Key = x.Key, Value = x.Value }).ToList();

      EnablePanel(false);

      Delegation.SetPriceDelegates += UpdatePrice;
      Delegation.SetDealDelegates += UpdateOperation;
      Delegation.SetChannelCloseDelegates += CloseConnection;
      Delegation.SetPositionOrderDelegates += UpdateOrders;
      Delegation.SetPositionDelegates += UpdatePositions;
    }

    public new void Dispose()
    {
      Delegation.SetPriceDelegates -= UpdatePrice;
      Delegation.SetDealDelegates -= UpdateOperation;
      Delegation.SetChannelCloseDelegates -= CloseConnection;
      Delegation.SetPositionOrderDelegates -= UpdateOrders;
      Delegation.SetPositionDelegates -= UpdatePositions;

      CloseConnection(string.Empty);

      base.Dispose(true);
    }

    public void EnablePanel(bool isActive)
    {
      foreach (TabPage tab in Tabs.TabPages)
      {
        if (tab.Name.Contains("Connection") == false)
        {
          tab.Enabled = isActive;
        }
      }
    }

    public void OpenConnection(string message)
    {
      Invoke((MethodInvoker)delegate
     {
       ConnectionLabelError.Text = message;
       ConnectionButtonOpen.Visible = false;
       ConnectionButtonClose.Visible = true;
     });
    }

    public void CloseConnection(string message)
    {
      Delegation.SetChannelStatusDelegates(true);

      Invoke((MethodInvoker)delegate
     {
       ConnectionLabelError.Text = message;
       ConnectionButtonOpen.Visible = true;
       ConnectionButtonClose.Visible = false;
       EnablePanel(false);
     });
    }

    public void UpdatePrice(PriceData price)
    {
      _price = price;

      Invoke((MethodInvoker)delegate
     {
       ConnectionLabelError.Text = "Trading is possible";
       LimitTextPriceCurrent.Text = Communication.S(_price.Close);
       EnablePanel(true);
     });
    }

    public void UpdateOrders(List<PositionData> orders)
    {
      Invoke((MethodInvoker)delegate
      {
        CleanEditors(OrdersGrid);

        UpdateGrid(orders, OrdersGrid,
          "OrdersColumnId",
          "OrdersColumnTime",
          "OrdersColumnSymbol",
          "OrdersColumnOperation",
          "OrdersColumnLots",
          "OrdersColumnOpening",
          "OrdersColumnSL",
          "OrdersColumnTP",
          "OrdersColumnPrice",
          "OrdersColumnProfit");

        UpdateGridActions(OrdersGrid, new Dictionary<string, string>
        {
          { "OrdersColumnActionsRemove", "X" }
        });
      });
    }

    public void UpdatePositions(List<PositionData> orders)
    {
      Invoke((MethodInvoker)delegate
      {
        CleanEditors(PositionsGrid);

        UpdateGrid(orders, PositionsGrid,
          "PositionsColumnId",
          "PositionsColumnTime",
          "PositionsColumnSymbol",
          "PositionsColumnOperation",
          "PositionsColumnLots",
          "PositionsColumnOpening",
          "PositionsColumnSL",
          "PositionsColumnTP",
          "PositionsColumnPrice",
          "PositionsColumnProfit");

        UpdateGridActions(PositionsGrid, new Dictionary<string, string>
        {
          { "PositionsColumnActionsRemove", "X" },
          { "PositionsColumnActionsBreakeven", "B" }
        });
      });
    }

    public void UpdateGrid(List<PositionData> orders, DataGridView grid, params string[] cells)
    {
      var rowIds = new List<ulong>();
      var serverIds = orders.Select(x => x.Id).ToList();
      var rows = grid.Rows.Cast<DataGridViewRow>().ToList();
      var baseTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

      foreach (DataGridViewRow row in rows)
      {
        var id = row.Cells[cells[0]].Value;

        if (id != null)
        {
          rowIds.Add(Communication.U(row.Cells[cells[0]].Value));

          if (serverIds.Contains(Communication.U(id)) == false)
          {
            grid.Rows.Remove(row);
          }
          else
          {
            var order = orders.First(x => x.Id == Communication.U(id));

            row.Cells[cells[4]].Value = Communication.S(order.Volume);
            row.Cells[cells[5]].Value = Communication.S(order.Opening);
            row.Cells[cells[6]].Value = Communication.S(order.SL);
            row.Cells[cells[7]].Value = Communication.S(order.TP);
            row.Cells[cells[8]].Value = Communication.S(order.Price);
            row.Cells[cells[9]].Value = Communication.S(order.Profit);
          }
        }
      }

      foreach (PositionData item in orders)
      {
        if (rowIds.Contains(item.Id) == false)
        {
          grid.Rows.Insert(0,
            item.Id,
            baseTime.AddSeconds(item.Time).ToString("yyyy.MM.dd HH:mm"),
            Encoding.Default.GetString(Encoding.Default.GetBytes(item.Currency)).Trim('\0'),
            Encoding.Default.GetString(Encoding.Default.GetBytes(item.Type)).Trim('\0'),
            Communication.S(item.Volume),
            Communication.S(item.Opening),
            Communication.S(item.SL),
            Communication.S(item.TP),
            Communication.S(item.Price),
            Communication.S(item.Profit));
        }
      }
    }

    public void UpdateGridActions(DataGridView grid, Dictionary<string, string> cells)
    {
      var rows = grid.Rows.Cast<DataGridViewRow>().ToList();

      foreach (DataGridViewRow row in rows)
      {
        foreach (KeyValuePair<string, string> cell in cells)
        {
          row.Cells[cell.Key].Value = cell.Value;
        }
      }
    }

    public void UpdateOperation(OperationData operation, OrderData order)
    {
      _operation = operation;

      Invoke((MethodInvoker)delegate
      {
        MainLabelMessage.Text = new string(operation.Message);
      });
    }

    public bool MarketOrderValidation(OrderData order)
    {
      var errors = new Dictionary<string, bool>();

      MainLabelMessage.Text = string.Empty;

      errors.Add("Lot is incorrect", order.Volume > 0);
      errors.Add("No connection with advisor", _price.Close > 0);

      if (errors.Any(x => x.Value == false))
      {
        MainLabelMessage.Text = errors.First(x => x.Value == false).Key;
        return false;
      }

      return true;
    }

    public OrderData MarketOrderPrepare(ENUM_ORDER_TYPE orderType)
    {
      var order = new OrderData();
      var SLPoints = Communication.D(MarketTextSLPoints.Text);
      var TPPoints = Communication.D(MarketTextTPPoints.Text);
      var isOrderBuy = orderType == ENUM_ORDER_TYPE.ORDER_TYPE_BUY;

      if (SLPoints > 0 && _price.Capacity > 0)
      {
        MarketTextSL.Text = Communication.S(isOrderBuy ? _price.Ask - SLPoints * _price.Capacity : _price.Bid + SLPoints * _price.Capacity);
      }

      if (TPPoints > 0 && _price.Capacity > 0)
      {
        MarketTextTP.Text = Communication.S(isOrderBuy ? _price.Ask + TPPoints * _price.Capacity : _price.Bid - TPPoints * _price.Capacity);
      }

      order.TypeFilling = Communication.U(ENUM_ORDER_TYPE_FILLING.ORDER_FILLING_FOK);
      order.Action = Communication.U(ENUM_TRADE_REQUEST_ACTIONS.TRADE_ACTION_DEAL);
      order.TypeTime = Communication.U(ENUM_ORDER_TYPE_TIME.ORDER_TIME_GTC);
      order.Comments = MarketTextComments.Text.PadRight(Constants.AVERAGE_CHAR).ToCharArray();
      order.Volume = Communication.D(MarketTextLots.Text);
      order.SL = Communication.D(MarketTextSL.Text);
      order.TP = Communication.D(MarketTextTP.Text);
      order.Type = Communication.U(orderType);
      order.OrderStatus = 1;
      order.Deviation = 0;
      order.Price = 0;

      return order;
    }

    public OrderData LimitOrderPrepare()
    {
      var order = new OrderData();
      var orderPrice = Communication.D(LimitTextOpen.Text);
      var SLPoints = Communication.D(LimitTextSLPoints.Text);
      var TPPoints = Communication.D(LimitTextTPPoints.Text);
      var orderIndex = Communication.U(LimitComboOrderType.SelectedValue);
      var isOrderBuy = orderIndex == Communication.U(ENUM_ORDER_TYPE.ORDER_TYPE_BUY_STOP) || orderIndex == Communication.U(ENUM_ORDER_TYPE.ORDER_TYPE_BUY_LIMIT);

      if (SLPoints > 0 && _price.Capacity > 0 && orderPrice > 0)
      {
        LimitTextSL.Text = Communication.S(isOrderBuy ? orderPrice - SLPoints * _price.Capacity : orderPrice + SLPoints * _price.Capacity);
      }

      if (TPPoints > 0 && _price.Capacity > 0 && orderPrice > 0)
      {
        LimitTextTP.Text = Communication.S(isOrderBuy ? orderPrice + TPPoints * _price.Capacity : orderPrice - TPPoints * _price.Capacity);
      }

      order.TypeFilling = Communication.U(ENUM_ORDER_TYPE_FILLING.ORDER_FILLING_FOK);
      order.Action = Communication.U(ENUM_TRADE_REQUEST_ACTIONS.TRADE_ACTION_PENDING);
      order.TypeTime = Communication.U(ENUM_ORDER_TYPE_TIME.ORDER_TIME_GTC);
      order.Comments = LimitTextComments.Text.PadRight(Constants.AVERAGE_CHAR).ToCharArray();
      order.Volume = Communication.D(LimitTextLots.Text);
      order.SL = Communication.D(LimitTextSL.Text);
      order.TP = Communication.D(LimitTextTP.Text);
      order.Price = orderPrice;
      order.Type = orderIndex;
      order.OrderStatus = 1;
      order.Deviation = 0;

      return order;
    }

    public bool LimitOrderValidation(OrderData order)
    {
      var errors = new Dictionary<string, bool>();

      MainLabelMessage.Text = string.Empty;

      errors.Add("Lot is incorrect", order.Volume > 0);
      errors.Add("Open price is incorrect", order.Price > 0);
      errors.Add("No connection with advisor", _price.Close > 0);

      if (errors.Any(x => x.Value == false))
      {
        MainLabelMessage.Text = errors.First(x => x.Value == false).Key;
        return false;
      }

      return true;
    }

    public void RemovePosition(DataGridViewRow selection)
    {
      var order = new OrderData();
      var orderType = Communication.S(selection.Cells["PositionsColumnOperation"].Value).ToUpper().Contains("BUY") ?
        Communication.U(ENUM_ORDER_TYPE.ORDER_TYPE_SELL) :
        Communication.U(ENUM_ORDER_TYPE.ORDER_TYPE_BUY);

      order.TypeTime = Communication.U(ENUM_ORDER_TYPE_TIME.ORDER_TIME_GTC);
      order.TypeFilling = Communication.U(ENUM_ORDER_TYPE_FILLING.ORDER_FILLING_FOK);
      order.Action = Communication.U(ENUM_TRADE_REQUEST_ACTIONS.TRADE_ACTION_DEAL);
      order.Volume = Communication.D(selection.Cells["PositionsColumnLots"].Value);
      order.Currency = Communication.S(selection.Cells["PositionsColumnSymbol"].Value).PadRight(Constants.SMALL_CHAR).ToCharArray();
      order.Type = orderType;
      order.OrderStatus = 1;
      order.Deviation = 0;

      Delegation.SetTradeDelegates(order, string.Empty);
    }

    public void ChangePosition(DataGridViewRow selection)
    {
      var order = new OrderData();

      order.TypeTime = Communication.U(ENUM_ORDER_TYPE_TIME.ORDER_TIME_GTC);
      order.Action = Communication.U(ENUM_TRADE_REQUEST_ACTIONS.TRADE_ACTION_SLTP);
      order.SL = Communication.D(selection.Cells["PositionsColumnSL"].Value);
      order.TP = Communication.D(selection.Cells["PositionsColumnTP"].Value);
      order.Currency = Communication.S(selection.Cells["PositionsColumnSymbol"].Value).PadRight(Constants.SMALL_CHAR).ToCharArray();
      order.OrderStatus = 1;

      Delegation.SetTradeDelegates(order, string.Empty);
    }

    public void RemoveOrder(DataGridViewRow selection)
    {
      var order = new OrderData();

      order.Action = Communication.U(ENUM_TRADE_REQUEST_ACTIONS.TRADE_ACTION_REMOVE);
      order.Ticket = Communication.U(selection.Cells["OrdersColumnId"].Value);
      order.OrderStatus = 1;

      Delegation.SetTradeDelegates(order, string.Empty);
    }

    public void ChangeOrder(DataGridViewRow selection)
    {
      var order = new OrderData();

      order.TypeTime = Communication.U(ENUM_ORDER_TYPE_TIME.ORDER_TIME_GTC);
      order.Action = Communication.U(ENUM_TRADE_REQUEST_ACTIONS.TRADE_ACTION_MODIFY);
      order.Price = Communication.D(selection.Cells["OrdersColumnOpening"].Value);
      order.Ticket = Communication.U(selection.Cells["OrdersColumnId"].Value);
      order.SL = Communication.D(selection.Cells["OrdersColumnSL"].Value);
      order.TP = Communication.D(selection.Cells["OrdersColumnTP"].Value);
      order.OrderStatus = 1;

      Delegation.SetTradeDelegates(order, string.Empty);
    }

    public void ActivateEditors(DataGridView grid, int X, int Y)
    {
      var activeCell = grid[X, Y];
      var name = string.Format("EDITOR-{0}-{1}-{2}", X, Y, grid.Name);
      var editor = grid.Parent.Controls.Find(name, false).FirstOrDefault();

      if (editor is TextBox)
      {
        editor.Focus();
      }
    }

    public void HideEditors(DataGridView grid, int rowIndex, bool canUpdate)
    {
      foreach (DataGridViewCell cell in grid.Rows[rowIndex].Cells)
      {
        var name = string.Format("EDITOR-{0}-{1}-{2}", cell.ColumnIndex, cell.RowIndex, grid.Name);
        var editor = grid.Parent.Controls.Find(name, false).FirstOrDefault();

        if (editor is TextBox)
        {
          if (canUpdate)
          {
            cell.Value = editor.Text;
          }

          editor.Hide();
        }

        if (cell.ReadOnly && cell.Visible)
        {
          grid[cell.ColumnIndex, cell.RowIndex].Selected = true;
        }
      }

      grid.ClearSelection();
    }

    public void CleanEditors(DataGridView grid)
    {
      List<System.Windows.Forms.Control> editors = grid.Parent.Controls.OfType<TextBox>().Cast<System.Windows.Forms.Control>().ToList();

      foreach (TextBox editor in editors)
      {
        var numbers = editor.Name.Split('-');
        var rowIndex = Communication.I(numbers[2]);
        var columnIndex = Communication.I(numbers[1]);

        try
        {
          var cell = grid[columnIndex, rowIndex];
        }
        catch (Exception)
        {
          editor.Hide();
        }
      }
    }

    public void DisplayEditors(DataGridView grid, DataGridViewRow row)
    {
      foreach (DataGridViewCell cell in row.Cells)
      {
        if (cell.ReadOnly == false && cell.GetType() != typeof(DataGridViewButtonCell))
        {
          var place = grid.GetCellDisplayRectangle(cell.ColumnIndex, cell.RowIndex, true);
          var name = string.Format("EDITOR-{0}-{1}-{2}", cell.ColumnIndex, cell.RowIndex, grid.Name);
          var editor = grid.Parent.Controls.Find(name, false).FirstOrDefault();

          if (editor == null)
          {
            editor = new TextBox();

            (editor as TextBox).Name = name;
            (editor as TextBox).PreviewKeyDown += ChangeEditorEvent;

            grid.Parent.Controls.Add(editor);
          }
          else
          {
            editor.Show();
          }

          Point lc = place.Location;

          lc.X += grid.Location.X;
          lc.Y += grid.Location.Y;

          place.Location = lc;
          editor.BringToFront();
          editor.Size = place.Size;
          editor.Location = place.Location;
          editor.Text = Communication.S(cell.Value);
        }
      }
    }

    public void ChangeEditorEvent(object sender, PreviewKeyDownEventArgs e)
    {
      var editor = sender as TextBox;
      var numbers = editor.Name.Split('-');
      var rowIndex = Communication.I(numbers[2]);
      var columnIndex = Communication.I(numbers[1]);
      var grid = editor.Parent.Controls.Find(numbers[3], false).FirstOrDefault() as DataGridView;

      if (e.KeyCode == Keys.Return)
      {
        HideEditors(grid, rowIndex, true);

        if (grid.Name.ToUpper().Contains("ORDER"))
        {
          ChangeOrder(grid.Rows[rowIndex]);
        }

        if (grid.Name.ToUpper().Contains("POSITION"))
        {
          ChangePosition(grid.Rows[rowIndex]);
        }
      }

      if (e.KeyCode == Keys.Escape)
      {
        HideEditors(grid, rowIndex, false);
      }
    }

    public void ConnectionButtonOpenClick(object sender, EventArgs e)
    {
      Protect();

      var address = ConnectionTextServerName.Text;

      ConnectionLabelError.Text = "Provide correct server name";

      if (string.IsNullOrEmpty(address) == false)
      {
        _channel = new CustomChannel(address);
        _channel.StartServer();

        OpenConnection(string.Empty);
      }
    }

    public void ConnectionButtonCloseClick(object sender, EventArgs e)
    {
      Protect();

      CloseConnection("No connection");
    }

    public void MarketButtonBuyClick(object sender, EventArgs e)
    {
      Protect();

      OrderData order = MarketOrderPrepare(ENUM_ORDER_TYPE.ORDER_TYPE_BUY);

      if (MarketOrderValidation(order))
      {
        Delegation.SetTradeDelegates(order, MarketTextComments.Text);
        MarketTextSL.Text = string.Empty;
        MarketTextTP.Text = string.Empty;
        MarketTextComments.Text = string.Empty;
      }
    }

    public void MarketButtonSellClick(object sender, EventArgs e)
    {
      Protect();

      OrderData order = MarketOrderPrepare(ENUM_ORDER_TYPE.ORDER_TYPE_SELL);

      if (MarketOrderValidation(order))
      {
        Delegation.SetTradeDelegates(order, MarketTextComments.Text);
        MarketTextSL.Text = string.Empty;
        MarketTextTP.Text = string.Empty;
        MarketTextComments.Text = string.Empty;
      }
    }

    public void LimitButtonSendClick(object sender, EventArgs e)
    {
      Protect();

      OrderData order = LimitOrderPrepare();

      if (LimitOrderValidation(order))
      {
        Delegation.SetTradeDelegates(order, LimitTextComments.Text);
        LimitTextSL.Text = string.Empty;
        LimitTextTP.Text = string.Empty;
        LimitTextComments.Text = string.Empty;
      }
    }

    public void LimitTextPriceCurrentClick(object sender, EventArgs e)
    {
      Protect();

      LimitTextOpen.Text = LimitTextPriceCurrent.Text;
    }

    private void OrdersGridKeyDown(object sender, KeyEventArgs e)
    {
      Protect();

      if (e.KeyCode == Keys.Delete)
      {
        HideEditors(OrdersGrid, OrdersGrid.CurrentCell.RowIndex, false);
        RemoveOrder(OrdersGrid.CurrentCell.OwningRow);
      }
    }

    private void PositionsGridKeyDown(object sender, KeyEventArgs e)
    {
      Protect();

      if (e.KeyCode == Keys.Delete)
      {
        HideEditors(PositionsGrid, PositionsGrid.CurrentCell.RowIndex, false);
        RemovePosition(PositionsGrid.CurrentCell.OwningRow);
      }
    }

    private void GridCellClick(object sender, DataGridViewCellEventArgs e)
    {
      Protect();

      var indexRow = e.RowIndex;
      var indexColumn = e.ColumnIndex;
      var grid = sender as DataGridView;

      if (indexRow > -1 && indexColumn > -1 && grid[indexColumn, indexRow].ReadOnly == false)
      {
        var activeCell = grid[indexColumn, indexRow];

        if (activeCell.GetType() == typeof(DataGridViewButtonCell))
        {
          var name = activeCell.OwningColumn.Name;

          HideEditors(grid, activeCell.RowIndex, false);

          if (name.Equals("PositionsColumnActionsBreakeven"))
          {
            var order = new OrderData();
            var selection = activeCell.OwningRow;

            order.TypeTime = Communication.U(ENUM_ORDER_TYPE_TIME.ORDER_TIME_GTC);
            order.Action = Communication.U(ENUM_TRADE_REQUEST_ACTIONS.TRADE_ACTION_SLTP);
            order.SL = Communication.D(selection.Cells["PositionsColumnOpening"].Value);
            order.TP = Communication.D(selection.Cells["PositionsColumnTP"].Value);
            order.Currency = Communication.S(selection.Cells["PositionsColumnSymbol"].Value).PadRight(Constants.SMALL_CHAR).ToCharArray();
            order.OrderStatus = 1;

            Delegation.SetTradeDelegates(order, string.Empty);
          }

          if (name.Equals("PositionsColumnActionsRemove"))
          {
            RemovePosition(activeCell.OwningRow);
          }

          if (name.Equals("OrdersColumnActionsRemove"))
          {
            RemoveOrder(activeCell.OwningRow);
          }
        }
        else
        {
          DisplayEditors(grid, activeCell.OwningRow);
          ActivateEditors(grid, e.ColumnIndex, e.RowIndex);
        }
      }
    }
  }
}