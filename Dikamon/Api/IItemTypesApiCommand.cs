using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dikamon.Models;
using Refit;

namespace Dikamon.Api
{
    public interface IItemTypesApiCommand
    {
        [Get("/types")]
        Task<ApiResponse<List<ItemTypes>>> GetItemTypes();
        [Post("/types")]
        Task<ApiResponse<ItemTypes>> AddItemType([Body] ItemTypes itemType);
        [Put("/types")]
        Task<ApiResponse<ItemTypes>> UpdateItemType([Body] ItemTypes itemType);
        [Get("/types/length")]
        Task<ApiResponse<int>> GetItemTypesLength();
        [Get("/types/from/{from}/to/{to}")]
        Task<ApiResponse<List<ItemTypes>>> GetItemTypesByRange(int from, int to);
        [Get("/types/search/{searchword}")]
        Task<ApiResponse<List<ItemTypes>>> GetItemTypesBySearch(string searchword);
        [Delete("/types/{id}")]
        Task<ApiResponse<ItemTypes>> DeleteItemType(int id);
    }
}
