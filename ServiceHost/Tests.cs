using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WoW.Services.Host.Tests
{
    public class TestMain
    {
        private bool _verbose = false;
        public TestMain(string acctUri, string characterUri)
        {
            TestData.charUri = characterUri;
            TestData.acctUri = acctUri;
            Console.Clear();
        }

        public void TestSelect()
        {
            ConsoleKey key = ConsoleKey.F17;
            string error = string.Empty;
            while (key != ConsoleKey.Q)
            {
                WriteMainHeader();
                Console.WriteLine("Please select testing method:");
                Console.WriteLine("1. Standard Test");
                Console.WriteLine("2. Stress Test");
                Console.WriteLine("V. Toggle Verbose Mode");
                Console.WriteLine("Q. Quit");
                if (error != string.Empty)
                {
                    Console.Write(error + " Selection (Verbose: " + _verbose.ToString() + "): ");
                    error = string.Empty;
                }
                else
                    Console.Write("Selection (Verbose: " + _verbose.ToString() + "): ");
                key = Console.ReadKey().Key;
                switch (key)
                {
                    case ConsoleKey.D1:
                        StandardTest();
                        Console.Clear();
                        break;
                    case ConsoleKey.D2:
                        StressTest();
                        Console.Clear();
                        break;
                    case ConsoleKey.D3:
                        PullTest();
                        Console.Clear();
                        break;
                    case ConsoleKey.V:
                        _verbose = !_verbose;
                        Console.Clear();
                        break;
                    case ConsoleKey.Q:
                        Console.Clear();
                        break;
                    default:
                        Console.Clear();
                        error = "Wrong selection!";
                        break;
                }

            }
        }

        public void StandardTest()
        {
            #region Description and count getting
            while (Console.KeyAvailable)
                Console.ReadKey(true);
            Console.WriteLine("The standard test runs two sets of tests. The first runs through valid data");
            Console.WriteLine("and tests each method in turn with both valid and invalid data. The second");
            Console.WriteLine("runs invalid data only, attempting to create an account with no password,");
            Console.WriteLine("an account with no name, and finally creating a legitimate account to fail");
            Console.WriteLine("to create characters against.");
            Console.WriteLine();
            int accts = GetNumber("Please enter number of accounts to create per test: ", 1, 200);
            int chars = GetNumber("Please enter number of characters to create per account: ", 1, 12);
            Console.WriteLine("Press Q at any point to cancel the test, or V to toggle verbose mode.");
            Console.WriteLine("Press any key when you're ready...");
            Console.ReadKey();
            #endregion
            WriteResultHeader();
            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;
            Task task = Task.Factory.StartNew(() => {
                RandomName acctName = new RandomName("acct", 1000);
                RandomName charName = new RandomName("chr", 10000);
                int failures = 0;
                int badSuccesses = 0;
                int counter = 0;
                while (counter < accts && !token.IsCancellationRequested)
                {
                    Console.WriteLine("NEXT ACCOUNT:");
                    var acct = new TestData.Account(acctName.GetName());
                    TestResult result = acct.Create(_verbose);
                    Console.WriteLine(result.Output);
                    if (!result.Success)
                        failures++;
                    else
                    {
                        if (result.Success)
                        {
                            WoW.Enums.Faction faction;

                            if (counter % 2 == 0)
                                faction = Enums.Faction.Alliance;
                            else
                                faction = Enums.Faction.Horde;
                            int charcounter = 0;
                            while (charcounter < chars && !token.IsCancellationRequested)
                            {
                                Console.WriteLine("SUCCESSFUL TESTS:");
                                //Create Character
                                var character = new TestData.Character(acct, charName.GetName(), faction, true);
                                result = character.Create(_verbose);
                                Console.WriteLine(result.Output);
                                if (!result.Success)
                                    failures++;
                                else
                                {
                                    #region Success testing
                                    result = character.ChangeLevel(56, _verbose);
                                    Console.WriteLine(result.Output);
                                    if (!result.Success)
                                        failures++;
                                    result = character.Update(true, _verbose);
                                    Console.WriteLine(result.Output);
                                    if (!result.Success)
                                        failures++;
                                    result = character.ChangeName(character.Name + "n", _verbose);
                                    Console.WriteLine(result.Output);
                                    if (!result.Success)
                                        failures++;
                                    result = character.Delete(_verbose);
                                    Console.WriteLine(result.Output);
                                    if (!result.Success)
                                        failures++;
                                    result = character.Update(true, _verbose);
                                    Console.WriteLine(result.Output);
                                    if (result.Success)
                                    {
                                        badSuccesses++;
                                    }
                                    result = character.Restore(_verbose);
                                    Console.WriteLine(result.Output);
                                    if (!result.Success)
                                        failures++;
                                    result = character.Update(true, _verbose);
                                    Console.WriteLine(result.Output);
                                    if (!result.Success)
                                        failures++;
                                    #endregion
                                    #region Failure Testing
                                    Console.WriteLine("EXPECTED FAILURES:");
                                    result = character.Update(false, _verbose);
                                    Console.WriteLine(result.Output);
                                    if (result.Success)
                                    {
                                        badSuccesses++;
                                    }
                                    result = character.ChangeLevel(0, _verbose);
                                    Console.WriteLine(result.Output);
                                    if (result.Success)
                                    {
                                        badSuccesses++;
                                    }
                                    result = character.ChangeLevel(55, _verbose);
                                    Console.WriteLine(result.Output);
                                    if (result.Success)
                                    {
                                        badSuccesses++;
                                    } result = character.ChangeLevel(90, _verbose);
                                    Console.WriteLine(result.Output);
                                    if (result.Success)
                                    {
                                        badSuccesses++;
                                    }
                                    result = character.ChangeName("\"{dude}", _verbose);
                                    Console.WriteLine(result.Output);
                                    if (result.Success)
                                    {
                                        badSuccesses++;
                                    }
                                    if (charcounter > 0)
                                    {
                                        result = character.ChangeName(charName.GetName(true, 1) + "n", _verbose);
                                        Console.WriteLine(result.Output);
                                        if (result.Success)
                                        {
                                            badSuccesses++;
                                        }
                                    }
                                    charcounter++;
                                    #endregion

                                }
                            }
                        }
                    }
                    counter++;
                }
                counter = 0;
                Console.WriteLine("Valid data test completed!");
                Console.WriteLine(accts.ToString() + " accounts created, each with " + chars.ToString() + " characters, for a total of " + ((int)(accts * chars)).ToString() + " requests.");
                Console.WriteLine("Unexpected Failures: " + failures.ToString() + ".");
                Console.WriteLine("Unexpected Successes: " + badSuccesses.ToString() + ".");
                badSuccesses = 0;
                if (!token.IsCancellationRequested)
                {       
                    Console.WriteLine("Press any key (other than Enter) to move on to failure testing...");
                    Console.ReadKey();
                    Console.WriteLine();
                    TestData.Account account;
                    TestResult result;
                    
                    if (!token.IsCancellationRequested)
                    {
                        Console.WriteLine("Creating account with no password...");
                        account = new TestData.Account("failure", string.Empty);
                        result = account.Create(_verbose);
                        Console.WriteLine(result.Output);
                        if (result.Success)
                            badSuccesses++;
                    }
                    if (!token.IsCancellationRequested)
                    {
                        Console.WriteLine("Creating 'good' account: ");
                        account = new TestData.Account(acctName.GetName());
                        result = account.Create(_verbose);
                        if (result.Success && !token.IsCancellationRequested)
                        {
                            Console.WriteLine("Creating character with bad password...");
                            TestData.Character character = new TestData.Character(account, "badChar", Enums.Faction.Alliance, true, string.Empty);
                            result = character.Create(_verbose);
                            Console.WriteLine(result.Output);
                            if (result.Success)
                                badSuccesses++;
                            if (!token.IsCancellationRequested)
                            {
                                Console.WriteLine("Creating character with bad name...");
                                character = new TestData.Character(account, "!\"{}", Enums.Faction.Horde, true);
                                result = character.Create(_verbose);
                                Console.WriteLine(result.Output);
                                if (result.Success)
                                    badSuccesses++;
                            }
                            if (!token.IsCancellationRequested)
                            {
                                Console.WriteLine("Creating character successfully...");
                                character = new TestData.Character(account, charName.GetName(), Enums.Faction.Horde, true);
                                result = character.Create(_verbose);
                                if (result.Success && !token.IsCancellationRequested)
                                {
                                    Console.WriteLine("Attempting to access good character with diff account...");
                                    TestData.Account badacct = new TestData.Account(acctName.GetName());
                                    badacct.Create(false);
                                    character.AccountName = badacct.Name;
                                    result = character.Update(true, _verbose);
                                    Console.WriteLine(result.Output);
                                    if (result.Success)
                                        badSuccesses++;
                                    Console.WriteLine("Creating character with same name...");
                                    character = new TestData.Character(account, charName.GetName(true), Enums.Faction.Horde, true);
                                    result = character.Create(_verbose);
                                    Console.WriteLine(result.Output);
                                    if (result.Success)
                                        badSuccesses++;
                                    
                                }

                            }
                        }
                    }
                }
                
                Console.WriteLine("Press any key to return to main test menu...");
            }, token);
            ConsoleKey key = ConsoleKey.F17;
            do
            {
                key = Console.ReadKey().Key;
                if (task.Status != TaskStatus.RanToCompletion)
                {
                    if (key == ConsoleKey.V)
                        _verbose = !_verbose;
                    else if (key == ConsoleKey.Q)
                    {
                        tokenSource.Cancel();
                        task.Wait();
                    }
                }
                else
                    key = ConsoleKey.Q;
            } while (key != ConsoleKey.Q);

        }


        public void StressTest()
        {
          //Console.WriteLine("1234578901234567890123456789012345678901234567890123456789012345679801234567980");
            Console.WriteLine("This test will generate ten threads, each making forty new accounts and 120 new");
            Console.WriteLine("characters. Press Q at any point to quit this test.");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            List<Task> tasks = new List<Task>();
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;
            RandomName acctName = new RandomName("acct", 1000);
            RandomName charName = new RandomName("chr", 10000);
            int failures = 0;
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(new Task(() => {
                    int counter = 0;
                    while (counter < 40 && !token.IsCancellationRequested)
                    {
                        string acct = string.Empty;
                        lock (acctName)
                        {
                            acct = acctName.GetName();
                        }
                        TestData.Account account = new TestData.Account(acct);
                        TestResult result = account.Create(false);
                        Console.WriteLine(result.Output);
                        if (!result.Success)
                            Interlocked.Increment(ref failures);
                        else
                        {
                            int charcounter = 0;
                            while (charcounter < 3 && !token.IsCancellationRequested)
                            {
                                string charname = string.Empty;
                                lock (charName)
                                {
                                    charname = charName.GetName();
                                }
                                TestData.Character character = new TestData.Character(account, charname, Enums.Faction.Alliance, true);
                                TestResult charResult = character.Create(false);
                                if (!charResult.Success)
                                    Interlocked.Increment(ref failures);
                                Console.WriteLine(charResult.Output);
                                charcounter++;
                            }
                        }
                        counter++;
                    }
                    Console.WriteLine("Thread complete!");
                }, token));
            }
            foreach (Task task in tasks)
                task.Start();
            bool halt = false;
            while(!halt)
            {
                if (!Console.KeyAvailable)
                {
                    Thread.Sleep(200);
                    if (tasks.Count(t => t.IsCompleted == false) == 0)
                    {
                        Console.WriteLine("Stress test complete! Total failed requests: " + failures.ToString());
                        Console.WriteLine("Press any key to continue...");
                        Console.ReadKey();
                        halt = true;
                    }
                }
                else
                {
                    ConsoleKey key = Console.ReadKey().Key;
                    if (key == ConsoleKey.Q)
                    {
                        source.Cancel();
                        while (tasks.Count(t => t.Status == TaskStatus.RanToCompletion) < 10)
                            Thread.Sleep(500);
                        halt = true;
                    }
                }

            }
        }

        public void PullTest()
        {
            Console.WriteLine("This test allows you to pull data for an existing class.");
            Console.WriteLine("Select whether to find an account or character, then enter");
            Console.WriteLine("The account|character to search for. If you look for an");
            Console.WriteLine("account, the system will also call the GetCharacters method");
            Console.WriteLine("in the Characters service.");

            ConsoleKey key = ConsoleKey.F17;
            while (key != ConsoleKey.Q)
            {
                Console.WriteLine("Menu:");
                Console.WriteLine("1. Account Pull");
                Console.WriteLine("2. Character Pull (not yet implemented)");
                Console.WriteLine("V. Verbose Mode");
                Console.WriteLine("Q. Quit");
                Console.WriteLine("Your request (Verbose: " + _verbose.ToString() + "): ");
                key = Console.ReadKey().Key;
                switch (key)
                {
                    case ConsoleKey.D1:
                        Console.Clear();
                        bool quit = false;
                        while (!quit)
                        {
                            Console.WriteLine("Enter account name (Q to quit): ");
                            string acct = Console.ReadLine();
                            if (acct.ToUpper() != "Q")
                            {
                                Console.WriteLine("Enter password (blank if a generated account): ");
                                string pass = Console.ReadLine();
                                if (pass == string.Empty)
                                    pass = null;
                                TestData.Account account = new TestData.Account(acct, pass);
                                TestResult result = account.Get(_verbose);
                                Console.WriteLine(result.Output);
                                result = account.GetCharacters(true);
                                Console.WriteLine(result.Output);
                            }
                            else
                                quit = true;
                        }
                        break;
                    case ConsoleKey.D2:
                        Console.Clear();
                        Console.WriteLine("Coming soon!");
                        //char
                        break;
                    case ConsoleKey.V:
                        Console.Clear();
                        _verbose = !_verbose;
                        break;
                    case ConsoleKey.Q:
                        break;
                    default:
                        Console.WriteLine("Bad selection!");
                        break;
                }
            }
        }
        #region Private Helper Classes and Methods

        #region Test Result Formatting and Input Processing

        public int GetNumber(string msg, int min, int max)
        {
            while (Console.KeyAvailable)
                Console.ReadKey();
            int result = Int32.MinValue;
            do
            {
                Console.Write(msg + " (Min: " + min.ToString() + ", Max: " + max.ToString()+ "): ");
            } while (!Int32.TryParse(Console.ReadLine(), out result) && result > min && result < max);
            return result;
        }

        public bool GetBool(string msg)
        {
            while (Console.KeyAvailable)
                Console.ReadKey();
            bool result;
            do
            {
                Console.Write(msg);  
            } while (!Boolean.TryParse(Console.ReadLine(), out result));
            return result;
        }


        public void WriteMainHeader()
        {
            Console.WriteLine(Environment.NewLine + "Welcome to the WoW Testing Center." + Environment.NewLine + Environment.NewLine);
            Console.WriteLine("**** WARNING: Running these tests will gunk up the current datastore with  ****");
            Console.WriteLine("**** data. Don't use on production! On completion, run the data killer by  ****");
            Console.WriteLine("**** pressing X on the main screen to prep for the next test run.          ****");
            Console.WriteLine();
            
            //For determining max string length
            Console.WriteLine("NOTE: Verbose mode shows the response data from the server. If you turn it off,");
            Console.WriteLine("      you'll only see a true/false for whether the request was successful.");
            Console.WriteLine("      At present, verbose mode is only available for Standard testing.");
            Console.WriteLine();
        }
        public void WriteResultHeader()
        {
            Console.WriteLine();
            Console.WriteLine();
            Console.Write(String.Format("{0, -7}", "[Good]"));
            Console.Write(String.Format("{0, -12}", "[Service]"));
            Console.Write(String.Format("{0, -10}", "[Acct]"));
            Console.Write(String.Format("{0, -11}", "[Char]"));
            Console.WriteLine(String.Format("{0, -38}", "[Additional Data]") + Environment.NewLine);
        }

        #endregion
        #region Test Data Generation and Parsing

        private class RandomName
        {
            private string _base;
            private int counter;
            public RandomName(string baseName, int max = 10000)
            {
                Random random = new Random();
                _base = baseName + random.Next(max).ToString() + "_";
            }
            public string GetName(bool last = false, int stepsBack = 0)
            {
                if (!last)
                    counter++;
                string result = _base + ((int)(counter - stepsBack)).ToString();
                return result;
            }
            public void Reset()
            {
                counter = 0;
            }
        }
        #endregion
        #endregion
    }
}
