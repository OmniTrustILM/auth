using Auth.Common.Data;
using Auth.Common.Models.Dto;

namespace Auth.Common.Services
{
    public interface ICrudService<TResponseDto, TDetailResponseDto>
        where TResponseDto : ICrudResponseDto, new()
        where TDetailResponseDto : ICrudResponseDto, new()
    {
        public Task<PagedResponse<TResponseDto>> GetAsync(IQueryRequestDto dto);
        public Task<TDetailResponseDto> CreateAsync(ICrudRequestDto dto);
        public Task<TDetailResponseDto> GetDetailAsync(Guid key);
        public Task<TDetailResponseDto> UpdateAsync(Guid key, ICrudRequestDto dto);
        public Task DeleteAsync(Guid key);

    }
}
