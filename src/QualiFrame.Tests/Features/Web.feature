Feature: Web Demo Application
  As a user, I want to be able to login to the application
  when I present valid credentials

  @P1-High @88657060 @TS-10 @Remove-Product-NotInCart @QGen-29 @WebDemo @Shopping
  Scenario Outline: Web Demo App-TcID-88657060-Shopping Cart Product Management - Attempt to remove a product that is not in the Cart
    Given Launch Application using "<tc_id>"
    Then  verify login page displayed successfully
    When  User enters userCredentials in text field
    And   User clicks Login button
    Then  verify home page displayed successfully
    When  User clicks SauceLabBackPack button
    Then  User should see SauceLabBackPack Page
    When  User clicks AddToCart button
    And   User clicks Cart button
    Then  User should see SauceLabBackPack Product
    When  User clicks Remove button
    Then  User should see NoProduct in Cart

    Examples:
      | tc_id                 |
      | SeleniumPracticeTest4 |

  @P1-High @88657067  @TC-13 @Add-multipleproducts-to-cart @QGen-30 @WebDemo
  Scenario Outline: Web Demo App-TcID-88657067-Add multiple products to cart Successfully
    Given Launch Application using "<tc_id>"
    Then  verify login page displayed successfully
    When  User enters userCredentials in text field
    And   User clicks Login button
    Then  verify home page displayed successfully
    When  User clicks SauceLabBackPack button
    Then  User should see SauceLabBackPack Page
    When  User clicks AddToCart button
    And   User clicks TopMenu button
    And   User clicks AllItem button
    When  User clicks SauceLabBikeLight button
    Then  User should see SauceLabBikeLight Page
    When  User clicks AddToCart button
    And   User clicks Cart button
    And   User should see SauceLabBackPack Product

    Examples:
      | tc_id                 |Login   |
      | SeleniumPracticeTest4 |btnLogin|

  @P1-High @88657058 @TS-10 @Remove-Product @QGen-29 @WebDemo
  Scenario Outline: Web Demo App-TcID- 88657058-Remove from Cart
    Given Launch Application using "<tc_id>"
    Then  verify login page displayed successfully
    When  User enters userCredentials in text field
    And   User clicks Login button
    Then  verify home page displayed successfully
    When  User clicks SauceLabBackPack button
    Then  User should see SauceLabBackPack Page
    When  User clicks AddToCart button
    And   User clicks Cart button
    Then  User should see SauceLabBackPack Product
    When  User clicks Remove button
    Then  User should see NoProduct in Cart

    Examples:
      | tc_id                 |
      | SeleniumPracticeTest4 |

  @P1-High @88657049 @TC-1 @User-login @QGen-26 @WebDemo
  Scenario Outline: Web Demo App-TcID-88657049-User Management Authentication - verify user login to SauceDemo website
    Given Launch Application using "<tc_id>"
    Then  verify login page displayed successfully
    When  User enters userCredentials in text field
    And   User clicks Login button
    Then  verify home page displayed successfully

    Examples:
      | tc_id                 |
      | SeleniumPracticeTest4 |

  @P1-High @88657050 @TC-1 @User-login-invalidPassword @QGen-26 @WebDemo
  Scenario Outline: Web Demo App-TcID-88657050-Verify error message for invalid username is entered during login
    Given Launch Application using "<tc_id>"
    Then  verify login page displayed successfully
    When  User enters invalid userCredentials in text field
    And   User clicks Login button
    Then  verify error message displayed successfully

    Examples:
      | tc_id                 |
      | SeleniumPracticeTest3 |

  @P2-Medium  @TC-13 @Add-product-to-cart @QGen-30 @WebDemo
  Scenario Outline: Web Demo App - Add product to cart
    Given Launch Application using "<tc_id>"
    Then  verify login page displayed successfully
    When  User enters userCredentials in text field
    And   User clicks Login button
    Then  verify home page displayed successfully
    When  User clicks SauceLabBackPack button
    Then  User should see SauceLabBackPack Page
    When  User clicks AddToCart button
    And   User clicks Cart button
    And   User should see SauceLabBackPack Product

    Examples:
      | tc_id                 |Login   |
      | SeleniumPracticeTest4 |btnLogin|

  @P2-Medium @TC-7 @Submit-order @QGen-28 @WebDemo
  Scenario Outline: Web Demo App - Submit Order
    Given Launch Application using "<tc_id>"
    Then  verify login page displayed successfully
    When  User enters userCredentials in text field
    And   User clicks Login button
    Then  verify home page displayed successfully
    When  User clicks SauceLabBackPack button
    Then  User should see SauceLabBackPack Page
    When  User clicks AddToCart button
    And   User clicks Cart button
    Then  User should see SauceLabBackPack Product
    When  User clicks CheckOut button
    And   User enters Checkout information
    And   User clicks Continue button
    And   User clicks Finish button
    Then  User should see ThankYouOrder Page

    Examples:
      | tc_id                 |
      | SeleniumPracticeTest4 |

  @P2-Medium @TC-4 @User-logout @QGen-27 @WebDemo
  Scenario Outline: Web Demo App - LogOut
    Given Launch Application using "<tc_id>"
    Then  verify login page displayed successfully
    When  User enters userCredentials in text field
    And   User clicks Login button
    Then  verify home page displayed successfully
    When  User clicks TopMenu button
    And   User clicks LogOut button
    Then  verify login page displayed successfully

    Examples:
      | tc_id                 |
      | SeleniumPracticeTest4 |

  @TabIssue
  Scenario Outline: Web Demo App - TabOutIssue
    Given Launch Application using "<tc_id>"
    Then  Verify swtich tab issue
    Then  Verify swtich tab issue
      Examples:
      | tc_id                 |
      | SeleniumPracticeTest4 |
