using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using AppleAuth;
using AppleAuth.Native;
using AppleAuth.Enums;
using AppleAuth.Interfaces;
using System.Text;
using AppleAuth.Extensions;
using Cysharp.Threading.Tasks;


public struct AppleSignInResult
{
    public string UserId;
    public string Email; // 첫 로그인에만 전달
    public string FullName; // 첫 로그인에만 전달
    public string IdentityToken;
    public string AuthorizationCode;
}

public class AppleLoginController : MonoBehaviour
{
    private IAppleAuthManager appleAuthManager;

    private string AppleUserIdKey = "AppleId";

    public void Init()
    {
        // If the current platform is supported
        if (AppleAuthManager.IsCurrentPlatformSupported)
        {
            // Creates a default JSON deserializer, to transform JSON Native responses to C# instances
            var deserializer = new PayloadDeserializer();
            // Creates an Apple Authentication manager with the deserializer
            this.appleAuthManager = new AppleAuthManager(deserializer);
        }
    }

    void Update()
    {
        // Updates the AppleAuthManager instance to execute
        // pending callbacks inside Unity's execution loop
        if (this.appleAuthManager != null)
        {
            this.appleAuthManager.Update();
        }
        // /
    }

    public UniTask<AppleSignInResult> SignInAsync()
        => SignInAsync(default);

    public UniTask<AppleSignInResult> SignInAsync(System.Threading.CancellationToken ct)
    {
        var tcs = new UniTaskCompletionSource<AppleSignInResult>();

        // 취소 요청 시 콜백 해제
        if (ct.CanBeCanceled)
        {
            ct.Register(() => tcs.TrySetCanceled(ct));
        }

        var loginArgs = new AppleAuthLoginArgs(LoginOptions.IncludeEmail | LoginOptions.IncludeFullName);

        appleAuthManager.LoginWithAppleId(
            loginArgs,
            credential =>
            {
                try
                {
                    if (credential is IAppleIDCredential appleIdCredential)
                    {
                        var userId = appleIdCredential.User;
                        PlayerPrefs.SetString(AppleUserIdKey, userId);

                        var email = appleIdCredential.Email; 
                        var fullName = appleIdCredential.FullName?.ToString();

                        var identityToken = appleIdCredential.IdentityToken != null
                            ? Encoding.UTF8.GetString(appleIdCredential.IdentityToken, 0,
                                appleIdCredential.IdentityToken.Length)
                            : null;

                        var authorizationCode = appleIdCredential.AuthorizationCode != null
                            ? Encoding.UTF8.GetString(appleIdCredential.AuthorizationCode, 0,
                                appleIdCredential.AuthorizationCode.Length)
                            : null;

                        var result = new AppleSignInResult
                        {
                            UserId = userId,
                            Email = email,
                            FullName = fullName,
                            IdentityToken = identityToken,
                            AuthorizationCode = authorizationCode
                        };

                        tcs.TrySetResult(result);
                    }
                    else
                    {
                        tcs.TrySetException(new Exception("Invalid credential type (not IAppleIDCredential)."));
                    }
                }
                catch (Exception e)
                {
                    tcs.TrySetException(e);
                }
            },
            error =>
            {
                // 필요시 error.GetAuthorizationErrorCode() 문자열화
                tcs.TrySetException(new Exception($"Apple Sign-In failed: {error.GetAuthorizationErrorCode()}"));
            });

        return tcs.Task;
    }

    public void SignIn(Action callback)
    {
        var loginArgs = new AppleAuthLoginArgs(LoginOptions.IncludeEmail | LoginOptions.IncludeFullName);

        this.appleAuthManager.LoginWithAppleId(
            loginArgs,
            credential =>
            {
                // Obtained credential, cast it to IAppleIDCredential
                var appleIdCredential = credential as IAppleIDCredential;
                if (appleIdCredential != null)
                {
                    // Apple User ID
                    // You should save the user ID somewhere in the device
                    var userId = appleIdCredential.User;
                    PlayerPrefs.SetString(AppleUserIdKey, userId);

                    // Email (Received ONLY in the first login)
                    var email = appleIdCredential.Email;

                    // Full name (Received ONLY in the first login)
                    var fullName = appleIdCredential.FullName;


                    // Identity token
                    var identityToken = Encoding.UTF8.GetString(
                        appleIdCredential.IdentityToken,
                        0,
                        appleIdCredential.IdentityToken.Length);

                    // Authorization code
                    var authorizationCode = Encoding.UTF8.GetString(
                        appleIdCredential.AuthorizationCode,
                        0,
                        appleIdCredential.AuthorizationCode.Length);

                    callback?.Invoke();
                    // And now you have all the information to create/login a user in your system
                }
            },
            error =>
            {
                // Something went wrong
                var authorizationErrorCode = error.GetAuthorizationErrorCode();
            });
    }
}