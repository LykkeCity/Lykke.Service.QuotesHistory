﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Domain.Prices.Contracts;

namespace Lykke.Service.QuotesHistory.Core.Domain.Quotes
{
    public interface IQuoteHistoryRepository
    {
        /// <summary>
        /// Returns quotes for the specified minute.
        /// </summary>
        /// <param name="minute">Seconds and milliseconds are not significant.</param>
        Task<IEnumerable<IQuote>> GetQuotesAsync(string asset, bool isBuy, DateTime minute);

        /// <summary>
        /// Returns quotes for the specified asset pair for the requested time period. Continous reading is enabled for large data sets.
        /// </summary>
        /// <param name="asset">Asset pair to filter quotes</param>
        /// <param name="isBuy">IsBuy sign to filter quotes</param>
        /// <param name="fromMoment">Starting date and time for the query (exclusive).</param>
        /// <param name="toMoment">Ending date and time for the query (inclusive)</param>
        /// <param name="continuationToken">Continuation token (if any).</param>
        /// <remarks>The maximum amount of returned quotes per query is limited by Azure - 1 000 items.</remarks>
        Task<(IEnumerable<IQuote> Quotes, string ContinuationToken)> GetQuotesBulkAsync(string asset, bool isBuy, DateTime fromMoment, DateTime toMoment, string continuationToken = null);

        /// <summary>
        /// Inserts or merges specified quote to the azure table
        /// </summary>
        /// <param name="quote"></param>
        /// <returns></returns>
        Task InsertOrMergeAsync(IQuote quote);

        /// <summary>
        /// Filters quotes with specified asset and isBuy sign and inserts them (or merges) to the azure table.
        /// </summary>
        /// <param name="quotes">Collection of quotes to insert/merge</param>
        /// <param name="asset">Asset pair to filter quotes</param>
        /// <param name="isBuy">IsBuy sign to filter quotes</param>
        Task InsertOrMergeAsync(IReadOnlyCollection<IQuote> quotes, string asset, bool isBuy);
    }
}
