
using System;
using Npgsql;

var connString = "Host=aws-0-ap-southeast-1.pooler.supabase.com;Port=5432;Username=postgres.xjubksrboxitiwegexns;Password=bowvorn0810064406;Database=postgres;Pooling=true;SslMode=Require;";
try {
    using var conn = new NpgsqlConnection(connString);
    conn.Open();
    using var cmd = new NpgsqlCommand("SELECT count(*) FROM \"Products\"", conn);
    var count = cmd.ExecuteScalar();
    Console.WriteLine($"Products count: {count}");

    using var cmd2 = new NpgsqlCommand("SELECT count(*) FROM \"Users\"", conn);
    var uCount = cmd2.ExecuteScalar();
    Console.WriteLine($"Users count: {uCount}");
} catch (Exception ex) {
    Console.WriteLine("Error: " + ex.Message);
}

