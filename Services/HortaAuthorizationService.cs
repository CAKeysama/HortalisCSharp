using HortalisCSharp.Models;

namespace HortalisCSharp.Services
{
    public class HortaAuthorizationService : IHortaAuthorizationService
    {
        public bool CanCreate(Usuario user)
        {
            return user.Papel == PapelUsuario.Gerente || user.Papel == PapelUsuario.Administrador;
        }

        public bool CanManage(Usuario user, Horta horta)
        {
            return user.Papel switch
            {
                PapelUsuario.Administrador => true,
                PapelUsuario.Gerente => horta.UsuarioId == user.Id,
                _ => false
            };
        }
    }
}