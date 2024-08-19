using System;
using System.Collections.Generic;
using System.Text;

namespace AngryPullRequests.Domain.Models
{
    public static class PullRequestReviewStates
    {
        public const string Approved = "APPROVED";
        public const string ChangesRequested = "CHANGES_REQUESTED";
        public const string Commented = "COMMENTED";
        public const string Dismissed = "DISMISSED";
        public const string Pending = "PENDING";
    }

    public class PullRequestReview
    {
        /// <summary>
        /// The review Id.
        /// </summary>
        public long Id { get; private set; }

        /// <summary>
        /// GraphQL Node Id
        /// </summary>
        public string NodeId { get; private set; }

        /// <summary>
        /// The state of the review
        /// </summary>
        public string State { get; private set; }

        /// <summary>
        /// The commit Id the review is associated with.
        /// </summary>
        public string CommitId { get; private set; }

        /// <summary>
        /// The user that created the review.
        /// </summary>
        public User User { get; private set; }

        /// <summary>
        /// The text of the review.
        /// </summary>
        public string Body { get; private set; }

        /// <summary>
        /// The URL for this review on GitHub.com
        /// </summary>
        public string HtmlUrl { get; private set; }

        /// <summary>
        /// The URL for the pull request via the API.
        /// </summary>
        public string PullRequestUrl { get; private set; }

        /// <summary>
        /// The comment author association with repository.
        /// </summary>
        public string AuthorAssociation { get; private set; }

        /// <summary>
        /// The time the review was submitted
        /// </summary>
        public DateTimeOffset SubmittedAt { get; private set; }
    }
}