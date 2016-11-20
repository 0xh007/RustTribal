
namespace Oxide.Plugins
{
    using System;
    using Oxide.Core.Plugins;
    using UnityEngine;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    using JetBrains.Annotations;
    using Core;

    using Network;

    using Oxide.Game.Rust.Libraries.Covalence;

    using ProtoBuf;

    using Rust;

    using UnityEngine.Analytics;

    [Info("RustTribal", "*Vic", 0.1)]
    [Description("Rust Tribal")]
    public class RustTribal : RustPlugin
    {
        #region Oxide Members

        private Game game;
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
            game = InitGame();
            gateWay = new GateWay();
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

            public bool IsPlayerKnown(ulong id)
            {
                world.FindPersonById(id);
                return false;
            }

            public bool IsBirthPlaceAvailable()
            {
                throw new NotImplementedException();
            }

            public bool IsPlayerAlive(ulong id)
            {
                var isAlive = false;
                var player = world.FindPersonById(id);
                if ((player != null) && player.IsAlive)
                {
                    isAlive = true;
                }

                return isAlive;
            }

            public void IncomingPlayer(ulong userId, string userName)
            {
                if (!IsPlayerAlive(userId))
                {
                    world.AddNewPerson(userId, userName);
                }
            }
        }

        #endregion Game Class

        #region World Class

        public class World
        {
            private const int MaxInitialTribes = 2;

            private bool isWorldPopulating;

            private List<Person> persons;

            private List<Tribe> tribes;


            public World()
            {
                isWorldPopulating = true;
                persons = new List<Person>();
                tribes = new List<Tribe>();

                AddNewTribe("Alpha");
                AddNewTribe("Bravo");
            }

            public Person FindPersonById(ulong id) => persons.FirstOrDefault(x => x.RPlayer.Id == id.ToString());

            public Tribe FindPopulatingTribe() => tribes.FirstOrDefault(x => x.IsTribePopulating);

            public void AddNewPerson(ulong userId, string userName)
            {
                var newPerson = new Person(userId, userName);
                persons.Add(newPerson);
            }

            private void AddNewTribalMember(Person newPerson)
            {
                throw new NotImplementedException();
            }

            public void AddNewTribe(string newTribeName)
            {
                var newTribe = new Tribe(newTribeName);
                tribes.Add(newTribe);
            }
        }

        #endregion World Class

        #region GateWay Class

        public class GateWay
        {
            /// <summary>
            /// Determines if a client may connect or not
            /// </summary>
            /// <param name="packet">The client connection packet</param>
            /// <param name="game">The Rust Tribal Game object</param>
            /// <returns>Returns true if authorized, false if rejected</returns>
            public AuthMessage IsClientAuthorized(Connection packet, Game game)
            {
                AuthMessage authMessage;
                var id = packet.userid;

                if (game.IsPlayerKnown(id) && game.IsPlayerAlive(id))
                {
                    authMessage = new AuthMessage(
                        AuthMessage.ResponseType.Accepted,
                        "Welcome back to Rust Tribal.");
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
        }

        #endregion GateWay Class

        #region Tribe Class

        public class Tribe
        {
            private const int maxInitialTribalMembers = 4;

            private const int maxInitialMales = 2;

            private const int maxInitialFemales = 2;

            public bool IsTribePopulating => (NumMales < maxInitialMales) && (NumFemales < maxInitialFemales);

            public bool IsMalesPopulating => (NumMales < maxInitialMales);

            public bool IsFemalesPopulating => (NumFemales < maxInitialFemales);

            private int NumMales => members.Count(x => x.Gender == Person.PlayerGender.Male);

            private int NumFemales => members.Count(x => x.Gender == Person.PlayerGender.Female);


            private string tribeName;

            private List<Person> members;

            public Tribe(string newTribeName)
            {
                tribeName = newTribeName;
            }
        }

        #endregion Tribe Class

        #region Person Class


        public class Person
        {
            private Demeanor demeanor;

            //Todo: Set Gender
            public PlayerGender Gender { get; private set; }

            public bool IsAlive { get; private set; }


            //Todo: Needs testing to understand functionality
            public RustPlayer RPlayer { get; private set; }

            public Person(ulong userId, string userName)
            {
                RPlayer = new RustPlayer(userId, userName);
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
