# ProGuard Rules for PSD Media Player Pro (Android Edition)
# Author: Prabhu Kannan (PSD Technologies)

# Keep Media3 and ExoPlayer codec classes
-keep class androidx.media3.** { *; }
-dontwarn androidx.media3.**

# Keep OkHttp & Kotlin reflection
-keepattributes *Annotation*,Signature,InnerClasses,EnclosingMethod
-dontwarn okhttp3.**
-dontwarn okio.**

# Keep Android audio effects
-keep class android.media.audiofx.** { *; }

# Keep native methods and JNI bindings
-keepclasseswithmembernames class * {
    native <methods>;
}

# Keep Parcelable / Serializable models
-keepclassmembers class * implements android.os.Parcelable {
    static ** CREATOR;
}
-keepclassmembers enum * {
    public static **[] values();
    public static ** valueOf(java.lang.String);
}
