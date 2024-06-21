namespace AppSwitcher.WindowDiscovery;

[Flags]
internal enum WindowStyle : uint
{
    None = 0,
    WS_CHILD = 0x40000000,
    WS_POPUP = 0x80000000
}
