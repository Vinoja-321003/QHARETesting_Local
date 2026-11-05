Feature: Mobile Application Testing
  As a mobile user, I want to test mobile application functionality
  to ensure it works correctly on mobile devices

  @Mobile @Android @P1-High
  Scenario: Launch mobile application
    Given I have a mobile device connected
    When I launch the mobile application
    Then I should see the element with id "welcome_screen"

  @Mobile @Android @P1-High
  Scenario: Login to mobile application
    Given I have a mobile device connected
    And I launch the mobile application
    When I enter "testuser" in the field with id "username_field"
    And I enter "testpass" in the field with id "password_field"
    And I tap on the element with id "login_button"
    Then I should see the element with id "dashboard"

  @Mobile @Android @P1-High
  Scenario: Navigate through mobile app
    Given I have a mobile device connected
    And I launch the mobile application
    When I tap on the element with id "menu_button"
    And I tap on the element with id "settings_option"
    Then I should see the element with id "settings_screen"

  @Mobile @Android @P2-Medium
  Scenario: Test mobile gestures
    Given I have a mobile device connected
    And I launch the mobile application
    When I swipe up
    Then I should see the element with id "bottom_content"
    When I swipe down
    Then I should see the element with id "top_content"

  @Mobile @Android @P2-Medium
  Scenario: Test mobile orientation
    Given I have a mobile device connected
    And I launch the mobile application
    When I rotate the device to landscape
    Then I should see the element with id "landscape_layout"
    When I rotate the device to portrait
    Then I should see the element with id "portrait_layout"

  @Mobile @Android @P2-Medium
  Scenario: Test mobile keyboard
    Given I have a mobile device connected
    And I launch the mobile application
    When I tap on the element with id "text_input_field"
    Then the keyboard should be visible
    When I hide the keyboard
    Then the keyboard should not be visible

  @Mobile @Android @P2-Medium
  Scenario: Test mobile navigation buttons
    Given I have a mobile device connected
    And I launch the mobile application
    When I tap on the element with id "next_screen_button"
    Then I should see the element with id "second_screen"
    When I press the back button
    Then I should see the element with id "first_screen"

  @Mobile @Android @P3-Low
  Scenario: Test mobile app background
    Given I have a mobile device connected
    And I launch the mobile application
    When I press the home button
    And I launch the mobile application
    Then I should see the element with id "welcome_screen"

  @Mobile @iOS @P1-High
  Scenario: Launch iOS mobile application
    Given I have a mobile device connected
    When I launch the mobile application
    Then I should see the element with id "ios_welcome_screen"

  @Mobile @iOS @P1-High
  Scenario: iOS login functionality
    Given I have a mobile device connected
    And I launch the mobile application
    When I enter "iosuser" in the field with id "ios_username_field"
    And I enter "iospass" in the field with id "ios_password_field"
    And I tap on the element with id "ios_login_button"
    Then I should see the element with id "ios_dashboard"

  @Mobile @Android @Hybrid @P2-Medium
  Scenario: Switch between native and webview contexts
    Given I have a mobile device connected
    And I launch the mobile application
    When I switch to the mobile webview
    And I switch to the native mobile context
    Then I should see the element with id "welcome_screen"