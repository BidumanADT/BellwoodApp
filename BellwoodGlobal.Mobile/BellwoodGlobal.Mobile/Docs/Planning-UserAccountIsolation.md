# User Accounts & Data Isolation - Current State Analysis

**Date:** January 10, 2026  
**Status:** ?? **ANALYSIS COMPLETE - READY FOR IMPLEMENTATION PLANNING**  
**Priority:** ?? **CRITICAL - BLOCKING ALPHA TESTING**

---

## ?? Executive Summary

**Current Problem:** The Bellwood Elite mobile app currently shows **all data in the system** to **all users**, regardless of who owns or created that data. This is a **critical security and privacy issue** that must be resolved before alpha testing on physical devices.

**Goal:** Implement proper user account isolation so each user only sees data associated with their account (quotes, bookings, saved locations, saved passengers, payment methods, etc.).

**Scope:** This document analyzes the current state of user accounts and data access patterns to guide implementation planning.

---

## ?? Current State Overview

### Authentication Status: ? **IMPLEMENTED**

**What Works:**
- ? Login page with username/password
- ? JWT token authentication
- ? Token storage in SecureStorage
- ? Token validation and expiration checking
- ? Automatic logout on token expiration
- ? HTTP client authorization headers

**Auth Flow:**
```
User enters credentials
  ?
POST /login to AuthServer
  ?
Receive JWT token
  ?
Store in SecureStorage ("access_token")
  ?
AuthHttpHandler adds "Authorization: Bearer {token}" to all requests
  ?
Backend validates token on each request
```

**Files:**
- `Services/AuthService.cs` - Token management
- `Services/AuthHttpHandler.cs` - Automatic token injection
- `Pages/LoginPage.xaml.cs` - Login UI

---

### Authorization Status: ? **NOT IMPLEMENTED**

**What's Missing:**
- ? User-specific data filtering in mobile app
- ? Backend endpoints filtering by authenticated user
- ? User ID/email extraction from JWT token
- ? Data ownership validation
- ? Saved locations/passengers scoped to user
- ? Quote/booking visibility scoped to user

**Current Behavior:**
- All users see **ALL quotes** in the system
- All users see **ALL bookings** in the system
- All users share **same saved locations** (in-memory hardcoded data)
- All users share **same saved passengers** (in-memory hardcoded data)
- All users share **same booker profile** ("Alice Morgan" hardcoded)

---

## ?? JWT Token Analysis

### Token Structure (Current)

**Claims in JWT (from AuthServer):**
```json
{
  "sub": "user-123",              // User ID (unique identifier)
  "email": "alice@example.com",   // Email address ? Available
  "role": "Admin",                 // User role ? Available
  "exp": 1703001234                // Expiration timestamp
}
```

**Verification Status:**
- ? Token includes `email` claim (confirmed by you in previous discussions)
- ? Token includes `role` claim (Admin for Alice and Bob)
- ? Token includes `sub` claim (user ID)
- ? Token is validated by backend on each request

**What's Available to Use:**
- ? **Email** - Can be extracted and used for user-specific filtering
- ? **User ID (sub)** - Can be extracted and used as unique identifier
- ? **Role** - Can be used for role-based access control (future)

---

## ?? Mobile App - Data Access Patterns

### 1. ProfileService (Hardcoded Data) ? **CRITICAL ISSUE**

**Current Implementation:**
```csharp
// Services/ProfileService.cs
public class ProfileService : IProfileService
{
    // HARDCODED BOOKER - Same for ALL users!
    private readonly Passenger _booker = new()
    {
        FirstName = "Alice",
        LastName = "Morgan",
        PhoneNumber = "312-555-7777",
        EmailAddress = "alice.morgan@example.com"
    };

    // HARDCODED PASSENGERS - Same for ALL users!
    private readonly List<Passenger> _passengers = new()
    {
        new Passenger { FirstName = "Taylor", ... },
        new Passenger { FirstName = "Jordan", ... }
    };

    // HARDCODED LOCATIONS - Same for ALL users!
    private readonly List<Location> _locations = new()
    {
        new Location { Label = "Home", ... },
        new Location { Label = "O'Hare", ... },
        new Location { Label = "Langham", ... },
        new Location { Label = "Signature FBO (ORD)", ... }
    };
}
```

**Problems:**
1. ? All users see "Alice Morgan" as their booker profile
2. ? All users share same saved passengers
3. ? All users share same saved locations
4. ? Data is in-memory only (lost on app restart)
5. ? No user identification or scoping

**Impact:** **CRITICAL** - Complete lack of data isolation

---

### 2. Quotes Dashboard ? **CRITICAL ISSUE**

**Current Implementation:**
```csharp
// Pages/QuoteDashboardPage.xaml.cs
private async Task LoadAsync()
{
    // Gets ALL quotes from system, no filtering
    var items = await _admin.GetQuotesAsync(100);
    
    // Client-side filtering by status only (not by user)
    var vms = items
        .Where(FilterFn)  // Filters by "All", "Pending", "Priced", etc.
        .Where(SearchFn)  // Filters by search text
        .Select(RowVm.From)
        .ToList();
}
```

**Backend Endpoint:**
```csharp
// AdminApi endpoint (assumed from usage)
GET /quotes/list?take=100

// Returns ALL quotes in system (no user filtering)
```

**Problems:**
1. ? User sees quotes created by other users
2. ? User sees quotes for other passengers
3. ? No privacy or data isolation
4. ? Backend not filtering by authenticated user

**Impact:** **CRITICAL** - Users can see each other's quotes

---

### 3. Bookings Dashboard ? **CRITICAL ISSUE**

**Current Implementation:**
```csharp
// Pages/BookingsPage.xaml.cs
private async Task LoadAsync()
{
    // Gets ALL bookings from system, no filtering
    var items = await _admin.GetBookingsAsync(100);
    
    // Client-side filtering by status only (not by user)
    var vms = items
        .Where(FilterFn)  // Filters by "All", "Requested", "Confirmed", etc.
        .Where(SearchFn)  // Filters by search text
        .Select(RowVm.From)
        .ToList();
}
```

**Backend Endpoint:**
```csharp
// AdminApi endpoint (assumed from usage)
GET /bookings/list?take=100

// Returns ALL bookings in system (no user filtering)
```

**Problems:**
1. ? User sees bookings created by other users
2. ? User sees bookings for other passengers
3. ? User can view details of other users' bookings
4. ? User can potentially cancel other users' bookings (if endpoint allows)
5. ? No privacy or data isolation

**Impact:** **CRITICAL** - Users can see and interact with each other's bookings

---

### 4. Driver Tracking ?? **PARTIALLY SECURED**

**Current Implementation:**
```csharp
// Services/DriverTrackingService.cs
public async Task<DriverLocation?> GetDriverLocationAsync(string rideId)
{
    // Uses passenger-safe endpoint with email-based authorization
    var response = await _http.GetAsync($"/passenger/rides/{rideId}/location");
    
    // Backend validates: userEmail == bookerEmail || userEmail == passengerEmail
}
```

**Backend Endpoint:**
```csharp
// AdminApi endpoint (from documentation)
GET /passenger/rides/{rideId}/location
Authorization: Bearer {jwt_token}

// Returns 403 Forbidden if user's email doesn't match booking
```

**Status:**
- ? Backend validates user's email against booking
- ? Returns 403 if unauthorized
- ? Proper isolation implemented

**Impact:** ? **SECURED** - Users can only track their own rides

---

### 5. Payment Methods ? **UNKNOWN - NEEDS INVESTIGATION**

**Current Implementation:**
```csharp
// Services/PaymentService.cs (assumed from usage)
public async Task<IReadOnlyList<PaymentMethod>> GetStoredPaymentMethodsAsync()
{
    // Endpoint unknown - needs investigation
    // Does it return ALL payment methods or just user's?
}

public async Task<PaymentMethod> SubmitPaymentMethodAsync(NewCardRequest request)
{
    // Endpoint unknown - needs investigation
    // Is payment method associated with current user?
}
```

**Questions:**
1. ? Are payment methods scoped to user?
2. ? Can users see other users' payment methods?
3. ? Is Stripe customer ID tied to user account?

**Impact:** ?? **UNKNOWN - HIGH PRIORITY FOR INVESTIGATION**

---

### 6. Form State Persistence ?? **PARTIALLY SECURED**

**Current Implementation:**
```csharp
// Services/FormStateService.cs
private static async Task<string> GetUserSpecificKeyAsync(string prefix)
{
    // Gets current user's email from SecureStorage
    var userEmail = await SecureStorage.GetAsync("user_email");
    
    if (string.IsNullOrWhiteSpace(userEmail))
    {
        return prefix; // Falls back to global key if no user logged in
    }
    
    // Create user-specific key
    var userKey = $"{prefix}_{userEmail}";
    return userKey;
}
```

**Storage:**
```
// Preferences storage (local device only)
QuotePage_FormState_alice@example.com
BookRidePage_FormState_alice@example.com
```

**Status:**
- ? Form state scoped to user email
- ? Each user has separate draft storage
- ?? Relies on `user_email` in SecureStorage (where is this set?)

**Questions:**
1. ? Where is `user_email` stored in SecureStorage?
2. ? Is it set during login?
3. ? Is it cleared on logout?

**Impact:** ?? **MOSTLY SECURED** - But need to verify email storage

---

## ?? Critical Gaps Summary

### High Priority (Blocking Alpha) ??

| Component | Current State | Required State | Severity |
|-----------|---------------|----------------|----------|
| **ProfileService** | Hardcoded "Alice" for all users | User-specific profile from backend | ?? Critical |
| **Saved Passengers** | Hardcoded list shared by all | User-specific list from backend | ?? Critical |
| **Saved Locations** | Hardcoded list shared by all | User-specific list from backend | ?? Critical |
| **Quotes List** | Shows ALL quotes | Filter by authenticated user | ?? Critical |
| **Bookings List** | Shows ALL bookings | Filter by authenticated user | ?? Critical |
| **Quote Details** | Anyone can view any quote | Only owner can view | ?? Critical |
| **Booking Details** | Anyone can view any booking | Only owner can view | ?? Critical |
| **Cancel Booking** | Anyone can cancel any booking? | Only owner can cancel | ?? Critical |

### Medium Priority (Important) ??

| Component | Current State | Required State | Severity |
|-----------|---------------|----------------|----------|
| **Payment Methods** | Unknown isolation | User-specific only | ?? High |
| **User Email Storage** | Unclear where set | Set during login, clear on logout | ?? High |
| **Profile Editing** | Not implemented | User can edit their own profile | ?? Medium |

### Low Priority (Nice to Have) ??

| Component | Current State | Required State | Severity |
|-----------|---------------|----------------|----------|
| **Role-Based Access** | Not implemented | Admins see all, users see own | ?? Low |
| **Multi-Device Sync** | No sync | Profile syncs across devices | ?? Low |

---

## ??? Backend API Requirements

### What Needs to Exist on Backend

#### 1. User Profile Endpoints ? **MISSING**

```http
# Get current user's profile
GET /profile
Authorization: Bearer {jwt_token}

Response:
{
  "userId": "user-123",
  "email": "alice@example.com",
  "firstName": "Alice",
  "lastName": "Morgan",
  "phoneNumber": "312-555-7777"
}
```

#### 2. Saved Passengers Endpoints ? **MISSING**

```http
# Get current user's saved passengers
GET /profile/passengers
Authorization: Bearer {jwt_token}

Response:
[
  {
    "id": "pax-001",
    "firstName": "Taylor",
    "lastName": "Reed",
    "phoneNumber": "773-555-1122",
    "emailAddress": "taylor.reed@example.com"
  }
]

# Add a saved passenger
POST /profile/passengers
Authorization: Bearer {jwt_token}
Body: { ... passenger details ... }

# Delete a saved passenger
DELETE /profile/passengers/{id}
Authorization: Bearer {jwt_token}
```

#### 3. Saved Locations Endpoints ? **MISSING**

```http
# Get current user's saved locations
GET /profile/locations
Authorization: Bearer {jwt_token}

Response:
[
  {
    "id": "loc-001",
    "label": "Home",
    "address": "123 Wacker Dr, Chicago, IL",
    "latitude": 41.8864,
    "longitude": -87.6365,
    "isFavorite": true
  }
]

# Add a saved location
POST /profile/locations
Authorization: Bearer {jwt_token}
Body: { ... location details ... }

# Update a saved location
PUT /profile/locations/{id}
Authorization: Bearer {jwt_token}
Body: { ... location details ... }

# Delete a saved location
DELETE /profile/locations/{id}
Authorization: Bearer {jwt_token}
```

#### 4. User-Scoped Quotes Endpoint ?? **NEEDS MODIFICATION**

```http
# Get current user's quotes only
GET /quotes/list?take=100
Authorization: Bearer {jwt_token}

# Backend must filter: WHERE booker.email = {token.email}

Response:
[
  {
    "id": "quote-001",
    "passengerName": "Alice Morgan",
    "bookerName": "Alice Morgan",  # Must match token email
    "pickupLocation": "O'Hare",
    ...
  }
]
```

#### 5. User-Scoped Bookings Endpoint ?? **NEEDS MODIFICATION**

```http
# Get current user's bookings only
GET /bookings/list?take=100
Authorization: Bearer {jwt_token}

# Backend must filter: WHERE booker.email = {token.email}

Response:
[
  {
    "id": "booking-001",
    "passengerName": "Alice Morgan",
    "bookerName": "Alice Morgan",  # Must match token email
    "pickupLocation": "O'Hare",
    ...
  }
]
```

#### 6. User-Scoped Quote/Booking Details ?? **NEEDS MODIFICATION**

```http
# Get quote details (only if user owns it)
GET /quotes/{id}
Authorization: Bearer {jwt_token}

# Backend must validate: booker.email == {token.email}
# Return 403 Forbidden if not owner

# Same for bookings
GET /bookings/{id}
Authorization: Bearer {jwt_token}
```

#### 7. User-Scoped Cancel Booking ?? **NEEDS MODIFICATION**

```http
# Cancel booking (only if user owns it)
POST /bookings/{id}/cancel
Authorization: Bearer {jwt_token}

# Backend must validate: booker.email == {token.email}
# Return 403 Forbidden if not owner
```

---

## ?? User Session Flow - Current vs. Required

### Current Flow ? **INSECURE**

```
1. User logs in ? Receives JWT token
2. Token stored in SecureStorage
3. HTTP requests include token in Authorization header
4. Backend validates token signature (authentication only)
5. Backend returns ALL data (no authorization filtering)
6. Mobile app shows ALL data to user
```

**Problem:** Authentication without authorization

---

### Required Flow ? **SECURE**

```
1. User logs in ? Receives JWT token with email/userId claims
2. Token stored in SecureStorage
3. HTTP requests include token in Authorization header
4. Backend validates token signature (authentication)
5. Backend extracts email/userId from token
6. Backend filters data: WHERE owner = {token.email}
7. Backend returns only user's data
8. Mobile app shows user's data only
```

**Solution:** Authentication + authorization

---

## ?? Data Ownership Model

### How to Determine "Who Owns This Data"

#### Quotes
```csharp
// A user owns a quote if:
quote.Booker.EmailAddress == currentUser.Email

// OR (if we want to include quotes where they're the passenger):
quote.Passenger.EmailAddress == currentUser.Email
```

#### Bookings
```csharp
// A user owns a booking if:
booking.Booker.EmailAddress == currentUser.Email

// OR (if we want to include bookings where they're the passenger):
booking.Passenger.EmailAddress == currentUser.Email
```

#### Saved Passengers
```csharp
// Saved passengers belong to the user who created them
savedPassenger.UserId == currentUser.Id
```

#### Saved Locations
```csharp
// Saved locations belong to the user who created them
savedLocation.UserId == currentUser.Id
```

#### Payment Methods
```csharp
// Payment methods belong to the user who added them
paymentMethod.UserId == currentUser.Id

// OR (if using Stripe):
paymentMethod.StripeCustomerId == currentUser.StripeCustomerId
```

---

## ?? Implementation Strategy - High-Level

### Phase 1: Backend Authorization (Backend Team) ?? **CRITICAL**

**Goal:** Ensure backend endpoints filter data by authenticated user

**Tasks:**
1. ? Verify JWT token includes `email` and/or `sub` claim
2. ? Create middleware to extract user info from token
3. ? Add user-scoping to `/quotes/list` endpoint
4. ? Add user-scoping to `/bookings/list` endpoint
5. ? Add user-scoping to `/quotes/{id}` endpoint (403 if not owner)
6. ? Add user-scoping to `/bookings/{id}` endpoint (403 if not owner)
7. ? Add user-scoping to `/bookings/{id}/cancel` endpoint (403 if not owner)
8. ? Create `/profile` endpoint (return current user's profile)
9. ? Create `/profile/passengers` CRUD endpoints
10. ? Create `/profile/locations` CRUD endpoints
11. ? Verify payment methods are user-scoped

**Estimated Effort:** 2-3 days (backend developer)

---

### Phase 2: Mobile App - Replace Hardcoded Data ?? **CRITICAL**

**Goal:** Fetch user profile data from backend instead of hardcoded values

**Tasks:**
1. ? Create `IProfileApiService` interface
2. ? Implement `ProfileApiService` with HTTP client
3. ? Add `GetProfileAsync()` method
4. ? Add `GetSavedPassengersAsync()` method
5. ? Add `GetSavedLocationsAsync()` method
6. ? Update `ProfileService` to call backend instead of using hardcoded data
7. ? Store user email in SecureStorage during login
8. ? Clear user email from SecureStorage during logout
9. ? Add loading states to pages while fetching profile data

**Estimated Effort:** 1-2 days (mobile developer)

---

### Phase 3: Mobile App - Trust Backend Filtering ?? **IMPORTANT**

**Goal:** Remove any client-side assumptions that "we see all data"

**Tasks:**
1. ? Remove any client-side filtering by user (backend handles it now)
2. ? Update UI text: "My Quotes" instead of "All Quotes"
3. ? Update UI text: "My Bookings" instead of "All Bookings"
4. ? Handle 403 Forbidden responses gracefully (show error message)
5. ? Add error handling for unauthorized access attempts

**Estimated Effort:** 0.5 days (mobile developer)

---

### Phase 4: Testing & Validation ?? **CRITICAL**

**Goal:** Verify data isolation works correctly

**Test Scenarios:**
1. ? Login as Alice ? See only Alice's quotes/bookings
2. ? Login as Bob ? See only Bob's quotes/bookings
3. ? Alice cannot view Bob's quote details (403 error)
4. ? Alice cannot cancel Bob's booking (403 error)
5. ? Alice's saved passengers != Bob's saved passengers
6. ? Alice's saved locations != Bob's saved locations
7. ? Alice's profile shows "Alice Morgan", Bob's shows "Bob"
8. ? Logout and login as different user ? See different data

**Estimated Effort:** 1 day (QA + developer)

---

## ?? Security Concerns

### Current Vulnerabilities (Must Fix Before Alpha)

#### 1. **Data Leakage** ?? **CRITICAL**
- **Risk:** Users can see other users' personal information, quotes, bookings
- **Impact:** Privacy violation, potential GDPR/CCPA issues
- **Mitigation:** Implement backend filtering immediately

#### 2. **Unauthorized Actions** ?? **CRITICAL**
- **Risk:** Users might be able to cancel other users' bookings
- **Impact:** Service disruption, user frustration, potential fraud
- **Mitigation:** Add authorization checks to all mutation endpoints

#### 3. **Shared Hardcoded Data** ?? **CRITICAL**
- **Risk:** All users appear as "Alice Morgan" in bookings
- **Impact:** Confusion, incorrect billing, support nightmares
- **Mitigation:** Replace hardcoded ProfileService with backend calls

#### 4. **Payment Method Isolation** ?? **HIGH**
- **Risk:** Unknown if payment methods are properly scoped
- **Impact:** Potential unauthorized charges, PCI compliance issues
- **Mitigation:** Investigate and verify payment API authorization

---

## ?? Open Questions (Need Answers)

### Backend Questions

1. ? **Do quote/booking endpoints already filter by user?**
   - Test: Login as different users, check if data differs
   - If not: Backend changes required

2. ? **Do profile endpoints exist?**
   - Check: `GET /profile`, `GET /profile/passengers`, `GET /profile/locations`
   - If not: Backend implementation required

3. ? **Are payment methods user-scoped?**
   - Check: `GET /payment-methods` endpoint
   - Verify: Does it return only current user's methods?

4. ? **Where is user profile stored?**
   - Database table: `Users`, `Profiles`, `Accounts`?
   - Schema: What fields are available?

### Mobile App Questions

1. ? **Where is `user_email` stored in SecureStorage?**
   - Search for: `SecureStorage.SetAsync("user_email", ...)`
   - Is it set during login?
   - Is it cleared during logout?

2. ? **How should we handle offline mode?**
   - Cache user profile locally?
   - Show stale data with indicator?
   - Require internet for profile data?

3. ? **What happens when user logs out then logs in as different user?**
   - Do we clear all cached data?
   - Do we clear form state drafts?
   - Do we clear saved locations/passengers?

---

## ?? Success Criteria

**User account isolation is considered COMPLETE when:**

1. ? Backend endpoints filter data by authenticated user (email or userId)
2. ? Mobile app fetches user profile from backend (no hardcoded data)
3. ? Each user sees only their own quotes and bookings
4. ? Users cannot view or modify other users' data (403 errors)
5. ? Saved passengers and locations are user-specific
6. ? Payment methods are user-scoped and isolated
7. ? All test scenarios pass with multiple user accounts
8. ? Documentation updated with new authentication/authorization flow

**Status: ? NOT STARTED - NEXT PRIORITY AFTER PERFORMANCE WORK**

---

## ?? Related Documentation

- `Docs/PassengerApp-AdminAPI-Alignment-Verification.md` - Driver tracking authorization
- `Services/AuthService.cs` - JWT token management
- `Services/AuthHttpHandler.cs` - HTTP authorization headers
- `Services/ProfileService.cs` - **Current hardcoded implementation** (needs replacement)

---

## ?? Next Steps

1. **Deep Research** - Answer open questions above
2. **Backend Coordination** - Meet with backend team to verify/implement API changes
3. **Implementation Plan** - Create detailed step-by-step plan based on research findings
4. **Proof of Concept** - Implement one endpoint (e.g., quotes list) end-to-end
5. **Full Implementation** - Roll out to all endpoints
6. **Testing** - Comprehensive multi-user testing
7. **Alpha Release** - Deploy to physical devices for testing

**Estimated Total Effort:** 4-6 days (backend + mobile combined)

---

**Version:** 1.0  
**Last Updated:** January 10, 2026  
**Status:** ?? **ANALYSIS COMPLETE**  
**Next:** Deep research and implementation planning
