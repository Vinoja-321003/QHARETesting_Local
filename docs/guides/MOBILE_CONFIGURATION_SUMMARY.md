# Mobile Configuration and Auto-Start Appium Summary

## ✅ Changes Completed

### 1. Updated Configuration Structure

**File**: `src/QualiFrame.Tests/Configuration/appsettings.json`

The mobile configuration has been updated to match the requested structure:

```json
"Mobile": {
  "AppiumServerUrl": "http://localhost:4723",
  "PlatformName": "Android",  // or "iOS"
  "DeviceName": "Android Emulator",  // or "iPhone Simulator"
  "AutomationName": "UiAutomator2",  // or "XCUITest" for iOS
  "AppPackage": "",  // Optional: path to your app
  "AutoStartAppium": true,  // Enable/disable automatic Appium server startup
  "AppiumStartTimeout": 30  // Timeout in seconds for Appium server to start
}
```

### 2. Updated TestConfiguration Class

**File**: `src/QualiFrame.Tests/Configuration/TestConfiguration.cs`

Added new properties to read from the updated configuration structure:
- `AppiumServerUrl` - Reads from `Mobile:AppiumServerUrl`
- `PlatformName` - Reads from `Mobile:PlatformName`
- `DeviceName` - Reads from `Mobile:DeviceName`
- `AutomationName` - Reads from `Mobile:AutomationName`
- `AppPackage` - Reads from `Mobile:AppPackage`
- `AutoStartAppium` - Reads from `Mobile:AutoStartAppium` (default: true)
- `AppiumStartTimeout` - Reads from `Mobile:AppiumStartTimeout` (default: 30 seconds)

### 3. Created AppiumServerManager

**File**: `src/QualiFrame.Tests/Utilities/Mobile/AppiumServerManager.cs`

A new utility class that manages Appium server lifecycle:

**Features**:
- `IsAppiumServerRunning(serverUrl)` - Checks if Appium server is already running
- `StartAppiumServerIfNeeded()` - Automatically starts Appium server if:
  - `AutoStartAppium` is enabled in configuration
  - Server URL is localhost (127.0.0.1, localhost, ::1, or 0.0.0.0)
  - Server is not already running
- `StopAppiumServer()` - Stops the Appium server if it was started by the manager

**How it works**:
1. Checks if Appium server is already running at the configured URL
2. If not running and auto-start is enabled, starts Appium using `appium --port <port>`
3. Waits for the server to become ready (configurable timeout)
4. Tracks the process so it can be stopped later

### 4. Integrated Auto-Start in Hooks

**File**: `src/QualiFrame.Tests/StepDefinitions/Hooks.cs`

**BeforeTestRun Hook**:
- Scans feature files for `@Mobile` tags
- If mobile features are detected, calls `AppiumServerManager.StartAppiumServerIfNeeded()`
- Logs the process and provides clear console output

**AfterTestRun Hook**:
- Calls `AppiumServerManager.StopAppiumServer()` to clean up
- Only stops the server if it was started by the framework

## 🚀 How It Works

### Automatic Appium Server Management

1. **Before Test Execution**:
   - Framework scans for mobile feature files (files containing `@Mobile` tag)
   - If mobile tests are detected:
     - Checks if `AutoStartAppium` is enabled
     - Checks if `AppiumServerUrl` points to localhost
     - Checks if Appium server is already running
     - If conditions are met, starts Appium server automatically
     - Waits for server to be ready (default: 30 seconds)

2. **During Test Execution**:
   - Mobile tests connect to the Appium server as normal
   - If server was already running, it continues to run
   - If server was started by framework, it's used for tests

3. **After Test Execution**:
   - Framework stops the Appium server if it was started automatically
   - If server was already running, it's left running

## 📋 Configuration Options

### Enable/Disable Auto-Start

Set `AutoStartAppium` to `false` in `appsettings.json`:
```json
"Mobile": {
  "AutoStartAppium": false
}
```

### Change Appium Server Port

Update `AppiumServerUrl`:
```json
"Mobile": {
  "AppiumServerUrl": "http://localhost:4724"
}
```

### Adjust Startup Timeout

Change `AppiumStartTimeout` (in seconds):
```json
"Mobile": {
  "AppiumStartTimeout": 60
}
```

### Use Remote Appium Server

If using a remote Appium server (not localhost), auto-start is automatically disabled:
```json
"Mobile": {
  "AppiumServerUrl": "http://remote-server:4723"
}
```

## 🔍 Detection Logic

The framework detects mobile tests by:
1. Scanning all `.feature` files in `src/QualiFrame.Tests/Features/`
2. Checking if any file contains `@Mobile` tag (case-insensitive)
3. If found, Appium server management is activated

## 📝 Example Configuration

### For Android Testing:
```json
"Mobile": {
  "AppiumServerUrl": "http://localhost:4723",
  "PlatformName": "Android",
  "DeviceName": "Android Emulator",
  "AutomationName": "UiAutomator2",
  "AppPackage": "com.example.app",
  "AutoStartAppium": true,
  "AppiumStartTimeout": 30
}
```

### For iOS Testing:
```json
"Mobile": {
  "AppiumServerUrl": "http://localhost:4723",
  "PlatformName": "iOS",
  "DeviceName": "iPhone Simulator",
  "AutomationName": "XCUITest",
  "AppPackage": "path/to/your/app.app",
  "AutoStartAppium": true,
  "AppiumStartTimeout": 30
}
```

## ⚠️ Important Notes

1. **Appium Must Be Installed**: The framework expects `appium` command to be available in your PATH
   - Install with: `npm install -g appium`
   - Verify with: `appium --version`

2. **Drivers Must Be Installed**: Required drivers must be installed:
   - Android: `appium driver install uiautomator2`
   - iOS: `appium driver install xcuitest`

3. **Port Availability**: Ensure the configured port (default: 4723) is available
   - If port is in use, either:
     - Stop the existing Appium server
     - Change the port in configuration
     - Set `AutoStartAppium: false` and start manually

4. **Manual Start Still Works**: You can still start Appium manually:
   ```bash
   appium
   ```
   The framework will detect it's running and skip auto-start.

## 🐛 Troubleshooting

### Appium Server Not Starting

**Check**:
1. Is `appium` in your PATH? Run: `appium --version`
2. Is the port available? Check: `netstat -an | findstr :4723`
3. Are there any errors in the console output?

**Solution**: Check the console output for detailed error messages. The framework logs all steps.

### Appium Server Takes Too Long to Start

**Solution**: Increase `AppiumStartTimeout`:
```json
"Mobile": {
  "AppiumStartTimeout": 60
}
```

### Auto-Start Not Working

**Check**:
1. Is `AutoStartAppium` set to `true`?
2. Is `AppiumServerUrl` pointing to localhost?
3. Are there mobile feature files with `@Mobile` tags?

**Solution**: Verify configuration and check console logs for detection messages.

## ✅ Summary

The framework now:
- ✅ Automatically detects mobile tests
- ✅ Automatically starts Appium server for local execution
- ✅ Automatically stops Appium server after tests complete
- ✅ Respects existing Appium server instances
- ✅ Provides clear logging and error messages
- ✅ Supports both Android and iOS configurations

You can now run mobile tests without manually starting Appium server first!

