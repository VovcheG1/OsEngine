using System.Collections.Generic;

namespace OsEngine.Market.Servers.KiteConnect.Json
{
    public class ResponseRestKite<T>
    {
        public string status { get; set; }
        public string message { get; set; }
        public string error_type { get; set; }
        public T data { get; set; }
    }

    public class UserProfile
    {
        public string user_id { get; set; } // The unique, permanent user id registered with the broker and the exchanges
        public string user_name { get; set; } // User's real name
        public string user_shortname { get; set; } // Shortened version of the user's real name
        public string email { get; set; } // User's email
        public string user_type { get; set; } // User's registered role at the broker. This will be individual for all retail users
        public string broker { get; set; } // The broker ID
        public List<string> exchanges { get; set; } // Exchanges enabled for trading on the user's account
        public List<string> products { get; set; } // Margin product types enabled for the user
        public List<string> order_types { get; set; } // Order types enabled for the user
        public Meta meta { get; set; } // demat_consent: empty, consent or physical
        public string avatar_url { get; set; } // Full URL to the user's avatar (PNG image) if there's one
    }

    public class Meta
    {
        public string demat_consent { get; set; } // demat_consent: empty, consent or physical
    }

    public class Data
    {
        public Equity equity { get; set; } // 
        public Commodity commodity { get; set; } // 
    }

    public class Equity
    {
        public string enabled { get; set; } // Indicates whether the segment is enabled for the user
        public string net { get; set; } // Net cash balance available for trading (intraday_payin + adhoc_margin + collateral)
        public Available available { get; set; } // 
        public Utilised utilised { get; set; } // 
    }

    public class Commodity
    {
        public string enabled { get; set; } // Indicates whether the segment is enabled for the user
        public string net { get; set; } // Net cash balance available for trading (intraday_payin + adhoc_margin + collateral)
        public Available available { get; set; } // 
        public Utilised utilised { get; set; } // 
    }

    public class Available
    {
        public string adhoc_margin { get; set; } // Additional margin provided by the broker
        public string cash { get; set; } // Raw cash balance in the account available for trading (also includes intraday_payin)
        public string opening_balance { get; set; } // Opening balance at the day start
        public string live_balance { get; set; } // Current available balance
        public string collateral { get; set; } // Margin derived from pledged stocks
        public string intraday_payin { get; set; } // Amount that was deposited during the day
    }

    public class Utilised
    {
        public string debits { get; set; } // Sum of all utilised margins (unrealised M2M + realised M2M + SPAN + Exposure + Premium + Holding sales)
        public string exposure { get; set; } // Exposure margin blocked for all open F&O positions
        public string m2m_realised { get; set; } // Booked intraday profits and losses
        public string m2m_unrealised { get; set; } // Un-booked (open) intraday profits and losses
        public string option_premium { get; set; } // Value of options premium received by shorting
        public string payout { get; set; } // Funds paid out or withdrawn to bank account during the day
        public string span { get; set; } // SPAN margin blocked for all open F&O positions
        public string holding_sales { get; set; } // Value of holdings sold during the day
        public string turnover { get; set; } // Utilised portion of the maximum turnover limit (only applicable to certain clients)
        public string liquid_collateral { get; set; } // Margin utilised against pledged liquidbees ETFs and liquid mutual funds
        public string stock_collateral { get; set; } // Margin utilised against pledged stocks/ETFs
        public string delivery { get; set; } // Margin blocked when you sell securities (20% of the value of stocks sold) from your demat or T1 holdings
    }
}
