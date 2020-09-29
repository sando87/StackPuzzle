using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

class LOG
{
    public enum LogLevel { NONE, INFO, WARN, ERROR };
    static public Action<string, LogLevel> LogWriter;
    //디버깅용: 코드 흐름 추적을 위한 정보를 콘솔창에 남긴다.
    static public void trace(
        [CallerFilePath] string file = null,
        [CallerMemberName] string caller = null,
        [CallerLineNumber] int lineNumber = 0 )
    {
        string msg = toString(file, caller, lineNumber) + "[trace]";
        LogWriter?.Invoke(msg, LogLevel.INFO);
    }

    //디버깅용: 간단한 정보와 함께 콘솔창에 남긴다.
    static public void echo<T>(T val,
        [CallerFilePath] string file = null,
        [CallerMemberName] string caller = null,
        [CallerLineNumber] int lineNumber = 0)
    {
        string msg = toString(file, caller, lineNumber) + "[echo][" + val + "]";
        LogWriter?.Invoke(msg, LogLevel.INFO);
    }

    //경고 메세지박스를 띄어주어 개발자에게 강하게 알린다.
    static public void warn(string val = null,
        [CallerFilePath] string file = null,
        [CallerMemberName] string caller = null,
        [CallerLineNumber] int lineNumber = 0)
    {
        var st = new StackTrace();
        string msg = "";
        if(val != null)
        {
            msg = toString(file, caller, lineNumber) + "[warn][" + val + "]";
        }
        else
        {
            foreach (var frame in st.GetFrames())
            {
                msg += frame.GetMethod().ToString();
                msg += "\n";
            }
        }
        
        LogWriter?.Invoke(msg, LogLevel.WARN);
    }

    //에러 메세지를 띄어주고 프로그램을 종료시킨다.(치명적 케이스인 경우 사용)
    static public void error(string val = null,
        [CallerFilePath] string file = null,
        [CallerMemberName] string caller = null,
        [CallerLineNumber] int lineNumber = 0)
    {
        var st = new StackTrace();
        string msg = "";
        if (val != null)
        {
            msg = toString(file, caller, lineNumber) + "[error][" + val + "]";
        }
        else
        {
            foreach (var frame in st.GetFrames())
            {
                msg += frame.GetMethod().ToString();
                msg += "\n";
            }
        }
        
        LogWriter?.Invoke(msg, LogLevel.ERROR);
    }
    static private string toString(string file, string caller, int lineNumber)
    {
        string filename = file.Split('\\').Last();
        string msg = "[" + filename + "][" + caller + "][" + lineNumber + "]";
        return msg;
    }

}
