namespace CovidLetter.Backend.Common.Application.Cbor;

using PeterO.Cbor;

// @author Henrik Bengtsson(extern.henrik.bengtsson @digg.se)
// @author Martin Lindstr�m(martin @idsec.se)
// @author Henric Norlander(extern.henric.norlander @digg.se)

/// <summary>
/// Representation of the supported signature algorithms.
/// </summary>
internal class SignatureAlgorithm
{
    public static readonly CBORObject ES256 = CBORObject.FromObject(-7);

    public static readonly CBORObject ES384 = CBORObject.FromObject(-35);

    public static readonly CBORObject ES512 = CBORObject.FromObject(-36);

    public static readonly CBORObject PS256 = CBORObject.FromObject(-37);

    public static readonly CBORObject PS384 = CBORObject.FromObject(-38);

    public static readonly CBORObject PS512 = CBORObject.FromObject(-39);

    private static readonly CBORObject[] SupportedAlgorithm =
    {
        ES256,
        PS256,
    };

    public static string? GetAlgorithmName(CBORObject cborValue) => cborValue.AsInt32() switch
    {
        -7 => "SHA256withECDSA",
        -35 => "SHA384withECDSA",
        -36 => "SHA512withECDSA",
        -37 => "SHA256withRSA/PSS",
        -38 => "SHA384withRSA/PSS",
        -39 => "SHA512withRSA/PSS",
        _ => null,
    };

    public static bool IsSupportedAlgorithm(CBORObject cborValue) => SupportedAlgorithm.Contains(cborValue);
}
