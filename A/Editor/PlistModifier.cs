using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

public class PlistModifier
{
    private const string GOOGLE_SERVICE_INFO_PATH = "Assets/GoogleService-Info.plist";
    
    [PostProcessBuild(100)]
    public static void OnPostProcessBuild(BuildTarget buildTarget, string pathToBuiltProject)
    {
        if (buildTarget != BuildTarget.iOS)
            return;

        string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
        PlistDocument plist = new PlistDocument();
        plist.ReadFromFile(plistPath);

        PlistElementDict rootDict = plist.root;

        //var atsDict = rootDict.CreateDict("NSAppTransportSecurity");
        //var exceptionDomains = atsDict.CreateDict("NSExceptionDomains");

        var atsDict = rootDict.CreateDict("NSAppTransportSecurity");
        atsDict.SetBoolean("NSAllowsArbitraryLoads", true);

        // 공통 설정 함수
        void AddExceptionDomain(PlistElementDict parent, string domain)
        {
            var domainDict = parent.CreateDict(domain);
            domainDict.SetBoolean("NSIncludesSubdomains", true);
            domainDict.SetBoolean("NSTemporaryExceptionAllowsInsecureHTTPLoads", true);
            domainDict.SetString("NSTemporaryExceptionMinimumTLSVersion", "TLSv1.1");
        }

        // 예외 도메인 추가
        //AddExceptionDomain(exceptionDomains, "login.dev.atozgames.net");
        //AddExceptionDomain(exceptionDomains, "login.atozgames.net");
        //AddExceptionDomain(exceptionDomains, "www.atozgames.net");

        AddGoogleSignInUrlScheme(rootDict);

        plist.WriteToFile(plistPath);
    }

    private static string GetReversedClientId()
    {
        if (!File.Exists(GOOGLE_SERVICE_INFO_PATH))
        {
            UnityEngine.Debug.LogError($"GoogleService-Info.plist를 찾을 수 없습니다: {GOOGLE_SERVICE_INFO_PATH}");
            return null;
        }
        
        var googlePlist = new PlistDocument();
        googlePlist.ReadFromFile(GOOGLE_SERVICE_INFO_PATH);
        
        var reversedClientId = googlePlist.root["REVERSED_CLIENT_ID"];
        if (reversedClientId == null)
        {
            UnityEngine.Debug.LogError("GoogleService-Info.plist에 REVERSED_CLIENT_ID가 없습니다.");
            return null;
        }
        
        return reversedClientId.AsString();
    }

    private static void AddGoogleSignInUrlScheme(PlistElementDict rootDict)
    {
        string reversedClientId = GetReversedClientId();
        if (string.IsNullOrEmpty(reversedClientId))
        {
            UnityEngine.Debug.LogError("Google Sign-In URL Scheme 추가 실패: REVERSED_CLIENT_ID를 가져올 수 없습니다.");
            return;
        }

        PlistElementArray urlTypesArray;
        if (rootDict["CFBundleURLTypes"] != null)
            urlTypesArray = rootDict["CFBundleURLTypes"].AsArray();
        else
            urlTypesArray = rootDict.CreateArray("CFBundleURLTypes");

        bool googleSchemeExists = false;
        foreach (var urlType in urlTypesArray.values)
        {
            var urlTypeDict = urlType.AsDict();
            if (urlTypeDict != null && urlTypeDict["CFBundleURLSchemes"] != null)
            {
                var schemes = urlTypeDict["CFBundleURLSchemes"].AsArray();
                foreach (var scheme in schemes.values)
                {
                    if (scheme.AsString() == reversedClientId)
                    {
                        googleSchemeExists = true;
                        break;
                    }
                }
            }
            if (googleSchemeExists)
                break;
        }

        if (!googleSchemeExists)
        {
            var googleUrlType = urlTypesArray.AddDict();
            googleUrlType.SetString("CFBundleTypeRole", "Editor");
            googleUrlType.SetString("CFBundleURLName", "Google Sign-In");
            var googleSchemes = googleUrlType.CreateArray("CFBundleURLSchemes");
            googleSchemes.AddString(reversedClientId);
            
            UnityEngine.Debug.Log("Google Sign-In URL Scheme 추가 완료: " + reversedClientId);
        }
        else
        {
            UnityEngine.Debug.Log("Google Sign-In URL Scheme이 이미 존재합니다: " + reversedClientId);
        }
    }
}
