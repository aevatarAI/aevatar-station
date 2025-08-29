# Functional Test Case Specification Guide

This document aims to provide a standardized guideline for writing functional test cases to ensure consistent formatting, clear content, and compatibility with tools such as XMind.

## Design Methods and Techniques

### Six Mandatory Test Design Methods

* **Equivalence Class Partitioning**

  * Divide input data into valid and invalid equivalence classes.
  * Select one or a few representative inputs from each class.
  * Design at least one test case for each equivalence class.

* **Boundary Value Analysis**

  * Focus on testing boundary conditions of input and output (e.g., min/max values, just above/below limits, empty, critical lengths).
  * A supplement to equivalence class partitioning.
  * Includes normal, abnormal, and special boundary testing.

* **Decision Table Testing**

  * Applicable to scenarios with multiple condition combinations resulting in different actions.
  * List all condition and action stubs to form a decision table.
  * Ensure all combinations are covered by test cases.

* **Scenario-Based Testing (Use Case Testing)**

  * Design test cases based on how users actually use the system.
  * Simulate user workflows to verify system behavior in real-world scenarios.
  * Include normal, abnormal, and edge-case scenarios.

* **Error Guessing**

  * Based on experience, intuition, and analysis of system weaknesses to predict possible defects.
  * Often used to complement structured test methods.
  * Focus on areas where the system is prone to errors.

* **State Transition Testing**

  * Suitable for systems/modules with defined state transitions.
  * Focus on state changes and triggering events.
  * Verify the correctness and completeness of state transitions.

## Example of Applying Test Design Methods

Using the "Set Nickname" feature to demonstrate multiple test design methods:

### Equivalence Class Partitioning

* **Valid Classes**: 1–20 characters; letters, numbers, Chinese characters, underscores, middle dot.
* **Invalid Classes**: Empty, too long, prohibited special characters, only symbols.

### Boundary Value Analysis

* **Min Boundary**: 1-character nickname.
* **Max Boundary**: 20-character nickname.
* **Invalid Boundary**: 0 characters (min – 1), 21 characters (max + 1).

### Scenario-Based Testing

* **Normal**: First-time nickname setup.
* **Modification**: Editing an existing nickname.
* **Conflict**: Handling duplicate nicknames.
* **Verification**: Display and confirmation after setting.

### Error Guessing

* **Security**: Special character/script injection.
* **Compatibility**: Emojis, multilingual characters.
* **Abnormal**: Network interruptions, concurrent operations.

### State Transition Testing

* **States**: Unset → Setting → Set → Modifying.
* **Transitions**: Normal and abnormal transitions between states.
* **Validation**: UI and feature availability in each state.

## Core Structure Overview

The structure of a functional test case document is as follows:

```markdown
# XX Functional Test Cases
## Feature Module Name
### Test Focus 1 (e.g., Avatar Settings)
#### Verification Point 1.1 (e.g., Upload Rule Validation)
##### Test Scenario 1.1.1
###### Expected Result
Expected result details 1
##### Test Scenario 1.1.2
###### Expected Result
Expected result details 2
#### Verification Point 1.2 (e.g., Default Logic Check)
##### Test Scenario 1.2.1
###### Expected Result
- Expected result details 1
### Test Focus 2 (e.g., Nickname Rules)
#### Verification Point 2.1 (e.g., Format Rule Check)
##### Test Scenario 2.1.1
###### Expected Result
Expected result details 1
```

## Detailed Explanation of Elements

### 1. Document Title (H1)

* **Tag**: `#`
* **Purpose**: Highest-level heading of the document.
* **Example**:

  ```markdown
  # Functional Test Cases
  ```

### 2. Feature Module Name (H2)

* **Tag**: `##`
* **Purpose**: Names a major feature module, user story, or test unit.
* **Tip**: Avoid numbering to keep it clean.
* **Example**:

  ```markdown
  ## User Login Feature
  ```

### 3. Test Focus (H3)

* **Tag**: `###`
* **Purpose**: Identifies a specific feature or related group of verifications.
* **Example**:

  ```markdown
  ### Avatar Settings
  ### Nickname Rule and Uniqueness Checks
  ```

### 4. Verification Point (H4)

* **Tag**: `####`
* **Purpose**: Further categorizes aspects under a test focus.
* **Example**:

  ```markdown
  #### Upload Rule Validation
  #### Uniqueness Check
  #### Dynamic Display Formatting
  ```

### 5. Test Scenario (H5)

* **Tag**: `#####`
* **Purpose**: Concisely and uniquely describe a test case scenario with actions/conditions.
* **Example**:

  ```markdown
  ##### User registers with a valid, unregistered email and a compliant password
  ```

### 6. Expected Result (H6)

* **Tag**: `######`
* **Header**: Typically titled `###### Expected Result`
* **Purpose**: Describes the measurable system outcome after executing the scenario.
* **Example**:

  ```markdown
  ###### Expected Result
  - Registration successful.
  - System displays "Registration successful" message.
  ```

## Sample Template

```markdown
# User Management Test Cases
## User Registration
### Input Validation
#### Email and Password Rules
##### User registers with a valid, unregistered email and compliant password
###### Expected Result
- Registration succeeds.
- System displays “Registration successful, please check your email to activate your account.”
- New user record is created with status "Pending Activation".
##### User attempts to register with existing email (existing@example.com)
###### Expected Result
- Registration fails.
- System displays “Email already registered. Try a different email or log in.”
- No change to user record count in the database.
## User Login
### Username and Password Login
#### Credential Validation
##### Activated user logs in with correct credentials (user@example.com)
###### Expected Result
- Login successful.
- Redirect to user dashboard.
- User session is correctly initialized.
##### User attempts to log in with non-existent email
###### Expected Result
- Login fails.
- System shows “Account does not exist or password is incorrect.”
```

## Test Case Management Recommendations

* Use version control for managing test case files.
* Regularly update test cases to reflect product changes.
* Maintain consistent structure and naming.
* Track test execution results promptly.
* Periodically review and optimize test cases.

## Requirements

* Generate test case files under: `test-cases/{version}/{feature-name}-test-cases.md`

## Notes

1. **Markdown Format**: Follow the H1–H6 hierarchy strictly, especially for tools like XMind to parse correctly.
2. **Clarity & Brevity**: Scenario and title text should clearly describe the test condition and action.
3. **Missing Step Details**: No dedicated section for "steps," so H5 titles must convey the operation and conditions succinctly. Split into smaller scenarios if complex.
4. **Verifiability**: Expected results must be specific and testable.
5. **Independence**: Each test scenario should be executable independently.
6. **Test Data**: Include critical test data in the H5 title or under the relevant H4 if needed.
7. **XMind Compatibility**: Each markdown header level becomes a node in XMind. Ensure syntax correctness.

## Design Principles

1. **Independence**: Each test case should run independently.
2. **Repeatability**: Should yield consistent results on repeated execution.
3. **Verifiability**: Outcomes must be specific and measurable.
4. **Clarity**: Descriptions must be unambiguous.
5. **Completeness**: Include all information needed for execution.
6. **Coverage First**: Prioritize test coverage to ensure full feature validation.
