@web @authentication @login @regression
Feature: HRM Login Functionality

  As an authorized HRM user
  I want to authenticate using my credentials
  So that I can securely access the HRM application

  Background:
    Given the user navigates to the HRM login page "https://qualitest-hrm-ui-production.up.railway.app/login"
    And the HRM login page is displayed

  @smoke @positive @TC_LOGIN_001
  Scenario: Successfully log in with valid administrator credentials
    Given the user is not authenticated
    When the user enters username "admin"
    And the user enters password "admin123"
    And the user submits the login request
    Then the user should be authenticated successfully
    And the authenticated application landing page should be displayed
    And the user should no longer be on the login page

  @negative @TC_LOGIN_002
  Scenario Outline: Prevent login when invalid credentials are provided
    Given the user is not authenticated
    When the user enters username "<Username>"
    And the user enters password "<Password>"
    And the user submits the login request
    Then the user should remain unauthenticated
    And the HRM login page should remain displayed
    And an authentication error message should be displayed

    Examples:
      | Username      | Password          |
      | Admin         | InvalidPassword1! |
      | invalid_admin | admin123          |
      | invalid_admin | InvalidPassword1! |

  @negative @validation @TC_LOGIN_003
  Scenario Outline: Prevent login when mandatory credentials are missing
    Given the user is not authenticated
    When the user enters username "<Username>"
    And the user enters password "<Password>"
    And the user submits the login request
    Then the user should remain unauthenticated
    And the HRM login page should remain displayed
    And the login request should remain blocked because required credentials are missing

    Examples:
      | Username | Password |
      |          | admin123 |
      | admin    |          |
      |          |          |

  @security @TC_LOGIN_004
  Scenario: Password is protected while entering login credentials
    When the user enters password "admin123"
    Then the entered password should be masked

  @session @TC_LOGIN_005
  Scenario: Authenticated session survives refresh and a later return to the application
    Given the user is not authenticated
    When the user enters username "admin"
    And the user enters password "admin123"
    And the user submits the login request
    Then the user should be authenticated successfully
    When the user refreshes the current page
    Then the user should remain authenticated
    And the login form should not be displayed
    When the user navigates back to the HRM login page
    Then the HRM login page should remain displayed
    When the user opens the authenticated application again
    Then the user should remain authenticated
    And the login form should not be displayed
