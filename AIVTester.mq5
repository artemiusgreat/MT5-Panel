#include <Trade/Trade.mqh>
#include <Trade/SymbolInfo.mqh>
#include <Trade/PositionInfo.mqh>
#include <Trade/OrderInfo.mqh>
#include <Trade/DealInfo.mqh>
#include <Files/FilePipe.mqh>

input string InpAddress = "AIV"; // Custom connection name
input bool InpUseBestPrice = true; // Find best price without spread
input int InpTrailingStep = 0; // Step for Trailing Stop in pips
input int InpBreakeven = 0; // Level in pips which start trailing from

// *** Structures *** //

ENUM_TRADE_REQUEST_ACTIONS iActions[5] = 
{ 
    TRADE_ACTION_DEAL,
    TRADE_ACTION_PENDING,
    TRADE_ACTION_SLTP,
    TRADE_ACTION_MODIFY,
    TRADE_ACTION_REMOVE
};

ENUM_ORDER_TYPE iOrderTypes[8] = 
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

ENUM_ORDER_TYPE_FILLING iFillTypes[3] = 
{
    ORDER_FILLING_FOK,
    ORDER_FILLING_IOC,
    ORDER_FILLING_RETURN
};

ENUM_ORDER_TYPE_TIME iTimeTypes[4] = 
{
    ORDER_TIME_GTC,
    ORDER_TIME_DAY,
    ORDER_TIME_SPECIFIED,
    ORDER_TIME_SPECIFIED_DAY
};

struct PriceData
{
    double StopLevel;
    double Open;
    double High;
    double Low;
    double Close;
    double Ask;
    double Bid;
    double Spread;
    double Capacity;
    datetime Time;
};

struct OrderData
{   
    double SL;
    double TP;
    double Price;
    double Volume;
    double StopLevel;
    ulong Magic;
    ulong Ticket;
    ulong Deviation;
    ulong Expiration;
    ulong OrderStatus;
    ulong Type;
    ulong TypeTime;
    ulong Action;
    ulong TypeFilling;
    char Currency[50];
    char Comments[250];
};

struct OperationData
{
    ulong RequestId;
    ulong ReturnCode;
    ulong Deal;
    ulong Order;
    ulong DealStatus;
    double Volume;
    double Price;
    double Bid;
    double Ask;
    char Message[250];
};

struct CountData
{
    long OrdersCount;
    long PositionsCount;
};

struct PositionData
{
    double Volume;
    double Opening;
    double SL;
    double TP;
    double Price;
    double Profit;
    ulong Id;
    datetime Time;
    char Type[50];
    char Currency[50];
};

// *** Classes *** //

class CClient : public CObject
{
    public:
       
    int Magic;
    string Address;
    CFilePipe iPipe;
   
    CClient(string address) 
    { 
        Address = address; 
        Magic = int(TimeLocal());
    };
    
    void Open() 
    { 
        while(IsStopped() == false)
        {
            if (iPipe.Open(Address, FILE_READ | FILE_WRITE | FILE_BIN) != INVALID_HANDLE) break;
        }
    };
    
    void Close() 
    {
        iPipe.Close();
    };
    
    void SetOperation(OperationData &operation) 
    {
        iPipe.WriteStruct(operation);
    };
    
    void SetPrice(PriceData &price) 
    {
        iPipe.WriteStruct(price);
    };
    
    void SetPosition(PositionData &position) 
    {
        iPipe.WriteStruct(position);
    };
    
    void GetOrder(OrderData &trade) 
    {
        iPipe.ReadStruct(trade);
    };
    
    void SetCount(CountData &count) 
    {
        iPipe.WriteStruct(count);
    };
};

// *** Variables *** //

CClient * iClient;

// *** Implementation *** //

int OnInit()
{
    iClient = new CClient("\\\\.\\pipe\\" + InpAddress);
    iClient.Open();

    if (MQL5InfoInteger(MQL5_TESTER) == 0)
    {
        EventSetTimer(1);
    }

    return(0);
}

void OnDeinit(const int reason)
{
    EventKillTimer();
    iClient.Close();
    delete(iClient);
}

void OnTick()
{
    Communication();
}

void OnTimer()
{
    Communication();
}

void Communication()
{
    CSymbolInfo symbolClass;

    // Price

    symbolClass.Name(_Symbol);
    symbolClass.RefreshRates();
    
    PriceData price = {0};

    double close[], open[], high[], low[];

    CopyClose(_Symbol, _Period, 0, 1, close);
    CopyOpen(_Symbol, _Period, 0, 1, open);
    CopyHigh(_Symbol, _Period, 0, 1, high);
    CopyLow(_Symbol, _Period, 0, 1, low);

    PositionTrailing();

    price.StopLevel = symbolClass.StopsLevel();
    price.Spread = symbolClass.Spread();
    price.Time = symbolClass.Time();
    price.Ask = symbolClass.Ask();
    price.Bid = symbolClass.Bid();
    price.Capacity = _Point;
    price.Close = close[0];
    price.Open = open[0];
    price.High = high[0];
    price.Low = low[0];
    
    iClient.SetPrice(price);

    // Order
    
    OrderData order;
    
    iClient.GetOrder(order);
 
    MqlTradeResult result = {0};
    MqlTradeRequest query = {0};

    OperationData operation = {0};
    
    if (order.OrderStatus > 0)
    {
        query.magic = iClient.Magic;
        query.volume = NormalizeDouble(order.Volume, _Digits);
        query.tp = NormalizeDouble(order.TP, _Digits);
        query.sl = NormalizeDouble(order.SL, _Digits);
        query.order = order.Ticket;
        query.deviation = order.Deviation;
        query.type = iOrderTypes[int(order.Type)];
        query.action = iActions[int(order.Action)];
        query.type_time = iTimeTypes[int(order.TypeTime)];
        query.type_filling = iFillTypes[int(order.TypeFilling)];
        query.symbol = Trim(CharArrayToString(order.Currency, 0, sizeof(order.Currency)));
        query.comment = CharArrayToString(order.Comments, 0, sizeof(order.Comments));

        if (StringLen(query.symbol) < 1)
        {
            query.symbol = _Symbol;
        }

        // Find best price

        bool success = FindBestPrice(query, result, symbolClass);

        if (success == false)
        {
            if (order.Price)
            {
                query.price = NormalizeDouble(order.Price, _Digits);
            }
        
            success = OrderSend(query, result);
        }
        
        operation.RequestId = result.request_id;
        operation.ReturnCode = result.retcode;
        operation.Deal = result.deal;
        operation.Order = result.order;
        operation.Volume = result.volume;
        operation.Price = result.price;
        operation.Bid = result.bid;
        operation.Ask = result.ask;
        operation.DealStatus = success ? 1 : 0;
        
        StringToCharArray(GetErrorDescription(result.retcode), operation.Message, 0, sizeof(operation.Message));
    }
    
    iClient.SetOperation(operation);
    
    // Orders and positions count
    
    CountData countData = {0};
    
    HistorySelect(0, TimeCurrent());
    
    countData.OrdersCount = OrdersTotal();
    countData.PositionsCount = PositionsTotal();
    
    iClient.SetCount(countData);

    // List orders
    
    HistorySelect(0, TimeCurrent());
    
    for (int i = 0; i < countData.OrdersCount; i++)
    {
        string orderType;
        COrderInfo currentOrder;
        PositionData positionData = {0};
        
        currentOrder.SelectByIndex(i);

        positionData.Profit = 0;        
        positionData.Time = currentOrder.TimeSetup();
        positionData.Opening = currentOrder.PriceOpen();
        positionData.Price = currentOrder.PriceCurrent();
        positionData.Volume = currentOrder.VolumeCurrent();
        positionData.SL = currentOrder.StopLoss();
        positionData.TP = currentOrder.TakeProfit();
        positionData.Id = currentOrder.Ticket();
        
        currentOrder.FormatType(orderType, currentOrder.OrderType());
        StringToCharArray(orderType, positionData.Type, 0, sizeof(positionData.Type));
        StringToCharArray(currentOrder.Symbol(), positionData.Currency, 0, sizeof(positionData.Currency));
        
        iClient.SetPosition(positionData);
    }
    
    // List positions
    
    HistorySelect(0, TimeCurrent());
    
    for (int i = 0; i < countData.PositionsCount; i++)
    {
        string positionType;
        CPositionInfo currentPosition;
        PositionData positionData = {0};
        
        currentPosition.SelectByIndex(i);

        positionData.Profit = currentPosition.Profit();
        positionData.Time = currentPosition.Time();
        positionData.Opening = currentPosition.PriceOpen();
        positionData.Price = currentPosition.PriceCurrent();
        positionData.Volume = currentPosition.Volume();
        positionData.SL = currentPosition.StopLoss();
        positionData.TP = currentPosition.TakeProfit();
        positionData.Id = currentPosition.Identifier();
        
        currentPosition.FormatType(positionType, currentPosition.PositionType());
        StringToCharArray(positionType, positionData.Type, 0, sizeof(positionData.Type));
        StringToCharArray(currentPosition.Symbol(), positionData.Currency, 0, sizeof(positionData.Currency));
        
        iClient.SetPosition(positionData);
    }
}

bool FindBestPrice(MqlTradeRequest &query, MqlTradeResult &result, CSymbolInfo &symbolClass)
{
    bool success = false;

    if (query.action == TRADE_ACTION_DEAL) 
    {
        if (InpUseBestPrice) 
        {
            if (query.type == ORDER_TYPE_BUY)
            {
                double price = symbolClass.Bid();
                while (success == false && price <= symbolClass.Ask())
                {
                    query.price = NormalizeDouble(price, _Digits);
                    success = OrderSend(query, result);
                    price += _Point;
                }
            }
            
            if (query.type == ORDER_TYPE_SELL)
            {
                double price = symbolClass.Ask();
                while (success == false && price >= symbolClass.Bid())
                {
                    query.price = NormalizeDouble(price, _Digits);
                    success = OrderSend(query, result);
                    price -= _Point;
                }
            }
        }
        else 
        {
            query.price = query.type == ORDER_TYPE_BUY ? NormalizeDouble(symbolClass.Ask(), _Digits) : NormalizeDouble(symbolClass.Bid(), _Digits);
        }
    }
    
    return success;
}

bool PositionTrailing()
{
    CTrade sTrade;
    CSymbolInfo sSym;
    COrderInfo sOrder;
    CPositionInfo sPos;

    sSym.Name(_Symbol);
    sSym.RefreshRates();

    double mAsk = sSym.Ask();
    double mBid = sSym.Bid();
    double mSpread = sSym.Spread() * _Point;
    double mStopLevel = sSym.StopsLevel() * _Point;

    for (int i = 0; i < PositionsTotal() && InpTrailingStep; i++)
    {
        if (_Symbol == PositionGetSymbol(i))
        {
            if (sPos.PositionType() == POSITION_TYPE_BUY)
            {
                double sl = MathMax(sPos.PriceOpen() + InpBreakeven * _Point, sPos.StopLoss()) + InpTrailingStep * _Point;

                if (mBid - mStopLevel > sl)
                {
                    sTrade.PositionModify(_Symbol, sl, sPos.TakeProfit());
                }
            }

            if (sPos.PositionType() == POSITION_TYPE_SELL)
            {
                double sl = MathMin(sPos.PriceOpen() - InpBreakeven * _Point, sPos.StopLoss()) - InpTrailingStep * _Point;

                if (mAsk + mStopLevel < sl)
                {
                    sTrade.PositionModify(_Symbol, sl, sPos.TakeProfit());
                }
            }
        }
    }

    return true;
}

string GetErrorDescription(int code)
{
    string str = "Unknown error";

    switch (code)
    {
        case TRADE_RETCODE_REQUOTE: str = "Requote"; break;
        case TRADE_RETCODE_REJECT: str = "Request rejected"; break;
        case TRADE_RETCODE_CANCEL: str = "Request canceled by trader"; break;
        case TRADE_RETCODE_PLACED: str = "Order placed"; break;
        case TRADE_RETCODE_DONE: str = "Request completed"; break;
        case TRADE_RETCODE_DONE_PARTIAL: str = "Only part of the request was completed"; break;
        case TRADE_RETCODE_ERROR: str = "Request processing error"; break;
        case TRADE_RETCODE_TIMEOUT: str = "Request canceled by timeout"; break;
        case TRADE_RETCODE_INVALID: str = "Invalid request"; break;
        case TRADE_RETCODE_INVALID_VOLUME: str = "Invalid volume in the request"; break;
        case TRADE_RETCODE_INVALID_PRICE: str = "Invalid price in the request"; break;
        case TRADE_RETCODE_INVALID_STOPS: str = "Invalid stops in the request"; break;
        case TRADE_RETCODE_TRADE_DISABLED: str = "Trade is disabled"; break;
        case TRADE_RETCODE_MARKET_CLOSED: str = "Market is closed"; break;
        case TRADE_RETCODE_NO_MONEY: str = "There is not enough money to complete the request"; break;
        case TRADE_RETCODE_PRICE_CHANGED: str = "Prices changed"; break;
        case TRADE_RETCODE_PRICE_OFF: str = "There are no quotes to process the request"; break;
        case TRADE_RETCODE_INVALID_EXPIRATION: str = "Invalid order expiration date in the request"; break;
        case TRADE_RETCODE_ORDER_CHANGED: str = "Order state changed"; break;
        case TRADE_RETCODE_TOO_MANY_REQUESTS: str = "Too frequent requests"; break;
        case TRADE_RETCODE_NO_CHANGES: str = "No changes in request"; break;
        case TRADE_RETCODE_SERVER_DISABLES_AT: str = "Autotrading disabled by server"; break;
        case TRADE_RETCODE_CLIENT_DISABLES_AT: str = "Autotrading disabled by client terminal"; break;
        case TRADE_RETCODE_LOCKED: str = "Request locked for processing"; break;
        case TRADE_RETCODE_FROZEN: str = "Order or position frozen"; break;
        case TRADE_RETCODE_INVALID_FILL: str = "Invalid order filling type"; break;
        case TRADE_RETCODE_CONNECTION: str = "No connection with the trade server"; break;
        case TRADE_RETCODE_ONLY_REAL: str = "Operation is allowed only for live accounts"; break;
        case TRADE_RETCODE_LIMIT_ORDERS: str = "The number of pending orders has reached the limit"; break;
        case TRADE_RETCODE_LIMIT_VOLUME: str = "The volume of orders and positions for the symbol has reached the limit"; break;
        case TRADE_RETCODE_INVALID_ORDER: str = "Incorrect or prohibited order type"; break;
        case TRADE_RETCODE_POSITION_CLOSED: str = "Position with the specified POSITION_IDENTIFIER has already been closed"; break;
    }

    return (str);
}

void CustomLog(const string message) 
{
    Print(message);
}

string Trim(string content)
{
    StringTrimLeft(content);
    StringTrimRight(content);
    return(content);
} 
