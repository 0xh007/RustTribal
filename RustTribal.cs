
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
            //Todo: Make login checks
            return null; // if client is allowed to login
        }

        [UsedImplicitly]
        private void Loaded()
        {
            Puts("Plugin loaded");
        }

        [UsedImplicitly]
        private void OnPlayerInit(BasePlayer player)
        {
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
            public object isPlayerAlive;

            #region Game Members

            private World world;

            #endregion Game Members

            #region Game Constructors

            public Game()
            {
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
                throw new NotImplementedException();
            }
        }

        #endregion Game Class

        #region World Class

        public class World
        {
            private List<Person> people;


            public World()
            {
            }

            public Person FindPersonById(ulong id)
            {
                var person = people.FirstOrDefault(x => x.Player.userid == id);
                return person;
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
            public bool IsClientAuthorized(Connection packet, Game game)
            {
                var id = packet.userid;
                return (game.IsPlayerKnown(id) && game.IsPlayerAlive(id))
                || game.IsBirthPlaceAvailable();
            }
        }

        #endregion GateWay Class

        #region Tribe Class

        public class Tribe
        {
            private string tribeName;

            private List<Person> members;


            public Tribe()
            {
            }

        }

        #endregion Tribe Class

        #region Person Class


        public class Person
        {
            private Demeanor demeanor;

            public BasePlayer Player { get; private set; }

            public Person()
            {
            }

            private enum Demeanor
            {
                Psychotic,
                Troubled ,
                Neutral,
                Warm,
                Friendly
            }
        }

        #endregion Person Class
    }

}
