using AngryPullRequests.Application.Github;
using AngryPullRequests.Application.Persistence;
using AngryPullRequests.Domain.Entities;
using AngryPullRequests.Domain.Models;
using Autofac.Features.OwnedInstances;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AngryPullRequests.Application.AngryPullRequests.Contributors.Commands
{
    public class IndexContributionsCommand : IRequest
    {
        public class Handler : IRequestHandler<IndexContributionsCommand>
        {
            private readonly IAngryPullRequestsContext dbContext;
            private readonly IPullRequestServiceFactory pullRequestServiceFactory;

            public Handler(IAngryPullRequestsContext dbContext, IPullRequestServiceFactory pullRequestServiceFactory)
            {
                this.dbContext = dbContext;
                this.pullRequestServiceFactory = pullRequestServiceFactory;
            }

            public async Task Handle(IndexContributionsCommand request, CancellationToken cancellationToken)
            {
                var repositories = await dbContext.Repositories.Include(r => r.AngryUser).ToListAsync(cancellationToken);

                var tasks = new List<Task<(Guid, Dictionary<string, UserExperience>)>>();

                foreach (var repository in repositories)
                {
                    tasks.Add(IndexRepository(repository, await pullRequestServiceFactory.Create(repository)));
                }

                var dictionaries = await Task.WhenAll(tasks);

                var contributors = await dbContext.RepositoryContributors
                    .Include(rc => rc.Repository)
                    .Include(rc => rc.Contributor)
                    .ToListAsync(cancellationToken);

                foreach (var repositoryContributions in dictionaries)
                {
                    foreach (var contribution in repositoryContributions.Item2)
                    {
                        await ProcessContribution(contributors, repositories, repositoryContributions.Item1, contribution);
                    }
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }

            private async Task ProcessContribution(
                List<RepositoryContributor> contributors,
                List<Repository> repositories,
                Guid repositoryId,
                KeyValuePair<string, UserExperience> contribution
            )
            {
                var contributorName = contribution.Key;
                var contributingUser = contributors.Select(c => c.Contributor).FirstOrDefault(u => u.GithubUsername == contributorName);

                if (contributingUser is null)
                {
                    var newContribution = new RepositoryContributor
                    {
                        Contributor = new Contributor { GithubUsername = contributorName },
                        Repository = repositories.First(r => r.Id == repositoryId),
                        FirstMergeAt = contribution.Value.FirstAuthoring,
                        LastMergeAt = contribution.Value.LastAuthoring,
                        MergedPullRequestCount = contribution.Value.PullRequestsAuthored,
                        CommentCount = contribution.Value.Comments,
                        ApprovalCount = contribution.Value.Approvals,
                        ChangeRequestCount = contribution.Value.ChangeRequests
                    };

                    contributors.Add(newContribution);
                    dbContext.RepositoryContributors.Add(newContribution);
                }
                else
                {
                    var existingContribution = contributors.FirstOrDefault(
                        rc => rc.ContributorId == contributingUser.Id && rc.RepositoryId == repositoryId
                    );

                    if (existingContribution is null)
                    {
                        var newContribution = new RepositoryContributor
                        {
                            Contributor = contributingUser,
                            Repository = repositories.First(r => r.Id == repositoryId),
                            RepositoryId = repositoryId,
                            FirstMergeAt = contribution.Value.FirstAuthoring,
                            LastMergeAt = contribution.Value.LastAuthoring,
                            MergedPullRequestCount = contribution.Value.PullRequestsAuthored,
                            CommentCount = contribution.Value.Comments,
                            ApprovalCount = contribution.Value.Approvals,
                            ChangeRequestCount = contribution.Value.ChangeRequests
                        };

                        contributors.Add(newContribution);
                        dbContext.RepositoryContributors.Add(newContribution);
                    }
                    else
                    {
                        existingContribution.FirstMergeAt = contribution.Value.FirstAuthoring;
                        existingContribution.LastMergeAt = contribution.Value.LastAuthoring;
                        existingContribution.MergedPullRequestCount = contribution.Value.PullRequestsAuthored;
                        existingContribution.CommentCount = contribution.Value.Comments;
                        existingContribution.ApprovalCount = contribution.Value.Approvals;
                        existingContribution.ChangeRequestCount = contribution.Value.ChangeRequests;
                    }
                }
            }

            private async Task<(Guid, Dictionary<string, UserExperience>)> IndexRepository(
                Repository repository,
                IPullRequestService pullRequestService
            )
            {
                int lastPageFetched = 1;
                Dictionary<string, UserExperience> authorExperienceMap = new();
                bool goNext;
                do
                {
                    var pullRequests = await pullRequestService.GetPullRequests(repository.Owner, repository.Name, true, 1, 100, lastPageFetched);

                    if (pullRequests.Length >= 100)
                    {
                        goNext = true;
                        lastPageFetched++;
                    }
                    else
                    {
                        goNext = false;
                    }

                    foreach (var pr in pullRequests)
                    {
                        await ProcessPullRequest(pr, repository, authorExperienceMap, pullRequestService);
                    }
                } while (goNext);

                return (repository.Id, authorExperienceMap);
            }

            private static async Task ProcessPullRequest(
                PullRequest pullRequest,
                Repository repository,
                Dictionary<string, UserExperience> authorExperienceMap,
                IPullRequestService pullRequestService
            )
            {
                var author = pullRequest.User.Login;

                if (pullRequest.Merged)
                {
                    var reviews = await pullRequestService.GetPullRequsetReviews(repository.Owner, repository.Name, pullRequest.Number);

                    if (!authorExperienceMap.TryGetValue(author, out var authorExperience))
                    {
                        authorExperience = new UserExperience();
                        authorExperienceMap[author] = authorExperience;
                    }

                    foreach (var review in reviews)
                    {
                        if (!authorExperienceMap.TryGetValue(review.User.Login, out var contributorExperience))
                        {
                            contributorExperience = new UserExperience();
                            authorExperienceMap[review.User.Login] = contributorExperience;
                        }

                        if (review.State == PullRequestReviewStates.Approved)
                        {
                            contributorExperience.Approvals++;
                        }
                        else if (review.State == PullRequestReviewStates.Commented)
                        {
                            contributorExperience.Comments++;
                        }
                        else if (review.State == PullRequestReviewStates.ChangesRequested)
                        {
                            contributorExperience.ChangeRequests++;
                        }
                    }

                    authorExperience.PullRequestsAuthored++;

                    if (pullRequest.CreatedAt < authorExperience.FirstAuthoring)
                    {
                        authorExperience.FirstAuthoring = pullRequest.CreatedAt.UtcDateTime;
                    }
                    if (pullRequest.CreatedAt > authorExperience.LastAuthoring)
                    {
                        authorExperience.LastAuthoring = pullRequest.CreatedAt.UtcDateTime;
                    }
                }
            }
        }
    }
}