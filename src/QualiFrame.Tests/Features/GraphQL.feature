@API @GraphQL
Feature: GraphQL client
  As a tester, I want to query a GraphQL API through the framework client
  so I do not create a parallel HTTP stack

  @P2-Medium
  Scenario: Query countries through the public GraphQL demo API
    Given I have a GraphQL client for "https://countries.trevorblades.com/"
    When I send a GraphQL query for country code "US"
    Then the GraphQL response should contain country code "US"
    And the GraphQL response should have a name
