# Bellwood Elite Mobile App

![.NET MAUI](https://img.shields.io/badge/.NET%20MAUI-9.0-512BD4?style=flat-square&logo=.net)
![Platform](https://img.shields.io/badge/platform-iOS%20%7C%20Android%20%7C%20Windows%20%7C%20macOS-lightgrey?style=flat-square)
![License](https://img.shields.io/badge/license-Proprietary-red?style=flat-square)

A premium cross-platform mobile application for Bellwood Global, providing seamless ride booking, quote management, and transportation services.

## ?? Overview

Bellwood Elite is a sophisticated .NET MAUI mobile application that enables users to:
- Book premium transportation services
- Request and manage ride quotes
- Track ride history and booking details
- Manage payment methods
- View real-time driver tracking
- Access personalized user profiles

## ??? Architecture

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

## ?? Features

### Core Functionality
- ? **User Authentication** - Secure login and authorization
- ? **Ride Booking** - Quick and easy ride reservation
- ? **Quote Management** - Request, view, and manage ride quotes
- ? **Booking History** - Complete ride history tracking
- ? **Driver Tracking** - Real-time driver location (feature branch)
- ? **Payment Integration** - Secure payment method management
- ? **Location Services** - Map integration and location picking
- ? **Flight Information** - Airport pickup with flight tracking

### User Experience
- ?? Custom branding with Bellwood Elite theme
- ?? Professional UI with gradient backgrounds
- ?? Responsive design across all platforms
- ? Fast and fluid animations
- ?? Status-based color coding for bookings

## ?? Project Structure

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

## ??? Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (17.8+) with MAUI workload
  - OR [Visual Studio Code](https://code.visualstudio.com/) with C# Dev Kit
- Platform-specific requirements:
  - **iOS:** macOS with Xcode 15+
  - **Android:** Android SDK API 21-35
  - **Windows:** Windows 10 Build 17763+
  - **macOS:** macOS 13.1+

## ?? Dependencies

### Main Application (BellwoodGlobal.Mobile)
```xml
<PackageReference Include="Microsoft.Maui.Controls" />
<PackageReference Include="Microsoft.Maui.Controls.Compatibility" />
<PackageReference Include="Microsoft.Extensions.Http" Version="8.0.1" />
<PackageReference Include="Microsoft.Extensions.Logging.Debug" Version="8.0.1" />
```

### Core Library (BellwoodGlobal.Core)
- .NET 8.0 Standard Library

## ?? Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/BidumanADT/BellwoodApp.git
cd BellwoodApp/BellwoodGlobal.Mobile
```

### 2. Restore Dependencies

```bash
dotnet restore
```

### 3. Configure API Endpoint

Update the API base URL in your service configuration files to point to your backend API instance.

### 4. Build the Solution

```bash
# Build for all platforms
dotnet build

# Build for specific platform
dotnet build -f net9.0-android
dotnet build -f net9.0-ios
dotnet build -f net9.0-windows10.0.19041.0
dotnet build -f net9.0-maccatalyst
```

### 5. Run the Application

#### Using Visual Studio
1. Open `BellwoodGlobal.Mobile.sln`
2. Select your target platform (Android, iOS, Windows, or macOS)
3. Choose your device/emulator
4. Press F5 or click Run

#### Using .NET CLI
```bash
# Android
dotnet build -t:Run -f net9.0-android

# iOS (requires macOS)
dotnet build -t:Run -f net9.0-ios

# Windows
dotnet build -t:Run -f net9.0-windows10.0.19041.0
```

## ?? Branding & Design

### Color Scheme
- **Primary:** `#1C2D5C` (Deep Navy Blue)
- **Accent:** Gold/Premium tones
- **Background:** Custom gradients defined in `Gradients.xaml`

### Fonts
- **Primary:** Montserrat (Regular, SemiBold)
- **Display:** Playfair Display (SemiBold)

### Icons & Splash
- App icon: 1200x1200 PNG with adaptive background
- Splash screen: Centered Bellwood Elite logo on navy background

## ?? Configuration

### Development Environment Setup

1. **Enable Developer Mode** (Windows)
   ```powershell
   # Run as Administrator
   Set-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock" -Name "AllowDevelopmentWithoutDevLicense" -Value 1
   ```

2. **Android Emulator Setup**
   - Install Android SDK through Visual Studio Installer
   - Create AVD with API 21+ through Android Device Manager

3. **iOS Simulator** (macOS only)
   - Install Xcode from App Store
   - Open Xcode and accept license agreements

## ?? Testing

```bash
# Run unit tests
dotnet test

# Run with code coverage
dotnet test --collect:"XPlat Code Coverage"
```

## ?? Platform-Specific Notes

### Android
- **Min SDK:** API 21 (Android 5.0 Lollipop)
- **Target SDK:** API 35 (Android 15)
- **Permissions:** Location, Internet, Network State

### iOS
- **Min Version:** iOS 11.0
- **Target Version:** iOS 18.0
- **Info.plist:** Configure location usage descriptions

### Windows
- **Min Version:** Windows 10 Build 17763
- **Package Identity:** Required for Store deployment

### macOS
- **Min Version:** macOS 13.1 (Ventura)
- **Entitlements:** Configure as needed

## ?? Deployment

### Android (Google Play Store)
```bash
dotnet publish -f net9.0-android -c Release
```

### iOS (App Store)
```bash
dotnet publish -f net9.0-ios -c Release
```

### Windows (Microsoft Store)
```bash
dotnet publish -f net9.0-windows10.0.19041.0 -c Release
```

## ?? Git Branches

- **main:** Stable production code
- **feature/driver-tracking:** Real-time driver tracking implementation
- **develop:** Integration branch for features

## ?? Development Team

**Repository:** [BidumanADT/BellwoodApp](https://github.com/BidumanADT/BellwoodApp)

## ?? API Integration

The mobile app integrates with the **RidesApi** backend service for:
- User authentication and authorization
- Ride booking and management
- Quote generation and tracking
- Payment processing
- User profile management

**Backend Repository:** [BidumanADT/RidesApi](https://github.com/BidumanADT/RidesApi)

## ?? Security

- ? JWT-based authentication
- ? Secure HTTP communication
- ? AuthHttpHandler for token management
- ? Encrypted credential storage
- ?? Never commit sensitive credentials or API keys

## ?? Code Style & Standards

- **Naming:** Follow C# naming conventions
- **Async/Await:** Use for all I/O operations
- **Dependency Injection:** Leverage MAUI DI container
- **MVVM Pattern:** Separation of concerns
- **Null Safety:** Enabled (`<Nullable>enable</Nullable>`)

## ?? Troubleshooting

### Common Issues

**Build Errors:**
```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build
```

**Android Deployment Issues:**
- Check Android SDK installation
- Verify emulator is running
- Enable USB debugging on physical device

**iOS Code Signing:**
- Configure provisioning profiles in Xcode
- Verify Apple Developer account settings

**Windows Packaging:**
- Ensure package identity is properly configured
- Check Windows SDK version compatibility

## ?? Additional Resources

- [.NET MAUI Documentation](https://docs.microsoft.com/dotnet/maui/)
- [.NET MAUI GitHub](https://github.com/dotnet/maui)
- [MAUI Community Toolkit](https://github.com/CommunityToolkit/Maui)
- [Microsoft Learn - MAUI](https://learn.microsoft.com/training/paths/build-apps-with-dotnet-maui/)

## ?? Project Statistics

- **Target Frameworks:** 4 (iOS, Android, Windows, macOS)
- **Pages:** 10+ XAML pages
- **Services:** 10+ business services
- **Models:** 15+ domain models
- **.NET Version:** 9.0 (Mobile), 8.0 (Core)

## ?? Version History

- **v1.0.0** - Initial release
  - User authentication
  - Ride booking
  - Quote management
  - Payment integration
  - Ride history

## ?? Support

For issues, questions, or contributions, please refer to the GitHub repository issue tracker.

---

**Built with ?? using .NET MAUI**

*© 2024 Bellwood Global, Inc. All rights reserved.*
