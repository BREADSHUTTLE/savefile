using Cysharp.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;


namespace CAPYBARA.Editor
{

    public class CAPYBARASheetAPI
    {
        static private string spreadSheetId = "1PFDI4S7iSLrqVCaRE7C8lVg0gk4t7piNRxqYZkeJ1rE";
        private const string jsonPath = "./Assets/13.CapyBara/StaticData/Editor/cloudsheet-468203-628891190789.json";

        private readonly string _appName;
        private readonly ServiceAccountCredential _credential;
        private readonly SheetsService _service;


        public static async UniTask<UserCredential> Authorize(params string[] scopes)
        {
            UserCredential credential;
            using var stream =
                new FileStream(
                    jsonPath,
                    FileMode.Open, FileAccess.Read);
            string credPath = "googleCredentialToken";

            GoogleClientSecrets gcs = await GoogleClientSecrets.FromStreamAsync(stream);
            credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                gcs.Secrets,
                scopes,
                "user",
                CancellationToken.None,
                new FileDataStore(credPath, true));
            Debug.Log("Credential file saved to: " + credPath);
            return credential;
        }

        public CAPYBARASheetAPI(string appName)
        {
            _appName = appName;
            using var stream =
                  new FileStream(
                      jsonPath,
                      FileMode.Open, FileAccess.Read);

            _credential = ServiceAccountCredential.FromServiceAccountData(stream);

            _service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = _credential,
                ApplicationName = _appName
            });
        }

        public async UniTask Update(string sheetName, IList<IList<object>> bodyValue)
        {
            ValueRange body = new ValueRange();
            body.Range = $"{sheetName}";


            var sheetNames = await GetSheetNames();
            if (!sheetNames.Contains(sheetName))
            {
                var addSheetRequest = new AddSheetRequest();
                addSheetRequest.Properties = new SheetProperties();
                addSheetRequest.Properties.Title = sheetName;
                BatchUpdateSpreadsheetRequest batchUpdateSpreadsheetRequest = new BatchUpdateSpreadsheetRequest();
                batchUpdateSpreadsheetRequest.Requests = new List<Request>();
                batchUpdateSpreadsheetRequest.Requests.Add(new Request
                {
                    AddSheet = addSheetRequest
                });
                var batchUpdateRequest =
                    _service.Spreadsheets.BatchUpdate(batchUpdateSpreadsheetRequest, spreadSheetId);

                await batchUpdateRequest.ExecuteAsync();
            }
            body.MajorDimension = SpreadsheetsResource.ValuesResource.GetRequest.MajorDimensionEnum.ROWS.ToString();
            body.Values = bodyValue;
            SpreadsheetsResource.ValuesResource.UpdateRequest request = _service.Spreadsheets.Values.Update(body, spreadSheetId, body.Range);
            request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            await request.ExecuteAsync();
        }

        public async UniTask<IList<IList<object>>> Read(string sheetName)
        {
            SpreadsheetsResource.ValuesResource.GetRequest request = _service.Spreadsheets.Values.Get(spreadSheetId, sheetName);

            ValueRange response = request.Execute();
            IList<IList<System.Object>> values = response.Values;
            return values;
        }

        async UniTask<List<string>> GetSheetNames()
        {
            bool includeGridData = false;
            List<string> names = new List<string>();
            SpreadsheetsResource.GetRequest request = _service.Spreadsheets.Get(spreadSheetId);
            request.IncludeGridData = includeGridData;

            Spreadsheet response = await request.ExecuteAsync();
            foreach (var sheet in response.Sheets)
            {
                names.Add(sheet.Properties.Title);
            }

            return names;
        }
    }

}
