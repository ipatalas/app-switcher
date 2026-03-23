using LiteDB;

namespace AppSwitcher.Configuration.Migrations;

internal interface IMigration
{
    int Version { get; }
    void Up(LiteDatabase db);
}