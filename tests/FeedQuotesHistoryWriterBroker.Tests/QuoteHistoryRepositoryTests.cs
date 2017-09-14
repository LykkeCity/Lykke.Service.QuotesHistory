﻿using System;
using System.Collections.Generic;
using System.Linq;
using AzureStorage;
using AzureStorage.Tables;
using AzureStorage.Tables.Decorators;
using Castle.Core.Logging;
using Common.Log;
using FeedQuotesHistoryWriterBroker.Repositories;
using Lykke.AzureStorage;
using Lykke.Domain.Prices.Model;
using Microsoft.WindowsAzure.Storage.Table;
using Xunit;

namespace FeedQuotesHistoryWriterBroker.Tests
{
    public class QuoteHistoryRepositoryTests
    {
        /// <summary>
        /// Two quotes with the same asset and isBuy sign can have different timestamp, and they will be both stored.
        /// </summary>
        [Fact(Skip = "Hangs in TC")]
        public void RepositoryCanStoreQuotesWithSameTimestamp()
        {
            var log = new LogToMemory();
            var storage = CreateStorage<QuoteTableEntity>(log);
            var repo = new QuoteHistoryRepository(storage);

            var asset = "EURUSD";
            var baseTime = new DateTime(2017, 03, 12, 13, 14, 05, DateTimeKind.Utc);
            var quotes = new List<Quote>()
            {
                new Quote() { AssetPair=asset, IsBuy = true, Price = 1, Timestamp = baseTime }
            };

            // Insert the same quote two times
            repo.InsertOrMergeAsync(quotes, asset, isBuy: true).Wait();
            repo.InsertOrMergeAsync(quotes, asset, isBuy: true).Wait();

            // Storage must contain two instances.
            var storedQuotes = repo.GetQuotesAsync(asset, true, baseTime).Result;

            Assert.NotNull(storedQuotes);
            Assert.Equal(2, storedQuotes.Count());
            Assert.Equal(0, log.Count);
        }

        /// <summary>
        /// Repository can store quotes with different row keys in one call
        /// </summary>
        [Fact(Skip = "Hangs in TC")]
        public void RepositoryCanStoreMultipleRows()
        {
            var log = new LogToMemory();
            var storage = CreateStorage<QuoteTableEntity>(log);
            var repo = new QuoteHistoryRepository(storage);

            var asset = "EURUSD";
            var baseTime = new DateTime(2017, 03, 12, 13, 14, 05, DateTimeKind.Utc);
            var quotes = new List<Quote>()
            {
                new Quote() { AssetPair=asset, IsBuy = true, Price = 1, Timestamp = baseTime },
                new Quote() { AssetPair=asset, IsBuy = true, Price = 2, Timestamp = baseTime.AddMinutes(1).AddSeconds(1) },
                new Quote() { AssetPair=asset, IsBuy = false, Price = 3, Timestamp = baseTime },
                new Quote() { AssetPair=asset, IsBuy = false, Price = 4, Timestamp = baseTime.AddMinutes(1).AddSeconds(2) }
            };

            // This call will insert only Buy prices
            repo.InsertOrMergeAsync(quotes, asset, isBuy: true).Wait();
            // This call will insert only Sell prices
            repo.InsertOrMergeAsync(quotes, asset, isBuy: false).Wait();

            // Storage contains 4 rows
            var storedQuotes = repo.GetQuotesAsync(asset, true, baseTime).Result;
            Assert.Equal(1, storedQuotes.Count());
            storedQuotes = repo.GetQuotesAsync(asset, true, baseTime.AddMinutes(1)).Result;
            Assert.Equal(1, storedQuotes.Count());

            storedQuotes = repo.GetQuotesAsync(asset, false, baseTime).Result;
            Assert.Equal(1, storedQuotes.Count());
            storedQuotes = repo.GetQuotesAsync(asset, false, baseTime.AddMinutes(1)).Result;
            Assert.Equal(1, storedQuotes.Count());

            Assert.Equal(0, log.Count);
        }

        /// <summary>
        /// Large quote collections will be distributed across separate properties ("columns").
        /// </summary>
        [Fact(Skip = "Hangs in TC")]
        public void RepositoryCanUtilizeMultipleProperties()
        {
            var log = new LogToMemory();
            var storage = CreateStorage<QuoteTableEntity>(log);
            var repo = new QuoteHistoryRepository(storage);

            var asset = "EURUSD";
            var baseTime = new DateTime(2017, 03, 12, 13, 14, 05, DateTimeKind.Utc);
            var quotes = new List<Quote>();
            // Create a lot of quotes in one second (will be placed in one row)
            for (int i = 0; i < 2000; i++)
            {
                quotes.Add(new Quote() { AssetPair = asset, IsBuy = true, Price = i / 10, Timestamp = baseTime.AddMilliseconds(i / 2) });
            }

            // Insert all quotes
            repo.InsertOrMergeAsync(quotes, asset, isBuy: true).Wait();

            // Storage must be able to read quotes.
            var storedQuotes = repo.GetQuotesAsync(asset, true, baseTime).Result;

            Assert.NotNull(storedQuotes);
            Assert.Equal(2000, storedQuotes.Count());
            Assert.Equal(0, log.Count);
        }
        
        private INoSQLTableStorage<T> CreateStorage<T>(ILog logger, bool clear = true) where T : class, ITableEntity, new()
        {
            //var table = new AzureTableStorage<T>("UseDevelopmentStorage=true;", "QuotesHistoryTest", logger);
            //if (clear)
            //{
            //    ClearTable(table);
            //}
            //return table;

            return new NoSqlTableInMemory<T>();
        }

        private static void ClearTable<T>(AzureTableStorage<T> table) where T : class, ITableEntity, new()
        {
            var entities = new List<T>();
            do
            {
                entities.Clear();
                table.GetDataByChunksAsync(collection => entities.AddRange(collection)).Wait();
                entities.ForEach(e => table.DeleteAsync(e).Wait());
            } while (entities.Count > 0);
        }
    }
}
