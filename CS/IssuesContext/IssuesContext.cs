﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace InfiniteSourceSample {
    public class IssuesContext : IDisposable {
        #region helpers
        static object SyncObject = new object();
        static Lazy<IssueData[]> AllIssues = new Lazy<IssueData[]>(() => {
            var date = DateTime.Today;
            var rnd = new Random(0);
            return Enumerable.Range(0, 100000)
                .Select(i => {
                    date = date.AddSeconds(-rnd.Next(20 * 60));
                    return new IssueData(
                        subject: OutlookDataGenerator.GetSubject(),
                        user: OutlookDataGenerator.GetFrom(),
                        created: date,
                        votes: rnd.Next(100),
                        priority: OutlookDataGenerator.GetPriority());
                }).ToArray();
        });
        class DefaultSortComparer : IComparer<IssueData> {
            int IComparer<IssueData>.Compare(IssueData x, IssueData y) {
                if(x.Created.Date != y.Created.Date)
                    return Comparer<DateTime>.Default.Compare(x.Created.Date, y.Created.Date);
                return Comparer<int>.Default.Compare(x.Votes, y.Votes);
            }
        }
        #endregion

        Thread workingThread;

        public IssuesContext() {
            Thread.Sleep(500);
            workingThread = Thread.CurrentThread;
        }

        public void Dispose() {
            Thread.Sleep(500);
        }

        public IssueData[] GetIssues(int page, int pageSize, IssueSortOrder sortOrder, IssueFilter filter) {
            CheckAccess();
            Thread.Sleep(300);
            var issues = SortIssues(sortOrder, AllIssues.Value);
            if(filter != null)
                issues = FilterIssues(filter, issues);
            return issues.Skip(page * pageSize).Take(pageSize).ToArray();
        }

        public IssuesSummaries GetSummaries(IssueFilter filter) {
            CheckAccess();
            Thread.Sleep(300);
            var issues = (IEnumerable<IssueData>)AllIssues.Value;
            if(filter != null)
                issues = FilterIssues(filter, issues);
            var lastCreated = issues.Any() ? issues.Max(x => x.Created) : default(DateTime?);
            return new IssuesSummaries(count: issues.Count(), lastCreated: lastCreated);
        }

        void CheckAccess() {
            if(workingThread != Thread.CurrentThread)
                throw new InvalidOperationException();
        }

        #region filter
        static IEnumerable<IssueData> FilterIssues(IssueFilter filter, IEnumerable<IssueData> issues) {
            if(filter.CreatedFrom != null || filter.CreatedTo != null) {
                if(filter.CreatedFrom == null || filter.CreatedTo == null) {
                    throw new InvalidOperationException();
                }
                issues = issues.Where(x => x.Created >= filter.CreatedFrom.Value && x.Created < filter.CreatedTo);
            }
            if(filter.MinVotes != null) {
                issues = issues.Where(x => x.Votes >= filter.MinVotes.Value);
            }
            if(filter.Priority != null) {
                issues = issues.Where(x => x.Priority == filter.Priority);
            }
            return issues;
        }
        #endregion

        #region sort
        static IEnumerable<IssueData> SortIssues(IssueSortOrder sortOrder, IEnumerable<IssueData> issues) {
            switch(sortOrder) {
            case IssueSortOrder.Default:
                return issues.OrderByDescending(x => x, new DefaultSortComparer()).ThenByDescending(x => x.Created);
            case IssueSortOrder.CreatedDescending:
                return issues.OrderByDescending(x => x.Created);
            case IssueSortOrder.VotesAscending:
                return issues.OrderBy(x => x.Votes).ThenByDescending(x => x.Created);
            case IssueSortOrder.VotesDescending:
                return issues.OrderByDescending(x => x.Votes).ThenByDescending(x => x.Created);
            default:
                throw new InvalidOperationException();
            }
        }
        #endregion
    }
}
