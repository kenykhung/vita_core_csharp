﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Htc.Vita.Core.Interop;
using Htc.Vita.Core.Log;

namespace Htc.Vita.Core.Runtime
{
    public class UserManager
    {
        private static readonly Logger Log = Logger.GetInstance(typeof(UserManager));

        public static string GetFirstActiveUser(string serverName = null)
        {
            var windowsUsers = GetWindowsUsers(serverName);
            return (from windowsUser
                    in windowsUsers
                    where windowsUser.State == Windows.Wtsapi32.WindowsTerminalServiceConnectStateClass.Active
                    select string.Format($"{windowsUser.Domain}\\{windowsUser.Username}")
            ).FirstOrDefault();
        }

        internal static string GetWindowsUsernameBySid(string userSid, string serverName = null)
        {
            if (string.IsNullOrWhiteSpace(userSid))
            {
                return null;
            }

            string result = null;
            var userSidPtr = IntPtr.Zero;
            var username = new StringBuilder();
            uint usernameLength = 0;
            var domain = new StringBuilder();
            uint domainLength = 0;
            try
            {
                var success = Windows.Advapi32.ConvertStringSidToSidW(
                    userSid,
                    userSidPtr
                );

                if (success)
                {
                    var sidType = Windows.Advapi32.SidType.Unknown;
                    success = Windows.Advapi32.LookupAccountSidW(
                        serverName,
                        userSidPtr,
                        username,
                        ref usernameLength,
                        domain,
                        ref domainLength,
                        ref sidType
                    );

                    if (success)
                    {
                        if (sidType == Windows.Advapi32.SidType.User)
                        {
                            result = string.Format($"{domain}\\{username}");
                        }
                        else
                        {
                            Log.Warn("Can not translate sid type " + sidType + " to username.");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Can not get Windows username: " + e.Message);
            }

            return result;
        }

        internal static List<WindowsUserInfo> GetWindowsUsers(string serverName = null)
        {
            if (string.IsNullOrWhiteSpace(serverName))
            {
                serverName = Environment.MachineName;
            }

            var results = new List<WindowsUserInfo>();
            var serverHandle = Windows.Wtsapi32.WTSOpenServerW(serverName);

            try
            {
                var sessionInfoPtr = IntPtr.Zero;
                var sessionCount = 0U;
                var success = Windows.Wtsapi32.WTSEnumerateSessionsW(
                        serverHandle,
                        0,
                        1,
                        ref sessionInfoPtr,
                        ref sessionCount
                );
                var dataSize = Marshal.SizeOf(typeof(Windows.Wtsapi32.WTS_SESSION_INFO));
                var currentSessionInfoPtr = sessionInfoPtr;

                if (success)
                {
                    for (var sessionIndex = 0U; sessionIndex < sessionCount; sessionIndex++)
                    {
                        var sessionInfo = (Windows.Wtsapi32.WTS_SESSION_INFO)Marshal.PtrToStructure(
                                currentSessionInfoPtr,
                                typeof(Windows.Wtsapi32.WTS_SESSION_INFO)
                        );
                        currentSessionInfoPtr += dataSize;

                        bool ret = false;
                        uint bytes = 0;
                        var usernamePtr = IntPtr.Zero;
                        ret = Windows.Wtsapi32.WTSQuerySessionInformationW(
                                serverHandle,
                                sessionInfo.sessionId,
                                Windows.Wtsapi32.WindowsTerminalServiceInfoClass.UserName,
                                ref usernamePtr,
                                ref bytes
                        );
                        if (ret == false)
                        {
                            continue;
                        }

                        string username = Marshal.PtrToStringUni(usernamePtr);
                        Windows.Wtsapi32.WTSFreeMemory(usernamePtr);

                        var domainPtr = IntPtr.Zero;
                        ret = Windows.Wtsapi32.WTSQuerySessionInformationW(
                                serverHandle,
                                sessionInfo.sessionId,
                                Windows.Wtsapi32.WindowsTerminalServiceInfoClass.DomainName,
                                ref domainPtr,
                                ref bytes
                        );
                        if (ret == false)
                        {
                            continue;
                        }

                        string domain = Marshal.PtrToStringUni(domainPtr);
                        Windows.Wtsapi32.WTSFreeMemory(domainPtr);

                        var userInfo = new WindowsUserInfo
                        {
                            State = sessionInfo.state,
                            Domain = domain,
                            Username = username
                        };
                        results.Add(userInfo);
                    }
                    Windows.Wtsapi32.WTSFreeMemory(sessionInfoPtr);
                }
            }
            catch (Exception e)
            {
                Log.Error("Can not get Windows user list: " + e.Message);
            }

            if (serverHandle != Windows.Wtsapi32.WTS_CURRENT_SERVER_HANDLE)
            {
                Windows.Wtsapi32.WTSCloseServer(serverHandle);
            }
            return results;
        }

        internal class WindowsUserInfo
        {
            public Windows.Wtsapi32.WindowsTerminalServiceConnectStateClass State { get; set; }
            public string Domain { get; set; }
            public string Username { get; set; }
        }
    }
}