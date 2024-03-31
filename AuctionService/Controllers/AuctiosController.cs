using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Controllers
{
    [ApiController]
    [Route("api/auctions")]
    public class AuctiosController : ControllerBase
    {
        private readonly AuctionDbContext context;
        private readonly IMapper mapper;
        private readonly IPublishEndpoint publishEndpoint;

        public AuctiosController(AuctionDbContext context, 
                                 IMapper mapper, 
                                 IPublishEndpoint publishEndpoint) 
        {
            this.context = context;
            this.mapper = mapper;
            this.publishEndpoint = publishEndpoint;
        }

        [HttpGet]
        public async Task<ActionResult<List<AuctionDto>>> GetAllAuctions(string date)
        {
            IQueryable<Auction> query = this.context.Auctions.OrderBy(x => x.Item.Make).AsQueryable();

            if (!string.IsNullOrEmpty(date))
            {
                query = query.Where(x => x.UpdatedAt.CompareTo(DateTime.Parse(date).ToUniversalTime()) > 0);
            }

            return await query.ProjectTo<AuctionDto>(this.mapper.ConfigurationProvider).ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AuctionDto>> GetAuctionById(Guid id)
        {
            Auction auction = await this.context.Auctions
                .Include(x => x.Item)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (auction == null)
            {
                return NotFound();
            }

            return this.mapper.Map<AuctionDto>(auction);
        }

        [HttpPost]
        public async Task<ActionResult<AuctionDto>> CreateAuction(CreateAuctionDto auctionDto)
        {
            Auction auction = this.mapper.Map<Auction>(auctionDto);
            //TODO: Add current user as seller

            auction.Seller = "test";
            this.context.Auctions.Add(auction);
            AuctionDto newAuction = this.mapper.Map<AuctionDto>(auction);
            await this.publishEndpoint.Publish(this.mapper.Map<AuctionCreated>(newAuction));
            bool result = await this.context.SaveChangesAsync() > 0;

            if(!result)
            {
                return BadRequest("Could not save changes to the DB");
            }

            return CreatedAtAction(nameof(GetAuctionById), new { auction.Id }, newAuction);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateAuction(Guid id, UpdateAuctionDto updateAuctionDto)
        {
            Auction auction = await this.context.Auctions
                .Include(x => x.Item)
                .FirstOrDefaultAsync(x => x.Id == id);

            if(auction == null)
            {
                return NotFound();
            }

            //TODO: check seller == username;

            auction.Item.Make = updateAuctionDto.Make ?? auction.Item.Make;
            auction.Item.Model = updateAuctionDto.Model ?? auction.Item.Model;
            auction.Item.Color = updateAuctionDto.Color ?? auction.Item.Color;
            auction.Item.Mileage = updateAuctionDto.Mileage ?? auction.Item.Mileage;
            auction.Item.Year = updateAuctionDto.Year ?? auction.Item.Year;

            await this.publishEndpoint.Publish(this.mapper.Map<AuctionUpdated>(auction));

            bool result = await this.context.SaveChangesAsync() > 0;
            
            if(result)
            {
                return Ok(result);
            }

            return BadRequest("Problem saving changes.");
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAuction(Guid id)
        {
            var auction = await this.context.Auctions.FindAsync(id);
            if(auction == null)
            {
                return NotFound();
            }

            // TODO: check seller == username

            this.context.Auctions.Remove(auction);

            await this.publishEndpoint.Publish(new AuctionDeleted { Id = auction.Id.ToString() });

            bool result = await this.context.SaveChangesAsync() > 0;

            if (!result)
            {
                return BadRequest("Could not update DB.");
            }

            return Ok();
        }
    }
}
