# Bellwood Global Mobile App

**Type**: .NET MAUI Multi-Platform Application  
**Framework**: .NET 9.0  
**Status**: ? Production Ready

---

## ?? Overview

The Bellwood Global Mobile App is a comprehensive transportation management solution built with .NET MAUI, providing seamless booking, tracking, and communication capabilities for both passengers and drivers.

**Key Capabilities**:
- ?? Cross-platform (iOS, Android, Windows, macOS)
- ?? Real-time driver location tracking
- ?? Google Places address autocomplete
- ?? Quote request and management
- ?? Booking creation and tracking
- ?? Secure authentication and authorization
- ? High-performance, optimized UI

---

## ? Features

### For Passengers
- ? **Smart Address Entry** - Google Places autocomplete with location bias
- ? **Quote Requests** - Submit and track transportation quotes
- ? **Real-Time Tracking** - Track driver location with ETA calculations
- ? **Booking Management** - View and manage all bookings
- ? **Secure Access** - Email-based authentication with JWT tokens

### For Drivers (Future)
- ? **Route Navigation** - Turn-by-turn directions
- ? **Location Broadcasting** - Automatic GPS updates
- ? **Booking Acceptance** - Accept/decline ride requests

### Technical Features
- ? **Offline-First Architecture** - Works without constant connectivity
- ? **Performance Optimized** - 72% faster config loading, zero UI blocking
- ? **Security Hardened** - API key restrictions, secure storage, JWT auth
- ? **Data Isolation** - Users only see their own data
- ? **Responsive Design** - Adapts to all screen sizes

---

## ?? Quick Start

### Prerequisites

**Required Software**:
- ? .NET 9.0 SDK ([Download](https://dotnet.microsoft.com/download))
- ? Visual Studio 2022 17.8+ or VS Code
- ? Android SDK (for Android deployment)
- ? Xcode (for iOS deployment on Mac)

**Verification**:
```bash
dotnet --version
# Expected: 9.0.x or higher
```

---

### Step 1: Clone Repository

```bash
git clone https://github.com/BidumanADT/BellwoodApp.git
cd BellwoodMobileApp/BellwoodGlobal.Mobile
```

---

### Step 2: Configure Settings

**Create Local Configuration** (optional):

`appsettings.Development.json`:
```json
{
  "AdminApiUrl": "https://localhost:5206",
  "AuthServerUrl": "https://localhost:5001",
  "GooglePlacesApiKey": "your-api-key-here"
}
```

**Environment Variables** (recommended for production):
```bash
# Windows PowerShell
$env:ADMIN_API_URL = "https://api.bellwood.com"
$env:AUTH_SERVER_URL = "https://auth.bellwood.com"
$env:GOOGLE_PLACES_API_KEY = "your-production-key"

# Linux/macOS
export ADMIN_API_URL="https://api.bellwood.com"
export AUTH_SERVER_URL="https://auth.bellwood.com"
export GOOGLE_PLACES_API_KEY="your-production-key"
```

See `22-Configuration.md` for detailed configuration options.

---

### Step 3: Restore Dependencies

```bash
dotnet restore
```

---

### Step 4: Build & Run

**Run on Android Emulator**:
```bash
dotnet build -t:Run -f net9.0-android
```

**Run on iOS Simulator** (macOS only):
```bash
dotnet build -t:Run -f net9.0-ios
```

**Run on Windows**:
```bash
dotnet build -t:Run -f net9.0-windows10.0.19041.0
```

---

### Step 5: Test Login

**Default Test Credentials**:
- Email: `testuser@example.com`
- Password: `Test123!`

**Verify**:
1. App launches successfully
2. Login screen appears
3. Can authenticate with test credentials
4. Main navigation loads

---

## ?? Documentation Library

### ?? Overview & Architecture
| Document | Description |
|----------|-------------|
| **[00-README.md](00-README.md)** | This document - Quick start & overview |
| **[01-System-Architecture.md](01-System-Architecture.md)** | MAUI architecture, components, data flow |
| **[02-Testing-Guide.md](02-Testing-Guide.md)** | Testing strategies, running tests, scenarios |

### ?? Feature Documentation
| Document | Description |
|----------|-------------|
| **[10-Google-Places-Autocomplete.md](10-Google-Places-Autocomplete.md)** | Address autocomplete implementation |
| **[11-Location-Tracking.md](11-Location-Tracking.md)** | Real-time driver/passenger tracking |
| **[12-Quote-Lifecycle.md](12-Quote-Lifecycle.md)** | Quote request and management (Phase Alpha) |
| **[13-Booking-Management.md](13-Booking-Management.md)** | Booking creation and tracking |
| **[14-Driver-Tracking-Map.md](14-Driver-Tracking-Map.md)** | Map integration & ETA calculation |

### ?? Technical References
| Document | Description |
|----------|-------------|
| **[20-API-Integration.md](20-API-Integration.md)** | AdminAPI endpoints used by mobile app |
| **[21-Data-Models.md](21-Data-Models.md)** | DTOs, client models, data structures |
| **[22-Configuration.md](22-Configuration.md)** | App settings, secure storage, API keys |
| **[23-Security-Model.md](23-Security-Model.md)** | Authentication, authorization, data isolation |

### ?? Deployment & Operations
| Document | Description |
|----------|-------------|
| **[30-Deployment-Guide.md](30-Deployment-Guide.md)** | Build, publish, deploy to app stores |
| **[31-Scripts-Reference.md](31-Scripts-Reference.md)** | Testing scripts and automation |
| **[32-Troubleshooting.md](32-Troubleshooting.md)** | Common issues & solutions |
| **[33-Performance.md](33-Performance.md)** | Performance optimization details |

---

## ??? Project Structure

```
BellwoodGlobal.Mobile/
??? BellwoodGlobal.Mobile/          # Main MAUI project
?   ??? Pages/                      # UI pages (ContentPage)
?   ??? ViewModels/                 # MVVM view models
?   ??? Services/                   # Business logic & API clients
?   ??? Models/                     # Client-side data models
?   ??? Controls/                   # Custom controls
?   ??? Resources/                  # Images, fonts, styles
?   ??? Platforms/                  # Platform-specific code
??? BellwoodGlobal.Core/            # Shared business logic
?   ??? Domain/                     # Domain entities
??? Docs/                           # This documentation library
??? Scripts/                        # Testing & automation scripts
```

---

## ??? Development Workflow

### Daily Development

```bash
# 1. Pull latest changes
git pull origin main

# 2. Create feature branch
git checkout -b feature/your-feature-name

# 3. Make changes & test
dotnet build
dotnet test

# 4. Commit & push
git add .
git commit -m "feat: your feature description"
git push origin feature/your-feature-name

# 5. Create pull request
```

### Testing

```bash
# Run all tests
dotnet test

# Run specific test category
dotnet test --filter Category=Unit
dotnet test --filter Category=Integration

# Generate coverage report
dotnet test --collect:"XPlat Code Coverage"
```

See `02-Testing-Guide.md` for comprehensive testing instructions.

---

## ?? Configuration Options

### Application Settings

**File**: `appsettings.json`

```json
{
  "AdminApiUrl": "https://api.bellwood.com",
  "AuthServerUrl": "https://auth.bellwood.com",
  "GooglePlacesApiKey": "AIza...",
  "LocationUpdateIntervalSeconds": 15,
  "QuotePollingIntervalSeconds": 30,
  "EnableDebugLogging": false
}
```

**Key Settings**:
- `AdminApiUrl` - Backend API endpoint
- `AuthServerUrl` - Authentication server
- `GooglePlacesApiKey` - Google Places API key (restricted)
- `LocationUpdateIntervalSeconds` - GPS tracking frequency (default: 15s)
- `QuotePollingIntervalSeconds` - Quote status polling (default: 30s)

See `22-Configuration.md` for all configuration options.

---

## ?? Security & Authentication

### Authentication Flow

1. User enters email/password on `LoginPage`
2. App calls AuthServer `/connect/token` endpoint
3. Receives JWT access token
4. Stores token in `SecureStorage`
5. Includes token in all AdminAPI requests

### Authorization

**User Roles**:
- `booker` - Passenger (submit quotes, book rides)
- `driver` - Driver (accept rides, broadcast location)
- `admin` - Administrator (full access)
- `dispatcher` - Dispatcher (manage quotes/bookings)

### Data Isolation

- ? Users only see their own quotes and bookings
- ? API filters by `CreatedByUserId` automatically
- ? Email-based authorization for tracking
- ? JWT claims validated on every request

See `23-Security-Model.md` for security details.

---

## ?? Performance Metrics

### Current Benchmarks

| Metric | Value | Status |
|--------|-------|--------|
| **App Startup Time** | <2 seconds | ? Excellent |
| **Config Load Time** | 252ms (was 914ms) | ? 72% improvement |
| **UI Blocking** | 0ms | ? Eliminated |
| **Memory Usage** | <100 MB | ? Optimized |
| **API Response Time** | <500ms | ? Fast |

### Optimizations Implemented

- ? Asynchronous configuration loading
- ? Lazy initialization of services
- ? Image caching
- ? Minimal polling intervals (30s quotes, 15s GPS)
- ? Efficient data binding

See `33-Performance.md` for optimization details.

---

## ?? Roadmap

### ? Completed (v1.0)
- Google Places Autocomplete
- Location Tracking
- Quote Management (Phase Alpha)
- Booking Management
- Performance Optimization
- Security Hardening

### ?? In Progress (v1.1)
- Real-time notifications (WebSockets)
- Offline mode improvements
- Driver app features

### ?? Planned (v2.0)
- In-app chat
- Payment integration
- Advanced route optimization
- Multi-language support

---

## ?? Known Issues

### Current Limitations

1. **Polling-Based Updates** (v1.0)
   - Quote status updates via 30-second polling
   - Driver location updates via 15-second polling
   - **Solution**: WebSockets planned for v1.1

2. **Manual Price Entry** (Phase Alpha)
   - Dispatcher manually enters estimated prices
   - Not integrated with Limo Anywhere yet
   - **Solution**: API integration planned for Phase Beta

3. **No Offline Booking Creation** (v1.0)
   - Requires internet connection to submit quotes
   - **Solution**: Offline queue planned for v1.1

See `32-Troubleshooting.md` for common issues and solutions.

---

## ?? Contributing

### Documentation Updates

**When to update docs**:
- ? New feature complete ? Update/create feature doc (10-19 series)
- ? API changes ? Update `20-API-Integration.md`
- ? Configuration added ? Update `22-Configuration.md`
- ? Bug fixed ? Add to `32-Troubleshooting.md`

**How to update**:
1. Edit the relevant numbered document
2. Update "Last Updated" date in header
3. Commit with message: `docs: update [document-name]`

### Code Contributions

**Before submitting PR**:
- [ ] All tests passing (`dotnet test`)
- [ ] No build warnings
- [ ] Documentation updated
- [ ] Code formatted (EditorConfig)
- [ ] Commit messages follow convention

**PR Template**:
```markdown
## Description
[What changed and why]

## Related Documentation
- Updated: `[document-name].md`
- Added: `[new-document].md` (if applicable)

## Testing
- [ ] Manual testing completed
- [ ] Unit tests added/updated
- [ ] Integration tests passing

## Screenshots (if UI changes)
[Add screenshots]
```

---

## ?? Support & Resources

### Getting Help

**Documentation**:
1. Check relevant doc in this library (see index above)
2. Review `32-Troubleshooting.md` for common issues
3. Search closed GitHub issues

**Community**:
- **GitHub Issues**: [Open an issue](https://github.com/BidumanADT/BellwoodApp/issues)
- **Email**: support@bellwood.com
- **Slack**: #mobile-dev (internal team)

### External Resources

- **.NET MAUI Docs**: [Microsoft Learn](https://learn.microsoft.com/dotnet/maui/)
- **Google Places API**: [Google Docs](https://developers.google.com/maps/documentation/places/web-service)
- **JWT Authentication**: [JWT.io](https://jwt.io/)

---

## ?? Acknowledgments

**Built With**:
- .NET MAUI (Cross-platform framework)
- Google Places API (Address autocomplete)
- AdminAPI (Backend services)
- AuthServer (Authentication)

**Team**:
- Development Team
- QA Team
- DevOps Team

---

## ?? Version History

### v1.0 (Current)
- Google Places Autocomplete
- Location Tracking
- Quote Management (Phase Alpha)
- Booking Management
- Performance Optimization
- Security Hardening

### v0.9 (Beta)
- Initial release
- Basic booking functionality
- Manual location entry

---

## ?? License

Proprietary - Bellwood Global  
© 2026 Bellwood Global. All rights reserved.

---

**Last Updated**: January 27, 2026  
**Version**: 1.0  
**Status**: ? Production Ready

---

**Ready to dive deeper?** Start with `01-System-Architecture.md` to understand how it all works! ??
