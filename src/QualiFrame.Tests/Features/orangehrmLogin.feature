@web @authentication @login @logout @orangehrm @P1-High
Feature: OrangeHRM Login and Logout Functionality
	As a valid OrangeHRM user
	I want to log in and log out
	So that I can access and exit the system securely

@TC001_Login_Logout_Validation @smoke @regression
Scenario: Verify successful login and logout
	Given the user navigates to "https://opensource-demo.orangehrmlive.com/web/index.php/auth/login"
	And the login page is displayed
	Then the OrangeHRM logo should be visible
	And the Username textbox should be visible and enabled
	And the Password textbox should be visible and enabled
	And the Login button should be visible and clickable
	And the Forgot your password link should be visible
	When the user enters username "Admin"
	And the user enters password "admin123"
	And the user clicks the Login button
	Then the user should be redirected to the Dashboard page
	And the Dashboard page should be displayed
	And the profile menu should be visible
	When the user clicks on the profile menu
	And the user clicks on Logout
	Then the user should be redirected to the Login page
	And the login page should be displayed
