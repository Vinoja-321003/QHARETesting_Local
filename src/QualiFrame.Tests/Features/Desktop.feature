Feature: Desktop Application Scenarios
  As a desktop user, I want to interact with Windows desktop applications
  so that I can validate critical desktop workflows end-to-end.

  @Desktop @P1-High
  Scenario Outline: Desktop login with valid credentials
    Given the desktop application is running
    When I login to the desktop app with username "<username>" and password "<password>"
    Then I should see the desktop main window

    Examples:
      | username | password      |
      | admin    | password123!  |

  @Desktop @P1-High @Calculator
  Scenario: Perform addition in Windows Calculator
    Given the Calculator application is launched
    When I press the number "4"
    And I press the addition operator
    And I press the number "6"
    And I press equals
    Then the result should be "10"
    And the Calculator application is closed

