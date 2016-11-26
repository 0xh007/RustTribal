    using System.Collections.Generic;
    using System.Linq;

    using JetBrains.Annotations;

    using Network;

    using Oxide.Game.Rust.Libraries.Covalence;


namespace Oxide.Plugins
{
    using System;

    using Oxide.Core;

    using Random = UnityEngine.Random;

    [Info("RustTribal", "*Vic", 0.1)]
    [Description("Rust Tribal")]
    public class RustTribal : RustPlugin
    {
        #region Rust Tribal Static Methods

        private static float GetRandomFloatBasedOnUserId(ulong userId, ulong seed)
        {
            var state = Random.state;
            Random.InitState((int)(seed + userId));
            var num = Random.Range(0f, 1f);
            Random.state = state;
            return num;
        }

        #endregion Rust Tribal Static Methods

        #region Oxide Members

        private ChatMenu chatMenu;
        protected Game game;
        private GateWay gateWay;

        #endregion Oxide Members

        #region Oxide Hooks

        [UsedImplicitly]
        private string CanClientLogin(Connection packet)
        {
            var message = gateWay.IsClientAuthorized(packet, game);
            if (message.Response == AuthMessage.ResponseType.Rejected)
            {
                return message.GetMessage();
            }
            else
            {
                //Todo: Send client a welcome message
                return null;
            }
        }

        [UsedImplicitly]
        private void Loaded()
        {
            Puts("Plugin loaded");
        }

        [UsedImplicitly]
        private void OnPlayerConnected(Message packet)
        {
            var userName = packet.connection.username;
            var userId = packet.connection.userid;
            game.IncomingPlayer(userId, userName);
        }

        /// <summary>
        /// Called when the server is initialized.
        /// Loads game data if data exists.
        /// Creates a new game if no data exists.
        /// </summary>
        [UsedImplicitly]
        private void OnServerInitialized()
        {
            Puts("Server init!");
            //game = InitGame();
            game = new Game();
            gateWay = new GateWay();
            chatMenu = new ChatMenu();
            game.Save();
        }

        private Game InitGame()
        {
            return Interface.Oxide.DataFileSystem.ExistsDatafile("RustTribalGameData")
                ? Interface.Oxide.DataFileSystem.ReadObject<Game>("RustTribalGameData")
                : new Game();
        }

        /// <summary>
        /// Called upon the server saving.
        /// Saves the game object to disk.
        /// </summary>
        [UsedImplicitly]
        private void OnServerSave()
        {
            game.Save();
        }

        /// <summary>
        /// Called upon the plugin being unloaded.
        /// Saves the game object to disk.
        /// </summary>
        [UsedImplicitly]
        private void Unload()
        {
            game.Save();
        }

        #endregion Oxide Hooks

        #region Oxide Chat Hooks

        [ChatCommand("help")]
        [UsedImplicitly]
        private void HelpChatCommand(BasePlayer player, string command, string[] args)
        {
            chatMenu.HelpCommand(player, command, args, game);
        }

        [ChatCommand("tribe")]
        [UsedImplicitly]
        private void TribeChatCommand(BasePlayer player, string command, string[] args)
        {
            chatMenu.TribeCommand(player, command, args, game);
        }

        #endregion Oxide Chat Hooks

        #region Chat Menu Class

        public class ChatMenu : RustTribal
        {
            #region Chat Menu Members

            #endregion Chat Menu Members

            #region Chat Menu Constructors

            #endregion Chat Menu Constructors

            #region Chat Menu Methods

            public void HelpCommand(BasePlayer player, string command, string[] args, Game game)
            {
                var message = "Available commands: \n"
                    + "help\t\t\t Display help.\n"
                    + "tribe\t\t\t Show tribe information.";

                SendReply(player, message);
            }

            public void TribeCommand(BasePlayer player, string command, string[] args, Game game)
            {
                var helpMessage = "Available commands: \n"
                    + "tribe -help\t\t\t Displays tribe related options.\n"
                    + "tribe -name\t\t\t Display name of your tribe.";

                if (!args.Any())
                {
                    SendReply(player, helpMessage);
                    return;
                }

                switch (args[0])
                {
                    case "-help":
                        {
                            SendReply(player, helpMessage);
                            break;
                        }
                    case "-name":
                        {
                            var tribeName = game.FindTribeNameByPersonId(player.userID);
                            var nameMessage = $"Tribe Name: {tribeName}";
                            SendReply(player, nameMessage);
                            break;
                        }

                    default:
                        {
                            break;
                        }

                }
            }

            #endregion Chat Menu Methods
        }

        #endregion Chat Menu Class

        #region Game Class

        public class Game
        {
            #region Game Members

            /// <summary>
            /// Identifier for the Rust Tribal Game Instance
            /// </summary>
            private Guid gameId;

            private World world;

            #endregion Game Members

            #region Game Properties

            public bool IsWorldPopulating => world.IsWorldPopulating;

            #endregion

            #region Game Constructors

            public Game()
            {
                gameId = Guid.NewGuid();
                world = new World();
            }

            #endregion Game Constructors

            #region Game Methods

            /// <summary>
            /// Writes the Game object to disk.
            /// </summary>
            public void Save() => Interface.Oxide.DataFileSystem.WriteObject("RustTribalGameData", this);

            #endregion

            public bool IsPlayerCorrectGender(Person.PlayerGender gender)
            {
                if (!world.IsWorldPopulating)
                {
                    return false;
                }

                var tribe = world.FindPopulatingTribe();

                return (gender == Person.PlayerGender.Female && tribe.IsFemalesPopulating)
                       || (gender == Person.PlayerGender.Male && tribe.IsMalesPopulating);
            }

            public bool IsPlayerKnown(ulong id)
            {
                var person = world.FindPersonById(id);
                return person != null;
            }

            public bool IsBirthPlaceAvailable()
            {
                throw new NotImplementedException();
            }

            public bool IsPlayerAlive(ulong id)
            {
                var player = world.FindPersonById(id);
                return (player != null) && player.IsAlive;
            }

            public void IncomingPlayer(ulong userId, string userName)
            {
                //The player should already be added if they are alive
                if (!IsPlayerAlive(userId))
                {
                    world.AddNewPerson(userId, userName);
                }
            }

            public string FindTribeNameByPersonId(ulong userId)
            {
                var name = world.FindTribeNameByPersonId(userId);
                return name == string.Empty ? "None" : name;
            }
        }

        #endregion Game Class

        #region World Class

        public class World : RustTribal
        {
            private Queue<int> birthPlaces;

            //Todo: Config Value
            private const int MaxInitialTribes = 2;

            //Todo: Config Value
            private const int MaxServerPopulation = 50;

            public bool IsWorldPopulating => tribes.Any(x => x.IsTribePopulating);

            private int ServerPopulationLimit => birthPlaces.Count() +
                persons.Count(x => { return new RustPlayerManager().Connected.Any(r => r.Id == x.Id.ToString()); });

            private List<Person> persons;

            private List<Tribe> tribes;


            public World()
            {
                persons = new List<Person>();
                tribes = new List<Tribe>();

                AddNewTribe("Alpha");
                AddNewTribe("Bravo");
            }

            public Person FindPersonById(ulong id) => persons.FirstOrDefault(x => x.Id == id);

            public Tribe FindPopulatingTribe() => tribes.FirstOrDefault(x => x.IsTribePopulating);

            public void AddNewPerson(ulong userId, string userName)
            {
                var newPerson = new Person(userId);
                persons.Add(newPerson);
                //Todo: Add logging here
                Puts($"Adding member to {FindPopulatingTribe().TribeName}");
                FindPopulatingTribe().AddNewMember(newPerson);
            }

            public void AddNewTribe(string newTribeName)
            {
                var newTribe = new Tribe(newTribeName);
                tribes.Add(newTribe);
            }

            public string FindTribeNameByPersonId(ulong userId) => tribes
                .Where(x => x.IsPersonInTribe(userId))
                .Select(r => r.TribeName)
                .FirstOrDefault();

        }

        #endregion World Class

        #region GateWay Class

        public class GateWay : RustTribal
        {
            /// <summary>
            /// Determines if a client may connect or not
            /// </summary>
            /// <param name="packet">The client connection packet</param>
            /// <param name="game">The Rust Tribal Game object</param>
            /// <returns>Returns true if authorized, false if rejected</returns>
            public AuthMessage IsClientAuthorized(Connection packet, Game game)
            {
                AuthMessage authMessage = null;
                var id = packet.userid;

                if (game.IsPlayerKnown(id) && game.IsPlayerAlive(id))
                {
                    Puts("Player known and player alive");
                    authMessage = new AuthMessage(
                        AuthMessage.ResponseType.Accepted,
                        "Welcome back to Rust Tribal.");
                }
                else if (game.IsWorldPopulating)
                {
                    Puts("World is populating");
                    var gender = GetGenderByUserId(id);
                    if (game.IsPlayerCorrectGender(gender))
                    {
                        Puts("Passed gender checks");
                        authMessage = new AuthMessage(
                            AuthMessage.ResponseType.Accepted,
                            "Welcome to Rust Tribal");
                    }
                    else
                    {
                        var oppositeGender =
                            gender == Person.PlayerGender.Female
                            ? "male"
                            : "female";
                        var message = $"Your character's gender is {gender}."
                                      + $"The game world is still populating and requires"
                                      + $"more {oppositeGender} players";

                        authMessage = new AuthMessage(AuthMessage.ResponseType.Rejected, message);
                    }
                }
                else if (game.IsBirthPlaceAvailable())
                {
                    authMessage = new AuthMessage(
                        AuthMessage.ResponseType.Accepted,
                        "Welcome to Rust Tribal.");
                }
                else
                {
                    authMessage = new AuthMessage(
                        AuthMessage.ResponseType.Rejected,
                        "There are currently no spawn points available.\n Visit RustTribal.com to join the queue.");
                }

                return authMessage;
            }


            private Person.PlayerGender GetGenderByUserId(ulong userId)
            {
                var isFemale = !(GetRandomFloatBasedOnUserId(userId, 0x10ecL) <= .5f);
                return isFemale ? Person.PlayerGender.Female : Person.PlayerGender.Male;
            }
        }

        #endregion GateWay Class

        #region Tribe Class

        public class Tribe
        {
            //Todo: Config Value
            private const int maxInitialTribalMembers = 4;

            //Todo: Config Value
            private const int maxInitialMales = 2;

            //Todo: Config Value
            private const int maxInitialFemales = 2;

            public bool IsTribePopulating => (NumMales < maxInitialMales) && (NumFemales < maxInitialFemales);

            public bool IsMalesPopulating => (NumMales < maxInitialMales);

            public bool IsFemalesPopulating => (NumFemales < maxInitialFemales);

            private int NumMales => members.Count(x => x.Gender == Person.PlayerGender.Male);

            private int NumFemales => members.Count(x => x.Gender == Person.PlayerGender.Female);

            public string TribeName { get; private set; }

            private List<Person> members;

            public List<Person> Members
            {
                get
                {
                    return members;
                }
            }

            public Tribe(string newTribeName)
            {
                members = new List<Person>();
                TribeName = newTribeName;
            }

            public void AddNewMember(Person newMember)
            {
                members.Add(newMember);
            }

            public bool IsPersonInTribe(ulong userId) => members.Any(x => x.Id == userId);
        }

        #endregion Tribe Class

        #region Person Class

        public class Person
        {
            private Demeanor demeanor;

            //Todo: Set Gender
            public PlayerGender Gender
            {
                get
                {
                    var bPlayer = (BasePlayer)new RustPlayerManager().FindPlayerById(Id.ToString()).Object;
                    return bPlayer.playerModel.IsFemale ? PlayerGender.Female : PlayerGender.Male;
                }
            }

            public bool IsAlive { get; private set; }

            public ulong Id { get; private set; }

            //Todo: Needs testing to understand functionality

            public Person(ulong userId)
            {
                Id = userId;
            }

            private enum Demeanor
            {
                Psychotic,
                Troubled ,
                Neutral,
                Warm,
                Friendly
            }

            public enum PlayerGender
            {
                Male,
                Female
            }
        }

        #endregion Person Class

        #region Auth Message

        public class AuthMessage
        {
            public ResponseType Response { get; private set; }
            public string Reason { get; private set; }

            public enum ResponseType
            {
                Accepted,
                Rejected
            }

            public AuthMessage(ResponseType type, string reason)
            {
                Response = type;
                Reason = reason;
            }

            public string GetMessage() => $"{Response}: {Reason}.";
        }

        #endregion
    }

}