﻿using Innovator.Client;
using InnovatorAdmin.Connections;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace InnovatorAdmin
{
  public static class ConnectionDataExtensions
  {
    public static string ScalcMD5(string val)
    {
      string result;
      using (var mD5CryptoServiceProvider = new MD5CryptoServiceProvider())
      {
        var aSCIIEncoding = new ASCIIEncoding();
        var bytes = aSCIIEncoding.GetBytes(val);
        string text = "";
        var array = mD5CryptoServiceProvider.ComputeHash(bytes);
        short num = 0;
        while ((int)num < array.GetLength(0))
        {
          string text2 = Convert.ToString(array[(int)num], 16).ToLowerInvariant();
          if (text2.Length == 1)
          {
            text2 = "0" + text2;
          }
          text += text2;
          num += 1;
        }
        result = text;
      }
      return result;
    }
    public static IAsyncConnection ArasLogin(this ConnectionData credentials)
    {
      return ArasLogin(credentials, false).Value;
    }
    public static IPromise<IAsyncConnection> ArasLogin(this ConnectionData credentials, bool async)
    {
      ICredentials cred;
      switch (credentials.Authentication)
      {
        case Authentication.Anonymous:
          cred = new AnonymousCredentials(credentials.Database);
          break;
        case Authentication.Windows:
          cred = new WindowsCredentials(credentials.Database);
          break;
        default:
          cred = new ExplicitCredentials(credentials.Database, credentials.UserName, credentials.Password);
          break;
      }

      return Factory.GetConnection(credentials.Url
        , new ConnectionPreferences() { UserAgent = "InnovatorAdmin" }
        , async)
      .Continue(c =>
      {
        return c.Login(cred, async)
          .Convert(u => (IAsyncConnection)c);
      });
    }
    public static void Explore(this ConnectionData conn)
    {
      if (conn.Type == ConnectionType.Innovator)
      {
        var arasUrl = conn.Url + "?database=" + conn.Database + "&username=" + conn.UserName + "&password=" + ScalcMD5(conn.Password);
        using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\IEXPLORE.EXE"))
        {
          if (key != null)
          {
            var iePath = (string)key.GetValue(null);
            System.Diagnostics.Process.Start(iePath, arasUrl);
          }
        }
      }
    }
  }
}