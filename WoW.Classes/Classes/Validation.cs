using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WoW.Enums;
using Newtonsoft.Json;

namespace WoW.Classes
{
    /// <summary>
    /// Second level of validations after basic data annotation 
    /// </summary>
    public class Validation
    {
        #region Lists and Dictionaries for validation rules
        private static byte maxLevel = 85;
        private static char[] BadChars = new char[5] {'!', '{', '}', '"', ','};
        private List<string> BadNames = new List<string>();
        //MessagesBy Dictionaries provide the error messages returned when validation fails.
        //For example, if user tries to create a new human, horde character, the RacesByFaction dictionary
        //will fail to find that combo, then the race test in the extensions below will return the message:
        //Error: current Horde races allowed are Blood Elves, Orcs and Tauren.
        private Dictionary<Faction, List<Race>> _racesbyfaction = new Dictionary<Faction, List<Race>>();
        private Dictionary<Faction, string> _messagesbyfaction = new Dictionary<Faction, string>();
        private Dictionary<Race, List<Class>> _classesbyrace = new Dictionary<Race, List<Class>>();
        private Dictionary<Race, String> _messagesbyrace = new Dictionary<Race, string>();

        public Dictionary<Faction, List<Race>> RacesByFaction { get { return _racesbyfaction; } }
        public Dictionary<Faction, String> MessagesByFaction { get { return _messagesbyfaction; } }
        public Dictionary<Race, List<Class>> ClassesByRace { get { return _classesbyrace; } }
        public Dictionary<Race, String> MessagesByRace { get { return _messagesbyrace; } }

        #endregion
        #region Singleton setup and constructor
        private Validation() 
        {
            ReadBadNames(PullFile("UnacceptableNames.json"));
            ReadClasses(PullFile("ClassesByRace.json"));
            ReadRaces(PullFile("RacesByFaction.json"));
        }
        private static Validation instance;

        public static Validation Instance
        {
            get
            {
                if (instance == null)
                    instance = new Validation();
                return instance;
            }
        }
        #endregion
        #region Validation rule initialization
        /// <summary>
        /// Generic process to pull the JSON strong out for each Validation file. Assumes that
        /// all validation files are stored in the app's working directory\Validation,
        /// and so the constructor for Validation provides only the specific file name to PullFile.
        /// </summary>
        /// <param name="file">Name of Validation file to read.</param>
        /// <returns>JSON string to be deserialized by the relevant method.</returns>
        private string PullFile(string file)
        {
            string result = string.Empty;
            string path = Environment.CurrentDirectory + Path.DirectorySeparatorChar + "Validations" + Path.DirectorySeparatorChar + file;
            FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            StreamReader reader = new StreamReader(stream);
            result = string.Empty;
            result += reader.ReadToEnd();
            reader.Close();
            return result;
        }

        private void ReadBadNames(string json)
        {
            dynamic jsonObj = JsonConvert.DeserializeObject(json);
            int test = jsonObj.names.Count;
            foreach (string name in jsonObj.names)
                BadNames.Add(name);
        }

        private void ReadClasses(string json)
        {
            dynamic jsonObj = JsonConvert.DeserializeObject(json);
            foreach (var jsonRace in jsonObj.races)
            {
                Race race = Enum.Parse(typeof(Race), jsonRace.name.ToString(), true);
                string message = "Warning: " + jsonRace.name + " characters can only be the following classes: ";
                List<Class> classes = new List<Class>();
                foreach (var cls in jsonRace.classes)
                {
                    classes.Add(Enum.Parse(typeof(Class), cls.ToString(), true));
                    message += cls + ", ";
                }
                message = message.Substring(0, message.Length - 2) + ".";
                _classesbyrace.Add(race, classes);
                _messagesbyrace.Add(race, message);
            }
        }
        private void ReadRaces(string json)
        {
            dynamic jsonObj = JsonConvert.DeserializeObject(json);
            foreach (var jsonFaction in jsonObj.factions)
            {
                Faction faction = Enum.Parse(typeof(Faction), jsonFaction.name.ToString(), true);
                string message = "Warning: " + jsonFaction.name + " characters can only be the following races: ";
                List<Race> races = new List<Race>();
                foreach (var race in jsonFaction.races)
                {
                    races.Add(Enum.Parse(typeof(Race), race.ToString(), true));
                    message += race + ", ";
                }
                message = message.Substring(0, message.Length - 2) + ".";
                _racesbyfaction.Add(faction, races);
                _messagesbyfaction.Add(faction, message);
            }
        }
        #endregion
        #region Validation Methods
        public string TestName(string name, bool account = false)
        {
            if (!account)
            {
                if (BadNames.Contains(name.ToLower()))
                    return "Error: name of character can not be an existing Warcraft Universe character name.";
            }
            foreach (char Evil in BadChars)
            {
                if (name.Contains(Evil))
                    return "Error: Bad characters in name. Cannot have !, {, \", } or comma.";
            }
            return string.Empty;
        }
        public string TestLevel(byte curLevel, byte newLevel)
        {
            if (newLevel > maxLevel || newLevel < 1)
                return "Error: Level must be between 1 and " + maxLevel.ToString() + ".";
            if (newLevel < curLevel)
                return "Error: Level must be greater than character's current level.";
            return string.Empty;
        }

        public string TestPassword(string pass)
        {
            if (pass.Length > 6)
            {
                return string.Empty;
            }
            else
                return "Error: Password must be at least 7 characters.";
        }
        #endregion
    }

    public static class ValidationExtensions
    {
        /// <summary>
        /// This method tests whether the requested race is allowed within the supplied faction. Returns string.Empty if valid,
        /// otherwise provides the reason for the rejection.
        /// </summary>
        /// <param name="race">The race requested for the new character</param>
        /// <param name="faction">The current faction for the account (or requested, if this is the first character)</param>
        /// <returns>string.Empty on success, otherwise the reason for the failure.</returns>
        public static string Validate(this Race race, Faction faction)
        {
            if (Validation.Instance.RacesByFaction[faction].Contains(race))
                return string.Empty;
            return Validation.Instance.MessagesByFaction[faction];
        }
        /// <summary>
        /// This method tests whether the requested race is allowed within the requested race. Returns string.Empty if valid,
        /// otherwise provides the reason for the rejection.
        /// </summary>
        /// <param name="cls">Class requested by the user.</param>
        /// <param name="race">Race requested by the user.</param>
        /// <returns>string.Empty on success, otherwise the reason for the failure</returns>
        public static string Validate(this Class cls, Race race)
        {
            if (Validation.Instance.ClassesByRace[race].Contains(cls))
                return string.Empty;
            return Validation.Instance.MessagesByRace[race];
        }
    }
}
