using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Atlasd.Battlenet.Protocols.MCP.Models
{
    class Realm
    {
        private ConcurrentDictionary<string, ConcurrentDictionary<string, Character>> _characters;
        //public ConcurrentDictionary<string, Game> Games { get; private set; }

        public Realm()
        {
            _characters = new ConcurrentDictionary<string, ConcurrentDictionary<string, Character>>();
        }

        public ConcurrentDictionary<string, Character> GetCharacters(string username)
        {
            ensureCharacters(username.ToLower());
            return getCharacters(username.ToLower());
        }

        public void AddCharacter(string username, string name, Character character)
        {
            var characters = GetCharacters(username.ToLower());
            characters.TryAdd(name.ToLower(), character);
        }

        public Character GetCharacter(string username, string name)
        {
            var characters = GetCharacters(username.ToLower());
            Character character;
            characters.TryGetValue(name.ToLower(), out character);
            return character;
        }

        private ConcurrentDictionary<string, Character> getCharacters(string username)
        {
            ConcurrentDictionary<string, Character> characters;
            _characters.TryGetValue(username.ToLower(), out characters);

            return characters;
        }

        private void ensureCharacters(string username)
        {
            var characters = getCharacters(username.ToLower());

            if (characters == null)
            {
                _characters.TryAdd(username.ToLower(), new ConcurrentDictionary<string, Character>());
            }
        }
    }
}
