# Maalouf Auto Parts Mobile

React Native mobile client for Android and iOS, built with Expo.

## Local setup

```bash
cd src/SpareParts.Mobile.ReactNative
npm install
copy .env.example .env
npx expo start
```

Set `EXPO_PUBLIC_API_BASE_URL` in `.env` to an API URL the device can reach.

- Android emulator: `http://10.0.2.2:5000`
- iOS simulator on macOS: `http://localhost:5000`
- Physical Android/iPhone: use the computer LAN IP, for example `http://192.168.1.20:5000`

The mobile app mirrors the shared admin screen catalog used by the WPF and web apps. The bottom tab bar keeps the primary workspaces close at hand, while the side menu exposes the full operations, finance, tools, platform-admin, and extra-marketplace screens from the same spec set.

## Social login

Google and Facebook login use Expo AuthSession, then send the provider token to the API's `/api/auth/external-login` endpoint.

Add the mobile client IDs to `.env`:

```bash
EXPO_PUBLIC_GOOGLE_CLIENT_ID=
EXPO_PUBLIC_GOOGLE_ANDROID_CLIENT_ID=
EXPO_PUBLIC_GOOGLE_IOS_CLIENT_ID=
EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID=
EXPO_PUBLIC_FACEBOOK_APP_ID=
```

The API also needs matching `ExternalAuth` settings in `appsettings.json` or environment variables. `ExternalAuth:GoogleClientId` must match the Google client ID that produced the id token, and Facebook requires `ExternalAuth:FacebookAppId` plus `ExternalAuth:FacebookAppSecret`.

Run the backend first:

```bash
dotnet run --project ../../src/SpareParts.Api/SpareParts.Api.csproj
```

## Run without Expo Go

Use a development build when Expo Go is unstable or too slow on the phone:

```bash
npm run build:android:dev
npm run start:dev-client
```

Install the APK from the EAS build link on the phone, then open the installed Maalouf Auto Parts app and connect it to the Metro server shown by `npm run start:dev-client`.

For a non-development internal APK that opens directly without Metro:

```bash
npm run build:android:preview
```

## Store builds

Expo EAS can build Android and iOS binaries:

```bash
npx eas build --platform android
npx eas build --platform ios
```

Android Play Store requires a Google Play Developer account. iOS App Store/TestFlight requires Apple Developer Program membership.
