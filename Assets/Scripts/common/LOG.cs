using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class LOG
{
    static public Func<bool> IsNetworkAlive;
    static public Action<string> LogWriterConsole;
    static public Func<string, bool> LogStringWriterDB;
    static public Func<byte[], bool> LogBytesWriterDB;

    static private Thread mThread = null;
    static private bool mRunFlag = false;
    static private string mFileLogPath = null;
    static private ConcurrentQueue<string> mQueue = new ConcurrentQueue<string>();

    static public void Initialize(string logPath)
    {
        if (mThread != null)
            return;

        if (IsNetworkAlive == null)
            IsNetworkAlive = () => { return false; };

        if (LogStringWriterDB == null)
            LogStringWriterDB = (data) => { return false; };

        if (LogBytesWriterDB == null)
            LogBytesWriterDB = (data) => { return false; };

        mFileLogPath = logPath + "/Log/";
        DirectoryInfo di = new DirectoryInfo(mFileLogPath);
        if (di.Exists == false)
            di.Create();

        //mRunFlag = true;
        //mThread = new Thread(new ThreadStart(Run));
        //mThread.Start();
    }
    static public void UnInitialize()
    {
        if(mThread != null)
        {
            mRunFlag = false;
            mThread.Join();
            mThread = null;
        }
    }
    static void Run()
    {
        while(mRunFlag)
        {
            Thread.Sleep(1000);

            ProcessToFlushLog();
        }
    }
    public static void ProcessToFlushLog()
    {
        if (IsNetworkAlive())
            WriteFilesToDB();

        string[] logs = FlushQueue();
        if (IsNetworkAlive())
        {
            if (!WriteLogsToDB(logs))
                WriteLogsToFile(logs);
        }
        else
        {
            WriteLogsToFile(logs);
        }
    }

    static void AddLog(string msg)
    {
        mQueue.Enqueue(msg);
    }
    static void WriteLogsToFile(string[] logs)
    {
        try
        {
            string filename = DateTime.Now.ToString("yyMMdd") + ".txt";
            string path = mFileLogPath + filename;
            using (var stream = new FileStream(path, FileMode.Append))
            {
                byte[] data = LogStringToByte(logs);
                stream.Write(data, 0, data.Length);
            }
        }
        catch
        {
            LogWriterConsole?.Invoke("Failed WriteLogsToFile");
        }
    }
    static bool WriteLogsToDB(string[] logs)
    {
        foreach (string log in logs)
        {
            if (!IsNetworkAlive())
                return false;

            if (!LogStringWriterDB.Invoke(log))
                return false;
        }
        return true;
    }
    static void WriteFilesToDB()
    {
        string[] filePaths = Directory.GetFiles(mFileLogPath);
        foreach(string file in filePaths)
        {
            if (!IsNetworkAlive())
                break;

            byte[] filedata = File.ReadAllBytes(file);
            if(LogBytesWriterDB.Invoke(filedata))
                File.Delete(file);
        }
    }
    static string[] FlushQueue()
    {
        List<string> logs = new List<string>();
        string msg = "";
        while (mQueue.TryDequeue(out msg))
            logs.Add(msg);

        return logs.ToArray();
    }

    public static byte[] LogStringToByte(string[] logs)
    {
        string log = String.Join<string>("\n", logs);
        return Encoding.UTF8.GetBytes(log);
    }
    public static string[] LogFileToString(byte[] bytes)
    {
        string log = Encoding.UTF8.GetString(bytes);
        return log.Split('\n');
    }



    //디버깅용: 코드 흐름 추적을 위한 정보를 콘솔창에 남긴다.
    static public void trace(
        [CallerFilePath] string file = null,
        [CallerMemberName] string caller = null,
        [CallerLineNumber] int lineNumber = 0 )
    {
        LogHeader log = new LogHeader();
        log.time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        log.threadID = Thread.CurrentThread.ManagedThreadId.ToString();
        log.logType = "trace";
        log.fileName = file.Split('\\').Last();
        log.funcName = caller;
        log.lineNumber = lineNumber.ToString();
        log.message = "";
        log.stackTrace = "";
        string msg = log.ToString();
        LogWriterConsole?.Invoke(msg);
        AddLog(msg);
    }

    //디버깅용: 간단한 정보와 함께 콘솔창에 남긴다.
    static public void echo<T>(T val,
        [CallerFilePath] string file = null,
        [CallerMemberName] string caller = null,
        [CallerLineNumber] int lineNumber = 0)
    {
        LogHeader log = new LogHeader();
        log.time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        log.threadID = Thread.CurrentThread.ManagedThreadId.ToString();
        log.logType = "echo";
        log.fileName = file.Split('\\').Last();
        log.funcName = caller;
        log.lineNumber = lineNumber.ToString();
        log.message = val.ToString();
        log.stackTrace = "";
        string msg = log.ToString();
        LogWriterConsole?.Invoke(msg);
        AddLog(msg);
    }

    //경고 메세지박스를 띄어주어 개발자에게 강하게 알린다.
    static public void warn(string val = null,
        [CallerFilePath] string file = null,
        [CallerMemberName] string caller = null,
        [CallerLineNumber] int lineNumber = 0)
    {
        LogHeader log = new LogHeader();
        log.time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        log.threadID = Thread.CurrentThread.ManagedThreadId.ToString();
        log.logType = "warn";
        log.fileName = file.Split('\\').Last();
        log.funcName = caller;
        log.lineNumber = lineNumber.ToString();
        log.message = val == null ? "" : val;
        log.stackTrace = "";

        var st = new StackTrace();
        foreach (var frame in st.GetFrames())
        {
            log.stackTrace += frame.GetMethod().Name;
            log.stackTrace += ">";
        }

        string msg = log.ToString();
        LogWriterConsole?.Invoke(msg);
        AddLog(msg);
    }

    //에러 메세지를 띄어주고 프로그램을 종료시킨다.(치명적 케이스인 경우 사용)
    static public void error(string val = null,
        [CallerFilePath] string file = null,
        [CallerMemberName] string caller = null,
        [CallerLineNumber] int lineNumber = 0)
    {
        LogHeader log = new LogHeader();
        log.time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        log.threadID = Thread.CurrentThread.ManagedThreadId.ToString();
        log.logType = "error";
        log.fileName = file.Split('\\').Last();
        log.funcName = caller;
        log.lineNumber = lineNumber.ToString();
        log.message = val == null ? "" : val;
        log.stackTrace = "";

        var st = new StackTrace();
        foreach (var frame in st.GetFrames())
        {
            log.stackTrace += frame.GetMethod().Name;
            log.stackTrace += ">";
        }

        string msg = log.ToString();
        LogWriterConsole?.Invoke(msg);
        AddLog(msg);
    }

}

class LogHeader
{
    public string time;
    public string threadID;
    public string logType;
    public string fileName;
    public string funcName;
    public string lineNumber;
    public string message;
    public string stackTrace;
    override public string ToString()
    {
        return time + "," + threadID + ",\t" + logType + ",\t" + fileName + ",\t" + funcName + ",\t" + lineNumber + ",\t" + message + ",\t" + stackTrace;
    }
}