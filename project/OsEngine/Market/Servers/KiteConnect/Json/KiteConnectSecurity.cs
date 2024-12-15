
namespace OsEngine.Market.Servers.KiteConnect.Json
{
    public class KiteConnectSecurity
    {
        public string instrument_token { get; set; } // Numerical identifier used for subscribing to live market quotes with the WebSocket API.
        public string exchange_token { get; set; } // The numerical identifier issued by the exchange representing the instrument.
        public string tradingsymbol { get; set; } // Exchange trading symbol of the instrument
        public string name { get; set; } // Name of the company (for equity instruments)
        public string last_price { get; set; } // Last traded market price
        public string expiry { get; set; } // Expiry date (for derivatives)
        public string strike { get; set; } // Strike (for options)
        public string tick_size { get; set; } // Value of a single price tick
        public string lot_size { get; set; } // Quantity of a single lot
        public string instrument_type { get; set; }  //  EQ, FUT, CE, PE
        public string segment { get; set; } // Segment the instrument belongs to
        public string exchange { get; set; } // Exchange
    }
}
