﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Security;
using MySql.Data.MySqlClient;

namespace Banker.Database
{
    public class SqlConnect
    {
        const string MyConnectionString = "server=qtserver.dynu.net;port=33306;uid=dang;" +
                                          "pwd=passtest;database=test;";

        /// <summary>
        /// Create new user in the database
        /// </summary>
        /// <param name="username">the username</param>
        /// <param name="password">the password</param>
        /// <returns>
        /// 1 - Duplicate username
        /// 2 - username or password is empty or whitespace
        /// 3 - password is too short (at least 8)
        /// </returns>
        public int CreateNewUser(string username, SecureString Spassword)
        {
            if (string.IsNullOrWhiteSpace(username) || Spassword.Length == 0)
                return 2;
            if (Spassword.Length < 7)
                return 3;
            using (var myConnection = new MySqlConnection {ConnectionString = MyConnectionString})
            {
                myConnection.Open();
                using (var myCommand = myConnection.CreateCommand())
                {
                    using (var myTrans = myConnection.BeginTransaction())
                    {
                        try
                        {
                            myCommand.Connection = myConnection;
                            myCommand.Transaction = myTrans;

                            var password = SecurePasswordHasher.Hash(SecurePasswordBox.ConvertToUnsecureString(Spassword));

                            myCommand.CommandText =
                                $"INSERT INTO User (username,password) VALUES (@username,@password);";
                            myCommand.Parameters.AddWithValue("@username", username);
                            myCommand.Parameters.AddWithValue("@password", password);

                            myCommand.ExecuteNonQuery();
                            myTrans.Commit();
                        }
                        catch (MySqlException e)
                        {
                            var message = e.Message;
                            if (message.Contains("Duplicate"))
                                return 1;
                            try
                            {
                                myTrans.Rollback();
                            }
                            catch (SqlException ex)
                            {
                                if (myTrans.Connection != null)
                                {
                                    Console.WriteLine(
                                        $@"An exception of type {ex.GetType()} was encountered while attempting to roll back the transaction.");
                                }
                            }
                        }
                    }
                }

                myConnection.Close();
            }

            return 0;
        }

        public UserInfo VerifyUser(string username, SecureString Spassword)
        {
            var hashPassword = "";
            var user = new UserInfo();
            using (var myConnection = new MySqlConnection {ConnectionString = MyConnectionString})
            {
                myConnection.Open();
                using (var myCommand = myConnection.CreateCommand())
                {
                    using (var myTrans = myConnection.BeginTransaction())
                    {
                        myCommand.Connection = myConnection;
                        myCommand.Transaction = myTrans;

                        try
                        {
                            myCommand.CommandText = "SELECT id, password FROM User WHERE username = @username;";
                            myCommand.Parameters.AddWithValue("@username", username);
                            using (var reader = myCommand.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    hashPassword = reader.GetString("password");
                                    user.Id = reader.GetInt32("id");
                                    Console.WriteLine(hashPassword);
                                }

                                reader.Close();

                                if (string.IsNullOrWhiteSpace(hashPassword))
                                {
                                    user.Valid = false;
                                    return user;
                                }


                                Console.WriteLine(@"Read record from the database.");
                            }
                        }
                        catch (Exception e)
                        {
                            try
                            {
                                myTrans.Rollback();
                            }
                            catch (SqlException ex)
                            {
                                if (myTrans.Connection != null)
                                {
                                    Console.WriteLine(
                                        $@"An exception of type {ex.GetType()} was encountered while attempting to roll back the transaction.");
                                }
                            }


                            Console.WriteLine(
                                $@"An exception of type {e.GetType()} was encountered while reading the data.");
                        }
                    }
                }
            }

            user.Valid = SecurePasswordHasher.Verify(SecurePasswordBox.ConvertToUnsecureString(Spassword), hashPassword);
            return user;
        }

        public void InsertMoney(string mode, int userid, decimal money, string category = null)
        {
            if (userid < 1)
                throw new Exception("UserID is positive number");
            if (mode.Equals("expense"))
            {
                if (string.IsNullOrWhiteSpace(category))
                    throw new Exception("Category can't be empty");
            }

            using (var myConnection = new MySqlConnection {ConnectionString = MyConnectionString})
            {
                myConnection.Open();
                using (var myCommand = myConnection.CreateCommand())
                {
                    //                    
                    using (var myTrans = myConnection.BeginTransaction())
                    {
                        try
                        {
                            myCommand.Connection = myConnection;
                            myCommand.Transaction = myTrans;

                            if (mode.Equals("expense"))
                            {
                                myCommand.CommandText =
                                    "INSERT INTO Expense (Uid,Category,$) VALUES (@userid,@category,@money);";
                                myCommand.Parameters.AddWithValue("@userid", userid);
                                myCommand.Parameters.AddWithValue("@category", category);
                                myCommand.Parameters.AddWithValue("@money", money);
                            }
                            else if (mode.Equals("total"))
                            {
                                myCommand.CommandText =
                                    $"INSERT INTO Total (Uid,$) VALUES (@userid,@money);";
                                myCommand.Parameters.AddWithValue("@userid", userid);
                                myCommand.Parameters.AddWithValue("@money", money);
                            }

                            myCommand.ExecuteNonQuery();
                            myTrans.Commit();
                        }
                        catch (Exception)
                        {
                            try
                            {
                                myTrans.Rollback();
                            }
                            catch (SqlException ex)
                            {
                                if (myTrans.Connection != null)
                                {
                                    Console.WriteLine(
                                        $@"An exception of type {ex.GetType()} was encountered while attempting to roll back the transaction.");
                                }
                            }
                        }
                    }
                }
            }
        }

        public void DeleteMoney(string mode, int itemId, int userId)
        {
            using (var myConnection = new MySqlConnection {ConnectionString = MyConnectionString})
            {
                myConnection.Open();
                using (var myCommand = myConnection.CreateCommand())
                {
                    //                    
                    using (var myTrans = myConnection.BeginTransaction())
                    {
                        try
                        {
                            myCommand.Connection = myConnection;
                            myCommand.Transaction = myTrans;

                            if (mode.Equals("expense"))
                            {
                                myCommand.CommandText = "DELETE FROM Expense WHERE id=@itemId AND Uid=@userId;";
                                myCommand.Parameters.AddWithValue("@itemId", itemId);
                                myCommand.Parameters.AddWithValue("@userId", userId);
//                                myCommand.CommandText =
//                                    $"DELETE FROM Expense WHERE TIME_TO_SEC(TIMEDIFF(`Timestamp`,'{timestamp}')) >= 0 AND TIME_TO_SEC(TIMEDIFF(`Timestamp`,'{timestamp}')) <= 5;";
                            }
                            else if (mode.Equals("total"))
                            {
                                myCommand.CommandText =
                                    $"DELETE FROM Total WHERE id=@itemId AND Uid=@userId;";
                                myCommand.Parameters.AddWithValue("@itemId", itemId);
                                myCommand.Parameters.AddWithValue("@userId", userId);
                            }

                            myCommand.ExecuteNonQuery();
                            myTrans.Commit();
                        }
                        catch (Exception)
                        {
                            try
                            {
                                myTrans.Rollback();
                            }
                            catch (SqlException ex)
                            {
                                if (myTrans.Connection != null)
                                {
                                    Console.WriteLine(
                                        $@"An exception of type {ex.GetType()} was encountered while attempting to roll back the transaction.");
                                }
                            }
                        }
                    }
                }
            }
        }

        public List<Bank> PullData(string mode, int userid)
        {
            var result = new List<Bank>();

            using (var myConnection = new MySqlConnection {ConnectionString = MyConnectionString})
            {
                myConnection.Open();
                using (var myCommand = myConnection.CreateCommand())
                {
                    var myTrans = myConnection.BeginTransaction();

                    myCommand.Connection = myConnection;
                    myCommand.Transaction = myTrans;

                    try
                    {
                        if (mode.Equals("expense"))
                        {
                            myCommand.CommandText = $"SELECT * FROM Expense WHERE Uid = @userId;";
                            myCommand.Parameters.AddWithValue("@userId", userid);
                            using (var reader = myCommand.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    var expense = new Bank(reader.GetDecimal("$"),
                                        reader.GetDateTime("Timestamp"), reader.GetString("Category"),
                                        reader.GetInt32("id"));
                                    result.Add(expense);
                                }
                            }
                        }
                        else if (mode.Equals("total"))
                        {
                            myCommand.CommandText = $"SELECT * FROM Total WHERE Uid = {userid};";
                            using (var reader = myCommand.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    var total = new Bank(reader.GetDecimal("$"), reader.GetDateTime("Timestamp"), null,
                                        reader.GetInt32("id"));
                                    result.Add(total);
                                }
                            }
                        }

                        Console.WriteLine(@"Read record from the database.");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(
                            $@"An exception of type {e.GetType()} was encountered while reading the data.");
                    }
                }
            }

            return result;
        }

        public class UserInfo
        {
            public int Id { get; set; }
            public bool Valid { get; set; }
        }

        public class Bank
        {
            public bool Selected { get; set; }
            public DateTime Timestamp { get; }
            public string Category { get; }
            public decimal Money { get; }
            public string Spend { get; }
            public int Id { get; }

//            .ToString("yyyy-MM-dd HH:mm:ss")
            public Bank(decimal money, DateTime timestamp, string category = null, int id = 0, bool selected = false)
            {
                Money = money;
                Timestamp = timestamp;
                Category = category;
                Spend = money.ToString("C");
                Id = id;
            }

            public override string ToString()
            {
                return Category == null ? $"{Timestamp}: Spent ${Money}" : $"{Timestamp}: Spent ${Money} in {Category}";
            }
        }
    }
}