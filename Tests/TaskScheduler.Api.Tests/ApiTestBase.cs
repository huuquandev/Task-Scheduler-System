using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;

namespace TaskScheduler.Api.Tests
{
    public class ApiTestBase : IClassFixture<CustomWebApplicationFactory>
    {
        protected readonly HttpClient Client;

        protected readonly CustomWebApplicationFactory Factory;

        protected ApiTestBase(CustomWebApplicationFactory factory)
        {
            Factory = factory;
            Client = factory.CreateClient();
        }
    }
}