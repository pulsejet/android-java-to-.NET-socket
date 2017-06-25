adb install -r bin/client-debug.apk
adb shell monkey -p com.radial.client -c android.intent.category.LAUNCHER 1