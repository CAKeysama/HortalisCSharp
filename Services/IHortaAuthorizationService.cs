using HortalisCSharp.Models;

namespace HortalisCSharp.Services
{
    public interface IHortaAuthorizationService
    {
        bool CanCreate(Usuario user);
        bool CanManage(Usuario user, Horta horta);
    }
}