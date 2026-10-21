# IT Help Desk Lite – Android

📱 **Download the ready APK:** [Releases → latest](https://github.com/RehanJamal-IT/it-helpdesk-lite/releases/latest)

Android version of [IT Help Desk Lite](../README.md), a free IT ticket app.
Tabs: **New ticket · My details · Settings · About**, the same as the Windows version.

* Plain Java, no extra libraries, **no permissions** (no internet, no data collected)
* minSdk 24 (Android 7.0), targetSdk 36 (Android 16)

## Build
1. Install **Java 17 or newer** (or Android Studio). If the Android SDK is missing, the script downloads it automatically
2. Double-click **`BUILD_ANDROID.bat`**
   * The first time it downloads Gradle and asks you to choose a **signing key password**
3. The results are in **`dist\`**:
   * `ITHelpDeskLite-1.1.0.apk` to install on phones
   * `ITHelpDeskLite-1.1.0.aab` to upload to Google Play

> Keep `upload-key.jks` and your password safe (back them up on a USB drive). They are never uploaded to GitHub.

© 2026 Rehan Jamal. See [LICENSE](../LICENSE).
