namespace CovidLetter.Backend.Common.Application.Cbor;

using CovidLetter.Backend.Common.Utilities;

using Org.BouncyCastle.Security;

using PeterO.Cbor;

/// @author Henrik Bengtsson(extern.henrik.bengtsson @digg.se)
/// @author Martin Lindstr�m(martin @idsec.se)
/// @author Henric Norlander(extern.henric.norlander @digg.se)
/// <summary>
/// A representation of a COSE_Sign1 object.
/// </summary>
public class CoseSign1 : ICoseSign1
{
    private const string ContextString = "Signature1";
    private const int MESSAGETAG = 18;

    private static readonly byte[] ExternalData = Array.Empty<byte>();

    private readonly CBORObject unprotectedAttributes;
    private readonly byte[]? content;
    private readonly byte[] signature;

    private CBORObject? protectedAttributes;
    private byte[]? protectedAttributesEncoding;

    /// <summary>
    /// Initializes a new instance of the <see cref="CoseSign1"/> class.
    /// </summary>
    /// <param name="data"> the binary representation of the COSE_Sign1 object.</param>
    public CoseSign1(byte[] data)
    {
        var message = CBORObject.DecodeFromBytes(data);
        if (message.Type != CBORType.Array)
        {
            throw new CBORException("Supplied message is not a valid COSE security object");
        }

        CheckIfMessageIsTagged(message);

        if (message.Count != 4)
        {
            throw new CBORException($"Invalid COSE_Sign1 structure - Expected an array of 4 items - but array has {message.Count} items");
        }

        if (message[0].Type == CBORType.ByteString)
        {
            this.AssignProtectedAttributes(message);
        }
        else
        {
            throw new CBORException(
                $"Invalid COSE_Sign1 structure -  Expected item at position 1/4 to be a bstr which is the encoding of the protected attributes, but was {message[0].Type}");
        }

        if (message[1].Type == CBORType.Map || message[1].Type == CBORType.ByteString)
        {
            this.unprotectedAttributes = message[1];
        }
        else
        {
            throw new CBORException(
                $"Invalid COSE_Sign1 structure - Expected item at position 2/4 to be a Map for unprotected attributes, but was {message[1].Type}");
        }

        if (message[2].Type == CBORType.ByteString)
        {
            this.content = message[2].GetByteString();
        }
        else if (!message[2].IsNull)
        {
            throw new CBORException($"Invalid COSE_Sign1 structure - Expected item at position 3/4 to be a bstr holding the payload, but was {message[2].Type}");
        }

        if (message[3].Type == CBORType.ByteString)
        {
            this.signature = message[3].GetByteString();
        }
        else
        {
            throw new CBORException(
                $"Invalid COSE_Sign1 structure - Expected item at position 4/4 to be a bstr holding the signature, but was {message[3].Type}");
        }
    }

    private void AssignProtectedAttributes(CBORObject message)
    {
        this.protectedAttributesEncoding = message[0].GetByteString();

        if (message[0].GetByteString().Length == 0)
        {
            this.protectedAttributes = CBORObject.NewMap();
        }
        else
        {
            this.protectedAttributes = CBORObject.DecodeFromBytes(this.protectedAttributesEncoding);
            if (this.protectedAttributes.Count == 0)
            {
                this.protectedAttributesEncoding = Array.Empty<byte>();
            }
        }
    }

    private static void CheckIfMessageIsTagged(CBORObject message)
    {
        if (message.IsTagged)
        {
            if (message.GetAllTags().Length != 1)
            {
                throw new CBORException("Invalid object - too many tags");
            }

            if (message.MostInnerTag.ToInt32Unchecked() != MESSAGETAG)
            {
                throw new CBORException(
                    $"Invalid COSE_Sign1 structure - Expected {MESSAGETAG} tag - but was {message.MostInnerTag.ToInt32Unchecked()}");
            }
        }
    }

    public byte[]? GetKeyIdentifier()
    {
        var kid = this.protectedAttributes?[HeaderParameterKey.KID] ?? this.unprotectedAttributes[HeaderParameterKey.KID];

        return kid?.GetByteString();
    }

    public string? GetJson()
    {
        if (this.content == null)
        {
            return null;
        }

        var cborObjectFromBytes = CBORObject.DecodeFromBytes(this.content);
        var jsonString = cborObjectFromBytes.ToJSONString();

        return jsonString;
    }

    public bool VerifySignature(byte[] publicKey)
    {
        if (this.signature == null)
        {
            return false;
        }

        var obj = CBORObject.NewArray();
        obj.Add(ContextString);
        obj.Add(this.protectedAttributesEncoding);
        obj.Add(ExternalData);
        if (this.content != null)
        {
            obj.Add(this.content);
        }
        else
        {
            obj.Add(null);
        }

        var signedData = obj.EncodeToBytes();

        var registeredAlgorithm = this.protectedAttributes?[HeaderParameterKey.ALGORITHIM];
        if (registeredAlgorithm == null)
        {
            return false;
        }

        var signatureToVerify = this.signature;
        if (!SignatureAlgorithm.IsSupportedAlgorithm(registeredAlgorithm))
        {
            return false;
        }

        if (registeredAlgorithm == SignatureAlgorithm.ES256
            || registeredAlgorithm == SignatureAlgorithm.ES384
            || registeredAlgorithm == SignatureAlgorithm.ES512)
        {
            signatureToVerify = ConvertToDer(this.signature);
        }

        var key = PublicKeyFactory.CreateKey(publicKey);

        var verifier = SignerUtilities.GetSigner(SignatureAlgorithm.GetAlgorithmName(registeredAlgorithm));
        verifier.Init(false, key);
        verifier.BlockUpdate(signedData, 0, signedData.Length);
        var result = verifier.VerifySignature(signatureToVerify);

        return result;
    }

    /// <summary>
    /// Given a signature according to section 8.1 in RFC8152 its corresponding DER encoding is returned.
    /// </summary>
    /// <param name="rsConcat">the ECDSA signature.</param>
    /// <returns>DER-encoded signature.</returns>
    private static byte[] ConvertToDer(byte[] rsConcat)
    {
        var len = rsConcat.Length / 2;
        var r = new byte[len];
        var s = new byte[len];
        Array.Copy(rsConcat, r, len);
        Array.Copy(rsConcat, len, s, 0, len);

        var seq = new List<byte[]> { Asn1Utils.ToUnsignedInteger(r), Asn1Utils.ToUnsignedInteger(s) };

        return Asn1Utils.ToSequence(seq);
    }
}
