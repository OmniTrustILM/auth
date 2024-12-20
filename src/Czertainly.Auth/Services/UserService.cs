using AutoMapper;
using Czertainly.Auth.Common.Data;
using Czertainly.Auth.Common.Exceptions;
using Czertainly.Auth.Common.Models.Dto;
using Czertainly.Auth.Common.Services;
using Czertainly.Auth.Data.Contracts;
using Czertainly.Auth.Models.Config;
using Czertainly.Auth.Models.Dto;
using Czertainly.Auth.Models.Entities;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace Czertainly.Auth.Services
{
    public class UserService : CrudService<User, UserDto, UserDetailDto>, IUserService
    {
        private AuthOptions _authOptions;
        private readonly IRoleService _roleService;
        private readonly IPermissionService _permissionService;

        public UserService(IRepositoryManager repositoryManager, IMapper mapper, ILogger<UserService> logger, IOptions<AuthOptions> authOptions, IRoleService roleService, IPermissionService permissionService)
            : base(repositoryManager, repositoryManager.User, mapper, logger)
        {
            _authOptions = authOptions.Value;

            _roleService = roleService;
            _permissionService = permissionService;
        }

        public override async Task<PagedResponse<UserDto>> GetAsync(IQueryRequestDto dto)
        {
            string? groupName = null;
            if (dto is UserQueryRequestDto queryDto)
            {
                groupName = queryDto.Group;
            }

            var queryParams = _mapper.Map<QueryStringParameters>(dto);
            var users = await _repository.GetAllAsync(queryParams);
            if (groupName != null)
            {
                var filteredUsers = users.Where(u => u.Groups != null && u.Groups.Count > 0 && u.Groups.Exists(g => g.Name.Equals(groupName))).ToList();
                users = PagedList<User>.CreateFromFullList(filteredUsers, queryParams.Page, queryParams.PageSize);
            }

            return new PagedResponse<UserDto>
            {
                Data = _mapper.Map<List<UserDto>>(users),
                Links = _mapper.Map<PagingMetadata>(users),
            };
        }

        public override async Task<UserDetailDto> CreateAsync(ICrudRequestDto dto)
        {
            var userRequestDto = dto as UserRequestDto;
            if (userRequestDto == null) throw new InvalidActionException("Cannot create user. Invalid DTO");

            // check uniqueness of user
            var checkedUser = await _repository.GetByConditionAsync(u => u.Username == userRequestDto.Username);
            if (checkedUser != null) throw new EntityNotUniqueException($"User with username '{userRequestDto.Username}' already exists");

            return await base.CreateAsync(dto);
        }

        public override async Task<UserDetailDto> UpdateAsync(Guid key, ICrudRequestDto dto)
        {
            var user = await _repository.GetByKeyAsync(key);
            if (user.SystemUser) throw new InvalidActionException("Cannot update system user.");

            return await base.UpdateAsync(key, dto);
        }

        public override async Task DeleteAsync(Guid key)
        {
            var user = await _repository.GetByKeyAsync(key);
            if (user.SystemUser) throw new InvalidActionException("Cannot delete system user.");

            await base.DeleteAsync(key);
        }

        public async Task<UserDetailDto> IdentifyUserAsync(AuthenticationRequestDto authenticationRequestDto)
        {
            User? user = null;
            if (!string.IsNullOrEmpty(authenticationRequestDto.CertificateContent))
            {
                _logger.LogInformation("Identifying user with certificate");
                var clientCertificate = ParseCertificate(authenticationRequestDto.CertificateContent);

                var sha256Fingerprint = Convert.ToHexString(clientCertificate.GetCertHash(HashAlgorithmName.SHA256)).ToLower();
                _logger.LogInformation("Certificate parsed. Fingerprint: " + sha256Fingerprint);

                user = await _repository.GetByConditionAsync(u => u.CertificateFingerprint == sha256Fingerprint);
            }
            else if (!string.IsNullOrEmpty(authenticationRequestDto.AuthenticationToken))
            {
                // Authentication token processing
                _logger.LogInformation("Authenticating user with JWT token");
                AuthenticationTokenDto? authenticationToken = null;

                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    var token = handler.ReadJwtToken(authenticationRequestDto.AuthenticationToken);
                    string payload = DecodeBase64Url(token.EncodedPayload);
                    authenticationToken = JsonSerializer.Deserialize<AuthenticationTokenDto>(payload);
                }
                catch (Exception ex)
                {
                    throw new InvalidFormatException("Wrong format of authentication token.", ex);
                }

                if (authenticationToken != null)
                {
                    user = await _repository.GetByConditionAsync(u => u.Username == authenticationToken.Username);
                }
            }
            else if (!string.IsNullOrEmpty(authenticationRequestDto.SystemUsername))
            {
                throw new InvalidActionException("Cannot identify user by system username");
            }

            if (user == null) throw new EntityNotFoundException("User to identify not found");

            return await GetDetailAsync(user.Uuid);
        }

        public async Task<AuthenticationResponseDto> AuthenticateUserAsync(AuthenticationRequestDto authenticationRequestDto)
        {
            User? user = null;
            if (!string.IsNullOrEmpty(authenticationRequestDto.CertificateContent))
            {
                _logger.LogDebug("Authenticating user with certificate");
                var clientCertificate = ParseCertificate(authenticationRequestDto.CertificateContent);
                var isCertValid = VerifyClientCertificate(clientCertificate, out var chainStatusInfos);
                if (!isCertValid) throw new UnauthorizedException("User client certificate is invalid.", new Exception(string.Join('\n', chainStatusInfos)));

                var sha256Fingerprint = Convert.ToHexString(clientCertificate.GetCertHash(HashAlgorithmName.SHA256)).ToLower();
                _logger.LogDebug("Certificate parsed and verified. Fingerprint: " + sha256Fingerprint);

                user = await _repository.GetByConditionAsync(u => u.CertificateFingerprint == sha256Fingerprint);

                if (user == null) throw new UnauthorizedException("Unknown user for specified client certificate.");
            }
            else if (!string.IsNullOrEmpty(authenticationRequestDto.AuthenticationToken))
            {
                // Authentication token processing
                _logger.LogDebug("Authenticating user with JWT token. Create users: {CreateUnknownUsers}. Create roles: {CreateUnknownRoles}. Sync policy: {SyncPolicy}", _authOptions.CreateUnknownUsers, _authOptions.CreateUnknownRoles, _authOptions.SyncPolicy);
                AuthenticationTokenDto? authenticationToken = null;
                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    var token = handler.ReadJwtToken(authenticationRequestDto.AuthenticationToken);
                    string payload = DecodeBase64Url(token.EncodedPayload);
                    authenticationToken = JsonSerializer.Deserialize<AuthenticationTokenDto>(payload);
                }
                catch (Exception ex)
                {
                    throw new UnauthorizedException("Wrong format of authentication token.", ex);
                }

                if (authenticationToken == null) throw new UnauthorizedException("Authentication token is empty or invalid JSON.");
                var roleNames = new HashSet<string>(authenticationToken.Roles) ?? new HashSet<string>();

                _logger.LogInformation($"Auth token contains user with username '{authenticationToken.Username}' and roles '{string.Join(',', roleNames)}'");

                user = await _repository.GetByConditionAsync(u => u.Username == authenticationToken.Username);
                if (user == null && !_authOptions.CreateUnknownUsers) throw new UnauthorizedException($"Unknown user with username '{authenticationToken.Username}'.");

                var transaction = await _repositoryManager.BeginTransactionAsync();

                try
                {
                    var isNewUser = false;
                    if (user == null)
                    {
                        isNewUser = true;
                        _logger.LogInformation($"Creating new user with username '{authenticationToken.Username}'");
                        user = _mapper.Map<User>(authenticationToken);
                        _repository.Create(user);
                        await _repositoryManager.SaveAsync();
                    }
                    else if (_authOptions.SyncPolicy == SyncPolicy.SyncData)
                    {
                        user.FirstName = authenticationToken.FirstName;
                        user.LastName = authenticationToken.LastName;
                        user.Email = authenticationToken.Email;
                        await _repositoryManager.SaveAsync();
                    }

                    if (isNewUser || _authOptions.SyncPolicy == SyncPolicy.SyncData)
                    {
                        var userRolesNames = new Dictionary<string, Guid>();
                        if (user.Roles != null)
                        {
                            foreach (var role in user.Roles)
                            {
                                userRolesNames.Add(role.Name, role.Uuid);
                            }
                        }

                        foreach (var roleName in roleNames)
                        {
                            Guid? roleUuid = null;
                            var role = await _repositoryManager.Role.GetByConditionAsync(r => r.Name == roleName);

                            if (role != null && !userRolesNames.ContainsKey(roleName)) roleUuid = role.Uuid;
                            if (role == null && _authOptions.CreateUnknownRoles)
                            {
                                _logger.LogInformation($"Creating new role with name '{roleName}'");
                                var roleDto = await _roleService.CreateAsync(new RoleRequestDto { Name = roleName });
                                roleUuid = roleDto.Uuid;
                            }

                            if (roleUuid.HasValue)
                            {
                                _logger.LogInformation($"Assign role '{roleName}' to user '{authenticationToken.Username}'");
                                await AssignRoleAsync(user.Uuid, roleUuid.Value);
                            }
                        }

                        if (!isNewUser && _authOptions.SyncPolicy == SyncPolicy.SyncData)
                        {
                            foreach (var removeRoleName in userRolesNames.Keys.Except(roleNames))
                            {
                                _logger.LogInformation($"Unassign role '{removeRoleName}' to user '{authenticationToken.Username}'");
                                await RemoveRoleAsync(user.Uuid, userRolesNames[removeRoleName]);
                            }
                        }
                    }
                }
                catch (UnauthorizedException) { throw; }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new UnauthorizedException("Error in creating user or assigning roles based on authentication token.", ex);
                }

                await transaction.CommitAsync();
            }
            else if (!string.IsNullOrEmpty(authenticationRequestDto.SystemUsername))
            {
                _logger.LogDebug($"Authenticating system user with username '{authenticationRequestDto.SystemUsername}'");
                user = await _repository.GetByConditionAsync(u => u.SystemUser && u.Username == authenticationRequestDto.SystemUsername);

                if (user == null) throw new UnauthorizedException("Unknown system user for specified username: " + authenticationRequestDto.SystemUsername);
            }
            else if (!string.IsNullOrEmpty(authenticationRequestDto.UserUuid))
            {
                _logger.LogDebug($"Authenticating user with UUID '{authenticationRequestDto.UserUuid}'");
                user = await _repository.GetByConditionAsync(u => u.Uuid == Guid.Parse(authenticationRequestDto.UserUuid));

                if (user == null) throw new UnauthorizedException("Unknown user for specified UUID: " + authenticationRequestDto.UserUuid);
            }

            if (user == null)
            {
                _logger.LogDebug("Authenticated as anonymous user");
                return new AuthenticationResponseDto { Authenticated = false };
            }

            if (!user.Enabled) throw new UnauthorizedException($"User '{user.Username}' is disabled");

            var permissions = await _permissionService.GetUserPermissionsAsync(user.Uuid);

            var result = new AuthenticationResponseDto
            {
                Authenticated = true,
                Data = new UserProfileDto
                {
                    User = _mapper.Map<UserDto>(user),
                    Roles = user.Roles.Select(r => new NameAndUuidDto { Uuid = r.Uuid, Name = r.Name }).ToList(),
                    Permissions = permissions,
                }
            };

            return result;
        }

        public async Task<UserDetailDto> EnableUserAsync(Guid userUuid, bool enableFlag)
        {
            var user = await _repository.GetByKeyAsync(userUuid);

            user.Enabled = enableFlag;
            await _repositoryManager.SaveAsync();

            return _mapper.Map<UserDetailDto>(user);
        }

        public async Task<UserDetailDto> AssignRoleAsync(Guid userUuid, Guid roleUuid)
        {
            var user = await _repository.GetByKeyAsync(userUuid);
            var role = await _repositoryManager.Role.GetByKeyAsync(roleUuid);

            user.Roles.Add(role);
            await _repositoryManager.SaveAsync();

            return _mapper.Map<UserDetailDto>(user);
        }

        public async Task<UserDetailDto> AssignRolesAsync(Guid userUuid, IEnumerable<Guid> roleUuids)
        {
            var user = await _repository.GetByKeyAsync(userUuid);
            var roles = await _repositoryManager.Role.GetByUuidsAsync(roleUuids);

            user.Roles.Clear();
            foreach (var role in roles) user.Roles.Add(role);
            await _repositoryManager.SaveAsync();

            return _mapper.Map<UserDetailDto>(user);
        }

        public async Task<UserDetailDto> RemoveRoleAsync(Guid userUuid, Guid roleUuid)
        {
            var user = await _repository.GetByKeyAsync(userUuid);
            var role = await _repositoryManager.Role.GetByKeyAsync(roleUuid);

            user.Roles.Remove(role);
            await _repositoryManager.SaveAsync();

            return _mapper.Map<UserDetailDto>(user);
        }

        private X509Certificate2 ParseCertificate(string clientCertificateContent)
        {
            try
            {
                return new X509Certificate2(Convert.FromBase64String(clientCertificateContent));
            }
            catch (FormatException ex)
            {
                throw new InvalidFormatException("Wrong format of user authentication certificate.", ex);
            }
        }

        private bool VerifyClientCertificate(X509Certificate2 certificate, out List<string> chainStatusInfos)
        {
            chainStatusInfos = new List<string>();

            var chain = new X509Chain();
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck; // no revocation checking for now
            chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;

            var isValid = chain.Build(certificate);
            if (!isValid)
            {
                foreach (X509ChainElement chainElement in chain.ChainElements)
                {
                    foreach (X509ChainStatus chainStatus in chainElement.ChainElementStatus)
                    {
                        chainStatusInfos.Add($"Certificate issued by '{chainElement.Certificate.IssuerName.Name}' with subject '{chainElement.Certificate.SubjectName.Name}' is invalid: {chainStatus.StatusInformation}");
                    }
                }
            }
            return isValid;
        }

        public async Task<List<UserDto>> GetRoleUsersAsync(Guid roleUuid)
        {
            var users = await _repositoryManager.User.GetRoleUsersAsync(roleUuid);
            return _mapper.Map<List<UserDto>>(users);
        }

        private static string DecodeBase64Url(string base64Url)
        {
            string base64 = base64Url.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
        }
    }


}
