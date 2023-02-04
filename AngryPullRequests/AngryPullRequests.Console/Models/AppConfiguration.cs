﻿using AngryPullRequests.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngryPullRequests.Console.Models
{
    public class AppConfiguration
    {
        public AngryPullRequestsConfiguration RepoConfiguration { get; set; }
        public RepositoryAccessConfiguration AccessConfiguration { get; set; }
    }
}