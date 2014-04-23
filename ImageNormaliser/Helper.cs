using System;
using System.Threading;
using System.Text.RegularExpressions;

namespace AlexIO
{
    public class Helper
    {
        /// <summary>
        /// The log lock.
        /// </summary>
        private static object _logLock = new  object();

        /// <summary>
        /// The _log.
        /// </summary>
        private static string _logBuffer = "";

        /// <summary>
        /// The _log thread action count.
        /// </summary>
        private static int _logThreadActionCount = 0;

        /// <summary>
        /// Keeps a log
        /// </summary>
        public static string LogBuffer { get { return _logBuffer; } }

        /// <summary>
        /// The randomizer for this class.
        /// </summary>
        public static Random Randomizer = new Random();

        /// <summary>
        /// Returns a new collection of threads
        /// </summary>
        /// <returns>A list containing new threads.</returns>
        /// <param name="max">The number of threads to make.</param>
        /// <param name="whichDo">What these threads do in their Start method.</param>
        public static Thread[] BunchOfNewThreads(int max, ThreadStart whichDo)
        {
            Thread[] retVal = new Thread[max];

            UserIO.Log ("Creating " + max.ToString () + " threads:\n");

            for (int i = -1; i < max; i++) 
            {
                if (i == -1) 
                {
                    Console.Write ("\t| Thread M |");
                } 
                else 
                {
                    Console.ForegroundColor = ThreadColor(i);
                    Thread t = new Thread (whichDo);
                    Console.Write ("\t| Thread " + i.ToString () + " |");
                    t.Name = i.ToString ();
                    retVal [i] = t;
                }
            }

            // New line for separators
            Console.WriteLine ();

            // Separators
            for (int i = -1; i < max; i++) 
            {
                if (i != -1)
                    Console.ForegroundColor = ThreadColor(i);
                else
                    Console.ForegroundColor = ConsoleColor.White;
                Console.Write ("\t|==========|");
            }
            // New line for next line
            Console.WriteLine ("\n");
            Console.ResetColor ();
            return retVal;
        }

        /// <summary>
        /// Determines console color for thread number given
        /// </summary>
        /// <returns>The from thread number.</returns>
        /// <param name="i">The index.</param>
        private static ConsoleColor ThreadColor(int i)
        {
            // Color offset by 1
            const int C_OFF = 1;

            int colNumb = (i % 11) + C_OFF;
            ConsoleColor retVal = (ConsoleColor)colNumb;

            return retVal;
        }

        /// <summary>
        /// Sleeps the current thread by a certain time
        /// </summary>
        /// <param name="secs">Seconds to sleep for.</param>
        public static void DoSleep(float secs)
        {
            string name = (Thread.CurrentThread.Name != "main") ? 
                ThreadNumber (Thread.CurrentThread) : "main";
            Helper.Log ("Sleeping thread " + name + " for "+secs+"s...");

            int sleepyTime = (int)secs * 1000;
            Thread.Sleep (sleepyTime);
        }

        /// <summary>
        /// Sleeps the current thread by a random time (<6 seconds)
        /// </summary>
        public static void DoSleep()
        {
            float secs = (float)Math.Round((Randomizer.NextDouble()*2+1), 1);
            DoSleep (secs);
        }

        /// <summary>
        /// Retrieves a formatted thread number given.
        /// </summary>
        /// <returns>The number.</returns>
        /// <param name="theThread">The thread.</param>
        public static string ThreadNumber (Thread theThread)
        {
            return "[t" + Thread.CurrentThread.Name + "]";
        }

        /// <summary>
        /// Retrieves the current the thread number.
        /// </summary>
        /// <returns>The thread number.</returns>
        public static string CurrentThreadNumber  { get { return ThreadNumber (Thread.CurrentThread);    } }
        public static int  CurrentThreadInteger { get { return Convert.ToInt16 (Thread.CurrentThread.Name); } }

        /// <summary>
        /// Cleans up the test by joining all threads back to main thread.
        /// </summary>
        /// <param name="threadCollection">Thread collection.</param>
        public static void Cleanup(params Thread[][] threadCollection)
        {
            foreach (Thread[] tC in threadCollection)
                foreach (Thread t in tC)
                    t.Join ();
        }

        /// <summary>
        /// Returns a random character
        /// </summary>
        /// <returns>A random char.</returns>
        public static char RandomChar()
        {
            return (char)(Randomizer.Next(26)+64);
        }

        /// <summary>
        /// Log the specified msg to the log
        /// </summary>
        /// <param name="msg">Message.</param>
        public static void Log(string msg)
        {
            _logBuffer += UserIO.Fmt (msg) + "\n";
        }

        /// <summary>
        /// Log the specified msg and consolePrint.
        /// </summary>
        /// <param name="msg">Message.</param>
        /// <param name="consolePrint">If set to <c>true</c>, write to console as well.</param>
        public static void Log(string msg, bool consolePrint)
        {
            Log (msg);
            UserIO.Log (msg);
        }

        /// <summary>
        /// Prints an explicit thread's action with the given message; also logs it too
        /// </summary>
        /// <param name="t">Thread</param>
        /// <param name="msg">Message.</param>
        public static void LogThread(Thread t, string msg)
        {
            lock (_logLock) 
            {
                // Pad message with whitespace
                if (msg.Length < 4) 
                {
                    int spaces = 4 - msg.Length;
                    int padLeft = spaces/2 + msg.Length;
                    msg = msg.PadLeft(padLeft).PadRight(4);
                }

                // Log this action
                Console.Write ("   {0}:", ++_logThreadActionCount);

                // use a regex to scan through name and replace [t\n] with msg
                string msgWithName = "";
                if (t.Name == "main")
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    msgWithName = "\t|  [" + msg + "]  |";
                } 
                else
                {
                    // Front tab
                    int currentThread = Convert.ToInt16 (t.Name);
                    msgWithName = "\t";

                    for (int i = -1; i < currentThread; i++)
                        msgWithName += "\t\t";

                    msgWithName += "|  [" + msg + "]  |";
                    Console.ForegroundColor = ThreadColor(currentThread);
                }

                // Write to console
                Console.WriteLine (msgWithName);
                Console.ResetColor ();
            }
        }

        /// <summary>
        /// Prints a thread's action with the given message
        /// </summary>
        /// <param name="msg">Message.</param>
        public static void LogThread(string msg)
        {
            LogThread (Thread.CurrentThread, msg);
        }

        /// <summary>
        /// Flushes the log of its content
        /// </summary>
        public static void FlushLog()
        {
            _logThreadActionCount = 0;
            _logBuffer = "";
        }
    }
}

