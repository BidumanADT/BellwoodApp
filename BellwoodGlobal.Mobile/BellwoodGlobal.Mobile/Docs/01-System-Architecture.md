# System Architecture

**Document Type**: Living Document - Technical Reference  
**Last Updated**: January 27, 2026  
**Status**: ? Production Ready

---

## ?? Overview

The Bellwood Global Mobile App is built on **.NET MAUI (Multi-platform App UI)**, a cross-platform framework that enables a single C# codebase to run on iOS, Android, Windows, and macOS. The architecture follows **MVVM (Model-View-ViewModel)** patterns with clean separation of concerns.

**Design Philosophy**:
- ?? **Single Codebase** - Write once, run everywhere
- ?? **Service-Oriented** - Decoupled, testable services
- ?? **Platform-Aware** - Leverage native capabilities when needed
- ?? **Security-First** - Authentication, authorization, data isolation
- ? **Performance-Optimized** - Async operations, minimal UI blocking

---

## ??? High-Level Architecture

```
???????????????????????????????????????????????????????????????
?                    Bellwood Mobile App                      ?
?                     (.NET MAUI 9.0)                         ?
???????????????????????????????????????????????????????????????
?                                                             ?
?  ????????????????  ????????????????  ????????????????     ?
?  ?   Pages      ?  ?  ViewModels  ?  ?   Controls   ?     ?
?  ?  (XAML/C#)   ?  ?   (Logic)    ?  ?  (Custom)    ?     ?
?  ????????????????  ????????????????  ????????????????     ?
?         ?                  ?                                ?
?         ?????????????????????????????????????              ?
?                                              ?              ?
?  ???????????????????????????????????????????????????????   ?
?  ?              Services Layer                         ?   ?
?  ???????????????????????????????????????????????????????   ?
?  ?  • AdminApi (HTTP Client)                          ?   ?
?  ?  • ConfigurationService (Settings)                 ?   ?
?  ?  • LocationService (GPS)                           ?   ?
?  ?  • PlacesService (Google Autocomplete)             ?   ?
?  ?  • AuthService (Authentication)                    ?   ?
?  ???????????????????????????????????????????????????????   ?
?                             ?                               ?
???????????????????????????????????????????????????????????????
                              ?
                 ???????????????????????????
                 ?                         ?
        ???????????????????       ??????????????????
        ?   AdminAPI      ?       ?  Google Places ?
        ? (Backend REST)  ?       ?      API       ?
        ???????????????????       ??????????????????
                 ?
        ???????????????????
        ?   AuthServer    ?
        ?  (IdentityServer)?
        ???????????????????
```

---

## ?? Core Components

### 1. Presentation Layer

#### Pages (XAML + C#)
**Location**: `BellwoodGlobal.Mobile/Pages/`

**Purpose**: UI definition and user interaction

**Key Pages**:
- `LoginPage.xaml` - User authentication
- `MainPage.xaml` - Main navigation hub
- `QuoteDashboardPage.xaml` - Quote list & management
- `QuoteDetailPage.xaml` - Quote details & actions
- `BookingsPage.xaml` - Booking list
- `BookingDetailPage.xaml` - Booking details & tracking
- `DriverTrackingPage.xaml` - Real-time driver location map
- `QuotePage.xaml` - Quote request form

**Pattern**:
```csharp
// XAML defines UI structure
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui">
    <VerticalStackLayout>
        <Label Text="Hello, MAUI!" />
        <Button Clicked="OnButtonClicked" />
    </VerticalStackLayout>
</ContentPage>

// Code-behind handles interactions
public partial class MyPage : ContentPage
{
    private readonly IAdminApi _api;
    
    public MyPage()
    {
        InitializeComponent();
        _api = ServiceHelper.GetRequiredService<IAdminApi>();
    }
    
    private async void OnButtonClicked(object sender, EventArgs e)
    {
        await _api.DoSomethingAsync();
    }
}
```

---

#### Custom Controls
**Location**: `BellwoodGlobal.Mobile/Controls/`

**Purpose**: Reusable UI components

**Examples**:
- `LocationPicker.xaml` - Address autocomplete control
- `StatusBadge.xaml` - Colored status indicators
- `LoadingIndicator.xaml` - Activity spinners

---

### 2. Services Layer

#### IAdminApi / AdminApi
**Location**: `BellwoodGlobal.Mobile/Services/AdminApi.cs`

**Purpose**: Backend API client for all server communication

**Key Methods**:
```csharp
public interface IAdminApi
{
    // Quotes
    Task SubmitQuoteAsync(QuoteDraft draft);
    Task<IReadOnlyList<QuoteListItem>> GetQuotesAsync(int take = 50);
    Task<QuoteDetail?> GetQuoteAsync(string id);
    Task<AcceptQuoteResponse> AcceptQuoteAsync(string quoteId);
    Task CancelQuoteAsync(string quoteId);
    
    // Bookings
    Task SubmitBookingAsync(QuoteDraft draft);
    Task<IReadOnlyList<BookingListItem>> GetBookingsAsync(int take = 50);
    Task<BookingDetail?> GetBookingAsync(string id);
    Task CancelBookingAsync(string id);
    
    // Driver Tracking
    Task<DriverLocation?> GetDriverLocationAsync(string rideId);
}
```

**Implementation**:
- Uses `HttpClient` with `IHttpClientFactory`
- JSON serialization with `System.Text.Json`
- JWT Bearer token authentication
- Automatic retry on transient failures
- Error handling with typed exceptions

---

#### ConfigurationService
**Location**: `BellwoodGlobal.Mobile/Services/ConfigurationService.cs`

**Purpose**: Centralized app configuration management

**Features**:
- Loads `appsettings.json` asynchronously
- Merges environment-specific settings
- Reads environment variables
- Secure storage for API keys
- Lazy initialization to avoid blocking startup

**Usage**:
```csharp
var config = ServiceHelper.GetRequiredService<IConfigurationService>();
string apiUrl = await config.GetAdminApiUrlAsync();
string apiKey = await config.GetGooglePlacesApiKeyAsync();
```

**Performance**:
- Async loading: 252ms (72% faster than sync)
- Zero UI blocking
- Cached after first load

---

#### PlacesService
**Location**: `BellwoodGlobal.Mobile/Services/PlacesService.cs`

**Purpose**: Google Places API integration for address autocomplete

**Features**:
- Real-time address suggestions
- Location biasing (prioritize nearby results)
- Coordinates retrieval
- Quota management (stays within free tier)

**Flow**:
```
User types "123 Main" 
    ?
PlacesService.GetPredictionsAsync()
    ?
Google Places API (Autocomplete)
    ?
Returns 5 suggestions
    ?
User selects "123 Main St, Chicago, IL"
    ?
PlacesService.GetPlaceDetailsAsync()
    ?
Google Places API (Place Details)
    ?
Returns coordinates (41.8781, -87.6298)
    ?
Stored in QuoteDraft
```

---

#### LocationService
**Location**: `BellwoodGlobal.Mobile/Services/LocationService.cs`

**Purpose**: GPS location access for driver tracking

**Features**:
- Continuous location updates
- Battery-efficient (configurable intervals)
- Permission handling
- Background location (when app minimized)

**Platform-Specific**:
- **Android**: Uses `FusedLocationProviderClient`
- **iOS**: Uses `CLLocationManager`
- **Abstracted** via MAUI `Geolocation` API

---

### 3. Data Models

#### Domain Entities
**Location**: `BellwoodGlobal.Core/Domain/`

**Purpose**: Shared business entities used across projects

**Key Entities**:
- `QuoteDraft` - Quote request data
- `BookingRequest` - Booking creation data
- `ContactInfo` - Person contact details
- `FlightInfo` - Flight details
- `PickupStyle` - Enum (Curbside, MeetAndGreet)
- `VehicleClass` - Enum (Sedan, SUV, Executive, etc.)

---

#### Client Models
**Location**: `BellwoodGlobal.Mobile/Models/`

**Purpose**: Mobile-specific DTOs and response models

**Key Models**:
- `QuoteListItem` - Quote list row data
- `QuoteDetail` - Full quote details
- `BookingListItem` - Booking list row data
- `BookingDetail` - Full booking details
- `DriverLocation` - GPS coordinates + metadata
- `AcceptQuoteResponse` - Accept quote API response

---

### 4. Platform Layer

#### Platform-Specific Code
**Location**: `BellwoodGlobal.Mobile/Platforms/`

**Structure**:
```
Platforms/
??? Android/
?   ??? MainActivity.cs
?   ??? MainApplication.cs
?   ??? AndroidManifest.xml
??? iOS/
?   ??? AppDelegate.cs
?   ??? Info.plist
?   ??? Entitlements.plist
??? Windows/
    ??? App.xaml.cs
    ??? Package.appxmanifest
```

**Purpose**:
- Platform initialization
- Permission handling
- Native API access
- Platform-specific configurations

---

## ?? Data Flow Diagrams

### Quote Submission Flow

```
???????????????
?  QuotePage  ? (User fills form)
???????????????
       ? 1. Submit
       ?
???????????????
? QuoteDraft  ? (Data model)
???????????????
       ? 2. Pass to API
       ?
???????????????
?  AdminApi   ? (HTTP POST /quotes)
???????????????
       ? 3. Send to backend
       ?
???????????????
?  AdminAPI   ? (Backend validates & stores)
?   Server    ?
???????????????
       ? 4. Return quote ID
       ?
???????????????
?  QuotePage  ? (Show success, navigate to dashboard)
???????????????
```

---

### Real-Time Location Tracking Flow

```
??????????????????
? Driver's Phone ? (Sends GPS updates every 15s)
??????????????????
         ? 1. POST /passenger/rides/{id}/location
         ?
??????????????????
?   AdminAPI     ? (Stores location, broadcasts via SignalR)
??????????????????
         ? 2. Polling (every 15s)
         ?
??????????????????
? Passenger App  ? (GET /passenger/rides/{id}/location)
?   AdminApi     ?
??????????????????
         ? 3. Parse response
         ?
??????????????????
?DriverTracking  ? (Update map marker + ETA)
?     Page       ?
??????????????????
```

**Note**: Polling-based in v1.0. WebSockets (SignalR) planned for v1.1 for real-time push.

---

### Authentication Flow

```
???????????????
?  LoginPage  ? (User enters email/password)
???????????????
       ? 1. Submit credentials
       ?
???????????????
? AuthService ? (POST /connect/token)
???????????????
       ? 2. Send to AuthServer
       ?
???????????????
? AuthServer  ? (Validate, generate JWT)
?(IdentityServer)?
???????????????
       ? 3. Return JWT access token
       ?
???????????????
?SecureStorage? (Store token)
???????????????
       ? 4. Token saved
       ?
???????????????
?  AdminApi   ? (Include in Authorization header)
?             ?  Authorization: Bearer {token}
???????????????
```

---

## ??? Technology Stack

### Frameworks & Libraries

| Technology | Version | Purpose |
|------------|---------|---------|
| **.NET MAUI** | 9.0 | Cross-platform UI framework |
| **C#** | 13.0 | Primary language |
| **.NET Runtime** | 9.0 | Application runtime |
| **System.Text.Json** | 9.0 | JSON serialization |
| **Microsoft.Extensions.Http** | 9.0 | HTTP client factory |
| **Microsoft.Maui.Essentials** | 9.0 | Device APIs (GPS, storage, etc.) |

---

### External APIs

| API | Purpose | Documentation |
|-----|---------|---------------|
| **AdminAPI** | Backend services (quotes, bookings, tracking) | `20-API-Integration.md` |
| **AuthServer** | JWT authentication | Internal IdentityServer |
| **Google Places API** | Address autocomplete | `10-Google-Places-Autocomplete.md` |

---

### Development Tools

| Tool | Purpose |
|------|---------|
| **Visual Studio 2022** | IDE (Windows) |
| **VS Code** | IDE (cross-platform) |
| **Android SDK** | Android development |
| **Xcode** | iOS development (macOS) |
| **Git** | Version control |
| **PowerShell** | Scripting & automation |

---

## ?? Design Decisions

### Decision 1: .NET MAUI over Xamarin.Forms

**Problem**: Need cross-platform mobile app with modern framework

**Options Considered**:
1. Xamarin.Forms (deprecated)
2. .NET MAUI (successor to Xamarin)
3. React Native
4. Flutter

**Decision**: .NET MAUI

**Rationale**:
- ? Native .NET stack (C# expertise)
- ? Single codebase for iOS, Android, Windows, macOS
- ? Active Microsoft support
- ? Seamless integration with backend (.NET APIs)
- ? Better performance than Xamarin
- ? Modern tooling

---

### Decision 2: Polling vs. WebSockets for Real-Time Updates

**Problem**: How to get real-time quote status and driver location updates?

**Options Considered**:
1. Short polling (current)
2. Long polling
3. WebSockets (SignalR)
4. Server-Sent Events (SSE)

**Decision**: Polling for v1.0, WebSockets for v1.1

**Rationale (v1.0)**:
- ? Simpler implementation (HTTP only)
- ? Works everywhere (no firewall issues)
- ? Adequate for alpha testing (30s quote polling, 15s GPS polling)
- ? Easier debugging
- ?? Less efficient (more requests)
- ?? Not truly real-time (delay up to polling interval)

**Planned Upgrade (v1.1)**:
- SignalR for instant push notifications
- Quote status changes pushed immediately
- Driver location streamed in real-time
- Fallback to polling if WebSocket fails

---

### Decision 3: Google Places API over Native Maps

**Problem**: Need address autocomplete for quote/booking forms

**Options Considered**:
1. Native Maps Autocomplete (iOS: MKLocalSearch, Android: Places SDK)
2. Google Places API (Web Service)
3. Bing Maps API
4. Manual address entry only

**Decision**: Google Places API

**Rationale**:
- ? Cross-platform (same API for all platforms)
- ? Superior autocomplete quality
- ? Consistent user experience
- ? Returns coordinates for backend
- ? Free tier sufficient (2,500 requests/day)
- ? API key restrictions for security
- ?? Requires internet connection
- ?? External dependency

---

### Decision 4: File-Based Configuration over Hardcoded Values

**Problem**: How to manage API URLs, keys, and settings?

**Options Considered**:
1. Hardcoded in C# classes
2. appsettings.json (current)
3. Environment variables only
4. Remote configuration service

**Decision**: appsettings.json + Environment Variables

**Rationale**:
- ? Standard .NET pattern
- ? Easy to change without recompiling
- ? Environment-specific overrides
- ? Secure storage for secrets (SecureStorage)
- ? Testable (mock configuration)
- ?? Requires app update for config changes (acceptable for mobile)

See `22-Configuration.md` for details.

---

### Decision 5: Email-Based Authorization for Driver Tracking

**Problem**: How to ensure passengers only track their own rides?

**Options Considered**:
1. Ride ID only (insecure)
2. JWT claims validation
3. Email-based authorization (current)
4. Ride-specific access tokens

**Decision**: Email-based authorization

**Rationale**:
- ? Simple to implement
- ? Leverages existing JWT email claim
- ? Backend validates booking passenger email matches JWT
- ? No additional tokens needed
- ? Works seamlessly with current auth flow
- ?? Requires email in JWT (already implemented)

See `23-Security-Model.md` for authorization details.

---

## ?? Integration Points

### AdminAPI Integration

**Base URL**: `https://api.bellwood.com` (production)

**Authentication**: JWT Bearer token

**Key Endpoints**:
- `POST /quotes` - Submit quote
- `GET /quotes/list` - List user's quotes
- `GET /quotes/{id}` - Quote details
- `POST /quotes/{id}/accept` - Accept quote
- `POST /quotes/{id}/cancel` - Cancel quote
- `POST /bookings` - Submit booking
- `GET /bookings/list` - List user's bookings
- `GET /bookings/{id}` - Booking details
- `GET /passenger/rides/{id}/location` - Driver location

See `20-API-Integration.md` for complete endpoint documentation.

---

### AuthServer Integration

**Base URL**: `https://auth.bellwood.com` (production)

**Protocol**: OAuth 2.0 / OpenID Connect

**Token Endpoint**: `POST /connect/token`

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

**Response**:
```json
{
  "access_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expires_in": 3600,
  "token_type": "Bearer"
}
```

---

### Google Places API Integration

**API Key**: Restricted by HTTP referrer and API

**Endpoints Used**:
1. **Autocomplete**: `/maps/api/place/autocomplete/json`
2. **Place Details**: `/maps/api/place/details/json`

**Quota**:
- 2,500 requests/day (free tier)
- ~83 quote submissions/day (30 autocomplete calls each)
- Monitoring via Google Cloud Console

See `10-Google-Places-Autocomplete.md` for implementation details.

---

## ?? Performance Characteristics

### Startup Performance

| Metric | Target | Actual | Status |
|--------|--------|--------|--------|
| **Cold Start** | <3s | ~2s | ? Exceeds |
| **Warm Start** | <1s | ~0.8s | ? Exceeds |
| **Config Load** | <500ms | 252ms | ? Exceeds |
| **UI Ready** | <2s | ~1.5s | ? Exceeds |

---

### Memory Footprint

| State | Memory Usage | Status |
|-------|--------------|--------|
| **Idle** | ~60 MB | ? Excellent |
| **Active Use** | ~85 MB | ? Good |
| **With Map** | ~120 MB | ? Acceptable |

---

### Network Efficiency

| Operation | Frequency | Payload Size |
|-----------|-----------|--------------|
| **Quote Polling** | Every 30s | ~2-5 KB |
| **GPS Polling** | Every 15s | ~500 bytes |
| **Quote Submit** | On-demand | ~3-5 KB |
| **Autocomplete** | Keystroke (debounced 300ms) | ~1 KB |

**Optimization**:
- Polling only when screen active
- Stops polling on page disappear
- Debounced autocomplete requests
- Compressed JSON responses

See `33-Performance.md` for optimization details.

---

## ?? Security Architecture

### Defense in Depth

```
???????????????????????????????????????????
?   Layer 1: Network Security            ?
?   • HTTPS only (TLS 1.2+)              ?
?   • Certificate pinning (future)       ?
???????????????????????????????????????????
             ?
???????????????????????????????????????????
?   Layer 2: Authentication               ?
?   • JWT Bearer tokens                   ?
?   • Token expiration (1 hour)           ?
?   • Secure storage (device keychain)    ?
???????????????????????????????????????????
             ?
???????????????????????????????????????????
?   Layer 3: Authorization                ?
?   • Role-based access control (RBAC)    ?
?   • Email-based ride access             ?
?   • CreatedByUserId filtering           ?
???????????????????????????????????????????
             ?
???????????????????????????????????????????
?   Layer 4: Data Isolation               ?
?   • User can only see own data          ?
?   • Backend enforces ownership          ?
?   • No client-side filtering bypass     ?
???????????????????????????????????????????
             ?
???????????????????????????????????????????
?   Layer 5: API Security                 ?
?   • API key restrictions (Google)       ?
?   • Rate limiting (backend)             ?
?   • Input validation                    ?
???????????????????????????????????????????
```

See `23-Security-Model.md` for security details.

---

## ?? Related Documentation

- **[00-README.md](00-README.md)** - Quick start & feature overview
- **[02-Testing-Guide.md](02-Testing-Guide.md)** - Testing strategies
- **[10-Google-Places-Autocomplete.md](10-Google-Places-Autocomplete.md)** - Autocomplete implementation
- **[11-Location-Tracking.md](11-Location-Tracking.md)** - GPS tracking details
- **[20-API-Integration.md](20-API-Integration.md)** - AdminAPI endpoints
- **[21-Data-Models.md](21-Data-Models.md)** - DTO specifications
- **[22-Configuration.md](22-Configuration.md)** - Configuration guide
- **[23-Security-Model.md](23-Security-Model.md)** - Security & authorization
- **[30-Deployment-Guide.md](30-Deployment-Guide.md)** - Build & deployment
- **[33-Performance.md](33-Performance.md)** - Performance optimizations

---

**Last Updated**: January 27, 2026  
**Version**: 1.0  
**Status**: ? Production Ready
