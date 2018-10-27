using System;
using WoW.Enums;
using System.Runtime.Serialization;
using WoW.Messages.Characters;

namespace WoW.Classes
{
    [DataContract]
    public class Character : Virtuals.Dirty
    {
        private static volatile StoreClass _store = new StoreClass();
        protected static StoreClass Store
        {
            get
            {
                return _store;
            }
        }
        /// <summary>
        /// Store is the data store for the Character class. This holds the active list of Characters in a singleton, and manages the push/pull to the JSON file storage.
        /// </summary>
        protected class StoreClass : Virtuals.DataStore<Character>
        {
            #region Singleton initialization and Constructor

            public StoreClass()
                : base("characters.json", "TEMP_characters.json")
            {

            }
            #endregion
        }
        //Private only, must be accessed by CharacterMessage-generating static methods.
        #region Constructor

        protected Character(string accountname, string name, byte level, Class cls, Race race, Faction faction, Guid? id = null)
        {
            _accountname = accountname;
            _level = level;
            _name = name;
            _class = cls;
            _faction = faction;
            _race = race;
            if (id.HasValue)
                _id = id.Value;
            else
            {
                IsDirty = true;
                _id = Guid.NewGuid();
            }
        }

        #endregion
        // Used for actually generating objects of this class - 
        // these interface with the private constructor above.
        #region Public/Static accessible constructors

        public static CharacterResponse Create(Account account, string name, Class cls, Race race, Faction faction)
        {
            Character character = null;
            string failedMessage = Validation.Instance.TestName(name);
            if (failedMessage == string.Empty)
            {
                cls.Validate(race);
                string raceTest = race.Validate(faction);

                if (raceTest != string.Empty)
                {
                    if (failedMessage != string.Empty)
                        failedMessage = failedMessage + Environment.NewLine;
                    failedMessage += raceTest;
                }

                if (failedMessage == string.Empty)
                {
                    byte level = 1;
                    if (cls == Class.DeathKnight)
                        level = 55;
                    // Final test - pulls each character for a given user. If user has a non-deleted character of the opposing faction, Create returns with failedMessage and null Character.
                    foreach (string characterName in account.CharacterNames)
                    {
                        Character existingCharacter = Store.Get(characterName);

                        if (existingCharacter.Faction != faction && !existingCharacter.Deleted)
                            failedMessage = "Error: You already have at least one active character of the opposing faction. You cannot create a " + faction.ToString() + " character without deleting that character.)";
                    }
                    if (failedMessage == string.Empty)
                    {
                        character = new Character(account.Name, name, level, cls, race, faction);
                        failedMessage = Store.Add(character);
                        if (failedMessage == string.Empty)
                            account.AddCharacterName(character.Name);
                    }
                }
            }
            if (failedMessage != string.Empty)
                character = null;

            return new CharacterResponse(failedMessage, character);
        }

        public static CharacterResponse Load(string accountname, string name)
        {
            string failedMessage = string.Empty;
            Character character = Store.Get(name);
            if (character == null)
                failedMessage = "Could not find character in storage.";
            else
            {
                if (character._accountname != accountname)
                {
                    failedMessage = "Error: Account doesn't match Account tied to character.";
                    character = null; //In the case of attempting to load a character not attached to the account
                    //character should not be returned to the client.
                }
                else
                {
                    if (character._deleted)
                        failedMessage = "Character is deleted. Make a restore request to restore character to accessibility.";
                }
            }
            return new CharacterResponse(failedMessage, character);
        }

        #endregion
        #region Private Fields

        //Owner of the character
        protected string _accountname;
        protected byte _level;
        protected Class _class;
        protected Race _race;
        protected Faction _faction;
        protected bool _deleted;
        protected DateTime _lastAccessed; // For use in trimming the Character list in Store.

        #endregion
        #region DataMembers

        [DataMember]
        public string AccountName { get { return _accountname; } protected set { _accountname = value; } }
        [DataMember]
        public byte Level { get { return _level; } protected set { _level = value; } }
        [DataMember]
        public Class Class { get { return _class; } protected set { _class = value; } }
        [DataMember]
        public Race Race { get { return _race; } protected set { _race = value; } }
        [DataMember]
        public Faction Faction { get { return _faction; } protected set { _faction = value; } }
        [DataMember]
        public bool Deleted { get { return _deleted; } protected set { _deleted = value; } }

        #endregion
        #region Methods

        public string SetName(Account account, string name)
        {
            string result = Validation.Instance.TestName(name);
            if (result == string.Empty)
            {
                result = Store.Remove(this);
                account.ChangeCharacterName(_name, name);
                _name = name;
                IsDirty = true;
                _lastAccessed = DateTime.Now;
                if (result == string.Empty)
                    result = Store.Add(this);
            }
            return result;
        }

        public string SetLevel(byte level)
        {
            string result = Validation.Instance.TestLevel(_level, level);
            if (result == string.Empty)
            {
                _level = level;
                IsDirty = true;
                _lastAccessed = DateTime.Now;
            }
            return result;
        }

        public string UpdateCharacter(Account account, Faction? faction, Race? race, Class? cls = null)
        {
            string response = string.Empty;
            Race newRace = _race;
            Class newClass = _class;
            Faction newFaction = _faction;
            foreach (string characterName in account.CharacterNames)
            {
                if (characterName != _name)
                {
                    Character existingCharacter = Store.Get(characterName);
                    if (existingCharacter.Faction != newFaction && !existingCharacter.Deleted)
                        response = "Error: You already have at least one active character of the opposing faction. You cannot create a " + newFaction.ToString() + " character without deleting that character.)";
                }
            }
            if (response == string.Empty)
            {
                if (race.HasValue)
                    newRace = race.Value;
                if (cls.HasValue)
                    newClass = cls.Value;
                if (faction.HasValue)
                    newFaction = faction.Value;
                string raceValidate = newRace.Validate(newFaction);
                if (raceValidate != string.Empty)
                {
                    if (response != string.Empty)
                        response += Environment.NewLine;
                    response += raceValidate;
                }
                string classValidate = newClass.Validate(newRace);
                if (classValidate != string.Empty)
                {
                    if (response != string.Empty)
                        response += Environment.NewLine;
                    response += classValidate;
                }
                if (response == string.Empty)
                {
                    _faction = newFaction;
                    _race = newRace;
                    _class = newClass;
                    IsDirty = true;
                    _lastAccessed = DateTime.Now;
                }
            }
            return response;
        }

        public string Delete()
        {
            string result = string.Empty;

            if (_deleted)
                result = "Error: Character already deleted.";
            else
            {
                _deleted = true;
                IsDirty = true;
            }

            return result;
        }

        public static CharacterResponse Restore(Account account, string name)
        {
            string result = string.Empty;
            Character character = Store.Get(name);
            if (character != null)
            {
                foreach (string characterName in account.CharacterNames)
                {
                    if (characterName != character.Name)
                    {
                        Character existingCharacter = Store.Get(characterName);
                        if (existingCharacter.Faction != character.Faction && !existingCharacter.Deleted)
                            result = "Error: You already have at least one active character of the opposing faction. You cannot create a " + existingCharacter.Faction.ToString() + " character without deleting that character.)";
                    }
                }
                if (!character._deleted)
                    result = "Error: Character is not deleted.";
                else
                {
                    character._deleted = false;
                    character.IsDirty = true;
                }
                character._lastAccessed = DateTime.Now;
            }
            else
                result = "Error: Could not find any character by that name.";
            return new CharacterResponse(result, character);
        }

        #endregion
    }
}
