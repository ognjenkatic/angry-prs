using AngryPullRequests.Application.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AngryPullRequests.Application.AngryPullRequests.Contributors.Queries
{
    public class UserRankingStats
    {
        public string Username { get; set; }
        public int AuthoredPullRequests { get; set; }
        public DateTimeOffset LastAuthoringAt { get; set; }
        public DateTimeOffset FirstAuthoringAt { get; set; }
        public string MostActiveEverOnRepository { get; set; }
        public string RecentlyMostActiveOn { get; set; }
        public int CommentedOn { get; set; }
        public int Approved { get; set; }
        public int ChangeRequests { get; set; }
    }

    public class GetUserContributionsRankedQuery : IRequest<List<UserRankingStats>>
    {
        public class Handler : IRequestHandler<GetUserContributionsRankedQuery, List<UserRankingStats>>
        {
            private readonly IAngryPullRequestsContext dbContext;

            public Handler(IAngryPullRequestsContext dbContext)
            {
                this.dbContext = dbContext;
            }

            public async Task<List<UserRankingStats>> Handle(GetUserContributionsRankedQuery request, CancellationToken cancellationToken)
            {
                var contributors = await dbContext.Contributors.Include(c => c.Contributions).ThenInclude(c => c.Repository).ToListAsync(cancellationToken);

                var response = new List<UserRankingStats>();

                foreach (var contributor in contributors)
                {
                    response.Add(
                        new UserRankingStats
                        {
                            AuthoredPullRequests = (int)contributor.Contributions.Select(c => c.MergedPullRequestCount).Sum(),
                            FirstAuthoringAt = (DateTimeOffset)contributor.Contributions.Select(c => c.FirstMergeAt).Min(),
                            LastAuthoringAt = (DateTimeOffset)contributor.Contributions.Select(C => C.LastMergeAt).Max(),
                            Username = contributor.GithubUsername,
                            RecentlyMostActiveOn = contributor.Contributions.OrderBy(c => c.LastMergeAt).First().Repository.Name,
                            Approved = contributor.Contributions.Select(c => c.ApprovalCount).Sum(),
                            ChangeRequests = contributor.Contributions.Select(c => c.ChangeRequestCount).Sum(),
                            CommentedOn = contributor.Contributions.Select(c => c.CommentCount).Sum()
                        }
                    );
                }

                return response.OrderByDescending(r => r.AuthoredPullRequests).ThenBy(r => r.Username).ToList();
            }
        }
    }
}