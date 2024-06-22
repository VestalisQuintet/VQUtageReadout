using UnityEngine;
using Microsoft.Extensions.Logging;
using System;

namespace VestalisQuintet.VQUtageReadout
{
    public class UnityLogger : Microsoft.Extensions.Logging.ILogger
    {
        public IDisposable BeginScope<TState>(TState state)
        {
            return null; // スコープは使用しない
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            // ここで必要に応じてログレベルをフィルタリングします。
            // 例えば、特定のレベル以上のログのみを許可することができます。
            return logLevel != LogLevel.None;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            // ログメッセージをフォーマットします。
            var message = formatter.Invoke(state, exception);

            // ログレベルに基づいて、Unityのデバッグメソッドを呼び出します。
            switch (logLevel)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
                    Debug.Log(message);
                    break;
                case LogLevel.Information:
                    Debug.Log(message);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(message);
                    break;
                case LogLevel.Error:
                    Debug.LogError(message);
                    break;
                case LogLevel.Critical:
                    Debug.LogError(message);
                    break;
                case LogLevel.None:
                    break;
            }
        }
    }
}
