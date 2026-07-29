using Czertainly.Auth.Common.Data.Repositories;
using Czertainly.Auth.Common.Mappings;
using Czertainly.Auth.Common.Models.Dto;
using Czertainly.Auth.Common.Models.Entities;
using Czertainly.Auth.Data.Contracts;

namespace Czertainly.Auth.Common.Services
{
    public abstract class CrudService<TEntity, TResponseDto, TDetailResponseDto> : ICrudService<TResponseDto, TDetailResponseDto>
        where TEntity : class, IBaseEntity, new()
        where TResponseDto : ICrudResponseDto, new()
        where TDetailResponseDto : ICrudResponseDto, new()
    {
        protected readonly IEntityMapper<TEntity, TResponseDto, TDetailResponseDto> _mapper;
        protected readonly ILogger _logger;
        protected readonly IBaseRepository<TEntity> _repository;
        protected readonly IRepositoryManager _repositoryManager;

        protected CrudService(IRepositoryManager repositoryManager, IBaseRepository<TEntity> repository, IEntityMapper<TEntity, TResponseDto, TDetailResponseDto> mapper, ILogger logger)
        {
            _mapper = mapper;
            _logger = logger;
            _repository = repository;
            _repositoryManager = repositoryManager;
        }
        public virtual async Task<PagedResponse<TResponseDto>> GetAsync(IQueryRequestDto dto)
        {
            var queryParams = dto.ToQueryStringParameters();
            var entities = await _repository.GetAllAsync(queryParams);

            return new PagedResponse<TResponseDto>
            {
                Data = entities.Select(entity => _mapper.ToDto(entity)).ToList(),
                Links = entities.ToPagingMetadata(),
            };
        }

        public virtual async Task<TDetailResponseDto> CreateAsync(ICrudRequestDto dto)
        {
            var entity = _mapper.ToEntity(dto);
            _repository.Create(entity);
            await _repositoryManager.SaveAsync();

            entity = await _repository.GetByKeyAsync(entity.Uuid);
            return _mapper.ToDetailDto(entity);
        }

        public virtual async Task<TDetailResponseDto> GetDetailAsync(Guid key)
        {
            var entity = await _repository.GetByKeyAsync(key);

            return _mapper.ToDetailDto(entity);
        }

        public virtual async Task<TDetailResponseDto> UpdateAsync(Guid key, ICrudRequestDto dto)
        {
            var entity = await _repository.GetByKeyAsync(key);
            _mapper.ApplyUpdate(dto, entity);

            //await _repository.UpdateAsync(key, entity);
            await _repositoryManager.SaveAsync();

            return _mapper.ToDetailDto(entity);
        }

        public virtual async Task DeleteAsync(Guid key)
        {
            await _repository.DeleteAsync(key);
            await _repositoryManager.SaveAsync();
        }
    }
}
