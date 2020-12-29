#region

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Rocket.Core.Logging;

#endregion

namespace RG.UnturnedExceptionsDiscordHook
{
    public static class Utils
    {
        public static Task TaskRun(this Action action, CancellationToken token = default)
        {
            return Task.Run(() => action.WrapTryCatchAction(), token);
        }

        public static void WrapTryCatchAction(this Action task, bool throwInsteadOfLog = false)
        {
            try
            {
                task.Invoke();
            }
            catch (Exception ex)
            {
                if (throwInsteadOfLog)
                    throw;

                ex.LogInternalException();
            }
        }

        public static async Task WrapTryCatchAction(this Func<Task> task, bool throwInsteadOfLog = false)
        {
            try
            {
                await task.Invoke(); //wait needed to catch exception
            }
            catch (Exception ex)
            {
                if (throwInsteadOfLog)
                    throw;

                ex.LogInternalException();
            }
        }

        public static void LogInternalException(this Exception ex)
        {
            /*if (ex is TaskCanceledException)
                 return;*/

            var exMsg = new StringBuilder("Fail to send exception to discord! Cause:");
            do
            {
                exMsg.Append($"{Environment.NewLine}{ex.Message}{Environment.NewLine}{ex.StackTrace}");
                ex = ex.InnerException;
            } while (ex != null);

            Logger.Log(exMsg.ToString());
        }
    }
}