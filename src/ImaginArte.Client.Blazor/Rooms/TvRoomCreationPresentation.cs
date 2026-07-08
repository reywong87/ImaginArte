using System.Security.Cryptography;
using ImaginArte.Contracts.Rooms;

namespace ImaginArte.Client.Blazor.Rooms;

public static class TvRoomCreationPresentation
{
    private const string RoomCodeCharacters = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int RoomCodeLength = 6;

    public static string CreateRoomCode()
    {
        return string.Create(RoomCodeLength, RoomCodeCharacters, static (buffer, characters) =>
        {
            for (var index = 0; index < buffer.Length; index++)
            {
                buffer[index] = characters[RandomNumberGenerator.GetInt32(characters.Length)];
            }
        });
    }

    public static string CreateLobbyPath(RoomStateResponse room)
    {
        return $"/tv/{Uri.EscapeDataString(room.RoomCode)}";
    }
}
