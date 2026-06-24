# Repo Allocation Documentation

This document explains the design, flow, assumptions, and allocation rules used in the Repo Allocation Exercise.

---

## Purpose

The purpose of this application is to suggest which available securities should be allocated as collateral for a repo cash-leg request.

The user provides:

- cash amount
- currency
- settlement date
- repo end date
- margin percentage
- available securities inventory

The application returns:

- selected securities
- allocated nominal per security
- allocated collateral value
- required collateral value
- total allocated collateral value
- shortfall

---

## High-Level Flow

```text
User enters repo request
  ↓
User enters, edits, or loads available securities
  ↓
User clicks Suggest allocation
  ↓
Angular frontend sends request to backend API
  ↓
Controller receives HTTP request
  ↓
Application service delegates to domain allocation engine
  ↓
Domain engine validates, filters, scores, and allocates securities
  ↓
Backend returns allocation result
  ↓
Frontend displays allocations and summary
```

---

## Application Layers

The backend is split into small layers.

```text
Controller
  ↓
Application service
  ↓
Domain allocation engine
```

---

## Controller Layer

Location:

```text
RepoAllocation.Api/Controllers/AllocationController.cs
```

Responsibilities:

- expose HTTP endpoints
- receive request DTOs
- call application service
- return successful responses
- convert validation exceptions into HTTP `400 Bad Request`

The controller does not contain allocation business logic.

---

## Application Layer

Location:

```text
RepoAllocation.Api/Application/
```

Main classes:

```text
AllocationApplicationService.cs
SampleSecurityProvider.cs
```

Responsibilities:

- orchestrate application use cases
- call the domain allocation engine
- provide deterministic sample securities
- keep controller thin

---

## Domain Layer

Location:

```text
RepoAllocation.Api/Domain/
```

Main classes:

```text
AllocationEngine.cs
AllocationValidationException.cs
```

Responsibilities:

- validate allocation input
- calculate required collateral
- determine eligible securities
- calculate scores
- sort securities
- allocate securities
- calculate shortfall

The domain engine is deterministic.

```text
same input → same output
```

It does not depend on:

- ASP.NET Core
- Angular
- database
- external services
- random numbers
- current system time
- global mutable state

---

## Frontend Overview

The frontend is an Angular standalone application.

It uses:

- standalone components
- `bootstrapApplication`
- reactive forms
- client-side validation
- backend validation error display
- pagination for securities table
- sample data loading

The frontend does not use:

```text
AppModule
NgModule-based application structure
```

---

## API Endpoints

### Suggest Allocation

```http
POST /api/allocation/suggest
```

This endpoint receives the repo request and available securities, then returns suggested allocation.

---

### Load Sample Securities

```http
GET /api/allocation/sample-securities?count=1000
```

This endpoint returns deterministic sample securities generated from C# code.

---

## Input Model

The allocation request contains:

```text
CashAmount
Currency
SettlementDate
RepoEndDate
MarginPercent
Securities
```

Each security contains:

```text
SecurityId
NominalAvailable
Price
HaircutPercent
FundingRate
NextCouponDate
```

---

## Output Model

The allocation response contains:

```text
RequiredCollateralValue
AllocatedCollateralValue
Shortfall
Allocations
```

Each allocation row contains:

```text
SecurityId
NominalAllocated
CollateralValue
Score
Reason
```

---

## Business Terms

### CashAmount

The repo cash-leg amount.

Example:

```text
CashAmount = 1,000,000 EUR
```

This amount needs to be covered by collateral.

---

### MarginPercent

The extra collateral percentage required above the cash amount.

Example:

```text
MarginPercent = 10
```

means:

```text
10%
```

---

### RequiredCollateralValue

The total collateral value required for the repo.

Formula:

```text
RequiredCollateralValue = CashAmount * (1 + MarginPercent / 100)
```

Example:

```text
CashAmount = 1,000,000
MarginPercent = 2

RequiredCollateralValue = 1,000,000 * 1.02
RequiredCollateralValue = 1,020,000
```

---

### NominalAvailable

The available face value or quantity of a security.

Example:

```text
NominalAvailable = 500,000
```

Nominal value is not the same as collateral value. Price and haircut must also be applied.

---

### Price

Price is represented as a multiplier.

```text
1.00 = 100%
0.98 = 98%
1.01 = 101%
```

Example:

```text
NominalAvailable = 500,000
Price = 0.98

Market value = 500,000 * 0.98
Market value = 490,000
```

---

### HaircutPercent

Haircut is a risk discount applied to market value.

Example:

```text
HaircutPercent = 5
```

means only 95% of the market value counts as collateral.

---

### FundingRate

Funding rate is a cost/rate used when ranking securities.

Lower funding rate is preferred.

Example:

```text
Security A FundingRate = 0.02
Security B FundingRate = 0.015
```

Security B is preferred because:

```text
0.015 < 0.02
```

---

### NextCouponDate

The next date when the security pays coupon/interest.

---

### daysToCoupon

The number of days between settlement date and next coupon date.

Formula:

```text
daysToCoupon = NextCouponDate - SettlementDate
```

If:

```text
daysToCoupon <= 3
```

the security is excluded from allocation.

---

## Required Collateral Formula

```text
RequiredCollateralValue = CashAmount * (1 + MarginPercent / 100)
```

Example:

```text
CashAmount = 1,000,000
MarginPercent = 2

RequiredCollateralValue = 1,020,000
```

---

## Collateral Value Formula

```text
CollateralValue = NominalAvailable * Price * (1 - HaircutPercent / 100)
```

Example:

```text
NominalAvailable = 500,000
Price = 0.98
HaircutPercent = 5

CollateralValue = 500,000 * 0.98 * 0.95
CollateralValue = 465,500
```

---

## Eligibility Rules

A security is eligible only when:

```text
NominalAvailable > 0
Price > 0
HaircutPercent >= 0
daysToCoupon > 3
```

A security is excluded when:

- nominal is zero or negative
- price is zero or negative
- haircut is negative
- coupon date is within 3 days of settlement date

---

## Scoring Formula

Eligible securities are ranked by score.

Lower score is better.

```text
Score = FundingRate + HaircutPenalty + CouponPenalty
```

---

## Haircut Penalty

```text
HaircutPenalty = HaircutPercent / 100
```

Example:

```text
HaircutPercent = 5

HaircutPenalty = 0.05
```

Higher haircut means the security is less efficient as collateral.

---

## Coupon Penalty

Coupon penalty applies when coupon date is near.

```text
CouponPenalty = (30 - daysToCoupon) / 100
```

Only applies when:

```text
daysToCoupon <= 30
```

If:

```text
daysToCoupon > 30
```

then:

```text
CouponPenalty = 0
```

Example:

```text
daysToCoupon = 20

CouponPenalty = (30 - 20) / 100
CouponPenalty = 0.10
```

---

## Funding Rate Assumption

Funding rate is used directly in the score.

Lower funding rate is preferred because it represents a lower cost/rate.

---

## Deterministic Ordering

If two securities have the same score, the engine orders them by `SecurityId`.

```text
Order by Score ascending
Then by SecurityId ascending
```

This keeps output deterministic.

---

## Allocation Behaviour

The allocation engine:

1. validates input
2. calculates required collateral value
3. filters out ineligible securities
4. scores eligible securities
5. sorts securities by score
6. allocates lower-score securities first
7. allows partial allocation for the final security
8. returns shortfall if collateral is insufficient

---

## Partial Allocation

If the last selected security has more collateral than needed, only part of its nominal is allocated.

Example:

```text
RequiredCollateralValue = 165
SEC1 CollateralValue = 100
SEC2 CollateralValue = 100
```

Then:

```text
SEC1 is fully allocated
SEC2 is partially allocated for only 65 collateral value
Shortfall = 0
```

---

## Shortfall

Shortfall means the available eligible collateral is not enough.

Formula:

```text
Shortfall = RequiredCollateralValue - AllocatedCollateralValue
```

If allocation fully covers the requirement:

```text
Shortfall = 0
```

---

## Validation

The backend returns HTTP `400 Bad Request` for invalid input.

Examples:

- empty securities list
- missing `SecurityId`
- negative `CashAmount`
- negative `NominalAvailable`
- negative `Price`
- negative `HaircutPercent`
- settlement date after repo end date

Example response:

```json
{
  "error": "SecurityId is required."
}
```

The frontend displays backend validation errors to the user.

---

## Sample Request

```json
{
  "cashAmount": 1000000,
  "currency": "EUR",
  "settlementDate": "2025-01-10",
  "repoEndDate": "2025-02-10",
  "marginPercent": 2,
  "securities": [
    {
      "securityId": "BOND1",
      "nominalAvailable": 500000,
      "price": 0.98,
      "haircutPercent": 5,
      "fundingRate": 0.02,
      "nextCouponDate": "2025-03-01"
    },
    {
      "securityId": "BOND2",
      "nominalAvailable": 700000,
      "price": 1.01,
      "haircutPercent": 3,
      "fundingRate": 0.015,
      "nextCouponDate": "2025-04-15"
    }
  ]
}
```

---

## Sample Response

```json
{
  "requiredCollateralValue": 1020000,
  "allocatedCollateralValue": 1020000,
  "shortfall": 0,
  "allocations": [
    {
      "securityId": "BOND2",
      "nominalAllocated": 700000,
      "collateralValue": 685790,
      "score": 0.045,
      "reason": "Selected with funding rate 0.015, haircut 3%, and 95 days to coupon."
    },
    {
      "securityId": "BOND1",
      "nominalAllocated": 358979.59,
      "collateralValue": 334210,
      "score": 0.07,
      "reason": "Selected with funding rate 0.02, haircut 5%, and 50 days to coupon."
    }
  ]
}
```

---

## Clean Architecture Implementation

The backend follows a small Clean Architecture-style structure. The goal is to keep HTTP concerns, application orchestration, and business rules separated.

```text
Controller layer
  ↓
Application layer
  ↓
Domain layer
```

### Controller Layer

Location:

```text
RepoAllocation.Api/Controllers/AllocationController.cs
```

The controller is responsible only for HTTP concerns:

- receives API requests
- calls the application service
- returns successful responses with `Ok(...)`
- converts domain validation errors into `BadRequest(...)`

The controller does not calculate collateral values, score securities, or perform allocation logic.

This keeps the API layer thin and easy to change without affecting business rules.

---

### Application Layer

Location:

```text
RepoAllocation.Api/Application/
```

The application layer coordinates use cases.

Main responsibilities:

- receives calls from the controller
- delegates allocation work to the domain engine
- delegates sample security generation to the sample data provider
- keeps orchestration separate from HTTP and domain calculation

For this exercise, the application layer is intentionally small. In a larger application, this layer could also handle authorization, logging, mapping, transaction boundaries, or use-case-specific validation.

---

### Domain Layer

Location:

```text
RepoAllocation.Api/Domain/
```

The domain layer contains the business rules.

Main responsibilities:

- validate allocation input
- calculate required collateral
- determine security eligibility
- calculate collateral value
- calculate score
- sort securities
- allocate securities
- calculate shortfall

The domain engine is independent from ASP.NET Core and frontend code. It does not know about controllers, HTTP, Swagger, Angular, or CORS.

This makes the allocation rules easy to unit test directly.

Example test style:

```csharp
var engine = new AllocationEngine();
var result = engine.SuggestAllocation(request);
```

No web server, database, or browser is required to test the core allocation rules.

---

### Dependency Direction

The dependency direction is inward:

```text
Controller depends on Application
Application depends on Domain
Domain does not depend on Controller or Infrastructure
```

This is the key Clean Architecture idea used in the project.

The most important business logic lives in the domain engine, while outer layers only coordinate or expose it.

---

## SOLID Principles Implementation

The backend also follows SOLID principles in a pragmatic way suitable for a small exercise.

---

### Single Responsibility Principle

Each class has one clear responsibility.

| Class | Responsibility |
|---|---|
| `AllocationController` | Handles HTTP requests and responses |
| `AllocationApplicationService` | Orchestrates allocation use cases |
| `AllocationEngine` | Contains allocation business rules |
| `SampleSecurityProvider` | Generates deterministic sample securities |
| `AllocationValidationException` | Represents domain validation failure |

This avoids putting all logic into one large service or controller.

---

### Open/Closed Principle

The allocation logic is isolated in `AllocationEngine`.

This means the API controller and frontend do not need to change when the scoring or allocation rules are adjusted.

Current scoring is intentionally simple:

```text
Score = FundingRate + HaircutPenalty + CouponPenalty
```

If scoring becomes more complex later, it could be extracted into a separate scoring service without changing the controller contract.

---

### Liskov Substitution Principle

The project does not use inheritance-heavy designs.

Most classes are simple, focused, and sealed. This avoids fragile inheritance hierarchies and reduces the risk of Liskov Substitution Principle violations.

---

### Interface Segregation Principle

The project avoids large, general-purpose interfaces.

There is no broad interface forcing classes to implement unrelated methods. Instead, responsibilities are separated by concrete classes with focused roles.

For example, sample data generation is separated from allocation calculation:

```text
SampleSecurityProvider → sample data only
AllocationEngine → allocation rules only
```

---

### Dependency Inversion Principle

The domain engine does not depend on outer technical details such as ASP.NET Core, HTTP, or Angular.

The controller depends on the application layer, and the application layer delegates to the domain layer.

For a small onboarding exercise, concrete class injection is sufficient and keeps the code simple. If the project grows, interfaces such as `IAllocationEngine` or `ISampleSecurityProvider` could be introduced, but they are not necessary at this stage.

---

### SOLID Summary

| Principle | How it is applied |
|---|---|
| Single Responsibility | Each class has one focused job |
| Open/Closed | Allocation rules are isolated and can be extended later |
| Liskov Substitution | No risky inheritance hierarchy |
| Interface Segregation | No large, unfocused interfaces |
| Dependency Inversion | Domain logic is independent of HTTP/frontend concerns |

The result is a small, readable, testable backend without unnecessary overengineering.

---

## Testing

Backend tests:

```powershell
dotnet test
```

Frontend tests:

```powershell
npm test -- --watch=false
```

Both test suites should pass before submission.

