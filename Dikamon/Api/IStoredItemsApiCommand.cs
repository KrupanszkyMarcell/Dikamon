using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dikamon.Models;
using Refit;

namespace Dikamon.Api
{
    public interface IStoredItemsApiCommand
    {
        [Get("/storeditems/{userid}")]
        Task<ApiResponse<List<Stores>>> GetStoredItems(int userid);

        [Get("/storage/{userid}/length")]
        Task<ApiResponse<int>> GetStoredItemsLength(int userid);

        [Get("/storage/{userid}/from/{from}/to/{to}")]
        Task<ApiResponse<List<Stores>>> GetStoredItemsByRange(int userid, int from, int to);

        [Get("/storage/{userid}/{typeid}")]
        Task<ApiResponse<List<Stores>>> GetStoredItemsByTypeId(int userid, int typeid);

        [Get("/storage/{userid}/{typeid}/length")]
        Task<ApiResponse<int>> GetStoredItemsByTypeIdLength(int userid, int typeid);

        [Get("/storage/{userid}/{typeid}/from/{from}/to/{to}")]
        Task<ApiResponse<List<Stores>>> GetStoredItemsByTypeIdRange(int userid, int typeid, int from, int to);

        [Get("/storage/{userid}/search/{searchword}")]
        Task<ApiResponse<List<Stores>>> GetStoredItemsBySearch(int userid, string searchword);

        [Get("/storage/{userid}/{typeid}/search/{searchword}")]
        Task<ApiResponse<List<Stores>>> GetStoredItemsByTypeIdAndSearch(int userid, int typeid, string searchword);

        [Post("/storage")]
        Task<ApiResponse<Stores>> AddStoredItem([Body] Stores storedItem);

        [Delete("/storage")]
        Task<ApiResponse<Stores>> DeleteStoredItem([Body] Stores storedItem);
    }
}