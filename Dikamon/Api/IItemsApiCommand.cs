using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dikamon.Models;
using Refit;

namespace Dikamon.Api
{
    public interface IItemsApiCommand
    {
        [Get("/items")]
        Task<ApiResponse<List<Items>>> GetItems();
        [Post("/items")]
        Task<ApiResponse<Items>> AddItem([Body] Items item);
        [Put("/items")]
        Task<ApiResponse<Items>> UpdateItem([Body] Items item);
        [Get ("/items/length")]
        Task<ApiResponse<int>> GetItemsLength();
        [Get ("/items/from/{from}/to/{to}")]
        Task<ApiResponse<List<Items>>> GetItemsByRange(int from, int to);
        [Get ("/items/typeid/{id}")]
        Task<ApiResponse<List<Items>>> GetItemsByTypeId(int id);
        [Get ("/items/search/{searchword}")]
        Task<ApiResponse<List<Items>>> GetItemsBySearch(string searchword);
        [Get ("/items/typeid/{id}/search/{searchword}")]
        Task<ApiResponse<List<Items>>> GetItemsByTypeIdAndSearch(int id, string searchword);
        [Delete("/items/{id}")]
        Task<ApiResponse<Items>> DeleteItem(int id);

    }
}
