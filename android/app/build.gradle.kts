plugins {
    id("com.android.application")
    id("org.jetbrains.kotlin.android")
}

android {
    namespace = "com.psdtechnologies.psdplayer"
    compileSdk = 34

    signingConfigs {
        create("release") {
            storeFile = file("debug.keystore")
            storePassword = "android"
            keyAlias = "androiddebugkey"
            keyPassword = "android"
            enableV1Signing = true // CRITICAL: Required for legacy Android JAR signature verification
            enableV2Signing = true // Full APK Signature Scheme v2 for modern Android
        }
    }

    defaultConfig {
        applicationId = "com.psdtechnologies.psdplayer"
        minSdk = 21 // Android 5.0+ compatibility matching GoogleNotes reference profile
        targetSdk = 33 // Stable legacy binder range for Android 5.0 - 6.0 compatibility
        versionCode = 2501
        versionName = "2.5.0-patch1"

        multiDexEnabled = true

        vectorDrawables {
            useSupportLibrary = true
        }

        testInstrumentationRunner = "androidx.test.runner.AndroidJUnitRunner"
    }

    buildTypes {
        release {
            isMinifyEnabled = false
            proguardFiles(
                getDefaultProguardFile("proguard-android-optimize.txt"),
                "proguard-rules.pro"
            )
            signingConfig = signingConfigs.getByName("release")
        }
        debug {
            isDebuggable = true
            signingConfig = signingConfigs.getByName("release")
        }
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    kotlinOptions {
        jvmTarget = "17"
    }

    buildFeatures {
        viewBinding = true
        buildConfig = true
    }

    packaging {
        resources {
            excludes += setOf(
                "META-INF/DEPENDENCIES",
                "META-INF/LICENSE",
                "META-INF/LICENSE.txt",
                "META-INF/license.txt",
                "META-INF/NOTICE",
                "META-INF/NOTICE.txt",
                "META-INF/notice.txt",
                "META-INF/ASL2.0",
                "META-INF/*.kotlin_module",
                "META-INF/*.version",
                "META-INF/version-control-info.textproto",
                "META-INF/com/android/build/gradle/app-metadata.properties"
            )
        }
    }
}

dependencies {
    implementation("androidx.multidex:multidex:2.0.1")
    implementation("androidx.core:core-ktx:1.13.1")
    implementation("androidx.appcompat:appcompat:1.7.0")
    implementation("com.google.android.material:material:1.12.0")
    implementation("androidx.constraintlayout:constraintlayout:2.1.4")
    implementation("androidx.media:media:1.7.0")

    // Media3 Playback & Hardware MediaCodec Engine
    implementation("androidx.media3:media3-exoplayer:1.3.1")
    implementation("androidx.media3:media3-ui:1.3.1")
    implementation("androidx.media3:media3-session:1.3.1")
    implementation("androidx.media3:media3-datasource:1.3.1")
    implementation("androidx.media3:media3-datasource-okhttp:1.3.1")
    implementation("androidx.media3:media3-exoplayer-hls:1.3.1")
    implementation("androidx.media3:media3-exoplayer-rtsp:1.3.1")

    // High-Performance Networking & Coroutines
    implementation("com.squareup.okhttp3:okhttp:4.12.0")
    implementation("org.jetbrains.kotlinx:kotlinx-coroutines-core:1.8.0")
    implementation("org.jetbrains.kotlinx:kotlinx-coroutines-android:1.8.0")
}
