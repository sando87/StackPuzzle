using System;
using System.Diagnostics;
using System.Net.NetworkInformation;

public class PerformanceMonitor
{
    PerformanceCounter cpuCounter = null;
    PerformanceCounter memCounter = null;
    PerformanceCounter diskReadCounter = null;
    PerformanceCounter diskWriteCounter = null;

    NetworkInterface[] nics = null;
    long[] prevBytesSent = null;
    long[] prevBytesReceived = null;

    public void Init()
    {
        cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        memCounter = new PerformanceCounter("Memory", "Available MBytes");

        diskReadCounter = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total");
        diskWriteCounter = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total");

        nics = NetworkInterface.GetAllNetworkInterfaces();
        prevBytesSent = new long[nics.Length];
        prevBytesReceived = new long[nics.Length];

        for (int i = 0; i < nics.Length; i++)
        {
            var stats = nics[i].GetIPv4Statistics();
            prevBytesSent[i] = stats.BytesSent;
            prevBytesReceived[i] = stats.BytesReceived;
        }

        // 첫 번째 샘플 무시용 초기화
        cpuCounter.NextValue();
        memCounter.NextValue();
        diskReadCounter.NextValue();
        diskWriteCounter.NextValue();
    }

    public string GetInfo()
    {
        float cpuUsage = cpuCounter.NextValue();
        float availableMem = memCounter.NextValue();
        float diskRead = diskReadCounter.NextValue();
        float diskWrite = diskWriteCounter.NextValue();

        long totalBytesSent = 0;
        long totalBytesReceived = 0;

        for (int i = 0; i < nics.Length; i++)
        {
            var stats = nics[i].GetIPv4Statistics();
            long sentDelta = stats.BytesSent - prevBytesSent[i];
            long recvDelta = stats.BytesReceived - prevBytesReceived[i];
            totalBytesSent += sentDelta;
            totalBytesReceived += recvDelta;

            prevBytesSent[i] = stats.BytesSent;
            prevBytesReceived[i] = stats.BytesReceived;
        }

        string log = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | " +
                        $"CPU: {cpuUsage:F1}% | " +
                        $"Available RAM: {availableMem}MB | " +
                        $"Disk Read: {diskRead / 1024:F1}KB/s | Disk Write: {diskWrite / 1024:F1}KB/s | " +
                        $"Net In: {totalBytesReceived / 1024}KB | Net Out: {totalBytesSent / 1024}KB";

        return log;
    }
}
