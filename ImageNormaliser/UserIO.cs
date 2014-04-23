using System;
using System.Collections.Generic;

namespace AlexIO
{
    /// <summary>
    /// Basic Input/Output Class for standard input
    /// </summary>
    /// <remarks>
    /// Written by Alex Cummaudo, 2014-02-25
    /// </remarks>
    public static class UserIO
    {
        /// Program name for welcome message
        private static String PROGRAM_NAME  = "Dodgy Brothers Bank, Inc.";
        /// Program subtitle for welcome message (if null, defaults to my name)
        private static String PROGRAM_SUB = "Concurrent Banking, Since 1984!";

        /// Whether or not prompt from console is active (for thread interrupt)
        private static bool _promptActive = false;
        /// The last prompt message (for thread interrupt)
        private static String _promptMsg = "";


        /// <summary>
        /// Returns a string in the standard log format
        /// </summary>
        /// <param name="msg">The message to log to the console</param>
        /// <returns>The message, properly formatted</returns>
        public static String Fmt(string msg)
        {
            return "    > " + msg;
        }

        /// <summary>
        /// Logs a message to the console in a standard format
        /// </summary>
        /// <param name="msg">The message to log to the console</param>
        public static void Log(string msg)
        {
            Console.WriteLine ((_promptActive ? "\n" : null)+UserIO.Fmt(msg));

            // Reprompt last prompt if it was still active
            if (_promptActive) UserIO.Prompt ();
        }


        /// <summary>
        /// Prints a menu with the specified title and options.
        /// </summary>
        /// <param name="title">Title of the menu.</param>
        /// <param name="options">Options supplied for the menu.</param>
        public static char Menu(string title, Dictionary<char, string> options)
        {
            // Print menu header
            Console.WriteLine("\n      " + title.ToUpper()                  + "\n" +
                "      " + UserIO.StringBuff(title, 0, '=') + "\n");

            // For every menu option, log it to user to see the options
            foreach (KeyValuePair<char, string> opt in options) 
                UserIO.Log ("[" + opt.Key + "] " + opt.Value);

            // Confirm that input captured is a valid menu option
            bool validInput = false;
            char input = ' ';

            while (!validInput)
            {
                input = UserIO.Option();

                // Check that this input was valid
                foreach (KeyValuePair<char, string> opt in options)
                    if (input == opt.Key) validInput = true;

                // If valid input still false, notify so
                if (!validInput) UserIO.Log("That option isn't in a valid menu option. Try again.");
            }

            // Return menu option captured
            return input;

        }


        /// <summary>
        /// Returns user input after logging a prompt message
        /// </summary>
        /// <param name="msg">Message.</param>
        /// <returns>The value read in from the user</returns>
        public static String Prompt(string msg = null)
        {

            // Set prompt msg to this msg if this msg is not null
            if (msg == null)
            {
                msg = _promptMsg;
            }
            // Otherwise update _promptMsg to this msg
            else
            {
                _promptMsg = msg;
            }

            Console.Write ("   >> " + msg + ": ");
            String retVal = null;

            // If not returning from a last prompt, then start reading line
            if (!_promptActive) 
            {
                // Prompt is now active
                _promptActive = true;

                retVal = Console.ReadLine ();

                // Prompt is no longer active
                _promptActive = false;
            }

            return retVal;
        }

        /// <summary>
        /// Returns user input after logging a prompt message for single
        /// menu-based options (e.g. Q for quit)
        /// </summary>
        /// <returns>The char opt in from the user</returns>
        public static char Option()
        {
            char retVal = ' ';
            try
            {
                retVal = UserIO.Prompt ("[?]").ToLower ()[0];
            }
            catch (IndexOutOfRangeException e) 
            {
                return Option ();
            }
            return retVal;
        }

        /// <summary>
        /// Writes a standard welcome message to the console
        /// </summary>
        /// <returns>The welcome message.</returns>
        /// <example>Console.WriteLn(UserIO.WelcomeMessage());</example>
        public static String WelcomeMessage()
        {
            // Replace Subtitle with my name if null
            if (PROGRAM_SUB == null)
                PROGRAM_SUB = "Alex Cummaudo / " + DateTime.Today.Date.ToString ("dd MMM yyyy");

            // Determine box size
            int lineSz = PROGRAM_NAME.Length > PROGRAM_SUB.Length ?
                PROGRAM_NAME.Length+4 : 
                PROGRAM_SUB.Length+4;

            // Return the message string
            return  
                "      +"   +                          UserIO.StringBuff("", lineSz, '-'          ) + "+\n" +
                "      |  " + PROGRAM_NAME.ToUpper() + UserIO.StringBuff(PROGRAM_NAME+"  ", lineSz) + "|\n" +
                "      |  " + PROGRAM_SUB            + UserIO.StringBuff(PROGRAM_SUB+"  " , lineSz) + "|\n" +
                "      +"   +                          UserIO.StringBuff("", lineSz, '-'          ) + "+\n";
        }

        /// <summary>
        /// Returns a buffered string with the specified size
        /// </summary>
        /// <param name="inString">The input string</param>
        /// <param name="inLength">The length of the buffer</param>
        /// <param name="printChar">The character to use for the buffer</param>
        /// <returns>The buffered string</returns>
        private static String StringBuff(String inString, int inLength, char printChar = ' ')
        {
            int charSz = Math.Abs(inLength - inString.Length);
            String resultStr = "";

            for (int i = 0; i < charSz; i++) resultStr += printChar;
            return resultStr;
        }
    }
}

