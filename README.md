# Bellwood Elite Mobile App

![.NET MAUI](https://img.shields.io/badge/.NET%20MAUI-9.0-512BD4?style=flat-square&logo=.net)
![Platform](https://img.shields.io/badge/platform-iOS%20%7C%20Android%20%7C%20Windows%20%7C%20macOS-lightgrey?style=flat-square)
![License](https://img.shields.io/badge/license-Proprietary-red?style=flat-square)

A premium cross-platform mobile application for Bellwood Global, providing seamless ride booking, quote management, and transportation services.

## Overview

Bellwood Elite is a .NET MAUI application that allows passengers to:
- Authenticate securely and manage their rider profile
- Book new trips and review booking history
- Request quotes (one-way or round trip) with flight information and special requests
- Save and re-use locations with native map integrations
- Track booking status updates as drivers progress through a trip
- View real-time driver location, ETA, and distance when tracking is active

## Architecture

The solution consists of three main projects:

### **BellwoodGlobal.Mobile** (Main Application)
- **Framework:** .NET 9.0
- **Target Platforms:**
  - iOS (18.0+)
  - Android (API 21+)
  - macOS Catalyst (13.1+)
  - Windows 10 (Build 17763+)
- **Application ID:** com.bellwoodglobal.mobile
- **Version:** 1.0

### **BellwoodGlobal.Core** (Domain Layer)
- **Framework:** .NET 8.0
- **Purpose:** Core domain models, business logic, and shared utilities

### **RidesApi** (Backend API)
- **Framework:** .NET 9.0/.NET 8.0
- **Purpose:** RESTful API for ride booking and management

## Current Capabilities

- **Authentication & Profiles:** JWT-secured login with stored tokens; profile data hydrates quote and booking flows.
- **Ride Booking & History:** Create rides, view historical trips, and see detailed booking information.
- **Quote Builder:** Booker/passenger selection, vehicle class, additional passengers, round-trip support, flight info, and additional requests with JSON preview/copy.
- **Payment Methods:** Manage and submit payment data for rides.
- **Location Services:** Cross-platform `LocationPickerService` with geocoding, reverse-geocoding, current-location lookup, and deep links to native map apps.
- **Booking Statuses:** Displays `CurrentRideStatus` (e.g., Driver En Route, Arrived, Passenger On Board) when present, falling back to booking `Status`; gold status chips for active driver states.
- **Driver Tracking:** Passenger-safe tracking via `GET /passenger/rides/{rideId}/location` with ETA/distance, stale-location warnings, and clear state messaging.

## Driver Tracking & Ride Status

- Passenger app polls the passenger-safe endpoint every ~15 seconds using the authenticated user's email claim for authorization.
- Supported tracking states: **Loading**, **Tracking**, **NotStarted** (driver has not begun), **Unavailable** (404 or GPS gap), **Unauthorized** (403 for non-owners), **Error**, and **Ended**. UI messages and retry visibility adapt per state.
- ETA uses Haversine distance plus driver speed (or a 35 km/h default) and marks estimates when speed is unknown.
- Map pins show pickup plus live driver location; auto-zoom keeps both visible when tracking.
- Backend must include `trackingActive: true` when location data is returned; otherwise the app treats the ride as "not started."
- Bookings list and detail views prioritize `CurrentRideStatus` so passengers see real-time driver progress and the "Track Driver" entry point appears for OnRoute/Arrived/PassengerOnboard.

## Project Structure

```
BellwoodGlobal.Mobile/
??? Pages/                          # XAML Pages & ViewModels
?   ??? LoginPage.xaml
?   ??? MainPage.xaml
?   ??? BookRidePage.xaml
?   ??? BookingsPage.xaml
?   ??? BookingDetailPage.xaml
?   ??? QuotePage.xaml
?   ??? QuoteDashboardPage.xaml
?   ??? QuoteDetailPage.xaml
?   ??? RideHistoryPage.xaml
?   ??? SplashPage.xaml
??? Services/                       # Business Services
?   ??? IAuthService.cs
?   ??? AuthService.cs
?   ??? IRideService.cs
?   ??? RideService.cs
?   ??? IQuoteService.cs
?   ??? QuoteService.cs
?   ??? IPaymentService.cs
?   ??? PaymentService.cs
?   ??? IProfileService.cs
?   ??? ProfileService.cs
?   ??? ILocationPickerService.cs
?   ??? LocationPickerService.cs
?   ??? ITripDraftBuilder.cs
?   ??? TripDraftBuilder.cs
?   ??? IQuoteDraftBuilder.cs
?   ??? QuoteDraftBuilder.cs
?   ??? IAdminApi.cs
?   ??? AdminApi.cs
?   ??? AuthHttpHandler.cs
??? Models/                         # Data Models
?   ??? BookingClientModels.cs
?   ??? QuotesClientModels.cs
?   ??? QuoteEstimate.cs
?   ??? Location.cs
?   ??? PaymentMethod.cs
?   ??? NewCardRequest.cs
?   ??? TripFormState.cs
?   ??? QuoteFormState.cs
?   ??? VehicleRules.cs
??? Converters/                     # Value Converters
?   ??? StatusToColorConverter.cs
??? Resources/                      # App Resources
?   ??? AppIcon/
?   ??? Images/
?   ??? Fonts/
?   ??? Styles/
?       ??? Styles.xaml
?       ??? Gradients.xaml
??? Platforms/                      # Platform-Specific Code
?   ??? Android/
?   ??? iOS/
?   ??? MacCatalyst/
?   ??? Windows/
??? App.xaml
??? AppShell.xaml
??? MauiProgram.cs
??? ServiceHelper.cs

BellwoodGlobal.Core/
??? Domain/                         # Domain Entities
?   ??? Ride.cs
?   ??? BookingRequest.cs
?   ??? BookingDetail.cs
?   ??? BookingStatus.cs
?   ??? QuoteDraft.cs
?   ??? Passenger.cs
?   ??? FlightInfo.cs
?   ??? PickupStyle.cs
??? Helpers/                        # Utility Classes
    ??? LocationHelper.cs
??? CapacityValidator.cs
```

## Documentation

- `Docs/PassengerLocationTracking-Implementation.md` – deep dive on tracking flow and models  
- `Docs/PassengerLocationTracking-TestingGuide.md` – edge cases and QA steps  
- `Docs/PassengerLocationTracking-QuickRef.md` – code snippets for developers  
- `Docs/PassengerLocationTracking-Summary.md` – executive summary  
- `Docs/CurrentRideStatus-Implementation-Summary.md` – status mapping and UI behavior  
- `Docs/LocationPickerService.md` & `Docs/LocationPickerService-Testing.md` – location picker design and validation  
- `Docs/DriverTracking-*` – diagnostic logging, polling loop fixes, and pending backend requirement (`trackingActive` flag)

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (17.8+) with MAUI workload  
  - OR [Visual Studio Code](https://code.visualstudio.com/) with C# Dev Kit
- Platform-specific requirements:
  - **iOS:** macOS with Xcode 15+
  - **Android:** Android SDK API 21-35
  - **Windows:** Windows 10 Build 17763+
  - **macOS:** macOS 13.1+

## Getting Started

1) **Clone**
```bash
git clone https://github.com/BidumanADT/BellwoodApp.git
cd BellwoodApp/BellwoodGlobal.Mobile
```

2) **Restore**
```bash
dotnet restore
```

3) **Configure API endpoints** in `MauiProgram.cs` and service options to point to your AuthServer/RidesAPI instances (Android uses `10.0.2.2` loopback).

4) **Build**
```bash
dotnet build           # all targets
dotnet build -f net9.0-android
dotnet build -f net9.0-ios
dotnet build -f net9.0-windows10.0.19041.0
dotnet build -f net9.0-maccatalyst
```

5) **Run**
- Visual Studio: open `BellwoodGlobal.Mobile.sln`, pick target platform/emulator, and start debugging.
- CLI examples:
```bash
dotnet build -t:Run -f net9.0-android
dotnet build -t:Run -f net9.0-ios
dotnet build -t:Run -f net9.0-windows10.0.19041.0
```

## Testing

```bash
dotnet test
dotnet test --collect:"XPlat Code Coverage"
```

## Platform Notes

- **Android:** Min SDK 21, Target SDK 35; requires Location/Internet/Network State permissions.
- **iOS:** Min 11.0, Target 18.0; ensure Info.plist includes location usage strings.
- **Windows:** Min build 17763.
- **macOS:** Min 13.1 (Ventura).

## Deployment

```bash
dotnet publish -f net9.0-android -c Release
dotnet publish -f net9.0-ios -c Release
dotnet publish -f net9.0-windows10.0.19041.0 -c Release
```

## Branches

- **main:** Stable production code  
- **feature/driver-tracking:** Passenger-safe tracking implementation and docs  
- **develop:** Integration branch for features

## Security & Standards

- JWT-based authentication with `AuthHttpHandler` attaching tokens to outbound requests.
- HTTPS for all service calls; dev builds allow local certificates.
- Follow C# naming conventions, async/await for I/O, DI-first architecture, MVVM separation, and nullable reference types enabled.

## Support

For issues or questions, please use the GitHub issue tracker.

---

**Built with care using .NET MAUI**

*© 2024 Bellwood Global, Inc. All rights reserved.*
