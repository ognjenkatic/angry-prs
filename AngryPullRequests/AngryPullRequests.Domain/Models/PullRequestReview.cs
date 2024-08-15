using System;
using System.Collections.Generic;
using System.Text;

namespace AngryPullRequests.Domain.Models
{
    public static class PullRequestReviewStates
    {
        private static readonly string _approved = "APPROVED";
        private static readonly string _changesRequested = "CHANGES_REQUESTED";
        private static readonly string _commented = "COMMENTED";
        private static readonly string _dismissed = "DISMISSED";
        private static readonly string _pending = "PENDING";

        public static string Approved => _approved;
        public static string ChangesRequested => _changesRequested;
        public static string Commented => _commented;
        public static string Dismissed => _dismissed;
        public static string Pending => _pending;
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