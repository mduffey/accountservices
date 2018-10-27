using System;
using System.Collections.Generic;
using System.Linq;
using WoW.Enums;
using System.Security.Cryptography;
using System.Runtime.Serialization;
using WoW.Messages.Accounts;

namespace WoW.Classes
{

    /// <summary>
    /// Public Account is used to return account information back to the client, since Hash and Salt are data members for easy storage and should not be sent back to the client. Holds Name, CharacterNames, and Faction if CharacterNames is not null.
    /// </summary>
    [DataContract]
    public class PublicAccount
    {
        public PublicAccount(string name, List<string> characternames, Faction? faction)
        {
            Name = name;
            CharacterNames = characternames;
            Faction = faction;
        }
        [DataMember]
        public readonly string Name;
        [DataMember]
        public readonly List<string> CharacterNames;
        [DataMember]
        public readonly Faction? Faction;

    }

    [DataContract]
    public class Account : Virtuals.Dirty
    {
        #region DataStore fields and singleton
        private static volatile StoreClass _store = new StoreClass();
        private static object syncRoot = new object();
        protected static StoreClass Store
        {
            get
            {
                //lock (_store)
                //{
                //    if (_store == null)
                //    {

                //        _store = new StoreClass();

                //    }
                //}
                return _store;
            }
        }
        /// <summary>
        /// Store is the data store for the Account class. This holds the active list of Accounts in a singleton, and manages the push/pull to the JSON file storage.
        /// </summary>
        protected class StoreClass : Virtuals.DataStore<Account>
        {
            #region Singleton initialization and Constructor

            public StoreClass()
                : base("accounts.json", "TEMP_accounts.json")
            {

            }
            #endregion
        }
        #endregion
        #region Protected Constructor
        protected Account(string user, string pass)
        {
            _name = user;
            _salt = new byte[8];
            _characternames = new List<string>();
            using (RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider())
            {
                rngCsp.GetBytes(_salt);
            }
            Rfc2898DeriveBytes hasher = new Rfc2898DeriveBytes(pass, _salt);
            _hash = hasher.GetBytes(16);
            IsDirty = true;
        }
        #endregion
        #region Public/Static constructors
        /// <summary>
        /// Used for generating new accounts. Sends the stripped down
        /// PublicAccount version back to the client, as the hash and salts
        /// are datamembers so they can be written to the store, so PublicAccount
        /// In order to avoid kicking back the salt and the hash to the client.
        /// </summary>
        /// <param name="name">Username for new account</param>
        /// <param name="pass">Password for new account</param>
        /// <returns></returns>
        public static AccountResponse Create(string name, string pass)
        {
            PublicAccount acct = null;
            string message = Validation.Instance.TestName(name, true);
            if (message == string.Empty)
            {
                message = Validation.Instance.TestPassword(pass);
                if (message == string.Empty)
                {
                    message = Store.Add(new Account(name, pass));
                    if (message == string.Empty)
                        acct = new PublicAccount(name, null, null);
                }
            }
            return new AccountResponse (message, acct);
        }
        /// <summary>
        /// Used for accessing existing accounts, requires password.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="pass"></param>
        /// <returns></returns>
        public static Account Login(string name, string pass)
        {
            Account account = Store.Get(name);
            if (account != null)
            {
                if (account.Check(pass))
                    return account;
            }
            return null; 
        }
        #endregion
        #region Protected Fields
        protected Faction? _faction;
        protected List<string> _characternames;
        #endregion
        #region DataMembers
        [DataMember]
        private byte[] _hash;
        [DataMember]
        private byte[] _salt;
        [DataMember]
        public Faction? Faction { get { return _faction; } protected set { _faction = value; } }
        [DataMember]
        public List<string> CharacterNames { get { return _characternames; } protected set { _characternames = value; } }
        #endregion
        #region Methods
        public void AddCharacterName(string name)
        {
            _characternames.Add(name);
            _isdirty = true;
        }
        public void ChangeCharacterName(string name, string newName)
        {
            _characternames.Remove(name);
            _characternames.Add(newName);
            _isdirty = true;
        }
        public bool Check(string password)
        {
            _lastaccessed = DateTime.Now;
            Rfc2898DeriveBytes hasher = new Rfc2898DeriveBytes(password, _salt);
            return hasher.GetBytes(16).SequenceEqual(_hash);
        }
        #endregion
    }
}
