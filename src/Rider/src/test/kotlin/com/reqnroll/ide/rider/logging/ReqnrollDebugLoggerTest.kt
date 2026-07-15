package com.reqnroll.ide.rider.logging

import java.io.File
import kotlin.test.Test
import kotlin.test.assertEquals

class ReqnrollDebugLoggerTest {
    @Test
    fun `logDirectory uses LOCALAPPDATA on Windows`() {
        assertEquals(
            File("C:\\Users\\me\\AppData\\Local", "Reqnroll"),
            ReqnrollDebugLogger.logDirectory("Windows 11", "C:\\Users\\me\\AppData\\Local", "C:\\Users\\me"),
        )
    }

    @Test
    fun `logDirectory falls back to home when LOCALAPPDATA is unset on Windows`() {
        assertEquals(
            File("C:\\Users\\me", "Reqnroll"),
            ReqnrollDebugLogger.logDirectory("Windows 11", null, "C:\\Users\\me"),
        )
    }

    @Test
    fun `logDirectory uses Library-Logs on macOS`() {
        assertEquals(
            File("/Users/me", "Library/Logs/Reqnroll"),
            ReqnrollDebugLogger.logDirectory("Mac OS X", null, "/Users/me"),
        )
    }

    @Test
    fun `logDirectory falls back to XDG-style local-share for anything else`() {
        assertEquals(
            File("/home/me", ".local/share/Reqnroll"),
            ReqnrollDebugLogger.logDirectory("Linux", null, "/home/me"),
        )
    }

    @Test
    fun `logDirectory os detection is case-insensitive`() {
        assertEquals(
            File("C:\\Users\\me", "Reqnroll"),
            ReqnrollDebugLogger.logDirectory("WINDOWS 10", null, "C:\\Users\\me"),
        )
    }
}
