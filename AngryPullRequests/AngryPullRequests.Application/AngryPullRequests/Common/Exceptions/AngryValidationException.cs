using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngryPullRequests.Application.AngryPullRequests.Common.Exceptions
{
    public abstract class AngryValidationException : Exception
    {
        protected AngryValidationException(string message) : base(message) { }
    }

    public class RepositoryExistsException : AngryValidationException
    {
        public RepositoryExistsException(string message) : base(message) { }
    }

    public class UnableToAccessRepositoryException : AngryValidationException
    {
        public UnableToAccessRepositoryException(string message) : base(message) { }
    }

    public class UnableToSendSlackMessageException : AngryValidationException
    {
        public UnableToSendSlackMessageException(string message) : base(message) { }
    }
}