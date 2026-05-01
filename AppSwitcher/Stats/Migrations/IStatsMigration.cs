using LiteDB;

namespace AppSwitcher.Stats.Migrations;

internal interface IStatsMigration
{
    int Version { get; }
    void Up(ILiteDatabase db);
}