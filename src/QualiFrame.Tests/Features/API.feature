Feature: Petstore Swagger API Testing
  As a developer, I want to test Petstore API endpoints
  to ensure they work correctly

  # ==================== Pet Operations ====================
  
  @API @P1-High
  Scenario: Get pet by ID
    Given I have a valid API endpoint
    # First, create a pet to ensure it exists
    When I send a POST request to "/pet" with the following data:
      """
      {
        "id": 1001,
        "category": {
          "id": 1,
          "name": "Dogs"
        },
        "name": "TestPet",
        "photoUrls": [
          "https://example.com/pet.jpg"
        ],
        "tags": [
          {
            "id": 1,
            "name": "test"
          }
        ],
        "status": "available"
      }
      """
    Then the response status code should be 200
    # Now retrieve the pet we just created
    When I send a GET request to "/pet/1001"
    Then the response status code should be 200
    And the response should be valid JSON
    And the response should have a "id" field
    And the response should have a "name" field
    And the response should have a "name" field with value "TestPet"

  @API @P1-High
  Scenario: Find pets by status
    Given I have a valid API endpoint
    When I add the following query parameter "status" with value "available"
    And I send a GET request to "/pet/findByStatus" with query parameters
    Then the response status code should be 200
    And the response should be valid JSON
    And the response should contain an array

  @API @P1-High
  Scenario: Create a new pet
    Given I have a valid API endpoint
    When I send a POST request to "/pet" with the following data:
      """
      {
        "id": 0,
        "category": {
          "id": 0,
          "name": "Dogs"
        },
        "name": "Buddy",
        "photoUrls": [
          "https://example.com/dog.jpg"
        ],
        "tags": [
          {
            "id": 0,
            "name": "friendly"
          }
        ],
        "status": "available"
      }
      """
    Then the response status code should be 200
    And the response should be valid JSON
    And the response should have a "name" field with value "Buddy"
    And the response should have a "status" field with value "available"

  @API @P1-High
  Scenario: Update an existing pet
    Given I have a valid API endpoint
    # First, create a pet to update
    When I send a POST request to "/pet" with the following data:
      """
      {
        "id": 1002,
        "category": {
          "id": 1,
          "name": "Dogs"
        },
        "name": "OriginalName",
        "photoUrls": [
          "https://example.com/dog1.jpg"
        ],
        "tags": [
          {
            "id": 1,
            "name": "original"
          }
        ],
        "status": "available"
      }
      """
    Then the response status code should be 200
    # Now update the pet
    When I send a PUT request to "/pet" with the following data:
      """
      {
        "id": 1002,
        "category": {
          "id": 1,
          "name": "Dogs"
        },
        "name": "Max",
        "photoUrls": [
          "https://example.com/dog2.jpg"
        ],
        "tags": [
          {
            "id": 1,
            "name": "playful"
          }
        ],
        "status": "available"
      }
      """
    Then the response status code should be 200
    And the response should be valid JSON
    And the response should have a "name" field with value "Max"

  @API @P1-High
  Scenario: Delete a pet
    Given I have a valid API endpoint
    # First, create a pet to delete
    When I send a POST request to "/pet" with the following data:
      """
      {
        "id": 1003,
        "category": {
          "id": 1,
          "name": "Dogs"
        },
        "name": "PetToDelete",
        "photoUrls": [
          "https://example.com/pet.jpg"
        ],
        "tags": [
          {
            "id": 1,
            "name": "temporary"
          }
        ],
        "status": "available"
      }
      """
    Then the response status code should be 200
    # Now delete the pet
    When I send a DELETE request to "/pet/1003"
    Then the response status code should be 200

  @API @P2-Medium
  Scenario: Find pets by tags
    Given I have a valid API endpoint
    When I add the following query parameter "tags" with value "friendly"
    And I send a GET request to "/pet/findByTags" with query parameters
    Then the response status code should be 200
    And the response should be valid JSON
    And the response should contain an array

  # ==================== Store Operations ====================

  @API @P1-High
  Scenario: Get store inventory
    Given I have a valid API endpoint
    When I send a GET request to "/store/inventory"
    Then the response status code should be 200
    And the response should be valid JSON

  @API @P1-High
  Scenario: Create a store order
    Given I have a valid API endpoint
    # First, create a pet for the order
    When I send a POST request to "/pet" with the following data:
      """
      {
        "id": 2002,
        "category": {
          "id": 1,
          "name": "Dogs"
        },
        "name": "OrderPet2",
        "photoUrls": [
          "https://example.com/pet.jpg"
        ],
        "tags": [
          {
            "id": 1,
            "name": "order"
          }
        ],
        "status": "available"
      }
      """
    Then the response status code should be 200
    # Now create an order for that pet
    When I send a POST request to "/store/order" with the following data:
      """
      {
        "id": 0,
        "petId": 2002,
        "quantity": 1,
        "shipDate": "2024-12-31T00:00:00.000Z",
        "status": "placed",
        "complete": false
      }
      """
    Then the response status code should be 200
    And the response should be valid JSON
    And the response should have a "status" field with value "placed"

  @API @P1-High
  Scenario: Get store order by ID
    Given I have a valid API endpoint
    # First, create a pet for the order
    When I send a POST request to "/pet" with the following data:
      """
      {
        "id": 2001,
        "category": {
          "id": 1,
          "name": "Dogs"
        },
        "name": "OrderPet",
        "photoUrls": [
          "https://example.com/pet.jpg"
        ],
        "tags": [
          {
            "id": 1,
            "name": "order"
          }
        ],
        "status": "available"
      }
      """
    Then the response status code should be 200
    # Now create an order for that pet
    When I send a POST request to "/store/order" with the following data:
      """
      {
        "id": 1001,
        "petId": 2001,
        "quantity": 1,
        "shipDate": "2024-12-31T00:00:00.000Z",
        "status": "placed",
        "complete": false
      }
      """
    Then the response status code should be 200
    # Now retrieve the order
    When I send a GET request to "/store/order/1001"
    Then the response status code should be 200
    And the response should be valid JSON
    And the response should have a "id" field

  @API @P1-High
  Scenario: Delete store order
    Given I have a valid API endpoint
    # First, create a pet for the order
    When I send a POST request to "/pet" with the following data:
      """
      {
        "id": 2003,
        "category": {
          "id": 1,
          "name": "Dogs"
        },
        "name": "OrderPet3",
        "photoUrls": [
          "https://example.com/pet.jpg"
        ],
        "tags": [
          {
            "id": 1,
            "name": "order"
          }
        ],
        "status": "available"
      }
      """
    Then the response status code should be 200
    # Now create an order for that pet
    When I send a POST request to "/store/order" with the following data:
      """
      {
        "id": 1002,
        "petId": 2003,
        "quantity": 1,
        "shipDate": "2024-12-31T00:00:00.000Z",
        "status": "placed",
        "complete": false
      }
      """
    Then the response status code should be 200
    # Now delete the order
    When I send a DELETE request to "/store/order/1002"
    Then the response status code should be 200

  # ==================== User Operations ====================

  @API @P1-High
  Scenario: Create a new user
    Given I have a valid API endpoint
    When I send a POST request to "/user" with the following data:
      """
      {
        "id": 0,
        "username": "testuser",
        "firstName": "Test",
        "lastName": "User",
        "email": "testuser@example.com",
        "password": "password123",
        "phone": "123-456-7890",
        "userStatus": 0
      }
      """
    Then the response status code should be 200
    And the response should be valid JSON

  @API @P1-High
  Scenario: Get user by username
    Given I have a valid API endpoint
    # First, create a user to retrieve
    When I send a POST request to "/user" with the following data:
      """
      {
        "id": 0,
        "username": "testuser",
        "firstName": "Test",
        "lastName": "User",
        "email": "testuser@example.com",
        "password": "password123",
        "phone": "123-456-7890",
        "userStatus": 0
      }
      """
    Then the response status code should be 200
    # Now retrieve the user
    When I send a GET request to "/user/testuser"
    Then the response status code should be 200
    And the response should be valid JSON
    And the response should have a "username" field

  @API @P1-High
  Scenario: Update an existing user
    Given I have a valid API endpoint
    # First, create a user to update
    When I send a POST request to "/user" with the following data:
      """
      {
        "id": 0,
        "username": "testuser",
        "firstName": "Original",
        "lastName": "User",
        "email": "original@example.com",
        "password": "password123",
        "phone": "111-111-1111",
        "userStatus": 0
      }
      """
    Then the response status code should be 200
    # Now update the user
    When I send a PUT request to "/user/testuser" with the following data:
      """
      {
        "id": 1,
        "username": "testuser",
        "firstName": "Updated",
        "lastName": "User",
        "email": "updated@example.com",
        "password": "newpassword123",
        "phone": "987-654-3210",
        "userStatus": 0
      }
      """
    Then the response status code should be 200
    And the response should be valid JSON

  @API @P1-High
  Scenario: Delete a user
    Given I have a valid API endpoint
    # First, create a user to delete
    When I send a POST request to "/user" with the following data:
      """
      {
        "id": 0,
        "username": "testuser",
        "firstName": "Test",
        "lastName": "User",
        "email": "testuser@example.com",
        "password": "password123",
        "phone": "123-456-7890",
        "userStatus": 0
      }
      """
    Then the response status code should be 200
    # Now delete the user
    When I send a DELETE request to "/user/testuser"
    Then the response status code should be 200

  @API @P2-Medium
  Scenario: User login
    Given I have a valid API endpoint
    # First, create a user to login
    When I send a POST request to "/user" with the following data:
      """
      {
        "id": 0,
        "username": "testuser",
        "firstName": "Test",
        "lastName": "User",
        "email": "testuser@example.com",
        "password": "password123",
        "phone": "123-456-7890",
        "userStatus": 0
      }
      """
    Then the response status code should be 200
    # Now try to login
    When I add the following query parameter "username" with value "testuser"
    And I add the following query parameter "password" with value "password123"
    And I send a GET request to "/user/login" with query parameters
    Then the response status code should be 200
    And the response should be valid JSON

  @API @P2-Medium
  Scenario: User logout
    Given I have a valid API endpoint
    When I send a GET request to "/user/logout"
    Then the response status code should be 200
    And the response should be valid JSON

  @API @P2-Medium
  Scenario: Create users with array
    Given I have a valid API endpoint
    When I send a POST request to "/user/createWithArray" with the following data:
      """
      [
        {
          "id": 0,
          "username": "user1",
          "firstName": "First",
          "lastName": "User",
          "email": "user1@example.com",
          "password": "password123",
          "phone": "111-111-1111",
          "userStatus": 0
        },
        {
          "id": 0,
          "username": "user2",
          "firstName": "Second",
          "lastName": "User",
          "email": "user2@example.com",
          "password": "password123",
          "phone": "222-222-2222",
          "userStatus": 0
        }
      ]
      """
    Then the response status code should be 200
    And the response should be valid JSON

  # ==================== Performance and Validation Tests ====================

  @API @P2-Medium
  Scenario: Test API response time
    Given I have a valid API endpoint
    When I send a GET request to "/pet/findByStatus?status=available"
    Then the response status code should be 200
    And the response time should be less than "5000" milliseconds

  @API @P2-Medium
  Scenario: Test API headers
    Given I have a valid API endpoint
    When I add the following header "Accept" with value "application/json"
    And I send a GET request to "/pet/findByStatus?status=available"
    Then the response status code should be 200
    And the response should have a "Content-Type" header

  @API @P3-Low
  Scenario: Test invalid pet ID
    Given I have a valid API endpoint
    When I send a GET request to "/pet/999999999"
    Then the response status code should be 404
    And the response should be valid JSON
    # Note: Petstore API returns 404 (Not Found) for non-existent pets
    # This is the correct HTTP status code for a resource that doesn't exist

  @API @P3-Low
  Scenario: Test invalid order ID
    Given I have a valid API endpoint
    When I send a GET request to "/store/order/999999999"
    Then the response status code should be 404
    # Note: Store orders typically return 404 for non-existent orders

  @API @P3-Low
  Scenario: Test invalid username
    Given I have a valid API endpoint
    When I send a GET request to "/user/invaliduser999"
    Then the response status code should be 404
    # Note: Users typically return 404 for non-existent usernames
