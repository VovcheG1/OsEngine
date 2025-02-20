﻿using OsEngine.Entity;
using OsEngine.Market.Servers.CoinEx.Spot.Entity.Enums;
using System;


namespace OsEngine.Market.Servers.CoinEx.Spot.Entity
{
    /*
        https://docs.coinex.com/api/v2/spot/deal/http/list-user-order-deals#return-parameters
    */
    struct CexTransaction
    {
        // Transaction id
        public long deal_id { get; set; }

        // Transaction timestamp, millisecond
        public long created_at { get; set; }

        // Market name
        public string market { get; set; }

        // Buy or sell
        public string side { get; set; }

        // Price
        public string price { get; set; }

        // Filled volume
        // Amount - объём в единицах тикера
        // Value - объём в деньгах
        public string amount { get; set; }

        public static explicit operator Trade(CexTransaction cexTrade)
        {
            Trade trade = new Trade();
            trade.Id = cexTrade.deal_id.ToString();
            //trade.SecurityNameCode = cexTrade.market;
            //trade.Time = CoinExServerRealization.ConvertToDateTimeFromUnixFromMilliseconds(cexTrade.created_at);
            trade.Time = new DateTime(1970, 1, 1).AddMilliseconds(cexTrade.created_at);
            trade.Side = (cexTrade.side == CexOrderSide.BUY.ToString()) ? Side.Buy : Side.Sell;
            trade.Price = cexTrade.price.ToString().ToDecimal();
            trade.Volume = cexTrade.amount.ToString().ToDecimal();

            return trade;
        }

    }
}
