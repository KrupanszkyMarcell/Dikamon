using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dikamon.Models;
using Refit;

namespace Dikamon.Api
{
    public interface IIngredientsApiCommand
    {
        [Get("/ingredients/{id}")]
        Task<ApiResponse<List<Contains>>> GetIngredientsById(int id);
        [Delete("/ingredients/{id}")]
        Task DeleteIngredient(int id);
        [Post("/ingredients")]
        Task<ApiResponse<Contains>> AddIngredient([Body] Contains ingredient);
    }
}
