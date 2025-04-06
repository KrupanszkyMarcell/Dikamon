using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dikamon.DelegatingHandlers;
using Dikamon.Models;
using Refit;

namespace Dikamon.Api
{
    public interface IUserApiCommand
    {
        [Get("/users")]
        Task<ApiResponse<List<Users>>> GetUsers();

        [Get("/users/{id}")]
        Task<ApiResponse<Users>> GetUserById(int id);

        [Post("/users/register")]
        Task<ApiResponse<Users>> RegisterUser([Body] Users user);

        [Post("/users/registeradmin")]
        Task RegisterAdmin([Body] Users user);

        [Put("/users/login")]
        Task<ApiResponse<Users>> LoginUser([Body(buffered: true)] Users user);

        [Put("/users/logout")]
        Task LogoutUser([Body] Users user);

    }
}
