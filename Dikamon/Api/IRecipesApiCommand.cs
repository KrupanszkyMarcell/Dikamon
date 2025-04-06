using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dikamon.Models;
using Refit;

namespace Dikamon.Api
{
    public interface IRecipesApiCommand
    {
        [Get("/recipes")]
        Task<ApiResponse<List<Recipes>>> GetRecipes();
        [Post("/recipes")]
        Task<ApiResponse<Recipes>> AddRecipe([Body] Recipes recipe);
        [Put("/recipes")]
        Task<ApiResponse<Recipes>> UpdateRecipe([Body] Recipes recipe);
        [Get("/recipes/length")]
        Task<ApiResponse<int>> GetRecipesLength();
        [Get("/recipes/from/{from}/to/{to}")]
        Task<ApiResponse<List<Recipes>>> GetRecipesByRange(int from, int to);
        [Get("/recipes/{id}")]
        Task<ApiResponse<List<Recipes>>> GetRecipesById(int id);
        [Delete("/recipes/{id}")]
        Task<ApiResponse<Recipes>> DeleteRecipe(int id);
        [Get("/recipes/type/{type}")]
        Task<ApiResponse<List<Recipes>>> GetRecipesByType(string type);
        [Get("/recipes/getTypes")]
        Task<ApiResponse<List<string>>> GetRecipeTypes();
    }
}
