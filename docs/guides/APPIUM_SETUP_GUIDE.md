# Appium Setup Guide for Windows

This guide will help you set up and run Appium server on your local machine.

## ✅ Installation Status

- **Node.js**: v22.21.0 ✅
- **npm**: 10.9.4 ✅
- **Appium**: 3.1.2 ✅
- **UiAutomator2 Driver** (Android): 6.7.8 ✅
- **XCUITest Driver** (iOS): 10.13.3 ✅

## 🚀 Starting Appium Server

### Option 1: Start Appium Server (Default Port 4723)

Open a new terminal/PowerShell window and run:

```bash
appium
```

This will start Appium server on `http://localhost:4723` (default port).

### Option 2: Start Appium Server with Custom Port

If you need to use a different port:

```bash
appium --port 4723
```

### Option 3: Start Appium Server with Logging

For detailed logs:

```bash
appium --log-level debug
```

### Option 4: Start Appium Server in Background (Windows)

To run Appium in the background, you can use PowerShell:

```powershell
Start-Process -NoNewWindow appium
```

Or create a batch file to start it easily.

## 🔍 Verifying Appium Server is Running

### Method 1: Check Server Status via Browser

Open your browser and navigate to:
```
http://localhost:4723/status
```

You should see a JSON response like:
```json
{
  "value": {
    "ready": true,
    "message": "The Appium server is ready to accept new sessions",
    "build": {
      "version": "3.1.2"
    }
  }
}
```

### Method 2: Check Server Status via Command Line

In a new terminal, run:

```bash
curl http://localhost:4723/status
```

Or using PowerShell:
```powershell
Invoke-WebRequest -Uri http://localhost:4723/status -UseBasicParsing
```

### Method 3: Check if Port is Listening

```powershell
netstat -an | findstr :4723
```

You should see something like:
```
TCP    0.0.0.0:4723           0.0.0.0:0              LISTENING
```

## 📋 Appium Configuration

### Default Configuration

Appium server runs with these defaults:
- **Host**: `0.0.0.0` (listens on all interfaces)
- **Port**: `4723`
- **Base Path**: `/`
- **Log Level**: `info`

### Configuration File (Optional)

You can create an `appium.config.json` file in your project root:

```json
{
  "server": {
    "port": 4723,
    "host": "0.0.0.0",
    "base-path": "/",
    "log-level": "info"
  }
}
```

Then start Appium with:
```bash
appium --config appium.config.json
```

## 🧪 Testing Appium Connection

### Using Your Test Framework

Your test framework is already configured to connect to `http://localhost:4723`. Once Appium is running, your mobile tests should be able to connect.

### Manual Test with curl

```powershell
# Check server status
Invoke-WebRequest -Uri http://localhost:4723/status -UseBasicParsing

# List available sessions (should be empty initially)
Invoke-WebRequest -Uri http://localhost:4723/sessions -UseBasicParsing
```

## 📱 Platform-Specific Setup

### For Android Testing

1. **Install Android SDK** (if not already installed)
   - Download Android Studio
   - Set `ANDROID_HOME` environment variable
   - Add `%ANDROID_HOME%\platform-tools` to PATH

2. **Verify ADB is working**:
   ```bash
   adb devices
   ```

3. **Connect Android device or start emulator**

### For iOS Testing (macOS only)

1. **Install Xcode** (macOS only)
2. **Install Xcode Command Line Tools**:
   ```bash
   xcode-select --install
   ```
3. **Install ios-deploy** (optional):
   ```bash
   npm install -g ios-deploy
   ```

**Note**: iOS testing requires macOS. If you're on Windows, you can only test Android.

## 🛠️ Troubleshooting

### Issue: "Port 4723 is already in use"

**Solution**: 
1. Find the process using port 4723:
   ```powershell
   netstat -ano | findstr :4723
   ```
2. Kill the process (replace PID with the actual process ID):
   ```powershell
   taskkill /PID <PID> /F
   ```
3. Or use a different port:
   ```bash
   appium --port 4724
   ```
   Then update your `appsettings.json`:
   ```json
   "Mobile": {
     "AppiumServerUrl": "http://localhost:4724"
   }
   ```

### Issue: "Cannot connect to Appium server"

**Solutions**:
1. Verify Appium is running: `appium --version`
2. Check if port 4723 is listening: `netstat -an | findstr :4723`
3. Check firewall settings
4. Try accessing `http://localhost:4723/status` in browser

### Issue: "Driver not found"

**Solution**: Install the required driver:
```bash
appium driver install uiautomator2  # For Android
appium driver install xcuitest      # For iOS
```

### Issue: "Android SDK not found"

**Solution**: 
1. Install Android Studio
2. Set `ANDROID_HOME` environment variable:
   ```powershell
   [System.Environment]::SetEnvironmentVariable('ANDROID_HOME', 'C:\Users\<YourUser>\AppData\Local\Android\Sdk', 'User')
   ```
3. Add to PATH:
   ```powershell
   $env:Path += ";$env:ANDROID_HOME\platform-tools;$env:ANDROID_HOME\tools"
   ```

## 📝 Quick Start Commands

```bash
# Start Appium server
appium

# Start with debug logging
appium --log-level debug

# Start on custom port
appium --port 4724

# Check installed drivers
appium driver list

# Install a driver
appium driver install uiautomator2

# Check Appium version
appium --version
```

## 🔗 Useful Links

- [Appium Documentation](https://appium.io/docs/en/latest/)
- [Appium GitHub](https://github.com/appium/appium)
- [UiAutomator2 Driver](https://github.com/appium/appium-uiautomator2-driver)
- [XCUITest Driver](https://github.com/appium/appium-xcuitest-driver)

## ✅ Next Steps

1. **Start Appium server**:
   ```bash
   appium
   ```

2. **Verify it's running**:
   - Open browser: `http://localhost:4723/status`
   - Or run: `curl http://localhost:4723/status`

3. **Run your mobile tests** - Your framework is already configured to use `http://localhost:4723`

4. **Monitor Appium logs** - The terminal where Appium is running will show all session activity

## 💡 Tips

- Keep the Appium server terminal window open while running tests
- Use `--log-level debug` when troubleshooting connection issues
- Check Appium logs for detailed error messages
- For production, consider running Appium as a Windows service

