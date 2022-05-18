﻿using AlwaysOn.Shared.Interfaces;
using AlwaysOn.Shared.Models;
using AlwaysOn.Shared.Models.DataTransfer;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AlwaysOn.Shared.Services
{
    public class SqlDatabaseService : IDatabaseService
    {
        //private readonly AoWriteDbContext _writeDbContext;
        //private readonly AoReadDbContext _readDbContext;
        private readonly AoDbContext _dbContext;
        

        //public SqlDatabaseService(AoWriteDbContext writeContext, AoReadDbContext readContext = null)
        //{
        //    _writeDbContext = writeContext;
        //    _readDbContext = readContext;
        //}

        public SqlDatabaseService(AoDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddNewCatalogItemAsync(CatalogItem catalogItem)
        {
            //_writeDbContext.CatalogItems.Add(catalogItem);
            _dbContext.CatalogItemsWrite.Add(catalogItem);

            //await _writeDbContext.SaveChangesAsync();
            await _dbContext.SaveChangesAsync();
        }

        public async Task AddNewCommentAsync(ItemComment comment)
        {
            //_writeDbContext.ItemComments.Add(comment);
            _dbContext.ItemCommentsWrite.Add(comment);

            await _dbContext.SaveChangesAsync(); // check if the number of results is 1
        }

        public async Task AddNewRatingAsync(ItemRating rating)
        {
            //_writeDbContext.ItemRatings.Add(rating);
            _dbContext.ItemRatingsWrite.Add(rating);

            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteItemAsync<T>(string itemId, string partitionKey = null)
        {
            var idGuid = Guid.Parse(itemId);

            if (typeof(T) == typeof(CatalogItem))
            {
                var item = new CatalogItem() { Id = idGuid };
                _dbContext.Entry(item).State = EntityState.Deleted;
            }
            else if (typeof(T) == typeof(ItemComment))
            {
                var item = new ItemComment() { Id = idGuid };
                _dbContext.Entry(item).State = EntityState.Deleted;
            }
            else if (typeof(T) == typeof(ItemRating))
            {
                var item = new ItemRating() { Id = idGuid };
                _dbContext.Entry(item).State = EntityState.Deleted;
            }
            else
            {
                //_logger.LogWarning($"Unsupported type {typeof(T).Name} for deletion");
            }

            await _dbContext.SaveChangesAsync();
        }


        public async Task<RatingDto> GetAverageRatingForCatalogItemAsync(Guid itemId)
        {
            RatingDto avgRating = null;
            
            // In Cosmos we do this:
            //  SELECT AVG(c.rating) as averageRating, count(1) as numberOfVotes FROM c WHERE c.catalogItemId = @itemId
            // TODO: Is that possible in EF without using raw query?

            try {
                var ratings = await _dbContext.ItemRatingsRead.Where(i => i.CatalogItemId == itemId).ToListAsync();

                avgRating = new RatingDto()
                {
                    AverageRating = ratings.Average(r => r.Rating),
                    NumberOfVotes = ratings.Count
                };
            }
            catch (Exception e)
            {

            }

            return avgRating;
        }

        public async Task<CatalogItem> GetCatalogItemByIdAsync(Guid itemId)
        {
            var res = await _dbContext
                                .CatalogItemsRead
                                .FirstOrDefaultAsync(i => i.Id == itemId);

            return res;
        }

        public async Task<ItemComment> GetCommentByIdAsync(Guid commentId, Guid itemId)
        {
            var res = await _dbContext
                                .ItemCommentsRead
                                .FirstOrDefaultAsync(c => c.Id == commentId);

            return res;
        }

        public async Task<IEnumerable<ItemComment>> GetCommentsForCatalogItemAsync(Guid itemId, int limit)
        {
            var comments = await _dbContext
                                    .ItemCommentsRead
                                    .Where(i => i.CatalogItemId == itemId)
                                    .Take(limit)
                                    .ToListAsync();

            return comments;
        }

        public async Task<ItemRating> GetRatingByIdAsync(Guid ratingId, Guid itemId)
        {
            var res = await _dbContext
                                .ItemRatingsRead
                                .FirstOrDefaultAsync(r => r.Id == ratingId);

            return res; // null == not found
        }

        public async Task<bool> IsHealthy(CancellationToken cancellationToken = default)
        {
            var res = await _dbContext.Database.ExecuteSqlRawAsync("SELECT CURRENT_TIMESTAMP");
            //TODO: add write context test

            // Expecting -1 as the number of affected rows, since this query is not working with data.
            if (res == -1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<IEnumerable<CatalogItem>> ListCatalogItemsAsync(int limit)
        {
            var res = await _dbContext
                                .CatalogItemsRead
                                .OrderBy(i => i.Name)
                                .Take(limit)
                                .ToListAsync();

            return res;
        }

        public async Task UpsertCatalogItemAsync(CatalogItem item)
        {
            // check if we're tracking this entity and if not, add it
            var existingItem = _dbContext.CatalogItemsRead.Where(i => i.Id == item.Id).FirstOrDefault();
            if (existingItem == null)
            {
                _dbContext.CatalogItemsWrite.Add(item);
            }
            else
            {
                _dbContext.CatalogItemsWrite.Update(item);
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}