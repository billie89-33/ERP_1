
#r "nuget: Npgsql, 8.0.2"
using Npgsql;
using System;

var connString = "Host=aws-0-ap-southeast-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.xjubksrboxitiwegexns;Password=bowvorn0810064406;Pooling=true;";
using var conn = new NpgsqlConnection(connString);
conn.Open();

using var cmd = new NpgsqlCommand("SELECT ""Username"", ""Email"", ""Role"" FROM ""Users""", conn);
using var reader = cmd.ExecuteReader();
Console.WriteLine("--- Users in Database ---");
while(reader.Read()) {
    Console.WriteLine($"Username: {reader[""Username""]}, Email: {reader[""Email""]}, Role: {reader[""Role""]}");
}

