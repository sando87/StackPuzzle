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
    static public Func<string, bool> LogWriterDB;

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

        mFileLogPath = logPath + "/Log/";
        DirectoryInfo di = new DirectoryInfo(mFileLogPath);
        if (di.Exists == false)
            di.Create();

        if(IsNetworkAlive())
            FlushFileToDB();

        mRunFlag = true;
        mThread = new Thread(new ThreadStart(Run));
        mThread.Start();
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
        int counter = 0;
        while(mRunFlag)
        {
            Thread.Sleep(1000);

            FlushQueue();

            if (counter % 60 == 0)
            {
                if (IsNetworkAlive())
                    FlushFileToDB();
            }

            counter++;
        }
    }
    static void AddLog(string msg)
    {
        if(mRunFlag)
            mQueue.Enqueue(msg);
    }
    static void FlushQueue()
    {
        string msg = "";
        bool isNetAlive = IsNetworkAlive() && LogWriterDB != null;
        while (mQueue.TryDequeue(out msg))
        {
            if(isNetAlive)
            {
                if(LogWriterDB.Invoke(msg) == false)
                {
                    isNetAlive = false;
                    FlushMessageToFile(msg);
                }
            }
            else
            {
                FlushMessageToFile(msg);
            }
        }
    }
    static void FlushMessageToFile(string msg)
    {
        try
        {
            string filename = DateTime.Now.ToString("yyMMdd") + ".txt";
            StreamWriter writer = File.AppendText(mFileLogPath + filename);
            writer.WriteLine(msg);
            writer.Close();
        }
        catch
        {
            LogWriterConsole?.Invoke(msg);
        }
    }
    static void FlushFileToDB()
    {
        if (LogWriterDB == null)
            return;

        string[] filePaths = Directory.GetFiles(mFileLogPath);
        foreach(string file in filePaths)
        {
            string[] lines = File.ReadAllLines(file);
            foreach (string line in lines)
            {
                bool success = LogWriterDB.Invoke(line);
                if (!success)
                    return;
            }
            File.Delete(file);
        }
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
        log.fileName = file;
        log.funcName = caller;
        log.lineNumber = lineNumber.ToString();
        log.message = "trace";
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
        log.fileName = file;
        log.funcName = caller;
        log.lineNumber = lineNumber.ToString();
        log.message = "echo : " + val;
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
        log.fileName = file;
        log.funcName = caller;
        log.lineNumber = lineNumber.ToString();
        
        if(val != null)
        {
            log.message = "warn : " + val;
        }
        else
        {
            var st = new StackTrace();
            foreach (var frame in st.GetFrames())
            {
                log.message += frame.GetMethod().ToString();
                log.message += ",";
            }
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
        log.fileName = file;
        log.funcName = caller;
        log.lineNumber = lineNumber.ToString();

        if (val != null)
        {
            log.message = "error : " + val;
        }
        else
        {
            var st = new StackTrace();
            foreach (var frame in st.GetFrames())
            {
                log.message += frame.GetMethod().ToString();
                log.message += ",";
            }
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
    override public string ToString()
    {
        return time + "," + threadID + "," + logType + "," + funcName + "," + lineNumber + "," + message;
    }
}