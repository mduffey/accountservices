using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;

namespace WoW.Services.Host.Tests
{
    public struct TestResult
    {
        public bool Success;
        public string Output;
    }
    /// <summary>
    /// The classes under TestData generate new accounts and characters, and submit them to the service.
    /// </summary>
    public class TestData
    {
        public static string charUri = string.Empty;
        public static string acctUri = string.Empty;
        private static WoW.Enums.Faction[] _factions = (WoW.Enums.Faction[])Enum.GetValues(typeof(WoW.Enums.Faction));
        private static WoW.Enums.Class[] _classes = (WoW.Enums.Class[])Enum.GetValues(typeof(WoW.Enums.Class));
        private static WoW.Enums.Race[] _races = (WoW.Enums.Race[])Enum.GetValues(typeof(WoW.Enums.Race));
        public class Character : CharacterExecute
        {
            public string AccountName;
            public string Name;
            public WoW.Messages.Login Login = new Messages.Login();
            public WoW.Enums.Faction Faction;
            public WoW.Enums.Race Race;
            public WoW.Enums.Class Class;
            public int Level;
            /// <summary>
            /// Creates a Character for the purposes of submitting it to the service.
            /// </summary>
            /// <param name="acct">The TestData.Account to possess this character</param>
            /// <param name="name">The name to be given to the character</param>
            /// <param name="faction">The faction of the character and account</param>
            /// <param name="goodData">If the data to generate for this should be 'good'</param>
            public Character(Account acct, string name, WoW.Enums.Faction faction, bool goodData, string pass = null)
            {
                Random random = new Random();
                AccountName = acct.Name;
                Name = name;
                Faction = faction;
                Generate(goodData);
                if (pass == null)
                    Login.Password = "testpass";
                else
                    Login.Password = pass;
            }
            //Generates the Race/Class and, if not supplied, Faction
            public void Generate(bool goodData, WoW.Enums.Faction? faction = null)
            {
                Random random = new Random();
                Enums.Faction usedFaction = faction.HasValue ? faction.Value : Faction;
                if (goodData)
                {
                    if (usedFaction == Enums.Faction.Alliance)
                    {
                        switch (random.Next(3))
                        {
                            case 0:
                                Race = Enums.Race.Human;
                                Class = _classes[random.Next(3)];
                                break;
                            case 1:
                                Race = Enums.Race.Gnome;
                                Class = _classes[random.Next(3)];
                                break;
                            case 2:
                                Race = Enums.Race.Worgen;
                                Class = _classes[random.Next(4)];
                                break;
                        }
                    }
                    else
                    {
                        switch (random.Next(3))
                        {
                            case 0:
                                Race = Enums.Race.BloodElf;
                                Class = _classes[random.Next(2)];
                                break;
                            case 1:
                                Race = Enums.Race.Orc;
                                Class = _classes[random.Next(3)];
                                break;
                            case 2:
                                Race = Enums.Race.Tauren;
                                Class = _classes[random.Next(4)];
                                break;
                        }
                    }
                }
                else
                {
                    //determine the cause of failure: case 0 sets a bad class against a
                    //race for the current faction. case 1 sets a bad race against the
                    //current faction (class is correct)
                    switch (random.Next(2))
                    {
                        case 0:
                            if (Faction == Enums.Faction.Alliance)
                            {
                                Race = Enums.Race.Human;
                                Class = Enums.Class.Druid;
                            }
                            break;
                        case 1:
                            if (Faction == Enums.Faction.Horde)
                            {
                                switch (random.Next(3))
                                {
                                    case 0:
                                        Race = Enums.Race.Human;
                                        Class = _classes[random.Next(3)];
                                        break;
                                    case 1:
                                        Race = Enums.Race.Gnome;
                                        Class = _classes[random.Next(3)];
                                        break;
                                    case 2:
                                        Race = Enums.Race.Worgen;
                                        Class = _classes[random.Next(4)];
                                        break;
                                }
                            }
                            else
                            {
                                switch (random.Next(3))
                                {
                                    case 0:
                                        Race = Enums.Race.BloodElf;
                                        Class = _classes[random.Next(2)];
                                        break;
                                    case 1:
                                        Race = Enums.Race.Orc;
                                        Class = _classes[random.Next(3)];
                                        break;
                                    case 2:
                                        Race = Enums.Race.Tauren;
                                        Class = _classes[random.Next(4)];
                                        break;
                                }
                            }
                            break;
                    }

                }
            }
            protected TestResult Submit(string addluri, string httpMethod, object request, bool verbose, string method, string newname = "")
            {
                TestResult result = new TestResult();
                var response = CharacterRequest(AccountName + "/" + Name + addluri, httpMethod, request);
                if (newname != string.Empty)
                    Name = newname;
                if (!response.Success || response.Character.Name != Name || response.Character.Race != Race || response.Character.Faction != Faction || response.Character.Class != Class || response.Character.Level != Level)
                {
                    result.Success = false;
                }
                else
                    result.Success = true;
                result.Output = BuildOutput(verbose, result.Success, method, response.ToJson());
                return result;
            }
            public TestResult Create(bool verbose)
            {
                if (Class == Enums.Class.DeathKnight)
                    Level = 55;
                else
                    Level = 1;
                var charRequest = new WoW.Messages.Characters.CreateRequest();
                charRequest.Login = Login;
                charRequest.Class = Class;
                charRequest.Faction = Faction;
                charRequest.Race = Race;
                return Submit(string.Empty, "POST", charRequest, verbose, "charCreate");
            }
            public TestResult ChangeLevel(int level, bool verbose)
            {
                int oldLevel = Level;
                Level = level;
                var charRequest = new WoW.Messages.Characters.CharacterBaseRequest();
                charRequest.Login = Login;
                TestResult result = Submit("?level=" + level.ToString(), "POST", charRequest, verbose, "charLevel");
                if (!result.Success)
                    Level = oldLevel;
                return result;
            }
            public TestResult ChangeName(string name, bool verbose)
            {
                string oldName = Name;
                var charRequest = new WoW.Messages.Characters.NameRequest();
                charRequest.Login = Login;
                charRequest.NewName = name;
                TestResult result = Submit("/namechange", "POST", charRequest, verbose, "charName", name);
                if (!result.Success)
                    Name = oldName;
                return result;
            }
            public TestResult Update(bool goodData, bool verbose)
            {
                var charRequest = new WoW.Messages.Characters.UpdateRequest();
                WoW.Enums.Class oldClass = Class;
                WoW.Enums.Race oldRace = Race;
                WoW.Enums.Faction oldFaction = Faction;
                charRequest.Login = Login;
                Generate(goodData);
                charRequest.Class = Class;
                charRequest.Faction = Faction;
                charRequest.Race = Race;
                TestResult result = Submit("/update", "POST", charRequest, verbose, "charUpdate");
                if (!result.Success)
                {
                    Class = oldClass;
                    Faction = oldFaction;
                    Race = oldRace;
                }
                return result;
            }

            public TestResult Delete(bool verbose)
            {
                var charRequest = new WoW.Messages.Characters.CharacterBaseRequest();
                charRequest.Login = Login;
                return Submit(string.Empty, "DELETE", charRequest, verbose, "charDelete");
            }

            public TestResult Restore(bool verbose)
            {
                var charRequest = new WoW.Messages.Characters.CharacterBaseRequest();
                charRequest.Login = Login;
                return Submit("/restore", "POST", charRequest, verbose, "charRestore");
            }
            private string BuildOutput(bool verbose, bool success, string method, string responseJson)
            {
                string output = String.Format("{0, -7}", success.ToString());
                output += String.Format("{0, -12}", method);
                output += String.Format("{0, -10}", AccountName);
                output += String.Format("{0, -11}", Name);
                output += String.Format("{0, -38}", "F:" + Faction.ToString() + ",R:" + Race.ToString() + ",C:" + Class.ToString() + ",L:" + Level.ToString());
                if (verbose)
                {
                    output += Environment.NewLine;
                    output += "Response: " + responseJson;
                }
                return output;
            }
        }

        public class Account : AccountExecute
        {
            public string Name;
            public WoW.Messages.Login Login = new Messages.Login();
            public List<Character> Characters = new List<Character>();
            public Account(string name, string pass = null)
            {
                Name = name;

                Login = new Messages.Login();
                if (pass == null)
                    Login.Password = "testpass";
                else
                    Login.Password = pass;
            }
            public TestResult Create(bool verbose)
            {
                return Submit(string.Empty, "POST", verbose, "acctCreate");
            }
            public TestResult Get(bool verbose)
            {
                return Submit(string.Empty, "PUT", verbose, "acctGet");
            }
            public TestResult GetCharacters(bool verbose)
            {
                TestResult result = new TestResult();
                var charsRequest = new WoW.Messages.Characters.CharacterBaseRequest();
                charsRequest.Login = Login;

                var response = CharactersRequest(Name, "PUT", charsRequest);

                if (!response.Success || response.Characters.Count > 0)
                {
                    result.Success = false;
                }
                else
                    result.Success = true;
                result.Output = BuildOutput(result.Success, verbose, "charGet", response.ToJson());
                return result;
            }
            protected TestResult Submit(string addluri, string httpMethod, bool verbose, string method)
            {
                TestResult result = new TestResult();
                var acctRequest = new WoW.Messages.Accounts.AccountRequest();
                acctRequest.Login = Login;

                var response = AccountRequest(Name + addluri, httpMethod, acctRequest);

                if (!response.Success || response.Account.Name != Name)
                {
                    result.Success = false;
                }
                else
                    result.Success = true;
                result.Output = BuildOutput(result.Success, verbose, method, response.ToJson());
                return result;
            }

            public string BuildOutput(bool success, bool verbose, string method, string responseJson)
            {
                string output = String.Format("{0, -7}", success.ToString());
                output += String.Format("{0, -12}", method);
                output += String.Format("{0, -10}", Name);
                output += String.Format("{0, -10}", "----");
                output += String.Format("{0, -38}", "----");
                if (verbose)
                {
                    output += Environment.NewLine;
                    output += "Response: " + responseJson;
                }
                return output;
            }
        }

        #region WebRequest and Response

        public class AccountExecute : HttpExecute
        {
            public WoW.Messages.Accounts.AccountResponse AccountRequest(string uriappend, string method, object data)
            {
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(WoW.Messages.Accounts.AccountResponse));
                return (WoW.Messages.Accounts.AccountResponse)ser.ReadObject(ExecuteRequest(acctUri + "/" + uriappend, method, data));
            }
            public WoW.Messages.Characters.CharactersResponse CharactersRequest(string uriappend, string method, object data)
            {
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(WoW.Messages.Characters.CharactersResponse));
                return (WoW.Messages.Characters.CharactersResponse)ser.ReadObject(base.ExecuteRequest(charUri + "/" + uriappend, method, data));
            }
        }
        public class CharacterExecute : HttpExecute
        {

            public WoW.Messages.Characters.CharacterResponse CharacterRequest(string uriappend, string method, object data)
            {
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(WoW.Messages.Characters.CharacterResponse));
                return (WoW.Messages.Characters.CharacterResponse)ser.ReadObject(base.ExecuteRequest(charUri + "/" + uriappend, method, data));
            }
        }
        public abstract class HttpExecute
        {
            protected string uri;
            public Stream ExecuteRequest(string uri, string method, object data)
            {
                var request = HttpWebRequest.Create(uri);
                request.ContentType = "application/json";
                request.Method = method;
                Stream stream = request.GetRequestStream();
                DataContractJsonSerializer ser = new DataContractJsonSerializer(data.GetType());
                ser.WriteObject(stream, data);
                //request.ContentLength = stream.Length;
                return request.GetResponse().GetResponseStream();
            }
        }
        #endregion
    }
}
