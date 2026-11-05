Feature: Home Page Navigation

  Scenario: Verify user can access the homepage
    Given user navigates to "https://sauce-demo.myshopify.com/"
    Then the homepage should be displayed

  Scenario: Verify navigation menu links are visible
    Given user navigates to the homepage
    Then the navigation menu should contain "Home"
    And the navigation menu should contain "Catalog"
    And the navigation menu should contain "About Us"

  Scenario: Verify homepage products are displayed
    Given user navigates to the homepage
    Then products should be visible on the homepage

  Scenario: Verify user can click a product from homepage
    Given user is on the homepage
    When user clicks on a product
    Then user should be navigated to the product detail page