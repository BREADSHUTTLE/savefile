/// Constants.cs
/// Author : KimJuHee

namespace DFramework.Common
{
    public static class Constants
    {
        // Scene
        public static readonly string SCENE_INTRO = "Intro";
        public static readonly string SCENE_LOADING = "Loading";
        public static readonly string SCENE_LOBBY = "Lobby";
        public static readonly string SCENE_GAME = "Game";

        // Tag
        public static readonly string GAME_MY_FLOOR_TAG = "my_floor";
        public static readonly string GAME_ENEMY_FLOOR_TAG = "enemy_floor";
        public static readonly string GAME_MY_PLAYER_TAG = "my_player";
        public static readonly string GAME_ENEMY_PLAYER_TAG = "enemy_player";

        // Character
        //public static readonly string CHARACTER_STATE_ENTER = "Enter";
        public static readonly string CHARACTER_STATE_SPAWN = "Spawn";
        public static readonly string CHARACTER_STATE_RUN = "Run";
        public static readonly string CHARACTER_STATE_ATTACK = "Attack";
        public static readonly string CHARACTER_STATE_DEATH = "Death";
    }
}