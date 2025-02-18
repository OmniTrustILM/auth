using AutoMapper;
using Czertainly.Auth.Common.Models.Dto;
using Czertainly.Auth.Common.Models.Entities;
using Czertainly.Auth.Models.Dto;
using Czertainly.Auth.Models.Entities;

namespace Czertainly.Auth.Models.Mappings
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            CreateMap<AuthenticationTokenClaimsDto, User>()
                .ForMember(dest => dest.Roles, o => o.MapFrom(src => new List<Role>()))
                .ForMember(dest => dest.AuthTokenSubjectId, o => o.MapFrom(src => src.SubjectId));
            CreateMap<UserRequestDto, User>();
            CreateMap<UserUpdateRequestDto, User>();
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.Groups, o => o.MapFrom(src => src.Groups == null ? new List<NameAndUuidDto>() : src.Groups));
            CreateMap<User, UserDetailDto>()
                .IncludeBase<User, UserDto>()
                .ForMember(dest => dest.Certificate, o => o.MapFrom(src => src.CertificateFingerprint == null ? null : new UserCertificateDto { Uuid = src.CertificateUuid, Fingerprint = src.CertificateFingerprint }));
        }
    }
}
