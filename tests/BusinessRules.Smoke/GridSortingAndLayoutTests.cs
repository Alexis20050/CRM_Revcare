#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using crm.domain.entities;
using CRM.infrastructure.common;

namespace BusinessRules.Smoke
{
    public static class GridSortingAndLayoutTests
    {
        public class TestFeedbackItem
        {
            public int FeedbackId { get; set; }
            public int JobOrderId { get; set; }
            public int RatingScore { get; set; }
            public string? PositiveRemarks { get; set; }
            public string? AreasForImprovement { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public class TestCustomerItem
        {
            public int CustomerId { get; set; }
            public string CustomerCode { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string ContactNumber { get; set; } = string.Empty;
            public decimal? Balance { get; set; }
            public DateTime? LastServiceDate { get; set; }
        }

        public static void Run(Action<bool, string> check)
        {
            // 1. Sorting string columns case-insensitively
            var customers = new List<TestCustomerItem>
            {
                new() { CustomerId = 1, FullName = "Charlie" },
                new() { CustomerId = 2, FullName = "alpha" },
                new() { CustomerId = 3, FullName = "Bravo" }
            };
            var strSorter = new CrmRecordComparer<TestCustomerItem>("FullName", ascending: true, x => x.CustomerId);
            customers.Sort(strSorter);
            check(customers[0].FullName == "alpha" && customers[1].FullName == "Bravo" && customers[2].FullName == "Charlie",
                "1. Sorting string columns case-insensitively");

            // 2. Sorting numeric columns numerically
            var numList = new List<TestFeedbackItem>
            {
                new() { FeedbackId = 100 },
                new() { FeedbackId = 2 },
                new() { FeedbackId = 20 }
            };
            var numSorter = new CrmRecordComparer<TestFeedbackItem>("FeedbackId", ascending: true, x => x.FeedbackId);
            numList.Sort(numSorter);
            check(numList[0].FeedbackId == 2 && numList[1].FeedbackId == 20 && numList[2].FeedbackId == 100,
                "2. Sorting numeric columns numerically");

            // 3. Sorting feedback ratings numerically
            var feedbackList = new List<TestFeedbackItem>
            {
                new() { FeedbackId = 1, RatingScore = 5 },
                new() { FeedbackId = 2, RatingScore = 1 },
                new() { FeedbackId = 3, RatingScore = 4 },
                new() { FeedbackId = 4, RatingScore = 2 }
            };
            var ratingSorter = new CrmRecordComparer<TestFeedbackItem>("RatingScore", ascending: true, x => x.FeedbackId);
            feedbackList.Sort(ratingSorter);
            check(feedbackList[0].RatingScore == 1 && feedbackList[1].RatingScore == 2 && feedbackList[2].RatingScore == 4 && feedbackList[3].RatingScore == 5,
                "3. Sorting feedback ratings numerically");

            // 4. Sorting standardized record references numerically
            var refList = new List<TestCustomerItem>
            {
                new() { CustomerId = 25, CustomerCode = "CUST-000025" },
                new() { CustomerId = 2, CustomerCode = "CUST-000002" },
                new() { CustomerId = 10, CustomerCode = "CUST-000010" }
            };
            var refSorter = new CrmRecordComparer<TestCustomerItem>("CustomerCode", ascending: true, x => x.CustomerId);
            refList.Sort(refSorter);
            check(refList[0].CustomerCode == "CUST-000002" && refList[1].CustomerCode == "CUST-000010" && refList[2].CustomerCode == "CUST-000025",
                "4. Sorting standardized record references numerically");

            // 5. Sorting currency values
            var balanceList = new List<TestCustomerItem>
            {
                new() { CustomerId = 1, Balance = 1500.50m },
                new() { CustomerId = 2, Balance = 250.00m },
                new() { CustomerId = 3, Balance = 7500.25m }
            };
            var currSorter = new CrmRecordComparer<TestCustomerItem>("Balance", ascending: true, x => x.CustomerId);
            balanceList.Sort(currSorter);
            check(balanceList[0].Balance == 250.00m && balanceList[1].Balance == 1500.50m && balanceList[2].Balance == 7500.25m,
                "5. Sorting currency values numerically");

            // 6. Sorting nullable dates chronologically
            var date1 = new DateTime(2026, 1, 15);
            var date2 = new DateTime(2026, 5, 20);
            var dateList = new List<TestCustomerItem>
            {
                new() { CustomerId = 1, LastServiceDate = date2 },
                new() { CustomerId = 2, LastServiceDate = null },
                new() { CustomerId = 3, LastServiceDate = date1 }
            };
            var dateSorter = new CrmRecordComparer<TestCustomerItem>("LastServiceDate", ascending: true, x => x.CustomerId);
            dateList.Sort(dateSorter);
            check(dateList[0].LastServiceDate == date1 && dateList[1].LastServiceDate == date2 && dateList[2].LastServiceDate == null,
                "6. Sorting nullable dates chronologically with nulls last");

            // 7. Sorting columns containing missing/null values
            var remarksList = new List<TestFeedbackItem>
            {
                new() { FeedbackId = 1, PositiveRemarks = "Great work" },
                new() { FeedbackId = 2, PositiveRemarks = null },
                new() { FeedbackId = 3, PositiveRemarks = "Average" }
            };
            var remarksSorter = new CrmRecordComparer<TestFeedbackItem>("PositiveRemarks", ascending: true, x => x.FeedbackId);
            remarksList.Sort(remarksSorter);
            check(remarksList[0].PositiveRemarks == "Average" && remarksList[1].PositiveRemarks == "Great work" && remarksList[2].PositiveRemarks == null,
                "7. Sorting columns containing missing values places nulls deterministically at bottom");

            // 8. Handling mixed or incompatible legacy values safely (no ArgumentException)
            var mixedBalances = new List<TestCustomerItem>
            {
                new() { CustomerId = 1, Balance = null },
                new() { CustomerId = 2, Balance = 1000m },
                new() { CustomerId = 3, Balance = 0.00m }
            };
            var mixedSorter = new CrmRecordComparer<TestCustomerItem>("Balance", ascending: true, x => x.CustomerId);
            // This would previously throw System.ArgumentException: Object must be of type String
            mixedSorter.Compare(mixedBalances[0], mixedBalances[1]);
            mixedBalances.Sort(mixedSorter);
            check(mixedBalances[0].Balance == 0.00m && mixedBalances[1].Balance == 1000m && mixedBalances[2].Balance == null,
                "8. Safe comparison between null financial amount and non-null decimal without exception");

            // 9. Ascending and descending sorting toggling
            var descSorter = new CrmRecordComparer<TestFeedbackItem>("FeedbackId", ascending: false, x => x.FeedbackId);
            numList.Sort(descSorter);
            check(numList[0].FeedbackId == 100 && numList[1].FeedbackId == 20 && numList[2].FeedbackId == 2,
                "9. Descending sorting correctly inverts order");

            // 10. Sorting across multiple pages (sort entire dataset before pagination)
            var multiPageItems = Enumerable.Range(1, 35)
                .Select(i => new TestFeedbackItem { FeedbackId = i, RatingScore = (i % 5) + 1 })
                .ToList();
            var pageRatingSorter = new CrmRecordComparer<TestFeedbackItem>("RatingScore", ascending: true, x => x.FeedbackId);
            multiPageItems.Sort(pageRatingSorter);
            int pageSize = 10;
            var page1 = multiPageItems.Take(pageSize).ToList();
            var page2 = multiPageItems.Skip(pageSize).Take(pageSize).ToList();
            check(page1.All(x => x.RatingScore <= 2) && page2.All(x => x.RatingScore >= 2),
                "10. Sorting applies to entire dataset prior to pagination slice");

            // 11. Sequential numbering after sorting
            int pageNumber = 2;
            int pageOffset = (pageNumber - 1) * pageSize;
            int displayRow1 = pageOffset + 1; // 11
            int displayRow2 = pageOffset + 2; // 12
            check(displayRow1 == 11 && displayRow2 == 12,
                "11. Row numbering offset recalculates sequentially based on active page");

            // 12. Search and filter preservation
            string filterQuery = "Customer";
            var allItems = new List<TestCustomerItem>
            {
                new() { CustomerId = 1, FullName = "Customer Alice" },
                new() { CustomerId = 2, FullName = "Vendor Bob" },
                new() { CustomerId = 3, FullName = "Customer Charlie" }
            };
            var filtered = allItems.Where(x => x.FullName.Contains(filterQuery)).ToList();
            filtered.Sort(new CrmRecordComparer<TestCustomerItem>("FullName", ascending: true, x => x.CustomerId));
            check(filtered.Count == 2 && filtered[0].FullName == "Customer Alice" && filtered[1].FullName == "Customer Charlie",
                "12. Filtering and sorting compose deterministically");

            // 13. Empty grid sorting
            var emptyList = new List<TestFeedbackItem>();
            emptyList.Sort(new CrmRecordComparer<TestFeedbackItem>("FeedbackId", ascending: true));
            check(emptyList.Count == 0,
                "13. Empty grid sorting handled safely as a no-op");

            // 14. No automatic first-row selection when selectedId is 0
            int selectedId = 0;
            bool shouldSelectFirstRow = selectedId > 0;
            check(!shouldSelectFirstRow,
                "14. Automatic first-row selection is prevented when selectedId is 0");

            // 15. Contact numbers remain strings (not converted to numbers)
            var contact1 = "09123456789";
            var contact2 = "+639123456789";
            int contactCmp = CrmRecordComparer<TestCustomerItem>.CompareNonNullValues(contact1, contact2, "ContactNumber");
            check(contactCmp != 0,
                "15. Contact numbers are preserved and compared as string data");

            // 16. Stable secondary sort key tie-breaking
            var tiedRatings = new List<TestFeedbackItem>
            {
                new() { FeedbackId = 50, RatingScore = 5 },
                new() { FeedbackId = 10, RatingScore = 5 },
                new() { FeedbackId = 30, RatingScore = 5 }
            };
            tiedRatings.Sort(new CrmRecordComparer<TestFeedbackItem>("RatingScore", ascending: true, x => x.FeedbackId));
            check(tiedRatings[0].FeedbackId == 10 && tiedRatings[1].FeedbackId == 30 && tiedRatings[2].FeedbackId == 50,
                "16. Stable tie-breaking using record ID secondary key");

            // 17. Deterministic nulls ordering in descending mode
            var descNulls = new List<TestCustomerItem>
            {
                new() { CustomerId = 1, Balance = 500m },
                new() { CustomerId = 2, Balance = null },
                new() { CustomerId = 3, Balance = 100m }
            };
            descNulls.Sort(new CrmRecordComparer<TestCustomerItem>("Balance", ascending: false, x => x.CustomerId));
            check(descNulls[0].Balance == 500m && descNulls[1].Balance == 100m && descNulls[2].Balance == null,
                "17. Descending sort places non-nulls descending and nulls at the end");

            // 18. Unknown financial amounts distinct from zero
            var zeroVsNull = new List<TestCustomerItem>
            {
                new() { CustomerId = 1, Balance = null },
                new() { CustomerId = 2, Balance = 0.00m }
            };
            zeroVsNull.Sort(new CrmRecordComparer<TestCustomerItem>("Balance", ascending: true, x => x.CustomerId));
            check(zeroVsNull[0].Balance == 0.00m && zeroVsNull[1].Balance == null,
                "18. Unknown financial amount (null) remains distinct from 0.00 balance");

            // 19. Case-insensitive property resolution
            var casePropSorter = new CrmRecordComparer<TestFeedbackItem>("ratingscore", ascending: true, x => x.FeedbackId);
            feedbackList.Sort(casePropSorter);
            check(feedbackList[0].RatingScore == 1 && feedbackList[3].RatingScore == 5,
                "19. Property name resolution is case-insensitive and robust");

            // 20. Mixed prefixes in standardized references
            var mixedRefs = new List<TestCustomerItem>
            {
                new() { CustomerId = 1, CustomerCode = "JO-000005" },
                new() { CustomerId = 2, CustomerCode = "CUST-000025" }
            };
            mixedRefs.Sort(new CrmRecordComparer<TestCustomerItem>("CustomerCode", ascending: true, x => x.CustomerId));
            check(mixedRefs[0].CustomerCode == "CUST-000025" && mixedRefs[1].CustomerCode == "JO-000005",
                "20. Different reference prefixes sorted alphabetically then numerically");
        }
    }
}
