# Security Model & Authorization

**Document Type**: Living Document - Technical Reference  
**Last Updated**: January 27, 2026  
**Status**: ? Production Ready

---

## ?? Overview

The Bellwood Global Mobile App implements a multi-layered security model using **JWT (JSON Web Tokens)** for authentication and **role-based access control (RBAC)** for authorization. This document describes the security architecture, authentication flow, authorization policies, and best practices.

**Security Layers**:
1. **Network Security** - HTTPS/TLS encryption
2. **Authentication** - JWT Bearer tokens
3. **Authorization** - Role-based access control
4. **Data Isolation** - User ownership filtering
5. **API Security** - Key restrictions, rate limiting

---

## ?? Authentication

### JWT Token-Based Authentication

**Provider**: IdentityServer (AuthServer)  
**Protocol**: OAuth 2.0 / OpenID Connect  
**Grant Type**: Resource Owner Password Credentials (ROPC)

**Token Format**:
```
Header:
{
  "alg": "HS256",
  "typ": "JWT"
}

Payload:
{
  "sub": "user@example.com",
  "email": "user@example.com",
  "role": "booker",
  "name": "John Doe",
  "iat": 1706349600,
  "exp": 1706353200
}

Signature:
HMACSHA256(
  base64UrlEncode(header) + "." + base64UrlEncode(payload),
  secret
)
```

---

### Authentication Flow

```
???????????????
?  LoginPage  ? (User enters email/password)
???????????????
       ? 1. POST /connect/token
       ?
???????????????
? AuthServer  ? (Validates credentials)
?(IdentityServer)?
???????????????
       ? 2. Generate JWT
       ?    - Claims: email, role, name
       ?    - Expiration: 1 hour
       ?
???????????????
?Mobile App   ? (Receive JWT access token)
???????????????
       ? 3. Store in SecureStorage
       ?
???????????????
?SecureStorage? (Device keychain/keystore)
???????????????
       ? 4. Include in API requests
       ?
???????????????
?  AdminAPI   ? (Validate JWT, authorize request)
?             ?  Authorization: Bearer {token}
???????????????
```

---

### Token Request

**Endpoint**: `POST {AuthServerUrl}/connect/token`

**Request**:
```http
POST /connect/token HTTP/1.1
Content-Type: application/x-www-form-urlencoded

grant_type=password
&username=user@example.com
&password=SecurePassword123!
&client_id=mobile-app
&scope=openid profile email admin-api
```

**Response** (200 OK):
```json
{
  "access_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expires_in": 3600,
  "token_type": "Bearer",
  "refresh_token": "def50200..."
}
```

**Mobile App Implementation**:
```csharp
// AuthService.cs
public async Task<AuthResponse> LoginAsync(string email, string password)
{
    var content = new FormUrlEncodedContent(new[]
    {
        new KeyValuePair<string, string>("grant_type", "password"),
        new KeyValuePair<string, string>("username", email),
        new KeyValuePair<string, string>("password", password),
        new KeyValuePair<string, string>("client_id", "mobile-app"),
        new KeyValuePair<string, string>("scope", "openid profile email admin-api")
    });
    
    var response = await _httpClient.PostAsync($"{_authServerUrl}/connect/token", content);
    response.EnsureSuccessStatusCode();
    
    var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
    
    // Store token securely
    await SecureStorage.SetAsync("access_token", result.AccessToken);
    await SecureStorage.SetAsync("refresh_token", result.RefreshToken);
    
    return result;
}
```

---

### Token Storage

**Platform**: MAUI `SecureStorage` API

**Storage**:
```csharp
// Store
await SecureStorage.SetAsync("access_token", token);

// Retrieve
string token = await SecureStorage.GetAsync("access_token");

// Clear (logout)
SecureStorage.Remove("access_token");
SecureStorage.Remove("refresh_token");
```

**Platform Implementation**:
- **Android**: Android Keystore (hardware-backed encryption)
- **iOS**: iOS Keychain (encrypted with device password)
- **Windows**: Windows Credential Locker (per-user encryption)

**Security Features**:
- Encrypted at rest
- Requires device authentication (PIN/biometric)
- Survives app uninstall (optional)
- Cannot be accessed by other apps

---

### Token Validation

**AdminAPI validates JWT on every request**:

```csharp
// AdminAPI Startup.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // Preserve claim names
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            IssuerSigningKey = signingKey,
            RoleClaimType = "role", // Map "role" claim to ClaimsPrincipal.IsInRole()
            NameClaimType = "email"
        };
    });
```

**Validation Checks**:
- ? Signature valid (HMAC-SHA256)
- ? Token not expired (`exp` claim)
- ? Token issued after valid time (`iat` claim)
- ? Claims present (`sub`, `email`, `role`)

---

### Token Expiration & Refresh

**Access Token Lifetime**: 1 hour  
**Refresh Token Lifetime**: 7 days (sliding)

**Refresh Flow**:
```csharp
public async Task<AuthResponse> RefreshTokenAsync()
{
    var refreshToken = await SecureStorage.GetAsync("refresh_token");
    
    var content = new FormUrlEncodedContent(new[]
    {
        new KeyValuePair<string, string>("grant_type", "refresh_token"),
        new KeyValuePair<string, string>("refresh_token", refreshToken),
        new KeyValuePair<string, string>("client_id", "mobile-app")
    });
    
    var response = await _httpClient.PostAsync($"{_authServerUrl}/connect/token", content);
    
    if (response.IsSuccessStatusCode)
    {
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        
        // Update stored tokens
        await SecureStorage.SetAsync("access_token", result.AccessToken);
        await SecureStorage.SetAsync("refresh_token", result.RefreshToken);
        
        return result;
    }
    else
    {
        // Refresh token expired, require re-login
        await Shell.Current.GoToAsync("//LoginPage");
        return null;
    }
}
```

---

## ?? Authorization

### Role-Based Access Control (RBAC)

**User Roles**:

| Role | Description | Permissions |
|------|-------------|-------------|
| `booker` | Passenger/booker | Submit quotes, create bookings, view own data |
| `driver` | Driver | Accept rides, broadcast location, update status |
| `dispatcher` | Dispatcher | View all quotes/bookings, respond to quotes, assign drivers |
| `admin` | Administrator | Full system access, user management |

---

### Role Assignment

**JWT Claim**:
```json
{
  "role": "booker"
}
```

**Mobile App Role Check**:
```csharp
// Check if user has role
public bool HasRole(string role)
{
    var token = await SecureStorage.GetAsync("access_token");
    var handler = new JwtSecurityTokenHandler();
    var jwt = handler.ReadJwtToken(token);
    
    var roleClaim = jwt.Claims.FirstOrDefault(c => c.Type == "role");
    return roleClaim?.Value == role;
}
```

---

### Authorization Policies

**AdminAPI Endpoint Authorization**:

```csharp
// Requires authentication (any authenticated user)
[Authorize]
[HttpGet("/quotes/list")]
public IActionResult GetQuotes() { ... }

// Requires specific role
[Authorize(Roles = "admin,dispatcher")]
[HttpGet("/quotes/all")]
public IActionResult GetAllQuotes() { ... }

// Custom policy (staff only)
[Authorize(Policy = "StaffOnly")]
[HttpPost("/quotes/{id}/acknowledge")]
public IActionResult AcknowledgeQuote(string id) { ... }
```

**Policy Definition**:
```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("StaffOnly", policy =>
        policy.RequireRole("admin", "dispatcher", "driver"));
    
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("admin"));
});
```

---

### Data Isolation

**User Ownership Filtering**:

All quotes and bookings are filtered by `CreatedByUserId` to ensure users only see their own data.

**AdminAPI Implementation**:
```csharp
[Authorize]
[HttpGet("/quotes/list")]
public IActionResult GetQuotes()
{
    // Get user ID from JWT claims
    var userId = User.FindFirst("sub")?.Value;
    
    // Filter quotes by owner
    var quotes = _quoteRepo.GetAll()
        .Where(q => q.CreatedByUserId == userId)
        .ToList();
    
    return Ok(quotes);
}
```

**Enforcement Points**:
- ? Quote list: Only user's quotes returned
- ? Quote details: 403 Forbidden if not owner
- ? Booking list: Only user's bookings returned
- ? Booking details: 403 Forbidden if not owner
- ? Accept/Cancel: Only owner can perform actions

---

### Email-Based Authorization (Driver Tracking)

**Special Case**: Passengers can track rides if their email matches booking

**AdminAPI Validation**:
```csharp
[Authorize]
[HttpGet("/passenger/rides/{rideId}/location")]
public async Task<IActionResult> GetDriverLocation(string rideId)
{
    var ride = await _rideRepo.GetAsync(rideId);
    if (ride == null) return NotFound();
    
    // Get user email from JWT
    var userEmail = User.FindFirst("email")?.Value;
    
    // Get booking associated with ride
    var booking = await _bookingRepo.GetAsync(ride.BookingId);
    
    // Verify passenger email matches JWT email
    if (booking.Draft.Passenger.EmailAddress != userEmail)
    {
        return Forbid(); // 403 Forbidden
    }
    
    // Authorized - return location
    var location = await _locationRepo.GetLatestAsync(rideId);
    return Ok(location);
}
```

**Mobile App Usage**:
```csharp
// JWT contains user@example.com
// Booking passenger email is user@example.com
// ? Authorized

// JWT contains different@example.com
// Booking passenger email is user@example.com
// ? 403 Forbidden
```

---

## ?? Security Best Practices

### ? DO

**1. Always Use HTTPS**
```csharp
// Production URLs must use HTTPS
const string AdminApiUrl = "https://api.bellwood.com"; // ?
const string AdminApiUrl = "http://api.bellwood.com";  // ?
```

**2. Store Tokens in Secure Storage**
```csharp
// ? GOOD
await SecureStorage.SetAsync("access_token", token);

// ? BAD
Preferences.Set("access_token", token); // Plain text storage
```

**3. Validate JWT Expiration**
```csharp
public async Task<string> GetValidTokenAsync()
{
    var token = await SecureStorage.GetAsync("access_token");
    
    var handler = new JwtSecurityTokenHandler();
    var jwt = handler.ReadJwtToken(token);
    
    if (jwt.ValidTo < DateTime.UtcNow)
    {
        // Token expired, refresh
        await RefreshTokenAsync();
        token = await SecureStorage.GetAsync("access_token");
    }
    
    return token;
}
```

**4. Handle 401 Unauthorized**
```csharp
try
{
    var result = await _adminApi.GetQuotesAsync();
}
catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
{
    // Token expired or invalid, redirect to login
    await Shell.Current.GoToAsync("//LoginPage");
}
```

**5. Clear Tokens on Logout**
```csharp
public void Logout()
{
    SecureStorage.Remove("access_token");
    SecureStorage.Remove("refresh_token");
    
    // Clear other sensitive data
    Preferences.Clear();
    
    // Navigate to login
    Shell.Current.GoToAsync("//LoginPage");
}
```

---

### ? DON'T

**1. Never Store Passwords**
```csharp
// ? BAD - Never store plain text passwords
Preferences.Set("password", password);

// ? GOOD - Only store tokens
await SecureStorage.SetAsync("access_token", token);
```

**2. Never Log Tokens**
```csharp
// ? BAD
Console.WriteLine($"Token: {token}");

// ? GOOD
Console.WriteLine($"Token: {token.Substring(0, 10)}...");
```

**3. Never Hardcode Credentials**
```csharp
// ? BAD
var username = "admin@example.com";
var password = "password123";

// ? GOOD - User enters credentials
var username = UsernameEntry.Text;
var password = PasswordEntry.Text;
```

**4. Never Bypass SSL Validation**
```csharp
// ? BAD - Never in production
ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, errors) => true;

// ? GOOD - Use proper certificates
// (default behavior validates certificates)
```

---

## ??? Threat Mitigation

### Token Theft Protection

**Threats**:
- Malware on device
- Physical device access
- Network interception

**Mitigations**:
- ? Tokens stored in platform-specific secure storage (Keychain/Keystore)
- ? HTTPS/TLS encryption in transit
- ? Short token lifetimes (1 hour)
- ? Refresh tokens for re-authentication
- ? Device PIN/biometric protection

---

### Man-in-the-Middle (MITM) Protection

**Threats**:
- Public WiFi interception
- DNS spoofing
- Certificate spoofing

**Mitigations**:
- ? HTTPS only (TLS 1.2+)
- ? Certificate validation (platform default)
- ? Certificate pinning (future enhancement)

---

### Data Isolation Bypass

**Threats**:
- User guessing other users' quote IDs
- User modifying API requests
- SQL injection (backend)

**Mitigations**:
- ? Backend filters by `CreatedByUserId` (from JWT)
- ? 403 Forbidden if unauthorized access attempted
- ? Parameterized queries (no SQL injection)
- ? Input validation on all endpoints

---

## ?? Security Testing

### Test Cases

**1. Authentication**:
- ? Valid credentials return JWT
- ? Invalid credentials return 401
- ? Expired tokens return 401
- ? Missing token returns 401

**2. Authorization**:
- ? User can access own quotes
- ? User cannot access other users' quotes (403)
- ? User can accept own quotes
- ? User cannot accept other users' quotes (403)

**3. Data Isolation**:
- ? Quote list only shows user's quotes
- ? Booking list only shows user's bookings
- ? Driver tracking only for passenger's rides

**4. Token Lifecycle**:
- ? Tokens stored securely
- ? Tokens cleared on logout
- ? Refresh tokens work correctly
- ? Expired tokens trigger re-login

---

## ?? Related Documentation

- **[00-README.md](00-README.md)** - Quick start & overview
- **[01-System-Architecture.md](01-System-Architecture.md)** - Architecture details
- **[20-API-Integration.md](20-API-Integration.md)** - API authentication
- **[22-Configuration.md](22-Configuration.md)** - Configuration security
- **[32-Troubleshooting.md](32-Troubleshooting.md)** - Security issues & solutions

---

**Last Updated**: January 27, 2026  
**Version**: 1.0  
**Status**: ? Production Ready
