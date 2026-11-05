using System;
using GraphQL;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using QualiFrame.Framework.API.GraphQL;
using QualiFrame.Framework.Reports;
using Reqnroll;

namespace QualiFrame.Tests.StepDefinitions
{
    [Binding]
    public class GraphQLSteps
    {
        private GraphQLClient? _client;
        private GraphQLResponse<JObject>? _response;

        [Given(@"I have a GraphQL client for ""(.*)""")]
        public void GivenIHaveAGraphQLClientFor(string endpoint)
        {
            _client = new GraphQLClient(endpoint);
            ExtentReportManager.LogPass($"GraphQL client initialized: {endpoint}");
        }

        [When(@"I send a GraphQL query for country code ""(.*)""")]
        public void WhenISendAGraphQLQueryForCountryCode(string code)
        {
            Assert.That(_client, Is.Not.Null, "GraphQL client was not initialized.");
            var query = new QueryBuilder()
                .Operation("GetCountry")
                .NestedField($"country(code: \"{code}\")", "code", "name")
                .Build();

            try
            {
                _response = _client!.QueryAsync(query).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Assert.Inconclusive($"GraphQL endpoint is not reachable: {ex.Message}");
            }

            ExtentReportManager.LogPass("GraphQL country query executed");
        }

        [Then(@"the GraphQL response should contain country code ""(.*)""")]
        public void ThenTheGraphQLResponseShouldContainCountryCode(string code)
        {
            Assert.That(_response, Is.Not.Null);
            Assert.That(_response!.Errors, Is.Null.Or.Empty);
            var actual = _response.Data?["country"]?["code"]?.ToString();
            Assert.That(actual, Is.EqualTo(code));
        }

        [Then(@"the GraphQL response should have a name")]
        public void ThenTheGraphQLResponseShouldHaveAName()
        {
            Assert.That(_response?.Data?["country"]?["name"]?.ToString(), Is.Not.Null.And.Not.Empty);
            ExtentReportManager.LogPass("GraphQL country name present");
        }
    }
}
