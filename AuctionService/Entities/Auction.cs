﻿using System.ComponentModel.DataAnnotations.Schema;

namespace AuctionService.Entities
{
    [Table("Auctions")]
    public class Auction
    {
        public Guid Id { get; set; }
        public int ReservedPrice { get; set; } = 0;
        public string Seller {  get; set; }
        public string Winner { get; set; }
        public int? SoldAmount { get; set; }
        public int? CurrentHighBid { get; set; }
        public DateTime CreateAt { get; set; } = DateTime.UtcNow;
        public DateTime AuctionEnd { get; set; } = DateTime.UtcNow;
        public Status Status { get; set; }
        public Item Item { get; set; }
    }
}