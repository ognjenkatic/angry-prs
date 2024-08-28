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

                var contributors = await dbContext.RepositoryContributors.Include(rc => rc.Repository).Include(rc => rc.Contributor).ToListAsync(cancellationToken);

                foreach (var repositoryContributions in dictionaries)
                {
                    foreach (var contibution in repositoryContributions.Item2)
                    {
                        var contributorName = contibution.Key;
                        var repositoryId = repositoryContributions.Item1;
                        var contributingUser = contributors.Select(c => c.Contributor).FirstOrDefault(u => u.GithubUsername == contributorName);

                        if (contributingUser is null)
                        {
                            var contribution = new RepositoryContributor
                            {
                                Contributor = new Contributor { GithubUsername = contributorName, },
                                Repository = repositories.First(r => r.Id == repositoryId),
                                FirstMergeAt = contibution.Value.FirstAuthoring,
                                LastMergeAt = contibution.Value.LastAuthoring,
                                MergedPullRequestCount = contibution.Value.PullRequestsAuthored,
                                CommentCount = contibution.Value.Comments,
                                ApprovalCount = contibution.Value.Approvals,
                                ChangeRequestCount = contibution.Value.ChangeRequests
                            };

                            contributors.Add(contribution);
                            dbContext.RepositoryContributors.Add(contribution);
                        }
                        else
                        {
                            var existingContribution = contributors.FirstOrDefault(
                                rc => rc.ContributorId == contributingUser.Id && rc.RepositoryId == repositoryId
                            );

                            if (existingContribution is null)
                            {
                                var contribution = new RepositoryContributor
                                {
                                    Contributor = contributingUser,
                                    Repository = repositories.First(r => r.Id == repositoryId),
                                    RepositoryId = repositories.First(r => r.Id == repositoryId).Id,
                                    FirstMergeAt = contibution.Value.FirstAuthoring,
                                    LastMergeAt = contibution.Value.LastAuthoring,
                                    MergedPullRequestCount = contibution.Value.PullRequestsAuthored,
                                    CommentCount = contibution.Value.Comments,
                                    ApprovalCount = contibution.Value.Approvals,
                                    ChangeRequestCount = contibution.Value.ChangeRequests
                                };

                                contributors.Add(contribution);
                                dbContext.RepositoryContributors.Add(contribution);
                            }
                            else
                            {
                                existingContribution.FirstMergeAt = contibution.Value.FirstAuthoring;
                                existingContribution.LastMergeAt = contibution.Value.LastAuthoring;
                                existingContribution.MergedPullRequestCount = contibution.Value.PullRequestsAuthored;
                                existingContribution.CommentCount = contibution.Value.Comments;
                                existingContribution.ApprovalCount = contibution.Value.Approvals;
                                existingContribution.ChangeRequestCount = contibution.Value.ChangeRequests;
                            }
                        }
                    }
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }

            private async Task<(Guid, Dictionary<string, UserExperience>)> IndexRepository(
                Repository repository,
                IPullRequestService pullRequestService
            )
            {
                int lastPageFetched = 1;
                int totalPrs = 0;
                Dictionary<string, UserExperience> authorExperienceMap = new();
                bool goNext;
                do
                {
                    var pullRequests = await pullRequestService.GetPullRequests(repository.Owner, repository.Name, true, 1, 100, lastPageFetched);

                    totalPrs += pullRequests.Length;

                    goNext = pullRequests.Length >= 100;
                    if (goNext)
                    {
                        lastPageFetched++;
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

                        switch (review.State)
                        {
                            case PullRequestReviewStates.Approved:
                                contributorExperience.Approvals++;
                                break;
                            case PullRequestReviewStates.Commented:
                                contributorExperience.Comments++;
                                break;
                            case PullRequestReviewStates.ChangesRequested:
                                contributorExperience.ChangeRequests++;
                                break;
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