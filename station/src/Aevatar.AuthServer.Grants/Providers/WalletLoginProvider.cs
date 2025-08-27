using System.Text;
using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.Cryptography;
using AElf.Types;
using Aevatar.AuthServer.Grants.Options;
using Aevatar.User;
using Google.Protobuf;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portkey.Contracts.CA;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace Aevatar.AuthServer.Grants.Providers;

public class WalletLoginProvider: IWalletLoginProvider, ISingletonDependency
{
    private readonly ILogger<WalletLoginProvider> _logger;
    private readonly SignatureGrantOptions _signatureGrantOptions;
    private readonly ChainOptions _chainOptions;
    
    private const string GetHolderInfoMethodName = "GetHolderInfo";
    private const string Nonce = "Nonce:";
    public WalletLoginProvider(ILogger<WalletLoginProvider> logger,
        IOptionsMonitor<SignatureGrantOptions> signatureOptions, IOptionsMonitor<ChainOptions> chainOptions)
    {
        _logger = logger;
        _signatureGrantOptions = signatureOptions.CurrentValue;
        _chainOptions = chainOptions.CurrentValue;
    }

    public List<string> CheckParams(string publicKeyVal, string signatureVal, string chainId, 
        string plainText)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(publicKeyVal))
        {
            errors.Add("invalid parameter publish_key.");
        }

        if (string.IsNullOrWhiteSpace(signatureVal))
        {
            errors.Add("invalid parameter signature.");
        }

        if (string.IsNullOrWhiteSpace(chainId))
        {
            errors.Add("invalid parameter chain_id.");
        }

        if (string.IsNullOrWhiteSpace(plainText))
        {
            errors.Add("invalid parameter plainText.");
        }

        return errors;
    }

    public async Task<string> VerifySignatureAndParseWalletAddressAsync(string publicKeyVal, string signatureVal,
        string plainText, string caHash,  string chainId)
    {
        var rawText = Encoding.UTF8.GetString(ByteArrayHelper.HexStringToByteArray(plainText));
        _logger.LogInformation("rawText:{rawText}", rawText);
        var timestampVal = rawText.TrimEnd().Substring(rawText.LastIndexOf(Nonce) + Nonce.Length);        
        var timestamp = long.Parse(timestampVal);
        _logger.LogInformation("timestamp:{timestamp}", timestamp);
        //Validate timestamp validity period
        if (IsTimeStampOutRange(timestamp, out int timeRange))
        {
            throw new UserFriendlyException(
                $"The time should be {timeRange} minutes before and after the current time.");
        }

        //Validate public key and signature
        var signAddress = VerifySignature(plainText, signatureVal, publicKeyVal);

        //If EOA wallet, signAddress is the wallet address; if CA wallet, signAddress is the manager address.
        _logger.LogInformation(
            "[VerifySignature] signatureVal:{signatureVal}, signAddress:{signAddress}, caHash:{caHash}, chainId:{chainId}, timestamp:{timestamp}",
            signatureVal, signAddress, caHash, chainId, timestamp);

        if (!string.IsNullOrWhiteSpace(caHash))
        {
            //If CA wallet connect
            var managerCheck = await CheckManagerAddressAsync(chainId, caHash, signAddress);
            if (!managerCheck.HasValue || !managerCheck.Value)
            {
                _logger.LogError(
                    "[VerifySignature] Manager validation failed. caHash:{caHash}, address:{signAddress}, chainId:{chainId}",
                    caHash, signAddress, chainId);
                throw new UserFriendlyException("Manager validation failed.");
            }

            List<UserChainAddressDto> addressInfos = await GetAddressInfosAsync(caHash);
            if (addressInfos == null || addressInfos.Count == 0)
            {
                _logger.LogError("[VerifySignature] Get ca address failed. caHash:{0}, chainId:{1}",
                    caHash, chainId);
                throw new UserFriendlyException($"Can not get ca address in chain {chainId}.");
            }

            var caAddress = addressInfos[0].Address;
            return caAddress;
        }
        return signAddress;
    }

    private string VerifySignature( string plainText, string signatureVal,string publicKeyVal)
    {
        var signature = ByteArrayHelper.HexStringToByteArray(signatureVal);
        var hash = Encoding.UTF8.GetBytes(plainText).ComputeHash();
        var publicKey = ByteArrayHelper.HexStringToByteArray(publicKeyVal);
        if (!CryptoHelper.VerifySignature(signature, hash, publicKey))
        {
            throw new UserFriendlyException("Signature validation failed new.");
        }
        
        //Since it is not possible to determine whether the CA wallet manager address is in managerPublicKey or in managerPublicKeyOld
        //therefore, the accurate manager address is obtained from publicKeyVal.
        var signAddress = Address.FromPublicKey(publicKey).ToBase58();
        return signAddress;
    }

    public string GetErrorMessage(List<string> errors)
    {
        var message = string.Empty;

        errors?.ForEach(t => message += $"{t}, ");
        if (message.Contains(','))
        {
            return message.TrimEnd().TrimEnd(',');
        }

        return message;
    }

    public bool IsTimeStampOutRange(long timestamp, out int timeRange)
    {
        var time = DateTime.UnixEpoch.AddMilliseconds(timestamp);
        timeRange = _signatureGrantOptions.TimestampValidityRangeMinutes;
        if (time < DateTime.UtcNow.AddMinutes(-timeRange) ||
            time > DateTime.UtcNow.AddMinutes(timeRange))
        {
            return true;
        }

        return false;
    }

   
    
    private async Task<bool?> CheckManagerAddressAsync(string chainId, string caHash, string manager)
    {
        string graphQlUrl = _signatureGrantOptions.PortkeyV2GraphQLUrl;
        var graphQlResult = await CheckManagerAddressFromGraphQlAsync(graphQlUrl, caHash, manager);
        if (!graphQlResult.HasValue || !graphQlResult.Value)
        {
            _logger.LogDebug("graphql is invalid.");
            var  contractResult = await CheckManagerAddressFromContractAsync(chainId, caHash, manager, _chainOptions);
            if (!contractResult.HasValue || !contractResult.Value)
            {
                _logger.LogDebug("contract is invalid.");
                return await ManagerCheckHelper.CheckManagerFromCache(_signatureGrantOptions.CheckManagerUrl, manager, caHash);
            }
            return true;
        }
        return true;
    }
    
    private async Task<bool?> CheckManagerAddressFromContractAsync(string chainId, string caHash, string manager,
        ChainOptions chainOptions)
    {
        var param = new GetHolderInfoInput
        {
            CaHash = Hash.LoadFromHex(caHash),
            LoginGuardianIdentifierHash = Hash.Empty
        };

        var output =
            await CallTransactionAsync<GetHolderInfoOutput>(chainId, GetHolderInfoMethodName, param, 
                chainOptions);

        return output?.ManagerInfos?.Any(t => t.Address.ToBase58() == manager);
    }
    
    private async Task<bool?> CheckManagerAddressFromGraphQlAsync(string url, string caHash,
        string managerAddress)
    {
        var cHolderInfos = await GetHolderInfosAsync(url, caHash);
        var loginChainHolderInfo =
            cHolderInfos.CaHolderInfo.Find(c => c.ChainId == _signatureGrantOptions.LoginChainId);
        var caHolderManagerInfos = loginChainHolderInfo?.ManagerInfos;
        return caHolderManagerInfos?.Any(t => t.Address == managerAddress);
    }
    
    private async Task<T> CallTransactionAsync<T>(string chainId, string methodName, IMessage param,
        ChainOptions chainOptions) where T : class, IMessage<T>, new()
    {
        try
        {
            var chainInfo = chainOptions.ChainInfos[chainId];

            var client = new AElfClient(chainInfo.AElfNodeBaseUrl);
            await client.IsConnectedAsync();
            var address = client.GetAddressFromPrivateKey(_signatureGrantOptions.CommonPrivateKeyForCallTx);

            var contractAddress = chainInfo.CAContractAddress;

            var transaction =
                await client.GenerateTransactionAsync(address, contractAddress,
                    methodName, param);

            var txWithSign = client.SignTransaction(_signatureGrantOptions.CommonPrivateKeyForCallTx, transaction);
            var result = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
            {
                RawTransaction = txWithSign.ToByteArray().ToHex()
            });

            var value = new T();
            value.MergeFrom(ByteArrayHelper.HexStringToByteArray(result));
            return value;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "CallTransaction error, chain id:{chainId}, methodName:{methodName}", chainId,
                methodName);
            return null;
        }
    }
    
    private async Task<HolderInfoIndexerDto> GetHolderInfosAsync(string url, string caHash)
    {
        using var graphQlClient = new GraphQLHttpClient(url, new NewtonsoftJsonSerializer());
        var request = new GraphQLRequest
        {
            Query = @"
			    query($caHash:String,$skipCount:Int!,$maxResultCount:Int!) {
                    caHolderInfo(dto: {caHash:$caHash,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                            id,chainId,caHash,caAddress,originChainId,managerInfos{address,extraData}}
                }",
            Variables = new
            {
                caHash, skipCount = 0, maxResultCount = 10
            }
        };

        var graphQlResponse = await graphQlClient.SendQueryAsync<HolderInfoIndexerDto>(request);
        return graphQlResponse.Data;
    }
    
    private async Task<List<UserChainAddressDto>> GetAddressInfosAsync(string caHash)
    {
        var addressInfos = new List<UserChainAddressDto>();
        //Get CaAddress from portkey V2 graphql
        var holderInfoDto = await GetHolderInfosAsync(_signatureGrantOptions.PortkeyV2GraphQLUrl, caHash);

        var chainIds = new List<string>();
        if (holderInfoDto != null && !holderInfoDto.CaHolderInfo.IsNullOrEmpty())
        {
            addressInfos.AddRange(holderInfoDto.CaHolderInfo
                .Select(t => new UserChainAddressDto { ChainId = t.ChainId, Address = t.CaAddress }));
            chainIds = holderInfoDto.CaHolderInfo.Select(t => t.ChainId).ToList();
        }

        //Get CaAddress from node contract
        var chains = _chainOptions.ChainInfos.Select(key => _chainOptions.ChainInfos[key.Key])
            .Select(chainOptionsChainInfo => chainOptionsChainInfo.ChainId).Where(t => !chainIds.Contains(t));

        foreach (var chainId in chains)
        {
            try
            {
                var addressInfo = await GetAddressInfoFromContractAsync(chainId, caHash);
                addressInfos.Add(addressInfo);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "get holder from chain error, caHash:{caHash}", caHash);
            }
        }

        return addressInfos;
    }
    
    private async Task<UserChainAddressDto> GetAddressInfoFromContractAsync(string chainId, string caHash)
    {
        var param = new GetHolderInfoInput
        {
            CaHash = Hash.LoadFromHex(caHash),
            LoginGuardianIdentifierHash = Hash.Empty
        };

        var output = await CallTransactionAsync<GetHolderInfoOutput>(chainId, GetHolderInfoMethodName, param, 
            _chainOptions);

        return new UserChainAddressDto()
        {
            Address = output.CaAddress.ToBase58(),
            ChainId = chainId
        };
    }
}