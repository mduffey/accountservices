using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using WoW.Utility;

namespace WoW.Classes
{
    public class Virtuals
    {
        /// <summary>
        /// Dirty is the primary object for user generated data, specifically accounts and characters
        /// for now. It stores whethere the data is "dirty" (needs to be upated in the store) and
        /// when it was made dirty, so that the LINQ query that clears out the dirty state cleans
        /// only the objects that haven't been updated since the initial intent to update (we're probably
        /// talking a few milliseconds, but all it takes is one guy who lost 500 experience points to get some forum screaming...).
        /// </summary>
        [DataContract]
        public abstract class Dirty : Object
        {
            public Dirty()
            {
                _lastaccessed = DateTime.Now;
            }
            protected bool _isdirty;
            //Guid used to match characters, allowing user to change character name.
            //Dictionary and all requests are still based on name.
            protected string _name;
            protected Guid _id = Guid.NewGuid();
            protected DateTime _lastaccessed;
            public bool IsDirty
            {
                get { return _isdirty; }
                set
                {
                    _isdirty = value;
                }
            }
            [DataMember]
            public string Name
            {
                get
                {
                    _lastaccessed = DateTime.Now;
                    return _name;
                }
                protected set
                {
                    _lastaccessed = DateTime.Now;
                    _isdirty = true;
                    _name = value;
                }
            }
            [DataMember]
            public Guid Id
            {
                get
                {
                    _lastaccessed = DateTime.Now;
                    return _id;
                }
                protected set
                {
                    _id = value;
                }
            }
            public DateTime LastAccessed { get { return _lastaccessed; } }
        }

        /// <summary>
        /// The DirtyCollection is used solely for writing the objects to the file. I handrolled the reading portion so that the file could be read through one at a time rather 
        /// than streamed completely into memory, as I thought the treatment of the JSON file should be as SQL-y as possible, rather than just a memory dump to be pulled to/from.
        /// My next step would be to create indexes, which would be quite fun but take probably another 20 hours to do, and I'd like to deliver this soon...
        /// </summary>
        /// <typeparam name="T"></typeparam>
        [DataContract]
        public class DirtyCollection<T> where T : Dirty
        {
            [DataMember]
            public List<T> Items = new List<T>();
        }
        /// <summary>
        /// This is the class that manages the collection of dirty objects. It maintains an active cache in the _items property,
        /// has methods to asynchronously pull user data out of the store if it's not in the cache, and a separate asynchronous method
        /// to save all the data. At present, it's 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public abstract class DataStore<T> where T : Dirty
        {
            private const int _PULSE = 20000; //The frequency in MS that the item list is cleaned and the list is saved to the file.
            private string _path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + Path.DirectorySeparatorChar + "WoW" + Path.DirectorySeparatorChar + "data";
            protected string _file;
            protected string _tempfile;
            protected static Reader _reader;
            protected Task _saveTask;

            #region Constructor
            //System.Timers.Timer SaveTimer = new System.Timers.Timer(600000);
            //System.Timers.Timer CleanTimer = new System.Timers.Timer(300000);
            System.Timers.Timer SaveTimer = new System.Timers.Timer(17500);
            System.Timers.Timer CleanTimer = new System.Timers.Timer(12000);
            protected DataStore(string file, string tempfile)
            {
                SaveTimer.Elapsed += SaveTick;
                SaveTimer.Start();
                CleanTimer.Elapsed += CleanTick;
                CleanTimer.Start();

                if (!Directory.Exists(_path))
                    Directory.CreateDirectory(_path);
                _file = file;
                _tempfile = tempfile;
                _reader = new Reader(_path + Path.DirectorySeparatorChar + file);
            }

            #endregion

            #region Request and Reader region
            /// <summary>
            /// Used for making requests to pull a given item out of a store.
            /// Everything is straight forward, except for wrapped. That is updated on a mass basis when the Reader wraps around and starts at the beginning of the file.
            /// If it wraps around after wrapped has been set to true and fulfilled is still false, it sets item to null, fulfilling it with an empty, and thus failed, request.
            /// </summary>
            public class Request
            {
                public Request(string name, Guid id)
                {
                    _fulfilled = false;
                    _received = false;
                    _name = name;
                    _id = id;
                }
                private bool _fulfilled;
                private bool _received;
                private bool _wrapped;
                private string _name;
                private Guid _id;
                private object _item;
                public bool Fulfilled { get { return _fulfilled; } }
                public string Name { get { return _name; } }
                public bool Wrapped
                {
                    get { return _wrapped; }
                    set
                    {
                        if (!value)
                            _wrapped = value;
                        else
                        {
                            if (!_wrapped)
                                _wrapped = value;
                            else
                                Item = null;
                        }
                    }
                }
                public Guid Id { get { return _id; } }
                public object Item
                {
                    get { return _item; }
                    set
                    {
                        _item = value;
                        _fulfilled = true;
                    }
                }
                public bool Received { get { return _received; } set { _received = value; } }
            }
            /// <summary>
            /// Handles the actual reading of the data file. Receives Requests in the Requests Dictionary, and 
            /// Read runs in its own thread, setting the Item value in a request to the matching value (by Guid or Name string), marking Fulfilled as true.
            /// Every 10 passes, it checks if any items have been received (by the requesting method watching its own request item in the dictionary) and if so removes those
            /// successful queries. 10 was chosen to keep it from happening constantly, and if so few requests are being made that it never gets to 10, then the requests are a very small strain on the system as it is.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            protected class Reader
            {
                protected FileStream _fStream;
                protected StreamReader _fileReader;
                protected DataContractJsonSerializer _ser = new DataContractJsonSerializer(typeof(T));
                protected string _file;
                public Dictionary<Guid, Request> Requests = new Dictionary<Guid, Request>();
                public Task ReadingTask;
                public bool Alive;
                public Reader(string file)
                {
                    _file = file;
                    Start();
                }
                ~Reader()
                {
                    _fileReader.Close();
                }
                public void Start()
                {
                    _fStream = new FileStream(_file, FileMode.OpenOrCreate, FileAccess.Read);
                    _fileReader = new StreamReader(_fStream);
                    _fStream.Position = 9; //offhanded guess at the position needed to start to bypass {"Items": to get to the real data.
                    Alive = true;
                    ReadingTask = Task.Factory.StartNew(() => Read());
                }
                public void Read()
                {
                    char[] refuse = null; //left over from reading the file past the last item
                    while (Alive)
                    {
                        lock (Requests)
                        {
                            var deletes = Requests.Where(t => t.Value.Received == true).ToList();
                            foreach (var request in deletes)
                                Requests.Remove(request.Key);
                        }
                        int unfulfilled = 0;
                        lock (Requests)
                            unfulfilled = Requests.Values.Count(t => t.Fulfilled == false);
                        if (unfulfilled == 0)
                            Thread.Sleep(250);
                        else
                        {
                            while (Requests.Values.Count(t => t.Fulfilled == false) > 0 && Alive)
                            {
                                if (_fStream.Position >= _fStream.Length - 1)
                                {
                                    _fStream.Position = 9; //offhanded guess at the position needed to start to bypass {"Items": to get to the real data.
                                    lock (Requests)
                                    {
                                        Requests.Values.Select(t => { t.Wrapped = true; return t; }).ToList();
                                    }
                                }
                                T item = (T)PullItem(ref refuse);
                                if (item != null)
                                {
                                    List<Request> requests;
                                    lock (Requests)
                                    {
                                        requests = Requests.Values.Where(t => t.Name == item.Name || t.Id == item.Id).ToList();
                                        foreach (Request request in requests)
                                        {
                                            request.Item = item;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    //Since the file is being rebuilt and the method has to start from scratch when restarted,
                    //Wrapped values are reverted to false;
                    Requests.Values.Select(t => { t.Wrapped = false; return t; }).ToList();
                    _fStream.Close();
                }
                // refuse is the data left over from the last item matched, start immediately after the closing bracket.
                protected T PullItem(ref char[] refuse)
                {
                    T result = null;
                    string allData = string.Empty;
                    char[] buffer = null;
                    if (_fileReader.EndOfStream)
                        buffer = refuse;
                    else
                        buffer = new char[4096];
                    int startReadPosition = 0;
                    if (refuse != null)
                    {
                        Array.Copy(refuse, buffer, refuse.Length);
                        startReadPosition = refuse.Length;
                    }

                    //refuse is supposed to give us anything immediately after the final closing bracket in JSON.
                    //As such, when openBrackets hits zero again (as data is read through to find total open and closed brackets) 
                    //the remaining string is split off into a new char array and we move on.
                    int openBrackets = 0;
                    while (result == null && (!_fileReader.EndOfStream || refuse != null))
                    {
                        if (!_fileReader.EndOfStream)
                            _fileReader.Read(buffer, startReadPosition, 4096 - startReadPosition);
                        refuse = null;
                        startReadPosition = 0;
                        string curData = new string(buffer);
                        allData += curData;
                        int Pos = 0;
                        while (Pos < curData.Length - 1)
                        {
                            int opener = curData.IndexOf('{', Pos);
                            int closer = curData.IndexOf('}', Pos);
                            if (opener >= 0 && opener < closer) //
                            {
                                openBrackets++;
                                Pos = opener + 1;
                            }
                            else
                            {
                                if (closer >= 0)
                                {
                                    openBrackets--;
                                    Pos = closer + 1;
                                }
                                else
                                {
                                    Pos = curData.Length;
                                }
                                if (openBrackets == 0)
                                {
                                    if (allData.Length > curData.Length)
                                        closer += allData.Length - 1;
                                    string json = allData.Substring(allData.IndexOf("{"), closer);
                                    result = (T)_ser.ReadObject(json.ToStream()); //search for { index juuuust in case I stripped the comma out poorly.
                                    refuse = allData.Substring(closer + 1).ToCharArray(); //Add 1 to closer position to strip the comma out
                                    Pos = curData.Length;
                                }
                                else if (openBrackets < 1) //possible at EOF, as the SaveFile system writes it as an overarching JSON object, so one extra bracket at the end for the end of Items collection.
                                {
                                    refuse = null;
                                    _fileReader.ReadToEnd();
                                }
                            }
                        }
                    }
                    return result;
                }
            }
            #endregion

            protected ConcurrentDictionary<string, T> _items = new ConcurrentDictionary<string, T>();

            protected virtual void SaveTick(object s, ElapsedEventArgs args)
            {
                SaveTimer.Interval = _PULSE;
                SaveTimer.Start();
                if (_saveTask == null || _saveTask.IsCompleted) //Prevents creating a new task when you're busy debugging
                {
                    DateTime saving = DateTime.Now;
                    List<T> saveItems;
                    lock (_items)
                    {
                        saveItems = _items.Values.Where(t => t.IsDirty).ToList();
                        foreach (T item in saveItems)
                            _items[item.Name].IsDirty = false;
                    }
                    _saveTask = Task.Factory.StartNew(() => SaveFile(saveItems));
                }
            }

            protected virtual void CleanTick(object s, ElapsedEventArgs args)
            {
                CleanTimer.Interval = _PULSE;
                CleanTimer.Start();
                Task.Factory.StartNew(() => CleanList());
            }
            private void SaveFile(List<T> dirtyItems)
            {
                if (dirtyItems.Count > 0)
                {
                    string tempPath = _path + Path.DirectorySeparatorChar + _tempfile;
                    string filePath = _path + Path.DirectorySeparatorChar + _file;
                    FileStream writeStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.Read);
                    DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(DirtyCollection<T>));
                    DirtyCollection<T> collection = new DirtyCollection<T>();
                    collection.Items.AddRange(dirtyItems);
                    FileStream readStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read);
                    if (readStream.Length > 0)
                    {
                        DirtyCollection<T> fileItems = (DirtyCollection<T>)ser.ReadObject(readStream);
                        foreach (T item in fileItems.Items)
                        {
                            if (dirtyItems.Count(t => t.Id == item.Id) == 0)
                                collection.Items.Add(item);
                        }
                    }
                    ser.WriteObject(writeStream, collection);
                    writeStream.Close();
                    _reader.Alive = false;
                    _reader.ReadingTask.Wait();
                    lock (this)
                    {
                        readStream.Close();
                        if (File.Exists(filePath))
                            File.Delete(filePath);
                        File.Move(tempPath, filePath);
                        _reader.Start();
                    }
                }
            }
            private void CleanList()
            {
                List<KeyValuePair<string, T>> list = _items.Where(t => t.Value.LastAccessed < DateTime.Now.AddMinutes(-15) && !t.Value.IsDirty).ToList();
                T value;
                foreach (KeyValuePair<string, T> kvp in list)
                    _items.TryRemove(kvp.Key, out value);
            }
            public void Get(Guid requestGuid, string name, Guid guid)
            {
                lock (_reader.Requests)
                {
                    _reader.Requests.Add(requestGuid, new Request(name, guid));
                }
                while (!_reader.Requests[requestGuid].Fulfilled)
                    Thread.Sleep(100);
            }
            public T Get(string name)
            {
                if (!_items.ContainsKey(name))
                {
                    Guid requestGuid = Guid.NewGuid();
                    Task task = Task.Factory.StartNew(() => Get(requestGuid, name, Guid.NewGuid()));
                    task.Wait();
                    T item = (T)_reader.Requests[requestGuid].Item;
                    lock (_reader.Requests)
                        _reader.Requests[requestGuid].Received = true;
                    if (item != null)
                        _items.TryAdd(item.Name, item);
                    return item;
                }
                return _items[name];
            }
            public T Get(Guid id)
            {
                T idItem = _items.Values.First(t => t.Id == id);
                if (idItem == null)
                {
                    Guid requestGuid = Guid.NewGuid();
                    Task task = Task.Factory.StartNew(() => Get(requestGuid, null, id));
                    task.Wait();
                    T item = (T)_reader.Requests[requestGuid].Item;
                    lock(_reader.Requests)
                        _reader.Requests[requestGuid].Received = true;
                    _items.TryAdd(item.Name, item);
                    return item;
                }
                return idItem;
            }

            public string Add(T item)
            {
                if (_items.ContainsKey(item.Name) || Get(item.Name) != null)
                    return "Error: A character with that name already exists.";

                if (!_items.TryAdd(item.Name, item))
                    return "Error: A character with that name already exists.";
                return string.Empty;
            }

            public string Remove(T item)
            {
                T oldItem;
                if (!_items.TryRemove(item.Name, out oldItem))
                    return "Error: Could not remove old item.";
                return string.Empty;
            }
        }
    }
}
