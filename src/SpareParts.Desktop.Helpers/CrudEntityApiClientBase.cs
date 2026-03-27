using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf
{
    public abstract class CrudEntityApiClientBase<TReadDto, TCreateRequest, TCreateResponse, TUpdateRequest, TKey>
        : FeatureApiClientBase
    {
        protected CrudEntityApiClientBase(string baseUrl) : base(baseUrl)
        {
        }

        public abstract Task<List<TReadDto>> RetrieveAsync();
        public abstract Task<TCreateResponse> AddAsync(TCreateRequest request);
        public abstract Task EditAsync(TKey id, TUpdateRequest request);
        public abstract Task DeleteAsync(TKey id);
    }
}
