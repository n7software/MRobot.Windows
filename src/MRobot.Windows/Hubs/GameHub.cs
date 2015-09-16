using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;

namespace MRobot.Windows.Hubs
{
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using Data;

    public class GameHub : BaseHub
    {
        #region Constructor
        public GameHub(HubConnection connection)
            : base("GameHub", connection)
        {
            HubProxy.On<Game>("gameCreated", game => GameCreated(game));
            HubProxy.On<Game>("gameUpdated", game => GameUpdated(game));
            HubProxy.On<Turn>("turnCreated", turn => TurnCreated(turn));
            HubProxy.On<Turn>("turnUpdated", turn => TurnUpdated(turn));
            HubProxy.On<Turn>("turnDeleted", turn => TurnDeleted(turn));
            HubProxy.On<int>("userJoinedGame", gameId => UserJoinedGame(gameId));
            HubProxy.On<int>("userLeftGame", gameId => UserLeftGame(gameId));
        } 
        #endregion

        #region Events

        public Action<Game> GameCreated = _ => { };
        public Action<Game> GameUpdated = _ => { };
        public Action<Turn> TurnCreated = _ => { };
        public Action<Turn> TurnUpdated = _ => { };
        public Action<Turn> TurnDeleted = _ => { };
        public Action<int> UserJoinedGame = _ => { };
        public Action<int> UserLeftGame = _ => { };

        #endregion

        #region Public Methods

        public Task<IEnumerable<Game>> GetUsersGames()
        {
            return HubProxy.Invoke<IEnumerable<Game>>("GetUsersGames", false);
        }

        public Task<Game> GetGame(int gameId)
        {
            return HubProxy.Invoke<Game>("GetGame", gameId);
        }

        public Task<Turn> GetCurrentTurn(int gameId)
        {
            return HubProxy.Invoke<Turn>("GetCurrentTurn", gameId);
        }

        public Task<Turn> GetLastTurn(int gameId)
        {
            return HubProxy.Invoke<Turn>("GetLastTurn", gameId);
        }

        public Task<IEnumerable<GameSlot>> GetGameSlots(int gameId)
        {
            return HubProxy.Invoke<IEnumerable<GameSlot>>("GetGameSlots", gameId);
        }

        public async Task<SaveUploadResult> UploadSave(Stream saveStream)
        {
            var client = CreateHttpClient();
            client.DefaultRequestHeaders.ExpectContinue = false;

            var content = new MultipartFormDataContent(string.Format("Upload----{0}", DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)));
            content.Add(new StringContent(App.LocalSettings.AuthenticationKey), "authKey");
            content.Add(new StringContent("2"), "source");
            content.Add(new StreamContent(saveStream), "file", "Upload.Civ5Save");

            var response = await client.PutAsync("Game/UploadSave", content);
            response.EnsureSuccessStatusCode();
            SaveUploadResult result = await response.Content.ReadAsAsync<SaveUploadResult>();

            saveStream.Close();
            saveStream.Dispose();

            return result;
        }

        #endregion
    }
}
