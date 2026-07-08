using ImaginArte.Contracts.Rooms;
using QRCoder;

namespace ImaginArte.Client.Blazor.Rooms;

public static class TvRoomJoinPresentation
{
    public static string CreateMobileJoinUrl(RoomStateResponse room, Uri applicationBaseUri)
    {
        ArgumentNullException.ThrowIfNull(room);
        ArgumentNullException.ThrowIfNull(applicationBaseUri);

        var escapedRoomCode = Uri.EscapeDataString(room.RoomCode);
        return new Uri(applicationBaseUri, $"join/{escapedRoomCode}").AbsoluteUri;
    }

    public static string CreateMobileJoinQrSvg(string mobileJoinUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mobileJoinUrl);

        return SvgQRCodeHelper.GetQRCode(
            mobileJoinUrl,
            pixelsPerModule: 12,
            darkColorHex: "#171229",
            lightColorHex: "#ffffff",
            QRCodeGenerator.ECCLevel.Q,
            sizingMode: SvgQRCode.SizingMode.ViewBoxAttribute);
    }
}
