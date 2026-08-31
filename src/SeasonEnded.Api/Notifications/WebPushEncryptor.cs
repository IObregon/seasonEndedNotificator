using System.Security.Cryptography;
using System.Text;

namespace SeasonEnded.Api.Notifications;

internal static class WebPushEncryptor
{
    public static EncryptedPayload Encrypt(PushSubscription subscription, string payload)
    {
        var userPublicKey = DecodeBase64Url(subscription.P256DH);
        var authSecret = DecodeBase64Url(subscription.Auth);

        using var serverEcdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var serverPublicKey = ECDiffieHellmanPublicKeyToRaw(serverEcdh.PublicKey);
        var salt = RandomNumberGenerator.GetBytes(16);

        var userEcdh = ECDiffieHellman.Create();
        userEcdh.ImportSubjectPublicKeyInfo(userPublicKey, out _);
        var userEcdhPublicKey = ECDiffieHellmanPublicKeyToRaw(userEcdh.PublicKey);

        var sharedSecret = serverEcdh.DeriveKeyFromHash(
            userEcdh.PublicKey,
            HashAlgorithmName.SHA256,
            salt,
            authSecret);

        var keyInfo = Combine(Encoding.UTF8.GetBytes("WebPush: info\0"), userEcdhPublicKey, serverPublicKey);
        var ikm = HkdfSha256(sharedSecret, keyInfo, 32);
        var contentEncryptionKey = HkdfSha256(ikm, Encoding.UTF8.GetBytes("Content-Encoding: aes128gcm\0\n"), 16);
        var nonce = HkdfSha256(ikm, Encoding.UTF8.GetBytes("Content-Encoding: nonce\0\n"), 12);

        var plaintext = Encoding.UTF8.GetBytes(payload);
        var paddedPayload = new byte[plaintext.Length + 1];
        Buffer.BlockCopy(plaintext, 0, paddedPayload, 0, plaintext.Length);
        paddedPayload[^1] = 2;

        var ciphertext = new byte[paddedPayload.Length];
        var tag = new byte[16];
        using var aesGcm = new AesGcm(contentEncryptionKey, tagSizeInBytes: 16);
        aesGcm.Encrypt(nonce, paddedPayload, ciphertext, tag);

        var header = BuildHeader(salt, serverPublicKey);
        var result = new byte[header.Length + ciphertext.Length + tag.Length];
        Buffer.BlockCopy(header, 0, result, 0, header.Length);
        Buffer.BlockCopy(ciphertext, 0, result, header.Length, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, result, header.Length + ciphertext.Length, tag.Length);

        return new EncryptedPayload(result, salt, serverPublicKey);
    }

    private static byte[] BuildHeader(byte[] salt, byte[] serverPublicKey)
    {
        var header = new byte[21 + serverPublicKey.Length];
        Buffer.BlockCopy(salt, 0, header, 0, 16);
        header[16] = 4;
        header[17] = 0;
        header[18] = 0;
        header[19] = 0;
        header[20] = (byte)serverPublicKey.Length;
        Buffer.BlockCopy(serverPublicKey, 0, header, 21, serverPublicKey.Length);
        return header;
    }

    private static byte[] HkdfSha256(byte[] ikm, byte[] info, int length)
    {
        var prk = HMACSHA256.HashData(new byte[32], ikm);
        var okm = new byte[length];
        var t = Array.Empty<byte>();
        var pos = 0;
        var i = 1;
        while (pos < length)
        {
            t = HMACSHA256.HashData(prk, Combine(t, info, [(byte)i]));
            var chunk = Math.Min(t.Length, length - pos);
            Buffer.BlockCopy(t, 0, okm, pos, chunk);
            pos += chunk;
            i++;
        }
        return okm;
    }

    private static byte[] Combine(params byte[][] arrays)
    {
        var result = new byte[arrays.Sum(a => a.Length)];
        var pos = 0;
        foreach (var array in arrays)
        {
            Buffer.BlockCopy(array, 0, result, pos, array.Length);
            pos += array.Length;
        }
        return result;
    }

    private static byte[] DecodeBase64Url(string value) =>
        Convert.FromBase64String(value.PadRight(value.Length + (4 - value.Length % 4) % 4, '=')
            .Replace('-', '+').Replace('_', '/'));

    private static byte[] ECDiffieHellmanPublicKeyToRaw(ECDiffieHellmanPublicKey publicKey)
    {
        var spki = publicKey.ExportSubjectPublicKeyInfo();
        var raw = new byte[65];
        raw[0] = 0x04;
        Buffer.BlockCopy(spki, spki.Length - 64, raw, 1, 64);
        return raw;
    }
}

internal sealed record EncryptedPayload(byte[] Payload, byte[] Salt, byte[] PublicKey);
