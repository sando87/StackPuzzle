using Npgsql;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DBManager : IDisposable
{
    static private DBManager mInst = null;
    private NpgsqlConnection mDBSession = null;

    private DBManager() { }
    public void Dispose()
    {
        if(mInst != null)
            mInst.Close();
    }
    static public DBManager Inst()
    {
        if (mInst == null)
        {
            mInst = new DBManager();
            mInst.Open("localhost", "stackpuzzle", "postgres", "root");
        }
        return mInst;
    }
    public void Open(string hostIP, string database, string userid, string password)
    {
        if (mDBSession != null)
            return;

        string connectParam =
            "host=" + hostIP +
            ";database=" + database +
            ";username=" + userid +
            ";password=" + password;
        mDBSession = new NpgsqlConnection(connectParam);
        try
        {
            mDBSession.Open();
        }
        catch (NpgsqlException ex)
        {
            mDBSession = null;
            Debug.Log(ex.Message);
        }
        catch (Exception ex)
        {
            mDBSession = null;
            Debug.Log(ex.Message);
        }
    }
    public void Close()
    {
        if (mDBSession != null)
        {
            mDBSession.Close();
            mDBSession = null;
        }
    }

    public int AddNewUser(UserInfo user, string ipAddr)
    {
        if (user.userPk != -1)
            return user.userPk;

        try
        {
            UserInfo info = GetUser(user.deviceName);
            if (info != null)
                return info.userPk;

            using (var cmd = new NpgsqlCommand())
            {
                string query = String.Format("INSERT INTO users (userName, score, deviceName, ipAddress, firstTime) VALUES ('{0}', {1}, '{2}', '{3}', now()) RETURNING userPk", 
                    user.userName, user.score, user.deviceName, ipAddr);
                cmd.Connection = mDBSession;
                cmd.CommandText = query;
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return (int)reader["userPk"];
                    }
                }
            }
        }
        catch (NpgsqlException ex) { Debug.Log(ex.Message); }
        catch (Exception ex) { Debug.Log(ex.Message); }
        return -1;
    }
    public void EditUserName(UserInfo user)
    {
        try
        {
            using (var cmd = new NpgsqlCommand())
            {
                string query = String.Format("UPDATE users SET userName='{1}' WHERE userPk = {0}", user.userPk, user.userName);
                cmd.Connection = mDBSession;
                cmd.CommandText = query;
                using (var reader = cmd.ExecuteReader())
                {
                    return;
                }
            }
        }
        catch (NpgsqlException ex) { Debug.Log(ex.Message); }
        catch (Exception ex) { Debug.Log(ex.Message); }
        return;
    }
    public void RenewUserScore(int userPk, int score)
    {
        try
        {
            using (var cmd = new NpgsqlCommand())
            {
                string query = String.Format("UPDATE users SET score={1} WHERE userPk = {0}", userPk, score);
                cmd.Connection = mDBSession;
                cmd.CommandText = query;
                using (var reader = cmd.ExecuteReader())
                {
                    return;
                }
            }
        }
        catch (NpgsqlException ex) { Debug.Log(ex.Message); }
        catch (Exception ex) { Debug.Log(ex.Message); }
        return;
    }
    public void DeleteUser(int userPk)
    {
        try
        {
            using (var cmd = new NpgsqlCommand())
            {
                string query = String.Format("DELETE FROM users WHERE userPk = {0}", userPk);
                cmd.Connection = mDBSession;
                cmd.CommandText = query;
                using (var reader = cmd.ExecuteReader())
                {
                    return;
                }
            }
        }
        catch (NpgsqlException ex) { Debug.Log(ex.Message); }
        catch (Exception ex) { Debug.Log(ex.Message); }
        return;
    }
    public UserInfo GetUser(int userPk)
    {
        try
        {
            using (var cmd = new NpgsqlCommand())
            {
                string query = String.Format("SELECT * FROM users WHERE userPk = {0}", userPk);
                cmd.Connection = mDBSession;
                cmd.CommandText = query;
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        UserInfo user = new UserInfo();
                        user.userPk = (int)reader["userPk"];
                        user.userName = reader["userName"].ToString();
                        user.score = (int)reader["score"];
                        user.deviceName = reader["deviceName"].ToString();
                        return user;
                    }
                }
            }
        }
        catch (NpgsqlException ex) { Debug.Log(ex.Message); }
        catch (Exception ex) { Debug.Log(ex.Message); }
        return null;
    }
    public UserInfo GetUser(string deviceName)
    {
        try
        {
            using (var cmd = new NpgsqlCommand())
            {
                string query = String.Format("SELECT * FROM users WHERE deviceName = '{0}'", deviceName);
                cmd.Connection = mDBSession;
                cmd.CommandText = query;
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        UserInfo user = new UserInfo();
                        user.userPk = (int)reader["userPk"];
                        user.userName = reader["userName"].ToString();
                        user.score = (int)reader["score"];
                        user.deviceName = reader["deviceName"].ToString();
                        return user;
                    }
                }
            }
        }
        catch (NpgsqlException ex) { Debug.Log(ex.Message); }
        catch (Exception ex) { Debug.Log(ex.Message); }
        return null;
    }
    public void AddLog(LogInfo log)
    {
        try
        {
            using (var cmd = new NpgsqlCommand())
            {
                string query = String.Format("INSERT INTO log (userPk, logTime, message) VALUES ({0}, now(), '{1}')", log.userPk, log.message);
                cmd.Connection = mDBSession;
                cmd.CommandText = query;
                using (var reader = cmd.ExecuteReader())
                {
                    return;
                }
            }
        }
        catch (NpgsqlException ex) { Debug.Log(ex.Message); }
        catch (Exception ex) { Debug.Log(ex.Message); }
        return;
    }
    public UserInfo[] GetUsers()
    {
        try
        {
            using (var cmd = new NpgsqlCommand())
            {
                string query = String.Format("SELECT * FROM users");
                cmd.Connection = mDBSession;
                cmd.CommandText = query;
                using (var reader = cmd.ExecuteReader())
                {
                    List<UserInfo> users = new List<UserInfo>();
                    while (reader.Read())
                    {
                        UserInfo user = new UserInfo();
                        user.userPk = (int)reader["userPk"];
                        user.userName = reader["userName"].ToString();
                        user.score = (int)reader["score"];
                        user.deviceName = reader["deviceName"].ToString();
                        users.Add(user);
                    }
                    return users.ToArray();
                }
            }
        }
        catch (NpgsqlException ex) { Debug.Log(ex.Message); }
        catch (Exception ex) { Debug.Log(ex.Message); }
        return null;
    }

}
