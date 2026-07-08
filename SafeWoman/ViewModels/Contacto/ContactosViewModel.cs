using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SafeWoman.Models;
using SafeWoman.Services;

namespace SafeWoman.ViewModels.Contacto;

public partial class ContactosViewModel : ObservableObject
{
    private readonly ApiService       _api;
    private readonly AuthStateService _authState;

    [ObservableProperty] private ObservableCollection<ContactoEmergenciaDto> _contactos = [];
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _nuevoNombre = string.Empty;
    [ObservableProperty] private string _nuevoTelefono = string.Empty;

    public ContactosViewModel(ApiService api, AuthStateService authState)
    {
        _api       = api;
        _authState = authState;
    }

    public async Task CargarAsync()
    {
        IsBusy = true;
        try
        {
            var result = await _api.ListarContactosAsync();
            if (result.Success && result.Data is not null)
            {
                Contactos.Clear();
                foreach (var c in result.Data)
                    Contactos.Add(c);
            }
            else if (!result.Success)
            {
                // Antes fallaba en silencio y el usuario veía la lista vacía sin explicación.
                await Shell.Current.DisplayAlert("No se pudieron cargar los contactos",
                    result.Error ?? "Verifique su conexión e intente nuevamente.",
                    "Aceptar");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AgregarAsync()
    {
        if (string.IsNullOrWhiteSpace(NuevoNombre) || string.IsNullOrWhiteSpace(NuevoTelefono))
        {
            await Shell.Current.DisplayAlert("Error", "Nombre y teléfono son obligatorios.", "Aceptar");
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _api.CrearContactoAsync(NuevoNombre.Trim(), NuevoTelefono.Trim());
            if (result.Success && result.Data is not null)
            {
                Contactos.Add(result.Data);
                NuevoNombre   = string.Empty;
                NuevoTelefono = string.Empty;
                _authState.InvalidarPerfil();   // HomeViewModel refrescará el conteo
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task EliminarAsync(ContactoEmergenciaDto contacto)
    {
        var confirm = await Shell.Current.DisplayAlert(
            "Eliminar contacto",
            $"¿Eliminar a {contacto.Nombre}?",
            "Sí", "No");

        if (!confirm) return;

        var res = await _api.EliminarContactoAsync(contacto.IdContacto);
        if (res.Success)
        {
            Contactos.Remove(contacto);
            _authState.InvalidarPerfil();
        }
        else
        {
            await Shell.Current.DisplayAlert("Error",
                res.Error ?? "No se pudo eliminar el contacto. Intente nuevamente.",
                "Aceptar");
        }
    }
}
