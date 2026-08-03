using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MiCachito.Mobile.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    [ObservableProperty]
    private string usuario = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private string mensaje = string.Empty;


    public LoginViewModel()
    {
        Title = "Inicio de sesión";
    }


    [RelayCommand]
    private void Ingresar()
    {
        if (string.IsNullOrEmpty(Usuario) || string.IsNullOrEmpty(Password))
        {
            Mensaje = "Ingrese usuario y contraseña";
            return;
        }

        Mensaje = $"Bienvenido {Usuario}";
    }
}